set(VCPKG_CHAINLOAD_TOOLCHAIN_FILE "${CMAKE_CURRENT_LIST_DIR}/../linux-arm64-toolchain.cmake")
set(VCPKG_TARGET_ARCHITECTURE arm64)
set(VCPKG_CMAKE_SYSTEM_NAME Linux)

include("${CMAKE_CURRENT_LIST_DIR}/tsw-triplet-common.cmake")

# vcpkg builds static libs without -fPIC by default on Linux; ours ends up linked into a shared
# library (libtsw.so), which fails at link time without it (relocations against TLS symbols). Raw
# flags alone weren't enough (confirmed via CI - ports can still override them), so the CMake cache
# variable is also forced for every port.
set(VCPKG_C_FLAGS "-fPIC")
set(VCPKG_CXX_FLAGS "-fPIC")
set(VCPKG_CMAKE_CONFIGURE_OPTIONS "-DCMAKE_POSITION_INDEPENDENT_CODE=ON")
