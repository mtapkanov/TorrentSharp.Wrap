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
    alert_read_piece = 6
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

#ifdef __cplusplus
}
#endif
#endif //TSW_EVENTS_H
