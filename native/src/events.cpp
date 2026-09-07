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

    message_temp->append(lt_alert->message());
    alert->message = message_temp->c_str();
}

void populate_peer_alert(cs_peer_alert* peer_alert, lt::peer_alert* alert, cs_peer_alert_type alert_type, std::string* message) {
    fill_event_info(&peer_alert->alert, alert, cs_alert_type::alert_peer_notification, message);

    peer_alert->type = alert_type;
    peer_alert->handle = &alert->handle;

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
    }

    events.clear();
    session->pop_alerts(&events);

    if (!events.empty()) {
        goto handle_events;
    }
}
