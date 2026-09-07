#!/usr/bin/env bash
# Builds the native tsw library (configuring CMake on first run only) and
# then the .NET solution. Safe to re-run - only reconfigures when the build
# directory doesn't exist yet.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="$ROOT_DIR/build"
VCPKG_ROOT="${VCPKG_ROOT:-$HOME/vcpkg}"

if [ ! -x "$VCPKG_ROOT/vcpkg" ]; then
    echo "error: vcpkg not found at $VCPKG_ROOT (set VCPKG_ROOT to override)" >&2
    exit 1
fi

case "$(uname -s)-$(uname -m)" in
    Darwin-arm64) PRESET=osx-arm64 ;;
    Darwin-x86_64) PRESET=osx-x64 ;;
    Linux-x86_64) PRESET=linux-x64 ;;
    Linux-aarch64) PRESET=linux-arm64 ;;
    *) echo "error: unsupported host $(uname -s)-$(uname -m) - pass a preset name explicitly via CMAKE_PRESET" >&2; exit 1 ;;
esac
PRESET="${CMAKE_PRESET:-$PRESET}"

if [ ! -f "$BUILD_DIR/CMakeCache.txt" ]; then
    echo "==> Configuring native build with preset '$PRESET' (first run - fetches/builds libtorrent via vcpkg, can take a while)"
    (cd "$ROOT_DIR" && VCPKG_ROOT="$VCPKG_ROOT" cmake --preset "$PRESET")
fi

echo "==> Building native tsw library"
cmake --build "$BUILD_DIR"

case "$(uname -s)" in
    Darwin) NATIVE_LIB_NAME=libtsw.dylib ;;
    Linux) NATIVE_LIB_NAME=libtsw.so ;;
    *) echo "error: packaging the native NuGet package isn't supported on this host yet" >&2; exit 1 ;;
esac

# TorrentSharp.Wrap picks up libtsw via a PackageReference on TorrentSharp.Wrap.Native rather than a
# direct file copy, so local builds need that package available from a local feed (see NuGet.Config).
# The global packages cache is keyed by id+version, so it's cleared here to pick up a freshly built lib
# even though the local package's version number never changes.
echo "==> Packing TorrentSharp.Wrap.Native into local-packages/ (local dev feed)"
mkdir -p "$ROOT_DIR/native/nuget/runtimes/$PRESET/native"
cp "$BUILD_DIR/$NATIVE_LIB_NAME" "$ROOT_DIR/native/nuget/runtimes/$PRESET/native/$NATIVE_LIB_NAME"
rm -rf "$HOME/.nuget/packages/torrentsharp.wrap.native"
dotnet pack "$ROOT_DIR/native/nuget/TorrentSharp.Wrap.Native.csproj" -o "$ROOT_DIR/local-packages" -v quiet

echo "==> Building TorrentSharp.Wrap.sln"
dotnet build "$ROOT_DIR/TorrentSharp.Wrap.sln"

echo "==> Done."
