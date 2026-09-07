//
// structs.hpp
// Created by Albie on 29/02/2024.
//

#ifndef TSW_STRUCTS_HPP
#define TSW_STRUCTS_HPP

#include "struct_align.h"

#include <libtorrent/session.hpp>

#ifdef __cplusplus
extern "C" {
#endif

TSW_STRUCT typedef struct cs_torrent_file_information {
    int32_t index;

    int64_t offset;
    int64_t file_size;

    time_t modified_time;

    char* file_name;
    char* file_path;

    bool file_path_is_absolute;
    bool pad_file;
} torrent_file_information;

TSW_STRUCT typedef struct cs_torrent_meta {
    char* name;
    char* creator;
    char* comment;

    int32_t total_files;
    int64_t total_size;

    time_t creation_date;

    uint8_t info_hash_v1[20];
    uint8_t info_hash_v2[32];
} torrent_metadata;

TSW_STRUCT typedef struct cs_torrent_file_list {
    int32_t length;
    torrent_file_information* files;
} torrent_file_list;

// the result of mapping a byte range within a file to its owning piece.
TSW_STRUCT typedef struct cs_piece_request {
    int32_t piece;
    int32_t offset;
    int32_t length;
} piece_request;

enum cs_torrent_state : int32_t {
    torrent_state_unknown = 0,
    torrent_checking = 1,
    torrent_checking_resume = 2,
    torrent_metadata_downloading = 3,
    torrent_downloading = 4,
    torrent_seeding = 5,
    torrent_finished = 6,
    torrent_error = 7
};

TSW_STRUCT typedef struct cs_torrent_status {
    cs_torrent_state state;

    float progress;

    int32_t count_peers;
    int32_t count_seeds;

    int64_t bytes_uploaded;
    int64_t bytes_downloaded;

    int64_t upload_rate;
    int64_t download_rate;
} torrent_status;

enum cs_peer_encryption : uint8_t {
    peer_encryption_none = 0,
    peer_encryption_rc4 = 1,
    peer_encryption_obfuscated = 2,
    peer_encryption_ssl = 3
};

enum cs_peer_direction : uint8_t {
    peer_direction_incoming = 0,
    peer_direction_outgoing = 1
};

TSW_STRUCT typedef struct cs_peer_info {
    char* address;
    char* client;

    int64_t total_download;
    int64_t total_upload;

    int32_t download_rate;
    int32_t upload_rate;

    cs_peer_encryption encryption_type;
    cs_peer_direction direction;

    bool is_seed;
    bool we_are_choking;
    bool they_are_choking;
    bool am_interested;
    bool is_interested;
} peer_info_entry;

TSW_STRUCT typedef struct cs_peer_list {
    int32_t length;
    peer_info_entry* peers;
} peer_list;

TSW_STRUCT typedef struct cs_tracker_info {
    int32_t tier;
    char* url;

    bool verified;

    uint8_t fails;
    bool updating;

    char* warning_message;
    char* failure_message;
} tracker_info_entry;

TSW_STRUCT typedef struct cs_tracker_list {
    int32_t length;
    tracker_info_entry* trackers;
} tracker_list;

#ifdef __cplusplus
}
#endif

#endif //TSW_STRUCTS_HPP
