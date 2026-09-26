//
// events.cpp - handles alert callbacks
// Created by Albie on 04/03/2024.
//

#include "events.h"
#include "locks.hpp"

#include <ctime>
#include <boost/system/system_error.hpp>
#include <libtorrent/session.hpp>
#include <libtorrent/alert_types.hpp>
#include <libtorrent/write_resume_data.hpp>

void fill_info_hash(const lt::info_hash_t &hashes, char* buffer) {
    // fill in the info hash
    if (hashes.has_v1()) {
        std::copy(hashes.v1.begin(), hashes.v1.end(), buffer);
    } else {
        std::fill(buffer, buffer + 20, 0xFF);
    }
}

// Some alerts carry a torrent_handle that can already be invalid by the time this runs - the
// torrent may have been removed from the session between the alert being generated and being
// popped off the queue. handle.info_hashes() throws in that case (see torrent_handle.hpp: "any
// operation on an uninitialized handle... will throw"), which would otherwise propagate out of
// this native callback and crash the process. Falls back to the same "unknown hash" sentinel
// fill_info_hash already uses when a v1 hash is unavailable, so the alert is safely dropped
// downstream (no attached TorrentManager will match it) instead of crashing.
void fill_info_hash_safe(const lt::torrent_handle &handle, char* buffer) {
    try {
        fill_info_hash(handle.info_hashes(), buffer);
    } catch (const boost::system::system_error&) {
        std::fill(buffer, buffer + 20, 0xFF);
    }
}

void fill_event_info(cs_alert* alert, lt::alert* lt_alert, cs_alert_type alert_type, std::string* message_temp) {
    alert->type = alert_type;

    alert->epoch = time(nullptr);
    alert->category = (int32_t) static_cast<uint32_t>(lt_alert->category());

    // assign, not append: message_temp is one std::string shared across every alert in the current
    // pop_alerts() batch (see on_events_available) - append()ing here left each alert after the first
    // in a batch with every prior alert's message still stuck to the front of its own.
    message_temp->assign(lt_alert->message());
    alert->message = message_temp->c_str();
}

void populate_peer_alert(cs_peer_alert* peer_alert, lt::peer_alert* alert, cs_peer_alert_type alert_type, std::string* message) {
    fill_event_info(&peer_alert->alert, alert, cs_alert_type::alert_peer_notification, message);

    peer_alert->type = alert_type;

    // Deliberately not `&alert->handle`: that would point into the popped lt::alert itself, which
    // libtorrent only keeps valid for this dispatch batch (the next pop_alerts() call can reuse or
    // free it) - a lifetime the C# side has no way to respect. The Handle field of PeerEvent is
    // currently unused downstream; if a real use ever needs it, it must go through a call that
    // fetches a fresh, independently-owned torrent_handle instead of reusing this one.
    peer_alert->handle = nullptr;

    auto v6_mapped_addr = alert->endpoint.address().to_v6().to_bytes();
    std::copy(v6_mapped_addr.begin(), v6_mapped_addr.end(), peer_alert->ipv6_address);

    fill_info_hash_safe(alert->handle, peer_alert->info_hash);
}

std::mutex& alert_dispatch_mutex() {
    static std::mutex mutex;
    return mutex;
}

