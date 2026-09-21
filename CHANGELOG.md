# Changelog

## [1.0.1](https://github.com/mtapkanov/TorrentSharp.Wrap/compare/v1.0.0...v1.0.1) (2026-09-20)


### Bug Fixes

* bump TorrentSharp.Wrap.Native dependency to 0.2.0 ([61f169c](https://github.com/mtapkanov/TorrentSharp.Wrap/commit/61f169c0497264311848a6934674f5f4f478aed7))

## [1.0.0](https://github.com/mtapkanov/TorrentSharp.Wrap/compare/v0.3.0...v1.0.0) (2026-09-20)


### ⚠ BREAKING CHANGES

* SettingsPack.Set(string, T) and Get<T>(string) have been removed in favor of Set(TEntry)/Get<TEntry>() using the typed setting records (e.g. AnonymousMode, ConnectionsLimit, UserAgent) under TorrentSharp.Wrap.Configurations.

### Features

* expose more libtorrent settings/controls and harden torrent_handle lifetime safety ([0de0c6c](https://github.com/mtapkanov/TorrentSharp.Wrap/commit/0de0c6cbeb3f34e025a08ec21ebb88e02a2c46e8))

## [0.3.0](https://github.com/mtapkanov/TorrentSharp.Wrap/compare/v0.2.0...v0.3.0) (2026-09-08)


### Features

* add TorrentManager.OpenFileStream for HTTP-style piece streaming ([74ecab0](https://github.com/mtapkanov/TorrentSharp.Wrap/commit/74ecab0723e2e3986f72d4152d729f6dd28e9a42))

## [0.2.0](https://github.com/mtapkanov/TorrentSharp.Wrap/compare/v0.1.0...v0.2.0) (2026-09-07)


### Features

* independent native build, DI package, and release automation ([fed1d57](https://github.com/mtapkanov/TorrentSharp.Wrap/commit/fed1d57eb0e05b15e9faa1fc371c91c834f4c99c))
