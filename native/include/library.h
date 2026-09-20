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

    // DHT
    TSW_EXPORT bool is_session_dht_running(lt::session* session);
    TSW_EXPORT void add_session_dht_node(lt::session* session, const char* host, int32_t port);

    // IP filter - additive; add_session_ip_filter_rule reads the current filter, adds one rule,
    // and writes it back, so rules accumulate across calls until clear_session_ip_filter resets it.
    TSW_EXPORT void add_session_ip_filter_rule(lt::session* session, const char* first_ip, const char* last_ip, bool blocked);
    TSW_EXPORT void clear_session_ip_filter(lt::session* session);

    // session statistics - get_session_stats_metrics returns the (process-constant) name/index
    // table once; post_session_stats triggers an async sample, reported via alert_session_stats
    // as a plain counters array indexed the same way.
    TSW_EXPORT void get_session_stats_metrics(session_stats_metric_list* list);
    TSW_EXPORT void destroy_session_stats_metric_list(session_stats_metric_list* list);
    TSW_EXPORT void post_session_stats(lt::session* session);

    // torrent control
    TSW_EXPORT lt::torrent_info* create_torrent_file(const char* file_path);
    TSW_EXPORT lt::torrent_info* create_torrent_bytes(const char* data, long length);
    TSW_EXPORT void destroy_torrent(lt::torrent_info* torrent);

    // resume_data/resume_data_len are optional (nullptr/0 to skip) - a buffer previously returned
    // by save_torrent_resume_data, parsed via lt::read_resume_data and merged into the attach.
    TSW_EXPORT lt::torrent_handle* attach_torrent(lt::session* session, lt::torrent_info* torrent, const char* save_path, const char* resume_data, int32_t resume_data_len);
    TSW_EXPORT lt::torrent_handle* attach_magnet(lt::session* session, const char* magnet_uri, const char* save_path, const char* resume_data, int32_t resume_data_len);
    TSW_EXPORT void detach_torrent(lt::session* session, lt::torrent_handle* torrent);

    // triggers an async save of a torrent's resume data; completion is reported via alert_resume_data.
    TSW_EXPORT void save_torrent_resume_data(lt::torrent_handle* torrent);

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

    // per-piece download priority
    TSW_EXPORT uint8_t get_piece_priority(lt::torrent_handle* torrent, int32_t piece_index);
    TSW_EXPORT void set_piece_priority(lt::torrent_handle* torrent, int32_t piece_index, uint8_t priority);
    TSW_EXPORT void get_torrent_piece_priorities(lt::torrent_handle* torrent, uint8_t* priorities_out, int32_t priorities_len);
    TSW_EXPORT void set_torrent_piece_priorities(lt::torrent_handle* torrent, const uint8_t* priorities, int32_t priorities_len);

    // maps a byte range within a file to the piece that owns it
    TSW_EXPORT piece_request map_file_range(lt::torrent_info* torrent, int32_t file_index, int64_t offset, int32_t size);

    // peer list - per-connection detail (address, client, rates, choke/interest state).
    TSW_EXPORT void get_torrent_peers(lt::torrent_handle* torrent, peer_list* list);
    TSW_EXPORT void destroy_torrent_peer_list(peer_list* list);

    // tracker list - per-tracker announce state (tier, url, last announce outcome).
    TSW_EXPORT void get_torrent_trackers(lt::torrent_handle* torrent, tracker_list* list);
    TSW_EXPORT void destroy_torrent_tracker_list(tracker_list* list);

    // tracker & web-seed management
    TSW_EXPORT void add_torrent_tracker(lt::torrent_handle* torrent, const char* url, uint8_t tier);
    TSW_EXPORT void replace_torrent_trackers(lt::torrent_handle* torrent, const char* const* urls, const uint8_t* tiers, int32_t count);
    TSW_EXPORT void add_torrent_url_seed(lt::torrent_handle* torrent, const char* url);
    TSW_EXPORT void add_torrent_http_seed(lt::torrent_handle* torrent, const char* url);

    // sends a scrape request to a tracker (idx -1 = last working tracker); completion is reported
    // asynchronously via alert_scrape (see events.cpp's scrape_reply_alert/scrape_failed_alert handling).
    TSW_EXPORT void scrape_torrent_tracker(lt::torrent_handle* torrent, int32_t tracker_index);

    // download control
    TSW_EXPORT void start_torrent(lt::torrent_handle* torrent);
    TSW_EXPORT void stop_torrent(lt::torrent_handle* torrent);
    TSW_EXPORT void reannounce_torrent(lt::torrent_handle* torrent, const int32_t seconds, const uint8_t ignore_min_interval);

    TSW_EXPORT void get_torrent_status(lt::torrent_handle* torrent, torrent_status* torrent_status);

    // storage management - all three are fire-and-forget triggers; completion is reported
    // asynchronously via alert_file_renamed/alert_storage_moved (force_recheck's progress is
    // already visible through the existing torrent_checking/torrent_checking_resume states).
    TSW_EXPORT void force_recheck(lt::torrent_handle* torrent);
    TSW_EXPORT void rename_torrent_file(lt::torrent_handle* torrent, int32_t file_index, const char* new_name);
    TSW_EXPORT void move_torrent_storage(lt::torrent_handle* torrent, const char* new_path);

    // per-torrent bandwidth limits, in bytes/sec. 0 means unlimited.
    TSW_EXPORT int32_t get_torrent_upload_limit(lt::torrent_handle* torrent);
    TSW_EXPORT void set_torrent_upload_limit(lt::torrent_handle* torrent, int32_t limit);
    TSW_EXPORT int32_t get_torrent_download_limit(lt::torrent_handle* torrent);
    TSW_EXPORT void set_torrent_download_limit(lt::torrent_handle* torrent, int32_t limit);

    // download queue position - lower positions are downloaded first among non-seeding torrents.
    TSW_EXPORT int32_t get_torrent_queue_position(lt::torrent_handle* torrent);
    TSW_EXPORT void torrent_queue_position_up(lt::torrent_handle* torrent);
    TSW_EXPORT void torrent_queue_position_down(lt::torrent_handle* torrent);
    TSW_EXPORT void torrent_queue_position_top(lt::torrent_handle* torrent);
    TSW_EXPORT void torrent_queue_position_bottom(lt::torrent_handle* torrent);

    // individual torrent_flags_t toggles.
    TSW_EXPORT bool get_torrent_sequential_download(lt::torrent_handle* torrent);
    TSW_EXPORT void set_torrent_sequential_download(lt::torrent_handle* torrent, bool value);
    TSW_EXPORT bool get_torrent_super_seeding(lt::torrent_handle* torrent);
    TSW_EXPORT void set_torrent_super_seeding(lt::torrent_handle* torrent, bool value);
    TSW_EXPORT bool get_torrent_share_mode(lt::torrent_handle* torrent);
    TSW_EXPORT void set_torrent_share_mode(lt::torrent_handle* torrent, bool value);
    TSW_EXPORT bool get_torrent_upload_mode(lt::torrent_handle* torrent);
    TSW_EXPORT void set_torrent_upload_mode(lt::torrent_handle* torrent, bool value);

#ifdef __cplusplus
}
#endif
#endif //TSW_LIBRARY_HPP
