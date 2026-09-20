//
// library.cpp
// Created by Albie on 29/02/2024.
//

#include "library.h"

#include <algorithm>
#include <fstream>
#include <mutex>
#include <unordered_set>
#include <boost/system/system_error.hpp>
#include <libtorrent/fingerprint.hpp>
#include <libtorrent/ip_filter.hpp>
#include <libtorrent/magnet_uri.hpp>
#include <libtorrent/peer_info.hpp>
#include <libtorrent/read_resume_data.hpp>
#include <libtorrent/session_stats.hpp>
#include <libtorrent/torrent_flags.hpp>
#include <libtorrent/torrent_handle.hpp>
#include <libtorrent/write_resume_data.hpp>

namespace
{
    // Every lt::torrent_handle this library hands out to C# is a heap allocation we own (created
    // in attach_torrent/attach_magnet, freed in detach_torrent) and passed around as a raw IntPtr.
    // Two problems follow from that, and neither is solved by the other:
    //
    // 1. Virtually every torrent_handle method used in this file - status(), torrent_file(),
    //    get_peer_info(), trackers(), have_piece(), file_priority() (both directions), pause(),
    //    resume(), set/reset_piece_deadline(), force_reannounce(), upload/download_limit,
    //    queue_position*, flags()/set_flags()/unset_flags() - is implemented in libtorrent
    //    (torrent_handle.cpp) via either sync_call/sync_call_ret or async_call. Both throw
    //    boost::system::system_error(errors::invalid_torrent_handle) up front (the difference
    //    between them is only what happens *after* that check passes - sync_call blocks for a
    //    result, async_call posts and returns) the moment the *torrent* a still-alive handle refers
    //    to has already been removed from the session - which can happen to a handle we never
    //    touched via detach_torrent at all, since libtorrent hands out a second torrent_handle to
    //    an *existing* torrent rather than erroring when add_torrent sees a duplicate info-hash, and
    //    detaching either handle removes the one torrent both of them refer to. An uncaught C++
    //    exception can't safely cross the P/Invoke boundary - it takes down the whole host process -
    //    so this needs catching regardless of which of the two call styles a given method uses.
    //
    // 2. detach_torrent doesn't just invalidate the target torrent, it deletes the torrent_handle
    //    *object itself*. Any call still touching that same pointer - the alert-dispatch thread
    //    calling back into another native function with it (e.g. MetadataReceivedHandle ->
    //    GetHandleTorrentInfo), or any other managed thread (a Timer callback, a background Task)
    //    calling a TorrentManager method directly - is now a genuine use-after-free. glibc's
    //    allocator overwrites a freed chunk's first bytes immediately (tcache bookkeeping),
    //    corrupting the handle's own std::weak_ptr member outright, so this isn't the "throws
    //    cleanly" case above - no catch block can make it safe. A plain mutex around the delete
    //    isn't enough either: a call already queued on that mutex when detach_torrent starts will
    //    still dereference the now-freed handle the moment it finally gets the lock. The two
    //    checks have to be inseparable - "is this handle still live" and "make it not live" must
    //    happen under the same lock as any use of it - hence the registry below instead.
    //
    // One process-wide mutex/set pair is coarser than a per-handle lock, but attach/detach/status
    // calls aren't a hot path, so the simplicity is worth it.
    std::mutex& torrent_handle_mutex()
    {
        static std::mutex mutex;
        return mutex;
    }

    std::unordered_set<lt::torrent_handle*>& live_torrent_handles()
    {
        static std::unordered_set<lt::torrent_handle*> handles;
        return handles;
    }

    void register_live_handle(lt::torrent_handle* handle)
    {
        std::lock_guard<std::mutex> guard(torrent_handle_mutex());
        live_torrent_handles().insert(handle);
    }

