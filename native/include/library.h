//
// library.hpp
// Created by Albie on 29/02/2024.
//

#ifndef TSW_LIBRARY_HPP
#define TSW_LIBRARY_HPP

#include "events.h"
#include "structs.h"
#include "lib_export.h"

#include <libtorrent/torrent_handle.hpp>

#ifdef __cplusplus
extern "C" {
#endif

    // session control
    TSW_EXPORT lt::session* create_session(lt::settings_pack* pack);
    TSW_EXPORT void destroy_session(lt::session* session);

    TSW_EXPORT void set_event_callback(lt::session* session, cs_alert_callback callback, bool include_unmapped_events);
    TSW_EXPORT void clear_event_callback(lt::session* session);

    TSW_EXPORT void apply_settings(lt::session* session, lt::settings_pack* settings);

    // torrent control
    TSW_EXPORT lt::torrent_info* create_torrent_file(const char* file_path);
    TSW_EXPORT lt::torrent_info* create_torrent_bytes(const char* data, long length);
    TSW_EXPORT void destroy_torrent(lt::torrent_info* torrent);

    TSW_EXPORT lt::torrent_handle* attach_torrent(lt::session* session, lt::torrent_info* torrent, const char* save_path);
    TSW_EXPORT lt::torrent_handle* attach_magnet(lt::session* session, const char* magnet_uri, const char* save_path);
    TSW_EXPORT void detach_torrent(lt::session* session, lt::torrent_handle* torrent);

    TSW_EXPORT lt::torrent_info* get_handle_torrent_info(lt::torrent_handle* handle);
    TSW_EXPORT void get_torrent_handle_info_hash(lt::torrent_handle* handle, char* hash_out);

    // torrent info
    TSW_EXPORT torrent_metadata* get_torrent_info(lt::torrent_info* torrent);
    TSW_EXPORT void destroy_torrent_info(torrent_metadata* info);

    TSW_EXPORT bool save_torrent_to_file(lt::torrent_info* torrent, const char* file_path);
    TSW_EXPORT void get_torrent_bytes(lt::torrent_info* torrent, char** out_data, long* out_size);
    TSW_EXPORT void free_torrent_bytes(char* data);

    // file listing
    TSW_EXPORT void get_torrent_file_list(lt::torrent_info* torrent, torrent_file_list* file_list);
    TSW_EXPORT void destroy_torrent_file_list(torrent_file_list* file_list);

    // priority control
    TSW_EXPORT uint8_t get_file_dl_priority(lt::torrent_handle* torrent, int32_t file_index);
    TSW_EXPORT void set_file_dl_priority(lt::torrent_handle* torrent, int32_t file_index, uint8_t priority);

    // piece-level streaming control
    TSW_EXPORT void set_piece_deadline(lt::torrent_handle* torrent, int32_t piece_index, int32_t deadline_ms);
    TSW_EXPORT void reset_piece_deadline(lt::torrent_handle* torrent, int32_t piece_index);
    TSW_EXPORT bool have_piece(lt::torrent_handle* torrent, int32_t piece_index);

    // piece completion bitmap - synchronous, no alert involved
    TSW_EXPORT int32_t get_torrent_piece_count(lt::torrent_handle* torrent);
    TSW_EXPORT void get_torrent_piece_map(lt::torrent_handle* torrent, uint8_t* pieces_out, int32_t pieces_len);

    // maps a byte range within a file to the piece that owns it
    TSW_EXPORT piece_request map_file_range(lt::torrent_info* torrent, int32_t file_index, int64_t offset, int32_t size);

    // peer list - per-connection detail (address, client, rates, choke/interest state).
    TSW_EXPORT void get_torrent_peers(lt::torrent_handle* torrent, peer_list* list);
    TSW_EXPORT void destroy_torrent_peer_list(peer_list* list);

    // tracker list - per-tracker announce state (tier, url, last announce outcome).
    TSW_EXPORT void get_torrent_trackers(lt::torrent_handle* torrent, tracker_list* list);
    TSW_EXPORT void destroy_torrent_tracker_list(tracker_list* list);

    // download control
    TSW_EXPORT void start_torrent(lt::torrent_handle* torrent);
    TSW_EXPORT void stop_torrent(lt::torrent_handle* torrent);
    TSW_EXPORT void reannounce_torrent(lt::torrent_handle* torrent, const int32_t seconds, const uint8_t ignore_min_interval);

    TSW_EXPORT void get_torrent_status(lt::torrent_handle* torrent, torrent_status* torrent_status);

#ifdef __cplusplus
}
#endif
#endif //TSW_LIBRARY_HPP