void on_events_available(lt::session* session, cs_alert_callback callback, bool include_unmapped) {
    lock l(alert_dispatch_mutex());

    if (!l.isLockTaken()) {
        return;
    }

    std::vector<lt::alert*> events;
    std::string message_temp;

    session->pop_alerts(&events);

    handle_events:
    for (auto &alert: events) {
        // Last-resort safety net: a native library must never be able to take down its whole host
        // process over a single bad alert. This doesn't replace guarding individual torrent_handle
        // accesses at their source (see library.cpp's safe_handle_call and fill_info_hash_safe above)
        // - by the time an exception reaches here it may already have skipped straight to
        // std::terminate if it was thrown from a *nested* native call made by the managed callback()
        // below (a C++ exception can't unwind back through managed frames), but it does catch
        // anything thrown directly within this function's own alert-handling code, including from
        // future alert types added here without going through the existing safe accessors.
        try {
        switch (alert->type()) {

            // torrent state changed
            case lt::state_changed_alert::alert_type: {
                auto state_alert = lt::alert_cast<lt::state_changed_alert>(alert);
                cs_torrent_status_alert status_alert{};

                status_alert.new_state = state_alert->state;
                status_alert.old_state = state_alert->prev_state;

                fill_info_hash_safe(state_alert->handle, status_alert.info_hash);
                fill_event_info(&status_alert.alert, alert, cs_alert_type::alert_torrent_status, &message_temp);
                callback(&status_alert);
                break;
            }

                // torrent removed
            case lt::torrent_removed_alert::alert_type: {
                auto removed_alert = lt::alert_cast<lt::torrent_removed_alert>(alert);
                cs_torrent_remove_alert removed_torrent{};

                // can't use handle as it's most likely been invalidated.
                fill_info_hash(removed_alert->info_hashes, removed_torrent.info_hash);
                fill_event_info(&removed_torrent.alert, alert, cs_alert_type::alert_torrent_removed, &message_temp);
                callback(&removed_torrent);
                break;
            }

                // performance warning
            case lt::performance_alert::alert_type: {
                auto perf_alert = lt::alert_cast<lt::performance_alert>(alert);
                cs_client_performance_alert perf_warning{};

                perf_warning.warning_type = perf_alert->warning_code;

                fill_event_info(&perf_warning.alert, alert, cs_alert_type::alert_client_performance, &message_temp);
                callback(&perf_warning);
                break;
            }

                // peer connected
            case lt::peer_connect_alert::alert_type: {
                auto peer_alert = lt::alert_cast<lt::peer_connect_alert>(alert);
                auto direction = (peer_alert->direction == lt::peer_connect_alert::direction_t::in) ? cs_peer_alert_type::connected_in : cs_peer_alert_type::connected_out;

                cs_peer_alert peer_connected{};

                populate_peer_alert(&peer_connected, peer_alert, direction, &message_temp);
                callback(&peer_connected);
                break;
            }

                // peer disconnected
            case lt::peer_disconnected_alert::alert_type: {
                auto peer_alert = lt::alert_cast<lt::peer_disconnected_alert>(alert);
                cs_peer_alert peer_disconnected{};

                populate_peer_alert(&peer_disconnected, peer_alert, cs_peer_alert_type::disconnected, &message_temp);
                callback(&peer_disconnected);
                break;
            }

                // peer banned
            case lt::peer_ban_alert::alert_type: {
                auto peer_alert = lt::alert_cast<lt::peer_ban_alert>(alert);
                cs_peer_alert peer_banned{};

                populate_peer_alert(&peer_banned, peer_alert, cs_peer_alert_type::banned, &message_temp);
                callback(&peer_banned);
                break;
            }

                // peer snubbed
            case lt::peer_snubbed_alert::alert_type: {
                auto peer_alert = lt::alert_cast<lt::peer_snubbed_alert>(alert);
                cs_peer_alert peer_snubbed{};

                populate_peer_alert(&peer_snubbed, peer_alert, cs_peer_alert_type::snubbed, &message_temp);
                callback(&peer_snubbed);
                break;
            }

                // peer unsnubbed
            case lt::peer_unsnubbed_alert::alert_type: {
                auto peer_alert = lt::alert_cast<lt::peer_unsnubbed_alert>(alert);
                cs_peer_alert peer_unsnubbed{};

                populate_peer_alert(&peer_unsnubbed, peer_alert, cs_peer_alert_type::unsnubbed, &message_temp);
                callback(&peer_unsnubbed);
                break;
            }

                // peer errored
            case lt::peer_error_alert::alert_type: {
                auto peer_alert = lt::alert_cast<lt::peer_error_alert>(alert);
                cs_peer_alert peer_errored{};

                populate_peer_alert(&peer_errored, peer_alert, cs_peer_alert_type::errored, &message_temp);
                callback(&peer_errored);
                break;
            }

            case lt::metadata_received_alert::alert_type: {
                auto* meta_alert = lt::alert_cast<lt::metadata_received_alert>(alert);
                cs_metadata_received_alert metadata_alert{};

                fill_info_hash_safe(meta_alert->handle, metadata_alert.info_hash);
                fill_event_info(&metadata_alert.alert, alert, cs_alert_type::alert_metadata_received, &message_temp);
                callback(&metadata_alert);
                break;
            }

                // a piece was read from disk, either because it was requested via a deadline with
                // alert_when_available, or because it was already downloaded when that deadline was set.
            case lt::read_piece_alert::alert_type: {
                auto* piece_alert = lt::alert_cast<lt::read_piece_alert>(alert);
                cs_read_piece_alert read_piece{};

                read_piece.piece = static_cast<int32_t>(static_cast<int>(piece_alert->piece));
                read_piece.succeeded = !piece_alert->error;
                read_piece.size = read_piece.succeeded ? piece_alert->size : 0;
                read_piece.buffer = read_piece.succeeded ? piece_alert->buffer.get() : nullptr;

                fill_info_hash_safe(piece_alert->handle, read_piece.info_hash);
                fill_event_info(&read_piece.alert, alert, cs_alert_type::alert_read_piece, &message_temp);
                callback(&read_piece);
                break;
            }

            case lt::file_renamed_alert::alert_type: {
                auto* renamed_alert = lt::alert_cast<lt::file_renamed_alert>(alert);
                cs_file_renamed_alert file_renamed{};

                file_renamed.file_index = static_cast<int32_t>(static_cast<int>(renamed_alert->index));
                file_renamed.succeeded = true;

                fill_info_hash_safe(renamed_alert->handle, file_renamed.info_hash);
                fill_event_info(&file_renamed.alert, alert, cs_alert_type::alert_file_renamed, &message_temp);
                callback(&file_renamed);
                break;
            }

            case lt::file_rename_failed_alert::alert_type: {
                auto* failed_alert = lt::alert_cast<lt::file_rename_failed_alert>(alert);
                cs_file_renamed_alert file_renamed{};

                file_renamed.file_index = static_cast<int32_t>(static_cast<int>(failed_alert->index));
                file_renamed.succeeded = false;

                fill_info_hash_safe(failed_alert->handle, file_renamed.info_hash);
                fill_event_info(&file_renamed.alert, alert, cs_alert_type::alert_file_renamed, &message_temp);
                callback(&file_renamed);
                break;
            }

            case lt::storage_moved_alert::alert_type: {
                auto* moved_alert = lt::alert_cast<lt::storage_moved_alert>(alert);
                cs_storage_moved_alert storage_moved{};

                storage_moved.succeeded = true;

                fill_info_hash_safe(moved_alert->handle, storage_moved.info_hash);
                fill_event_info(&storage_moved.alert, alert, cs_alert_type::alert_storage_moved, &message_temp);
                callback(&storage_moved);
                break;
            }

            case lt::storage_moved_failed_alert::alert_type: {
                auto* failed_alert = lt::alert_cast<lt::storage_moved_failed_alert>(alert);
                cs_storage_moved_alert storage_moved{};

                storage_moved.succeeded = false;

                fill_info_hash_safe(failed_alert->handle, storage_moved.info_hash);
                fill_event_info(&storage_moved.alert, alert, cs_alert_type::alert_storage_moved, &message_temp);
                callback(&storage_moved);
                break;
            }

            case lt::scrape_reply_alert::alert_type: {
                auto* scrape_alert = lt::alert_cast<lt::scrape_reply_alert>(alert);
                cs_scrape_alert scrape{};

                scrape.succeeded = true;
                scrape.incomplete = scrape_alert->incomplete;
                scrape.complete = scrape_alert->complete;

                fill_info_hash_safe(scrape_alert->handle, scrape.info_hash);
                fill_event_info(&scrape.alert, alert, cs_alert_type::alert_scrape, &message_temp);
                callback(&scrape);
                break;
            }

            case lt::scrape_failed_alert::alert_type: {
                auto* failed_alert = lt::alert_cast<lt::scrape_failed_alert>(alert);
                cs_scrape_alert scrape{};

                scrape.succeeded = false;
                scrape.incomplete = -1;
                scrape.complete = -1;

                fill_info_hash_safe(failed_alert->handle, scrape.info_hash);
                fill_event_info(&scrape.alert, alert, cs_alert_type::alert_scrape, &message_temp);
                callback(&scrape);
                break;
            }

            case lt::save_resume_data_alert::alert_type: {
                auto* resume_alert = lt::alert_cast<lt::save_resume_data_alert>(alert);
                cs_resume_data_alert resume_data{};

                // serialized here (not in library.cpp) so the buffer's lifetime is scoped to this
                // one dispatch iteration, same as read_piece_alert's buffer - freed the moment this
                // switch case ends, well after callback() (and its synchronous Marshal.Copy on the
                // C# side) has returned.
                std::vector<char> buf;

                try {
                    buf = lt::write_resume_data_buf(resume_alert->params);
                    resume_data.succeeded = true;
                    resume_data.size = static_cast<int32_t>(buf.size());
                    resume_data.buffer = buf.data();
                } catch (const std::exception&) {
                    resume_data.succeeded = false;
                    resume_data.size = 0;
                    resume_data.buffer = nullptr;
                }

                fill_info_hash_safe(resume_alert->handle, resume_data.info_hash);
                fill_event_info(&resume_data.alert, alert, cs_alert_type::alert_resume_data, &message_temp);
                callback(&resume_data);
                break;
            }

            case lt::save_resume_data_failed_alert::alert_type: {
                auto* failed_alert = lt::alert_cast<lt::save_resume_data_failed_alert>(alert);
                cs_resume_data_alert resume_data{};

                resume_data.succeeded = false;
                resume_data.size = 0;
                resume_data.buffer = nullptr;

                fill_info_hash_safe(failed_alert->handle, resume_data.info_hash);
                fill_event_info(&resume_data.alert, alert, cs_alert_type::alert_resume_data, &message_temp);
                callback(&resume_data);
                break;
            }

            case lt::file_error_alert::alert_type: {
                auto* file_err_alert = lt::alert_cast<lt::file_error_alert>(alert);
                cs_file_error_alert file_error{};

                file_error.error_value = file_err_alert->error.value();
                file_error.operation = static_cast<uint8_t>(file_err_alert->op);
                file_error.filename = file_err_alert->filename();

                fill_info_hash_safe(file_err_alert->handle, file_error.info_hash);
                fill_event_info(&file_error.alert, alert, cs_alert_type::alert_file_error, &message_temp);
                callback(&file_error);
                break;
            }

            case lt::session_stats_alert::alert_type: {
                auto* stats_alert = lt::alert_cast<lt::session_stats_alert>(alert);
                cs_session_stats_alert session_stats{};

                const auto counters = stats_alert->counters();
                session_stats.count = static_cast<int32_t>(counters.size());
                session_stats.values = counters.data();

                fill_event_info(&session_stats.alert, alert, cs_alert_type::alert_session_stats, &message_temp);
                callback(&session_stats);
                break;
            }

            default: {
                if (!include_unmapped) {
                    break;
                }

                cs_alert generic_alert{};

                fill_event_info(&generic_alert, alert, cs_alert_type::alert_generic, &message_temp);
                callback(&generic_alert);
                break;
            }
        }
        } catch (const std::exception&) {
            // Drop the bad alert and keep draining the rest of the batch.
        } catch (...) {
        }
    }

    events.clear();
    session->pop_alerts(&events);

    if (!events.empty()) {
        goto handle_events;
    }
}