    // Runs fn(*handle) while holding torrent_handle_mutex(), but only if handle is still registered
    // as live - returns fallback immediately otherwise, without ever dereferencing a dangling
    // pointer. Also catches boost::system::system_error from fn() itself (see point 1 above) so
    // every torrent_handle access in this file goes through one place for both problems.
    template <typename Ret, typename Fun>
    Ret with_live_handle(lt::torrent_handle* handle, Fun&& fn, Ret fallback)
    {
        std::lock_guard<std::mutex> guard(torrent_handle_mutex());

        if (handle == nullptr || !live_torrent_handles().contains(handle))
        {
            return fallback;
        }

        try
        {
            return fn(*handle);
        }
        catch (const boost::system::system_error&)
        {
            return fallback;
        }
    }

    template <typename Fun>
    void with_live_handle(lt::torrent_handle* handle, Fun&& fn)
    {
        std::lock_guard<std::mutex> guard(torrent_handle_mutex());

        if (handle == nullptr || !live_torrent_handles().contains(handle))
        {
            return;
        }

        try
        {
            fn(*handle);
        }
        catch (const boost::system::system_error&)
        {
        }
    }
}

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

bool is_session_dht_running(lt::session* session)
{
    return session != nullptr && session->is_dht_running();
}

void add_session_dht_node(lt::session* session, const char* host, const int32_t port)
{
    if (session == nullptr || host == nullptr)
    {
        return;
    }

    session->add_dht_node(std::make_pair(std::string(host), static_cast<int>(port)));
}

// reads the current filter, adds one rule, writes it back - rules accumulate across calls.
void add_session_ip_filter_rule(lt::session* session, const char* first_ip, const char* last_ip, const bool blocked)
{
    if (session == nullptr || first_ip == nullptr || last_ip == nullptr)
    {
        return;
    }

    lt::error_code ec;

    const auto first = lt::make_address(first_ip, ec);
    if (ec)
    {
        return;
    }

    const auto last = lt::make_address(last_ip, ec);
    if (ec)
    {
        return;
    }

    auto filter = session->get_ip_filter();
    filter.add_rule(first, last, blocked ? lt::ip_filter::blocked : 0);
    session->set_ip_filter(filter);
}

void clear_session_ip_filter(lt::session* session)
{
    if (session == nullptr)
    {
        return;
    }

    session->set_ip_filter(lt::ip_filter());
}

// returns the (process-constant) name/index table for every metric alert_session_stats' counters
// array can carry. The caller is expected to fetch this once and cache it, matching libtorrent's
// own documented usage (session_stats_metrics() doesn't depend on any particular session instance).
void get_session_stats_metrics(session_stats_metric_list* list)
{
    if (list == nullptr)
    {
        return;
    }

    const auto metrics = lt::session_stats_metrics();
    const auto count = static_cast<int32_t>(metrics.size());
    const auto entries = new session_stats_metric[count];

    for (int32_t i = 0; i < count; i++)
    {
        const auto& metric = metrics[i];
        const auto name_len = std::char_traits<char>::length(metric.name);
        const auto name = new char[name_len + 1]();

        std::copy(metric.name, metric.name + name_len, name);

        entries[i] = {
            name,
            metric.value_index,
            metric.type == lt::metric_type_t::gauge ? static_cast<uint8_t>(1) : static_cast<uint8_t>(0)
        };
    }

    list->length = count;
    list->metrics = entries;
}

void destroy_session_stats_metric_list(session_stats_metric_list* list)
{
    if (list == nullptr || list->metrics == nullptr)
    {
        return;
    }

    for (int32_t i = 0; i < list->length; i++)
    {
        delete[] list->metrics[i].name;
    }

    delete[] list->metrics;
}

