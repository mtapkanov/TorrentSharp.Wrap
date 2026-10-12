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

// Alert dispatch is tracked per session. libtorrent only calls the notify function when the alert
// queue goes from empty to non-empty, so a notification that is ever dropped leaves the queue
// non-empty and silences every later one too - the session's alerts stop until something pops them.
// register_alert_dispatch starts tracking a session and stores the managed callback to deliver to;
// on_events_available (run on a detached thread per libtorrent notification) drains that session's
// queue; release_alert_dispatch stops tracking it before the session is deleted.
TSW_NO_EXPORT void register_alert_dispatch(lt::session *session, cs_alert_callback callback, bool include_unmapped);

TSW_NO_EXPORT void on_events_available(lt::session *session);

// Blocks until any in-flight on_events_available for this session has finished and guarantees no later
// one touches the session again, so the caller can delete it right after. A notification racing the
// delete finds the session unregistered (or marked destroyed) and no-ops instead of touching freed
// memory.
TSW_NO_EXPORT void release_alert_dispatch(lt::session *session);

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
    alert_file_error = 12,
    alert_listen_failed = 13,
    alert_listen_succeeded = 14,
    alert_portmap = 15,
    alert_portmap_error = 16,
    alert_external_ip = 17,
    alert_piece_finished = 18,
    alert_hash_failed = 19,
    alert_torrent_finished = 20,
    alert_torrent_error = 21,
    alert_metadata_failed = 22
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
// callback; nullptr if !succeeded. not_modified marks the one failure that isn't one: a save
// requested only-if-modified, for a torrent with nothing new to save.
struct TSW_STRUCT cs_resume_data_alert {
    cs_alert alert;

    bool succeeded;

    char info_hash[20];

    int32_t size;
    const char *buffer;

    bool not_modified;
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

// libtorrent's listen_failed_alert: the session failed to bind a listen socket on this
// interface/port/protocol - session-wide, not tied to any torrent. If this fires for the socket
// libtorrent picked for DHT, DHT will report itself as running while never actually bootstrapping
// (no known nodes) - see cs_listen_succeeded_alert for the positive counterpart. socket_type mirrors
// lt::socket_type_t (tcp = 0, ..., utp = 3, ...) - see libtorrent/socket_type.hpp for the full list.
struct TSW_STRUCT cs_listen_failed_alert {
    cs_alert alert;

    int32_t error_value;
    uint8_t operation;
    uint8_t socket_type;

    int32_t port;

    // only valid for the duration of the callback
    const char *listen_interface;
};

// libtorrent's listen_succeeded_alert: the positive counterpart to cs_listen_failed_alert.
struct TSW_STRUCT cs_listen_succeeded_alert {
    cs_alert alert;

    uint8_t socket_type;
    int32_t port;

    // only valid for the duration of the callback
    const char *address;
};

// mirrors lt::portmap_transport.
enum cs_portmap_transport : uint8_t {
    portmap_transport_natpmp = 0,
    portmap_transport_upnp = 1
};

// libtorrent's portmap_alert: a NAT-PMP/UPnP router successfully mapped one of our ports - the
// standard proxy most torrent clients use for "is my port open" without an external check.
struct TSW_STRUCT cs_portmap_alert {
    cs_alert alert;

    int32_t external_port;
    cs_portmap_transport transport;
};

// libtorrent's portmap_error_alert: a NAT-PMP/UPnP mapping attempt failed.
struct TSW_STRUCT cs_portmap_error_alert {
    cs_alert alert;

    int32_t error_value;
    cs_portmap_transport transport;
};

// libtorrent's external_ip_alert: libtorrent learned/changed its believed external IP -
// session-wide, not tied to any torrent.
struct TSW_STRUCT cs_external_ip_alert {
    cs_alert alert;

    // only valid for the duration of the callback
    const char *external_address;
};

// libtorrent's piece_finished_alert: a piece finished downloading and passed its hash check. Note
// this can fire before the piece is fully flushed to disk - torrent_handle::have_piece() may still
// return false right after this.
struct TSW_STRUCT cs_piece_finished_alert {
    cs_alert alert;

    int32_t piece_index;

    char info_hash[20];
};

// libtorrent's hash_failed_alert: a downloaded piece failed its hash check (corrupt/bad data from a
// peer) - discarded and re-requested, never written to disk with bad content.
struct TSW_STRUCT cs_hash_failed_alert {
    cs_alert alert;

    int32_t piece_index;

    char info_hash[20];
};

// libtorrent's torrent_finished_alert: this torrent finished downloading all its selected files.
struct TSW_STRUCT cs_torrent_finished_alert {
    cs_alert alert;

    char info_hash[20];
};

// libtorrent's torrent_error_alert: this torrent transitioned into the error state. filename is only
// valid for the duration of the callback, same lifetime rule as file_error's filename.
struct TSW_STRUCT cs_torrent_error_alert {
    cs_alert alert;

    int32_t error_value;

    char info_hash[20];

    const char *filename;
};

// libtorrent's metadata_failed_alert: fetching/parsing this magnet's metadata failed.
struct TSW_STRUCT cs_metadata_failed_alert {
    cs_alert alert;

    int32_t error_value;

    char info_hash[20];
};

#ifdef __cplusplus
}
#endif
#endif //TSW_EVENTS_H
