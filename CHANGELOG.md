# Changelog

All notable changes to ProjectBullet will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.5] - 2026-02-11

### Added

#### Job Auto-Restart & Stale Detection
- New **Auto Restart** option: automatically restart jobs after completion with configurable delay
- New **Stale Job Detection**: detects jobs stuck at high progress and auto-restarts them
- Configurable threshold percentage and timeout duration for stale detection
- Scheduling options available in both Job Viewer info panel and Job Options Dialog
- Available in both WPF (Native) and Avalonia (Cross-Platform) builds

#### Updated Branding
- New application logo and icons across all platforms (Native, Avalonia, Console, Updater)
- Generated multi-resolution ICO files (16, 32, 48, 64, 128, 256)
- Updated square padded logo variants with transparent and dark backgrounds

## [0.0.4] - 2026-02-11

### Added

#### Cross-Platform Support (Avalonia UI)
- New **ProjectBullet.Avalonia** project — a full port of the WPF desktop app to Avalonia UI 11.2
- Supports **Windows**, **macOS** (Intel & Apple Silicon), and **Linux** (x64, ARM64)
- All 23 pages, 26 dialogs, and 22 custom controls converted from WPF XAML to Avalonia AXAML
- All 18 ViewModels migrated with namespace changes and Avalonia API adaptations
- Cross-platform file dialogs using Avalonia `StorageProvider` API (replaces `Microsoft.Win32` dialogs)
- Cross-platform clipboard using `TopLevel.Clipboard` (replaces `System.Windows.Clipboard`)
- Cross-platform sound playback using platform-native commands (`afplay` on macOS, `aplay` on Linux, PowerShell on Windows)
- Cross-platform screenshot service with platform-specific implementations
- Cross-platform URL opening (`xdg-open` for Linux, `open` for macOS, `explorer` for Windows)
- Cross-platform console helper with `RuntimeInformation` platform checks

#### UI Framework Migration Details
- **FluentAvalonia 2.2** replaces MahApps.Metro for modern theming and controls
- **Projektanker.Icons.Avalonia** (MaterialDesign + FontAwesome) replaces MahApps.Metro.IconPacks
- **AvaloniaEdit 11.2** replaces WPF AvalonEdit for code editing with syntax highlighting
- **LiveCharts2 SkiaSharp Avalonia** replaces WPF LiveCharts for real-time charting
- `AutoCompleteBox` replaces WPF editable `ComboBox` for suggestion-based inputs
- `TransitioningContentControl` replaces WPF `Frame` for page navigation
- `DataGrid` replaces WPF `ListView`/`GridView` for tabular data display
- Avalonia pseudoclass selectors (`:pointerover`, `:pressed`) replace WPF Triggers
- CSS-like style classes (`Classes="styled"`) replace WPF StaticResource styles
- `StyledProperty` replaces WPF `DependencyProperty` in custom controls
- `Dispatcher.UIThread` replaces `Application.Current.Dispatcher`

#### CI/CD Enhancements
- GitHub Actions build workflow now produces both WPF and Avalonia artifacts
- Avalonia builds for 6 platforms: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`
- WPF builds continue for: `win-x64`, `win-x86`, `win-arm64`

### Changed
- Updated README with cross-platform download table, installation instructions for all platforms, and updated project structure
- Updated Tech Stack section to include Avalonia UI, FluentAvalonia, and AvaloniaEdit
- Solution file updated to include ProjectBullet.Avalonia project
- Version bumped to 0.0.4

### Fixed
- Fixed icon provider registration for Projektanker.Icons.Avalonia (MaterialDesign + FontAwesome)
- Fixed `mdi-vpn-key` invalid icon name (changed to `mdi-key`)
- Fixed `Avalonia.Threading` namespace collision with `ProjectBullet.Avalonia` using `global::` prefix
- Fixed `Brush` class ambiguity between `Helpers.Brush` and `Avalonia.Media.Brush`
- Fixed Avalonia `ComboBox` missing `IsEditable`/`Text` properties (replaced with `AutoCompleteBox`)
- Fixed `SoundPlayer` cross-platform compatibility (replaced `System.Media.SoundPlayer` with process-based audio)

## [0.0.3] - 2026-02-10

### Added

#### OpenBullet 2 Migration
- One-click migration tool to import existing OpenBullet 2 data into ProjectBullet
- Supports both SQLite and LiteDB database formats
- Selective migration options for configs, proxy groups, wordlists, jobs, hits, and records
- Automatic namespace remapping, wordlist ID mapping, WAL checkpoint handling
- Duplicate detection with skip logic
- Accessible from Settings page with a visual progress dialog

## [0.0.2] - 2026-02-10

### Added
- 17 New Automation Blocks: GraphQL, gRPC, DNS Lookup, DNS-over-HTTPS, MQTT, TOTP/HOTP, QR Code, XML Parse, HTML Form Parser, Retry/Loop Control, Variable Watch, TOR Proxy, Python Script Enhancement
- HTTP/2 & HTTP/3 Support
- TLS Fingerprint Profiles (Chrome 120, Firefox 121, Safari 17, Edge 120)
- Monitor Page Redesign with summary cards, progress bars, hit rate tracking
- Enhanced Syntax Highlighting with category-based keyword coloring
- Auto-Save Config every 2 minutes
- Channel-Based Parallelizer using `System.Threading.Channels`
- SQLite WAL Mode for concurrent database performance
- ListView Virtualization with recycling mode

## [0.0.1] - 2026-02-10

### Added

#### Config Favorites & Pinning
- Added star icon column to the config list for quick favoriting
- Favorited configs are automatically sorted to the top of the list
- Favorite state is persisted in config metadata and saved with the config file

#### Drag & Drop Config Import
- Configs can now be imported by dragging and dropping `.opk` files directly onto the config list
- Encrypted `.pbc` files can also be dropped — a password dialog will prompt for decryption

#### Cookie Manager Blocks
- New **Cookies** block category with 9 blocks: SaveCookies, LoadCookies, ClearAllCookies, GetCookie, SetCookie, DeleteCookie, CookieCount, ExportCookiesNetscape, ImportCookiesNetscape

#### HTTP/2 Support
- Added HTTP/2 protocol support configurable via `httpVersion` parameter
- Uses .NET native `HttpVersionPolicy.RequestVersionOrLower` for graceful fallback

#### TLS Fingerprint Profiles
- Pre-built profiles: Chrome 120, Firefox 121, Safari 17, Edge 120, Random

#### Anti-Detection Header Profiles
- Automatically sets browser-accurate Sec-CH-UA, Sec-Fetch-* headers per profile

#### Cloudflare Bypass Enhancement
- CloudflareBypassPuppeteer, IsCloudflareChallenge, GetClearanceCookie blocks

#### Telegram Bot Enhancement
- New commands: `/stats`, `/search`, `/configs`, `/proxies`, `/help`
- Enhanced hit notifications, job start/stop notifications, daily summaries

#### Plugin Marketplace
- Browse, install, update, and uninstall plugins from marketplace

#### App Lock (Password Protection)
- BCrypt-hashed password protection on startup

### Changed
- Upgraded CI/CD workflows from .NET 8 to .NET 9
- Updated GitHub Actions to latest versions

### Fixed
- Fixed config import using correct `Stream` API
- Fixed Telegram hit notification proxy display
- Fixed Plugin Marketplace service stream handling