// triggers an async sample of the session's counters/gauges, reported via alert_session_stats.
void post_session_stats(lt::session* session)
{
    if (session == nullptr)
    {
        return;
    }

    session->post_session_stats();
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
// resume_data/resume_data_len are optional (nullptr/0 to skip).
lt::torrent_handle* attach_torrent(lt::session* session, lt::torrent_info* torrent, const char* save_path, const char* resume_data, const int32_t resume_data_len)
{
    if (session == nullptr || torrent == nullptr)
    {
        return nullptr;
    }

    lt::add_torrent_params params;

    if (resume_data != nullptr && resume_data_len > 0)
    {
        lt::error_code ec;
        auto resume_params = lt::read_resume_data(lt::span(resume_data, resume_data_len), ec);

        // malformed/stale resume data shouldn't fail the whole attach - just fall back to a
        // regular attach without it.
        if (!ec)
        {
            params = std::move(resume_params);
        }
    }

    std::string save_path_copy(save_path);

    if (!save_path_copy.empty())
    {
        params.save_path = save_path_copy;
    }

    // enable paused-by-default, disable auto-management
    params.flags |= lt::torrent_flags::paused;
    params.flags &= ~lt::torrent_flags::auto_managed;

    // set torrent info - make_shared creates a copy. Always taken from the caller-supplied
    // torrent, even when resume data was applied above, since that's the definitive metadata the
    // caller asked to attach - resume data mainly contributes piece/file state, not authority over
    // which torrent this handle represents.
    params.ti = std::make_shared<lt::torrent_info>(*torrent);
    const auto handle = new lt::torrent_handle(session->add_torrent(params));

    if (handle->is_valid())
    {
        register_live_handle(handle);
        return handle;
    }

    delete handle;
    return nullptr;
}

// resume_data/resume_data_len are optional (nullptr/0 to skip).
lt::torrent_handle* attach_magnet(lt::session* session, const char* magnet_uri, const char* save_path, const char* resume_data, const int32_t resume_data_len)
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

    if (resume_data != nullptr && resume_data_len > 0)
    {
        lt::error_code resume_ec;
        auto resume_params = lt::read_resume_data(lt::span(resume_data, resume_data_len), resume_ec);

        // resume data, once it parses, fully describes an add_torrent_params (trackers, and - once
        // metadata was fetched before it was saved - the torrent_info itself), richer than what a
        // bare magnet URI carries, so it takes over entirely. Falls back to the magnet-only params
        // on a parse failure rather than fail the whole attach over stale/corrupt resume data.
        if (!resume_ec)
        {
            params = std::move(resume_params);
        }
    }

    if (save_path != nullptr && *save_path != '\0')
    {
        params.save_path = std::string(save_path);
    }

    params.flags |= lt::torrent_flags::paused;
    params.flags &= ~lt::torrent_flags::auto_managed;

    const auto handle = new lt::torrent_handle(session->add_torrent(params));

    if (handle->is_valid())
    {
        register_live_handle(handle);
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

    // Unregistering and deleting under the same lock every other torrent_handle access takes (see
    // with_live_handle above) is what actually closes the use-after-free: any call that already
    // has the lock finishes untouched by this; any call still waiting for it will find the handle
    // gone from the registry once it gets in, and bail out instead of touching freed memory.
    std::lock_guard<std::mutex> guard(torrent_handle_mutex());

    if (!live_torrent_handles().contains(torrent))
    {
        return;
    }

    live_torrent_handles().erase(torrent);

    try
    {
        torrent->pause();
        session->remove_torrent(*torrent);
    }
    catch (const boost::system::system_error&)
    {
        // Our wrapper is alive (checked above) but the torrent it weakly refers to is already
        // gone - libtorrent hands out a second torrent_handle to an *existing* torrent instead of
        // erroring when add_torrent sees a duplicate info-hash (see attach_torrent/attach_magnet),
        // so a different handle to the same underlying torrent may have already been detached.
        // pause()/remove_torrent() go through the same throwing validity check as the sync_call
        // methods with_live_handle guards elsewhere in this file (torrent_handle::async_call also
        // throws boost::system::system_error(errors::invalid_torrent_handle) up front, it just
        // doesn't block on the result the way sync_call does) - nothing left to do here beyond
        // freeing our own wrapper object below.
    }

    delete torrent;
}

// triggers an async save of a torrent's resume data. completion is reported via alert_resume_data
// (see events.cpp's save_resume_data_alert/save_resume_data_failed_alert handling).
void save_torrent_resume_data(lt::torrent_handle* torrent)
{
    with_live_handle(torrent, [](lt::torrent_handle& h) { h.save_resume_data(); });
}

// returns a heap-allocated copy of the torrent_info from a torrent_handle (available after metadata is fetched).
// the returned pointer must be freed with destroy_torrent.
lt::torrent_info* get_handle_torrent_info(lt::torrent_handle* handle)
{
    return with_live_handle<lt::torrent_info*>(handle, [](lt::torrent_handle& h) -> lt::torrent_info*
    {
        const auto ti = h.torrent_file();
        return ti ? new lt::torrent_info(*ti) : nullptr;
    }, nullptr);
}

void get_torrent_handle_info_hash(lt::torrent_handle* handle, char* hash_out)
{
    if (hash_out == nullptr)
    {
        return;
    }

    // info_hashes() is not sync_call-based (it's a plain weak_ptr check with no throw), so the only
    // failure mode here is a dead/live-freed handle - with_live_handle's registry check covers that;
    // fill the same "unknown hash" sentinel it would if info_hashes() came back empty either way.
    std::fill_n(hash_out, 20, static_cast<char>(0xFF));

    with_live_handle(handle, [hash_out](lt::torrent_handle& h)
    {
        const auto& hashes = h.info_hashes();

        if (hashes.has_v1())
        {
            std::copy(hashes.v1.begin(), hashes.v1.end(), hash_out);
        }
    });
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
    with_live_handle(torrent, [file_index, priority](lt::torrent_handle& h)
    {
        h.file_priority(static_cast<lt::file_index_t>(file_index), static_cast<lt::download_priority_t>(priority));
    });
}

// get the download priority for a file in a torrent.
uint8_t get_file_dl_priority(lt::torrent_handle* torrent, const int32_t file_index)
{
    return with_live_handle<uint8_t>(torrent, [file_index](lt::torrent_handle& h)
    {
        return static_cast<uint8_t>(h.file_priority(static_cast<lt::file_index_t>(file_index)));
    }, 0);
}

// set the deadline for a piece to be downloaded, requesting an alert once it's available -
// whether it still needs downloading or is already on disk (see read_piece_alert in events.cpp).
void set_piece_deadline(lt::torrent_handle* torrent, const int32_t piece_index, const int32_t deadline_ms)
{
    with_live_handle(torrent, [piece_index, deadline_ms](lt::torrent_handle& h)
    {
        h.set_piece_deadline(
            static_cast<lt::piece_index_t>(piece_index),
            deadline_ms,
            lt::torrent_handle::alert_when_available);
    });
}

// removes the deadline from a piece - it's no longer considered a priority to download ahead of others.
void reset_piece_deadline(lt::torrent_handle* torrent, const int32_t piece_index)
{
    with_live_handle(torrent, [piece_index](lt::torrent_handle& h)
    {
        h.reset_piece_deadline(static_cast<lt::piece_index_t>(piece_index));
    });
}

// returns true if this piece has been completely downloaded and written to disk.
bool have_piece(lt::torrent_handle* torrent, const int32_t piece_index)
{
    return with_live_handle<bool>(torrent, [piece_index](lt::torrent_handle& h)
    {
        return h.have_piece(static_cast<lt::piece_index_t>(piece_index));
    }, false);
}

// the total number of pieces in the torrent, once metadata is available.
int32_t get_torrent_piece_count(lt::torrent_handle* torrent)
{
    return with_live_handle<int32_t>(torrent, [](lt::torrent_handle& h) -> int32_t
    {
        const auto ti = h.torrent_file();
        return ti ? static_cast<int32_t>(static_cast<int>(ti->num_pieces())) : 0;
    }, 0);
}

// fills pieces_out with one byte per piece (0 or 1) describing whether it's been downloaded.
// synchronous - avoids needing an alert round-trip just to read piece completion.
void get_torrent_piece_map(lt::torrent_handle* torrent, uint8_t* pieces_out, const int32_t pieces_len)
{
    if (pieces_out == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [pieces_out, pieces_len](lt::torrent_handle& h)
    {
        const auto s = h.status(lt::torrent_handle::query_pieces);
        const auto count = std::min(pieces_len, static_cast<int32_t>(s.pieces.size()));

        for (int32_t i = 0; i < count; i++)
        {
            pieces_out[i] = s.pieces[lt::piece_index_t(i)] ? 1 : 0;
        }
    });
}

// reads the download priority of a single piece.
uint8_t get_piece_priority(lt::torrent_handle* torrent, const int32_t piece_index)
{
    return with_live_handle<uint8_t>(torrent, [piece_index](lt::torrent_handle& h)
    {
        return static_cast<uint8_t>(h.piece_priority(static_cast<lt::piece_index_t>(piece_index)));
    }, 0);
}

// sets the download priority of a single piece.
void set_piece_priority(lt::torrent_handle* torrent, const int32_t piece_index, const uint8_t priority)
{
    with_live_handle(torrent, [piece_index, priority](lt::torrent_handle& h)
    {
        h.piece_priority(static_cast<lt::piece_index_t>(piece_index), static_cast<lt::download_priority_t>(priority));
    });
}

// fills priorities_out with one byte per piece describing its download priority (0-7).
void get_torrent_piece_priorities(lt::torrent_handle* torrent, uint8_t* priorities_out, const int32_t priorities_len)
{
    if (priorities_out == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [priorities_out, priorities_len](lt::torrent_handle& h)
    {
        const auto priorities = h.get_piece_priorities();
        const auto count = std::min(priorities_len, static_cast<int32_t>(priorities.size()));

        for (int32_t i = 0; i < count; i++)
        {
            priorities_out[i] = static_cast<uint8_t>(priorities[i]);
        }
    });
}

// sets the download priority of every piece in the torrent at once, one byte per piece (0-7).
void set_torrent_piece_priorities(lt::torrent_handle* torrent, const uint8_t* priorities, const int32_t priorities_len)
{
    if (priorities == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [priorities, priorities_len](lt::torrent_handle& h)
    {
        std::vector<lt::download_priority_t> values(priorities_len);

        for (int32_t i = 0; i < priorities_len; i++)
        {
            values[i] = static_cast<lt::download_priority_t>(priorities[i]);
        }

        h.prioritize_pieces(values);
    });
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
    with_live_handle(torrent, [](lt::torrent_handle& h) { h.resume(); });
}

// start and stop the download of a torrent.
void stop_torrent(lt::torrent_handle* torrent)
{
    with_live_handle(torrent, [](lt::torrent_handle& h) { h.pause(); });
}

void reannounce_torrent(lt::torrent_handle* torrent, const int32_t seconds, const uint8_t ignore_min_interval)
{
    with_live_handle(torrent, [seconds, ignore_min_interval](lt::torrent_handle& h)
    {
        lt::reannounce_flags_t flags = {};

        if (ignore_min_interval)
        {
            flags |= lt::torrent_handle::ignore_min_interval;
        }

        h.force_reannounce(seconds, -1, flags);
    });
}

// get the progress of a torrent.
void get_torrent_status(lt::torrent_handle* torrent, torrent_status* torrent_status)
{
    if (torrent_status == nullptr)
    {
        return;
    }

    *torrent_status = {};
    torrent_status->state = cs_torrent_state::torrent_error;

    with_live_handle(torrent, [torrent_status](lt::torrent_handle& handle)
    {
        const auto s = handle.status();

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

        torrent_status->total_wanted = s.total_wanted;
        torrent_status->total_wanted_done = s.total_wanted_done;

        torrent_status->all_time_upload = s.all_time_upload;
        torrent_status->all_time_download = s.all_time_download;

        torrent_status->added_time = s.added_time;

        torrent_status->num_connections = s.num_connections;
        torrent_status->queue_position = static_cast<int32_t>(static_cast<int>(s.queue_position));

        torrent_status->distributed_full_copies = s.distributed_full_copies;

        torrent_status->is_finished = s.is_finished;
        torrent_status->moving_storage = s.moving_storage;
    });
}

// triggers a full recheck of this torrent's data on disk. progress is reported through the
// existing torrent_checking/torrent_checking_resume states (see get_torrent_status) - no
// dedicated alert needed.
void force_recheck(lt::torrent_handle* torrent)
{
    with_live_handle(torrent, [](lt::torrent_handle& h) { h.force_recheck(); });
}

// triggers a rename of one of this torrent's files. completion is reported asynchronously via
// alert_file_renamed (see events.cpp's file_renamed_alert/file_rename_failed_alert handling).
void rename_torrent_file(lt::torrent_handle* torrent, const int32_t file_index, const char* new_name)
{
    if (new_name == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [file_index, new_name](lt::torrent_handle& h)
    {
        h.rename_file(static_cast<lt::file_index_t>(file_index), std::string(new_name));
    });
}

// triggers a move of this torrent's storage to a new path. completion is reported asynchronously
// via alert_storage_moved (see events.cpp's storage_moved_alert/storage_moved_failed_alert handling).
void move_torrent_storage(lt::torrent_handle* torrent, const char* new_path)
{
    if (new_path == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [new_path](lt::torrent_handle& h)
    {
        h.move_storage(std::string(new_path));
    });
}

// per-torrent upload/download bandwidth limits, in bytes/sec. 0 means unlimited.
int32_t get_torrent_upload_limit(lt::torrent_handle* torrent)
{
    return with_live_handle<int32_t>(torrent, [](lt::torrent_handle& h)
    {
        return h.upload_limit();
    }, 0);
}

void set_torrent_upload_limit(lt::torrent_handle* torrent, const int32_t limit)
{
    with_live_handle(torrent, [limit](lt::torrent_handle& h)
    {
        h.set_upload_limit(limit);
    });
}

int32_t get_torrent_download_limit(lt::torrent_handle* torrent)
{
    return with_live_handle<int32_t>(torrent, [](lt::torrent_handle& h)
    {
        return h.download_limit();
    }, 0);
}

void set_torrent_download_limit(lt::torrent_handle* torrent, const int32_t limit)
{
    with_live_handle(torrent, [limit](lt::torrent_handle& h)
    {
        h.set_download_limit(limit);
    });
}

// download queue position - lower positions are downloaded first among non-seeding torrents.
int32_t get_torrent_queue_position(lt::torrent_handle* torrent)
{
    return with_live_handle<int32_t>(torrent, [](lt::torrent_handle& h)
    {
        return static_cast<int32_t>(static_cast<int>(h.queue_position()));
    }, -1);
}

void torrent_queue_position_up(lt::torrent_handle* torrent)
{
    with_live_handle(torrent, [](lt::torrent_handle& h) { h.queue_position_up(); });
}

void torrent_queue_position_down(lt::torrent_handle* torrent)
{
    with_live_handle(torrent, [](lt::torrent_handle& h) { h.queue_position_down(); });
}

void torrent_queue_position_top(lt::torrent_handle* torrent)
{
    with_live_handle(torrent, [](lt::torrent_handle& h) { h.queue_position_top(); });
}

void torrent_queue_position_bottom(lt::torrent_handle* torrent)
{
    with_live_handle(torrent, [](lt::torrent_handle& h) { h.queue_position_bottom(); });
}

bool get_torrent_sequential_download(lt::torrent_handle* torrent)
{
    return with_live_handle<bool>(torrent, [](lt::torrent_handle& h)
    {
        return static_cast<bool>(h.flags() & lt::torrent_flags::sequential_download);
    }, false);
}

void set_torrent_sequential_download(lt::torrent_handle* torrent, const bool value)
{
    with_live_handle(torrent, [value](lt::torrent_handle& h)
    {
        if (value) h.set_flags(lt::torrent_flags::sequential_download);
        else h.unset_flags(lt::torrent_flags::sequential_download);
    });
}

bool get_torrent_super_seeding(lt::torrent_handle* torrent)
{
    return with_live_handle<bool>(torrent, [](lt::torrent_handle& h)
    {
        return static_cast<bool>(h.flags() & lt::torrent_flags::super_seeding);
    }, false);
}

void set_torrent_super_seeding(lt::torrent_handle* torrent, const bool value)
{
    with_live_handle(torrent, [value](lt::torrent_handle& h)
    {
        if (value) h.set_flags(lt::torrent_flags::super_seeding);
        else h.unset_flags(lt::torrent_flags::super_seeding);
    });
}

bool get_torrent_share_mode(lt::torrent_handle* torrent)
{
    return with_live_handle<bool>(torrent, [](lt::torrent_handle& h)
    {
        return static_cast<bool>(h.flags() & lt::torrent_flags::share_mode);
    }, false);
}

void set_torrent_share_mode(lt::torrent_handle* torrent, const bool value)
{
    with_live_handle(torrent, [value](lt::torrent_handle& h)
    {
        if (value) h.set_flags(lt::torrent_flags::share_mode);
        else h.unset_flags(lt::torrent_flags::share_mode);
    });
}

bool get_torrent_upload_mode(lt::torrent_handle* torrent)
{
    return with_live_handle<bool>(torrent, [](lt::torrent_handle& h)
    {
        return static_cast<bool>(h.flags() & lt::torrent_flags::upload_mode);
    }, false);
}

void set_torrent_upload_mode(lt::torrent_handle* torrent, const bool value)
{
    with_live_handle(torrent, [value](lt::torrent_handle& h)
    {
        if (value) h.set_flags(lt::torrent_flags::upload_mode);
        else h.unset_flags(lt::torrent_flags::upload_mode);
    });
}

// gets per-connection detail for every peer currently connected to a torrent.
// the returned list must be freed with destroy_torrent_peer_list.
void get_torrent_peers(lt::torrent_handle* torrent, peer_list* list)
{
    if (list == nullptr)
    {
        return;
    }

    list->length = 0;
    list->peers = nullptr;

    // Fetched under the lock (see with_live_handle), formatted into the output list outside it -
    // no need to hold torrent_handle_mutex() while allocating/copying strings for every peer.
    const auto peers = with_live_handle<std::vector<lt::peer_info>>(torrent, [](lt::torrent_handle& h)
    {
        std::vector<lt::peer_info> result;
        h.get_peer_info(result);
        return result;
    }, {});

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

    list->length = 0;
    list->trackers = nullptr;

    // Fetched under the lock (see with_live_handle), formatted into the output list outside it -
    // no need to hold torrent_handle_mutex() while allocating/copying strings for every tracker.
    const auto trackers = with_live_handle<std::vector<lt::announce_entry>>(torrent, [](lt::torrent_handle& h)
    {
        return h.trackers();
    }, {});

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

// adds a tracker to the torrent's announce list.
void add_torrent_tracker(lt::torrent_handle* torrent, const char* url, const uint8_t tier)
{
    if (url == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [url, tier](lt::torrent_handle& h)
    {
        lt::announce_entry entry{std::string(url)};
        entry.tier = tier;
        h.add_tracker(entry);
    });
}

// replaces the torrent's entire announce list. urls[i] pairs with tiers[i].
void replace_torrent_trackers(lt::torrent_handle* torrent, const char* const* urls, const uint8_t* tiers, const int32_t count)
{
    if (urls == nullptr || tiers == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [urls, tiers, count](lt::torrent_handle& h)
    {
        std::vector<lt::announce_entry> entries;
        entries.reserve(count);

        for (int32_t i = 0; i < count; i++)
        {
            if (urls[i] == nullptr)
            {
                continue;
            }

            lt::announce_entry entry{std::string(urls[i])};
            entry.tier = tiers[i];
            entries.push_back(entry);
        }

        h.replace_trackers(entries);
    });
}

// adds a BEP 19 (GetRight-style) web seed to the torrent.
void add_torrent_url_seed(lt::torrent_handle* torrent, const char* url)
{
    if (url == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [url](lt::torrent_handle& h) { h.add_url_seed(std::string(url)); });
}

// adds a BEP 17 (Hoffman-style) web seed to the torrent.
void add_torrent_http_seed(lt::torrent_handle* torrent, const char* url)
{
    if (url == nullptr)
    {
        return;
    }

    with_live_handle(torrent, [url](lt::torrent_handle& h) { h.add_http_seed(std::string(url)); });
}

// sends a scrape request to a tracker. completion is reported asynchronously via alert_scrape.
void scrape_torrent_tracker(lt::torrent_handle* torrent, const int32_t tracker_index)
{
    with_live_handle(torrent, [tracker_index](lt::torrent_handle& h) { h.scrape_tracker(tracker_index); });
}

}
