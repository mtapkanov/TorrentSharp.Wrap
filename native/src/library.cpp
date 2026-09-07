//
// library.cpp
// Created by Albie on 29/02/2024.
//

#include "library.h"

#include <algorithm>
#include <fstream>
#include <mutex>
#include <libtorrent/fingerprint.hpp>
#include <libtorrent/magnet_uri.hpp>
#include <libtorrent/peer_info.hpp>
#include <libtorrent/torrent_handle.hpp>
#include <libtorrent/write_resume_data.hpp>

#if defined(__linux__)
#include <clocale>
#include <locale>

namespace
{
    // libtorrent's own path sanitization (torrent_info.cpp's sanitize_append_path_element)
    // classifies decoded UTF-8 codepoints as printable via the C++ global locale, which starts as
    // the "C" locale (ASCII-only) regardless of the process's LANG/LC_ALL environment variables -
    // nothing makes it pick those up until something explicitly opts in. Without this, every
    // non-ASCII character in a torrent/file name (e.g. Cyrillic) gets silently replaced with '.'
    // on disk. Calling both std::setlocale (the C runtime's locale, which is what iswprint/mbstowcs
    // consult) and std::locale::global (the separate C++ locale libtorrent's classification actually
    // reads) covers whichever one is in play. Runs automatically the moment this shared library is
    // loaded, so it doesn't depend on the hosting process (e.g. .NET) cooperating. Linux-only: the
    // macOS build has never exhibited this - its "C" locale's classification already treats UTF-8
    // multi-byte sequences as printable.
    void ensure_utf8_locale() __attribute__((constructor));

    void ensure_utf8_locale()
    {
        std::setlocale(LC_ALL, "");
        try
        {
            std::locale::global(std::locale(""));
        }
        catch (const std::exception&)
        {
            // Falls back to whatever std::setlocale above achieved - happens on minimal systems
            // without full locale data installed for the environment's requested locale name.
        }
    }
}
#endif

