//
// settings.h - settings_pack interop
// Created by Albie on 09/09/24.
//

#ifndef TSW_SETTINGS_H
#define TSW_SETTINGS_H

#include "lib_export.h"
#include "struct_align.h"

#include <libtorrent/settings_pack.hpp>

#ifdef __cplusplus
extern "C" {
#endif

    TSW_EXPORT lt::settings_pack* create_settings_pack();
    TSW_EXPORT void destroy_settings_pack(lt::settings_pack* pack);

    TSW_EXPORT uint8_t settings_pack_set_str(lt::settings_pack* pack, const char* key, const char* value);
    TSW_EXPORT uint8_t settings_pack_set_bool(lt::settings_pack* pack, const char* key, uint8_t value);
    TSW_EXPORT uint8_t settings_pack_set_int(lt::settings_pack* pack, const char* key, int value);

#ifdef __cplusplus
}
#endif
#endif //TSW_SETTINGS_H
