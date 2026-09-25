//
// events.hpp - ported event structures
// Created by Albie on 04/03/2024.
//

#ifndef TSW_EVENTS_H
#define TSW_EVENTS_H

#ifdef _MSC_VER
#define CALL_CONV __cdecl
#else
#define CALL_CONV
#endif

#include "lib_export.h"
#include "struct_align.h"

#include <ctime>
#include <mutex>
#include <libtorrent/alert.hpp>
#include <libtorrent/error_code.hpp>
#include <libtorrent/torrent_status.hpp>
#include <libtorrent/torrent_handle.hpp>

// used internally in main library, not intended for public use
typedef void (CALL_CONV *cs_alert_callback)(void *alert);

TSW_NO_EXPORT void on_events_available(lt::session *session, cs_alert_callback callback, bool include_unmapped);

// Guards a session's alert queue against being torn down while a detached on_events_available
// thread is still inside session->pop_alerts() for it. destroy_session blocks on this same mutex
// before deleting the session, so any in-flight (or about to start) alert dispatch either
// finishes first or is skipped (on_events_available's own try_lock is non-blocking, so a new
// invocation racing the delete simply no-ops instead of touching freed memory).
TSW_NO_EXPORT std::mutex& alert_dispatch_mutex();

#ifdef __cplusplus
extern "C" {
#endif

enum cs_alert_type : int32_t {
    alert_generic = 0,
    alert_torrent_status = 1,
    alert_client_performance = 2,
    alert_peer_notification = 3,
    alert_torrent_removed = 4,
    alert_metadata_received = 5,
    alert_read_piece = 6,
    alert_file_renamed = 7,
    alert_storage_moved = 8,
    alert_scrape = 9,
    alert_resume_data = 10,
    alert_session_stats = 11,
    alert_file_error = 12
};

// base format for all alerts
struct TSW_STRUCT cs_alert {
    cs_alert_type type;

    int32_t category;
    int64_t epoch;

    const char *message;
};

struct TSW_STRUCT cs_torrent_status_alert {
    cs_alert alert;

    uint32_t old_state;
    uint32_t new_state;

    char info_hash[20];
};

struct TSW_STRUCT cs_torrent_remove_alert {
    cs_alert alert;

    char info_hash[20];
};

struct TSW_STRUCT cs_client_performance_alert {
    cs_alert alert;

    uint8_t warning_type;
};

enum cs_peer_alert_type : uint8_t {
    connected_in = 0,
    connected_out = 1,
    disconnected = 2,
    banned = 3,
    snubbed = 4,
    unsnubbed = 5,
    errored = 6
};

struct TSW_STRUCT cs_peer_alert {
    cs_alert alert;

    lt::torrent_handle *handle;
    cs_peer_alert_type type;

    char info_hash[20];
    char ipv6_address[16];
};

struct TSW_STRUCT cs_metadata_received_alert {
    cs_alert alert;

    char info_hash[20];
};

struct TSW_STRUCT cs_read_piece_alert {
    cs_alert alert;

    int32_t piece;
    int32_t size;
    bool succeeded;

    char info_hash[20];

    // only valid for the duration of the callback; nullptr if !succeeded
    const char *buffer;
};

// covers both file_renamed_alert and file_rename_failed_alert, differentiated by `succeeded`.
struct TSW_STRUCT cs_file_renamed_alert {
    cs_alert alert;

    int32_t file_index;
    bool succeeded;

    char info_hash[20];
};

// covers both storage_moved_alert and storage_moved_failed_alert, differentiated by `succeeded`.
struct TSW_STRUCT cs_storage_moved_alert {
    cs_alert alert;

    bool succeeded;

    char info_hash[20];
};

// covers both scrape_reply_alert and scrape_failed_alert, differentiated by `succeeded`.
// incomplete/complete are -1 when unknown (failure, or a malformed response).
struct TSW_STRUCT cs_scrape_alert {
    cs_alert alert;

    bool succeeded;

    int32_t incomplete;
    int32_t complete;

    char info_hash[20];
};

// covers both save_resume_data_alert and save_resume_data_failed_alert, differentiated by
// `succeeded`. buffer holds the bencoded resume data - only valid for the duration of the
// callback; nullptr if !succeeded.
struct TSW_STRUCT cs_resume_data_alert {
    cs_alert alert;

    bool succeeded;

    char info_hash[20];

    int32_t size;
    const char *buffer;
};

// session-wide, not associated with any particular torrent (no info_hash field). values is indexed
// the same way as get_session_stats_metrics' value_index - only valid for the duration of the callback.
struct TSW_STRUCT cs_session_stats_alert {
    cs_alert alert;

    int32_t count;
    const int64_t *values;
};

// libtorrent's file_error_alert: the storage layer failed to read or write a file it needs -
// libtorrent auto-pauses the torrent when this fires (see file_error_alert's own docs), so this is
// the only signal that a torrent stuck in checking/downloading is actually blocked on a disk-level
// problem (a missing or inaccessible file, a full disk, a permissions issue, ...) rather than just
// being slow. error_value is the raw error_code value (an errno-equivalent on POSIX); operation is
// libtorrent's operation_t as a raw byte (see libtorrent/operations.hpp for the meaning of each
// value - file, file_read, file_write, file_stat, ... - not re-declared here since most of its ~40
// values are socket-related and never apply to this alert). filename is only valid for the
// duration of the callback, same lifetime rule as read_piece's buffer/resume_data's buffer.
struct TSW_STRUCT cs_file_error_alert {
    cs_alert alert;

    int32_t error_value;
    uint8_t operation;

    char info_hash[20];

    const char *filename;
};

#ifdef __cplusplus
}
#endif
#endif //TSW_EVENTS_H