extern "C" {

lt::session* create_session(lt::settings_pack* pack)
{
    lt::session_params params;

    if (pack != nullptr)
    {
        params.settings = *pack;
    }

    return new lt::session(params);
}

void destroy_session(lt::session* session)
{
    if (session == nullptr)
    {
        return;
    }

    session->abort();

    // Blocks until any in-flight on_events_available invocation for this session finishes, then
    // holds the same mutex while deleting - a new invocation racing this delete will simply find
    // the mutex held (its own lock is a non-blocking try_lock) and no-op instead of touching freed
    // memory. See alert_dispatch_mutex's declaration in events.h for the full race this closes.
    std::lock_guard<std::mutex> guard(alert_dispatch_mutex());
    delete session;
}

void apply_settings(lt::session* session, lt::settings_pack* settings)
{
    if (session == nullptr || settings == nullptr)
    {
        return;
    }

    session->apply_settings(*settings);
}

void set_event_callback(lt::session* session, cs_alert_callback callback, bool include_unmapped_events)
{
    if (session == nullptr)
    {
        return;
    }

    if (callback == nullptr)
    {
        clear_event_callback(session);
        return;
    }

    auto session_callback = [session, callback, include_unmapped_events]() -> void
    {
        std::thread(on_events_available, session, callback, include_unmapped_events).detach();
    };

    session->set_alert_notify(session_callback);
}

void clear_event_callback(lt::session* session)
{
    if (session == nullptr)
    {
        return;
    }

    session->set_alert_notify(nullptr);
}

// lt::torrent_info's constructors throw on malformed input (invalid bencoding, missing
// required keys, etc.) - catch here so a corrupt/hostile .torrent doesn't crash the process
// with an unhandled C++ exception at the P/Invoke boundary, which can't marshal it anyway.
lt::torrent_info* create_torrent_bytes(const char* data, long length)
{
    try
    {
        const lt::span buffer(data, length);
        const lt::load_torrent_limits cfg;

        return new lt::torrent_info(buffer, cfg, lt::from_span);
    }
    catch (...)
    {
        return nullptr;
    }
}

lt::torrent_info* create_torrent_file(const char* file_path)
{
    try
    {
        return new lt::torrent_info(std::string(file_path));
    }
    catch (...)
    {
        return nullptr;
    }
}

void destroy_torrent(lt::torrent_info* torrent)
{
    delete torrent;
}

// attach a torrent to the session, returning a handle that can be used to control the download.
// the torrent info handle is copied, and can be freed after the call to attach_torrent with a call to destroy_torrent_info.
lt::torrent_handle* attach_torrent(lt::session* session, lt::torrent_info* torrent, const char* save_path)
{
    if (session == nullptr || torrent == nullptr)
    {
        return nullptr;
    }

    lt::add_torrent_params params;
    std::string save_path_copy(save_path);

    if (!save_path_copy.empty())
    {
        params.save_path = save_path_copy;
    }

    // enable paused-by-default, disable auto-management
    params.flags |= lt::torrent_flags::paused;
    params.flags &= ~lt::torrent_flags::auto_managed;

    // set torrent info - make_shared creates a copy
    params.ti = std::make_shared<lt::torrent_info>(*torrent);
    const auto handle = new lt::torrent_handle(session->add_torrent(params));

    if (handle->is_valid())
    {
        return handle;
    }

    delete handle;
    return nullptr;
}

lt::torrent_handle* attach_magnet(lt::session* session, const char* magnet_uri, const char* save_path)
{
    if (session == nullptr || magnet_uri == nullptr)
    {
        return nullptr;
    }

    lt::error_code ec;
    lt::add_torrent_params params = lt::parse_magnet_uri(std::string(magnet_uri), ec);

    if (ec)
    {
        return nullptr;
    }

    if (save_path != nullptr)
    {
        params.save_path = std::string(save_path);
    }

    params.flags |= lt::torrent_flags::paused;
    params.flags &= ~lt::torrent_flags::auto_managed;

    const auto handle = new lt::torrent_handle(session->add_torrent(params));

    if (handle->is_valid())
    {
        return handle;
    }

    delete handle;
    return nullptr;
}

// after detaching the torrent, the torrent handle is no longer valid.
// additionally, a call to destroy_torrent is not needed.
void detach_torrent(lt::session* session, lt::torrent_handle* torrent)
{
    if (session == nullptr || torrent == nullptr)
    {
        return;
    }

    torrent->pause();
    session->remove_torrent(*torrent);

    delete torrent;
}

// returns a heap-allocated copy of the torrent_info from a torrent_handle (available after metadata is fetched).
// the returned pointer must be freed with destroy_torrent.
lt::torrent_info* get_handle_torrent_info(lt::torrent_handle* handle)
{
    if (handle == nullptr)
    {
        return nullptr;
    }

    const auto ti = handle->torrent_file();

    if (!ti)
    {
        return nullptr;
    }

    return new lt::torrent_info(*ti);
}

void get_torrent_handle_info_hash(lt::torrent_handle* handle, char* hash_out)
{
    if (handle == nullptr || hash_out == nullptr)
    {
        return;
    }

    const auto& hashes = handle->info_hashes();

    if (hashes.has_v1())
    {
        std::copy(hashes.v1.begin(), hashes.v1.end(), hash_out);
    }
    else
    {
        std::fill_n(hash_out, 20, static_cast<char>(0xFF));
    }
}

// get the info for a torrent.
// the torrent_info struct is allocated on the heap and must be freed with a call to destroy_torrent_info.
torrent_metadata* get_torrent_info(lt::torrent_info* torrent)
{
    if (torrent == nullptr)
    {
        return nullptr;
    }

    auto name = torrent->name();
    auto author = torrent->creator();
    auto comment = torrent->comment();

    const auto torrent_name = new char[name.size() + 1]();
    const auto torrent_author = new char[author.size() + 1]();
    const auto torrent_comment = new char[comment.size() + 1]();

    std::ranges::copy(name, torrent_name);
    std::ranges::copy(author, torrent_author);
    std::ranges::copy(comment, torrent_comment);

    const auto info = new torrent_metadata();

    info->name = torrent_name;
    info->creator = torrent_author;
    info->comment = torrent_comment;

    info->total_files = torrent->num_files();
    info->total_size = torrent->total_size();
    info->creation_date = torrent->creation_date();

    auto hash = torrent->info_hashes();

    // fill in the info hash
    if (hash.has_v1())
    {
        std::ranges::copy(hash.v1, info->info_hash_v1);
    }
    else
    {
        std::fill_n(info->info_hash_v1, 20, 0);
    }

    // fill in the info hash v2
    if (hash.has_v2())
    {
        std::ranges::copy(hash.v2, info->info_hash_v2);
    }
    else
    {
        std::fill_n(info->info_hash_v2, 32, 0);
    }

    return info;
}

void destroy_torrent_info(torrent_metadata* info)
{
    if (info == nullptr)
    {
        return;
    }

    delete[] info->name;
    delete[] info->creator;
    delete[] info->comment;

    delete info;
}

bool save_torrent_to_file(lt::torrent_info* torrent, const char* file_path)
{
    if (torrent == nullptr || file_path == nullptr)
    {
        return false;
    }

    try
    {
        lt::add_torrent_params params;
        params.ti = std::make_shared<lt::torrent_info>(*torrent);

        const auto buf = lt::write_torrent_file_buf(params, {});

        std::ofstream out(file_path, std::ios::binary);

        if (!out)
        {
            return false;
        }

        out.write(buf.data(), static_cast<std::streamsize>(buf.size()));

        return out.good();
    }
    catch (const std::exception&)
    {
        return false;
    }
}

void get_torrent_bytes(lt::torrent_info* torrent, char** out_data, long* out_size)
{
    if (torrent == nullptr || out_data == nullptr || out_size == nullptr)
    {
        return;
    }

    try
    {
        lt::add_torrent_params params;
        params.ti = std::make_shared<lt::torrent_info>(*torrent);

        const auto buf = lt::write_torrent_file_buf(params, {});

        const auto alloc = new char[buf.size()];
        std::copy(buf.begin(), buf.end(), alloc);

        *out_data = alloc;
        *out_size = static_cast<long>(buf.size());
    }
    catch (const std::exception&)
    {
        *out_data = nullptr;
        *out_size = 0;
    }
}

void free_torrent_bytes(char* data)
{
    delete[] data;
}

// given a torrent handle, get the list of files in the torrent.
void get_torrent_file_list(lt::torrent_info* torrent, torrent_file_list* file_list)
{
    if (torrent == nullptr || file_list == nullptr)
    {
        return;
    }

    const auto& files = torrent->files();

    const auto num_files = files.num_files();
    const auto list = new torrent_file_information[num_files];

    for (lt::file_index_t i(0); i != files.end_file(); i++)
    {
        auto index = static_cast<int32_t>(i);

        auto name = files.file_name(i);
        auto path = files.file_path(i);

        auto file_name = new char[name.size() + 1]();
        auto file_path = new char[path.size() + 1]();

        list[index] = {
            index,
            files.file_offset(i),
            files.file_size(i),
            files.mtime(i),
            file_name,
            file_path,
            files.file_absolute_path(i),
            files.pad_file_at(i)
        };

        std::ranges::copy(name, file_name);
        std::ranges::copy(path, file_path);
    }

    file_list->files = list;
    file_list->length = num_files;
}

void destroy_torrent_file_list(torrent_file_list* file_list)
{
    if (file_list == nullptr || file_list->files == nullptr)
    {
        return;
    }

    for (int i = 0; i < file_list->length; i++)
    {
        delete[] file_list->files[i].file_name;
        delete[] file_list->files[i].file_path;
    }

    delete[] file_list->files;
}

// set the download priority for a file in a torrent.
void set_file_dl_priority(lt::torrent_handle* torrent, const int32_t file_index, const uint8_t priority)
{
    if (torrent == nullptr)
    {
        return;
    }

    torrent->file_priority(static_cast<lt::file_index_t>(file_index), static_cast<lt::download_priority_t>(priority));
}

// get the download priority for a file in a torrent.
uint8_t get_file_dl_priority(lt::torrent_handle* torrent, const int32_t file_index)
{
    if (torrent == nullptr)
    {
        return 0;
    }

    return static_cast<uint8_t>(torrent->file_priority(static_cast<lt::file_index_t>(file_index)));
}

// set the deadline for a piece to be downloaded, requesting an alert once it's available -
// whether it still needs downloading or is already on disk (see read_piece_alert in events.cpp).
void set_piece_deadline(lt::torrent_handle* torrent, const int32_t piece_index, const int32_t deadline_ms)
{
    if (torrent == nullptr)
    {
        return;
    }

    torrent->set_piece_deadline(
        static_cast<lt::piece_index_t>(piece_index),
        deadline_ms,
        lt::torrent_handle::alert_when_available);
}

// removes the deadline from a piece - it's no longer considered a priority to download ahead of others.
void reset_piece_deadline(lt::torrent_handle* torrent, const int32_t piece_index)
{
    if (torrent == nullptr)
    {
        return;
    }

    torrent->reset_piece_deadline(static_cast<lt::piece_index_t>(piece_index));
}

// returns true if this piece has been completely downloaded and written to disk.
bool have_piece(lt::torrent_handle* torrent, const int32_t piece_index)
{
    if (torrent == nullptr)
    {
        return false;
    }

    return torrent->have_piece(static_cast<lt::piece_index_t>(piece_index));
}

// the total number of pieces in the torrent, once metadata is available.
int32_t get_torrent_piece_count(lt::torrent_handle* torrent)
{
    if (torrent == nullptr)
    {
        return 0;
    }

    const auto ti = torrent->torrent_file();
    return ti ? static_cast<int32_t>(static_cast<int>(ti->num_pieces())) : 0;
}

// fills pieces_out with one byte per piece (0 or 1) describing whether it's been downloaded.
// synchronous - avoids needing an alert round-trip just to read piece completion.
void get_torrent_piece_map(lt::torrent_handle* torrent, uint8_t* pieces_out, const int32_t pieces_len)
{
    if (torrent == nullptr || pieces_out == nullptr)
    {
        return;
    }

    const auto s = torrent->status(lt::torrent_handle::query_pieces);
    const auto count = std::min(pieces_len, static_cast<int32_t>(s.pieces.size()));

    for (int32_t i = 0; i < count; i++)
    {
        pieces_out[i] = s.pieces[lt::piece_index_t(i)] ? 1 : 0;
    }
}

// maps a byte range within a file to the piece that owns it, for translating a stream read
// request (file + byte offset) into a piece index to set a deadline on.
piece_request map_file_range(lt::torrent_info* torrent, const int32_t file_index, const int64_t offset, const int32_t size)
{
    if (torrent == nullptr)
    {
        return { -1, 0, 0 };
    }

    const auto req = torrent->map_file(static_cast<lt::file_index_t>(file_index), offset, size);
    return { static_cast<int32_t>(static_cast<int>(req.piece)), req.start, req.length };
}

// start and stop the download of a torrent.
void start_torrent(lt::torrent_handle* torrent)
{
    if (torrent == nullptr)
    {
        return;
    }

    torrent->resume();
}

// start and stop the download of a torrent.
void stop_torrent(lt::torrent_handle* torrent)
{
    if (torrent == nullptr)
    {
        return;
    }

    torrent->pause();
}

void reannounce_torrent(lt::torrent_handle* torrent, const int32_t seconds, const uint8_t ignore_min_interval)
{
    if (torrent == nullptr)
    {
        return;
    }

    lt::reannounce_flags_t flags = {};

    if (ignore_min_interval)
    {
        flags |= lt::torrent_handle::ignore_min_interval;
    }

    torrent->force_reannounce(seconds, -1, flags);
}

// get the progress of a torrent.
void get_torrent_status(lt::torrent_handle* torrent, torrent_status* torrent_status)
{
    if (torrent == nullptr || torrent_status == nullptr)
    {
        return;
    }

    const auto s = torrent->status();

    if (s.errc != lt::error_code())
    {
        torrent_status->state = cs_torrent_state::torrent_error;
    }
    else
    {
        switch (s.state)
        {
        case lt::torrent_status::state_t::checking_files:
            torrent_status->state = cs_torrent_state::torrent_checking;
            break;

        case lt::torrent_status::state_t::checking_resume_data:
            torrent_status->state = cs_torrent_state::torrent_checking_resume;
            break;

        case lt::torrent_status::state_t::downloading_metadata:
            torrent_status->state = cs_torrent_state::torrent_metadata_downloading;
            break;

        case lt::torrent_status::state_t::downloading:
            torrent_status->state = cs_torrent_state::torrent_downloading;
            break;

        case lt::torrent_status::state_t::seeding:
            torrent_status->state = cs_torrent_state::torrent_seeding;
            break;

        case lt::torrent_status::state_t::finished:
            torrent_status->state = cs_torrent_state::torrent_finished;
            break;

        default:
            torrent_status->state = cs_torrent_state::torrent_state_unknown;
            break;
        }
    }

    torrent_status->progress = s.progress;

    torrent_status->count_peers = s.num_peers;
    torrent_status->count_seeds = s.num_seeds;

    torrent_status->bytes_uploaded = s.total_payload_upload;
    torrent_status->bytes_downloaded = s.total_payload_download;

    torrent_status->upload_rate = s.upload_payload_rate;
    torrent_status->download_rate = s.download_payload_rate;
}

// gets per-connection detail for every peer currently connected to a torrent.
// the returned list must be freed with destroy_torrent_peer_list.
void get_torrent_peers(lt::torrent_handle* torrent, peer_list* list)
{
    if (list == nullptr)
    {
        return;
    }

    if (torrent == nullptr)
    {
        list->length = 0;
        list->peers = nullptr;
        return;
    }

    std::vector<lt::peer_info> peers;
    torrent->get_peer_info(peers);

    const auto count = static_cast<int32_t>(peers.size());
    const auto entries = new peer_info_entry[count];

    for (int32_t i = 0; i < count; i++)
    {
        const auto& p = peers[i];

        auto address_str = p.ip.address().to_string() + ":" + std::to_string(p.ip.port());
        const auto address = new char[address_str.size() + 1]();
        const auto client = new char[p.client.size() + 1]();

        std::ranges::copy(address_str, address);
        std::ranges::copy(p.client, client);

        auto encryption = cs_peer_encryption::peer_encryption_none;
        if (static_cast<bool>(p.flags & lt::peer_info::rc4_encrypted))
        {
            encryption = cs_peer_encryption::peer_encryption_rc4;
        }
        else if (static_cast<bool>(p.flags & lt::peer_info::plaintext_encrypted))
        {
            encryption = cs_peer_encryption::peer_encryption_obfuscated;
        }
        if (static_cast<bool>(p.flags & lt::peer_info::ssl_socket))
        {
            encryption = cs_peer_encryption::peer_encryption_ssl;
        }

        const auto direction = static_cast<bool>(p.flags & lt::peer_info::outgoing_connection)
            ? cs_peer_direction::peer_direction_outgoing
            : cs_peer_direction::peer_direction_incoming;

        entries[i] = {
            address,
            client,
            p.total_download,
            p.total_upload,
            p.payload_down_speed,
            p.payload_up_speed,
            encryption,
            direction,
            static_cast<bool>(p.flags & lt::peer_info::seed),
            static_cast<bool>(p.flags & lt::peer_info::choked),
            static_cast<bool>(p.flags & lt::peer_info::remote_choked),
            static_cast<bool>(p.flags & lt::peer_info::interesting),
            static_cast<bool>(p.flags & lt::peer_info::remote_interested)
        };
    }

    list->length = count;
    list->peers = entries;
}

void destroy_torrent_peer_list(peer_list* list)
{
    if (list == nullptr || list->peers == nullptr)
    {
        return;
    }

    for (int32_t i = 0; i < list->length; i++)
    {
        delete[] list->peers[i].address;
        delete[] list->peers[i].client;
    }

    delete[] list->peers;
}

// gets per-tracker announce state for every tracker attached to a torrent, across all tiers.
// the returned list must be freed with destroy_torrent_tracker_list.
void get_torrent_trackers(lt::torrent_handle* torrent, tracker_list* list)
{
    if (list == nullptr)
    {
        return;
    }

    if (torrent == nullptr)
    {
        list->length = 0;
        list->trackers = nullptr;
        return;
    }

    const auto trackers = torrent->trackers();

    const auto count = static_cast<int32_t>(trackers.size());
    const auto entries = new tracker_info_entry[count];

    for (int32_t i = 0; i < count; i++)
    {
        const auto& t = trackers[i];

        const auto url = new char[t.url.size() + 1]();
        std::ranges::copy(t.url, url);

        // per-tracker announce state is tracked per listen-socket endpoint, per protocol version
        // (v1/v2 info hash) within that endpoint. This app only has one listen interface, and most
        // torrents only use one protocol version, so aggregate across both dimensions rather than
        // picking one arbitrarily: take the worst failure count, latest message/error seen.
        std::uint8_t fails = 0;
        bool updating = false;
        std::string warning;
        std::string failure;

        for (const auto& endpoint : t.endpoints)
        {
            for (const auto& info_hash : endpoint.info_hashes)
            {
                fails = std::max(fails, info_hash.fails);
                updating = updating || info_hash.updating;

                if (!info_hash.message.empty())
                {
                    warning = info_hash.message;
                }

                if (info_hash.last_error)
                {
                    failure = info_hash.last_error.message();
                }
            }
        }

        const auto warning_message = new char[warning.size() + 1]();
        const auto failure_message = new char[failure.size() + 1]();

        std::ranges::copy(warning, warning_message);
        std::ranges::copy(failure, failure_message);

        entries[i] = {
            static_cast<int32_t>(t.tier),
            url,
            t.verified,
            fails,
            updating,
            warning_message,
            failure_message
        };
    }

    list->length = count;
    list->trackers = entries;
}

void destroy_torrent_tracker_list(tracker_list* list)
{
    if (list == nullptr || list->trackers == nullptr)
    {
        return;
    }

    for (int32_t i = 0; i < list->length; i++)
    {
        delete[] list->trackers[i].url;
        delete[] list->trackers[i].warning_message;
        delete[] list->trackers[i].failure_message;
    }

    delete[] list->trackers;
}

}
