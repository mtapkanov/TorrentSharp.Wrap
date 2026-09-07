# TorrentSharp.Wrap.Native
Native libraries for TorrentSharp.Wrap.

## Overview
TorrentSharp.Wrap requires a native library to function, `libtsw`, which re-exposes the required `libtorrent` functionality in a C-style interface so it can be called via P/Invoke.

For 99% of cases, this package should not be referenced directly - `TorrentSharp.Wrap` already depends on it.

### License
`libtsw` is provided under the Apache 2.0 license, while `libtorrent` uses the BSD 3-Clause license. Please refer to [license.md](license.md) for more information.
