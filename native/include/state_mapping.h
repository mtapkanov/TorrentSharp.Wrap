//
// state_mapping.h - libtorrent torrent state -> cs_torrent_state
//

#ifndef TSW_STATE_MAPPING_H
#define TSW_STATE_MAPPING_H

#include "structs.h"

#include <libtorrent/torrent_status.hpp>

// libtorrent's torrent_status::state_t and cs_torrent_state are two different numberings of the same
// idea (libtorrent: checking_files=1, downloading_metadata=2, downloading=3, finished=4, seeding=5,
// checking_resume_data=7; cs_torrent_state: checking=1, checking_resume=2, metadata_downloading=3,
// downloading=4, seeding=5, finished=6, error=7), and the managed TorrentState enum follows the
// latter. Every native path that hands a torrent state to C# - the polled status AND the
// state_changed_alert event - has to go through this one function, never a raw cast: the event path
// used to cast, which made a libtorrent "finished" (4) arrive as Downloading and "checking resume
// data" (7) arrive as Errored.
inline cs_torrent_state to_cs_torrent_state(const lt::torrent_status::state_t state)
{
    switch (state)
    {
    case lt::torrent_status::state_t::checking_files:
        return cs_torrent_state::torrent_checking;

    case lt::torrent_status::state_t::checking_resume_data:
        return cs_torrent_state::torrent_checking_resume;

    case lt::torrent_status::state_t::downloading_metadata:
        return cs_torrent_state::torrent_metadata_downloading;

    case lt::torrent_status::state_t::downloading:
        return cs_torrent_state::torrent_downloading;

    case lt::torrent_status::state_t::seeding:
        return cs_torrent_state::torrent_seeding;

    case lt::torrent_status::state_t::finished:
        return cs_torrent_state::torrent_finished;

    default:
        return cs_torrent_state::torrent_state_unknown;
    }
}

#endif //TSW_STATE_MAPPING_H
