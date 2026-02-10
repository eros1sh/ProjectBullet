# Changelog

All notable changes to ProjectBullet will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.1] - 2026-02-10

### Added

#### Config Favorites & Pinning
- Added star icon column to the config list for quick favoriting
- Favorited configs are automatically sorted to the top of the list
- Favorite state is persisted in config metadata and saved with the config file

#### Drag & Drop Config Import
- Configs can now be imported by dragging and dropping `.opk` files directly onto the config list
- Encrypted `.pbc` files can also be dropped — a password dialog will prompt for decryption
- Follows the same pattern as existing wordlist and proxy drag-and-drop import

#### Cookie Manager Blocks
- New **Cookies** block category with 9 blocks for managing HTTP cookies:
  - `SaveCookies` — Save all cookies to a JSON file
  - `LoadCookies` — Load cookies from a JSON file
  - `ClearAllCookies` — Clear all cookies from the bot session
  - `GetCookie` — Get a specific cookie value by name
  - `SetCookie` — Set a specific cookie by name and value
  - `DeleteCookie` — Delete a specific cookie by name
  - `CookieCount` — Get the total number of cookies
  - `ExportCookiesNetscape` — Export cookies in Netscape/Mozilla format
  - `ImportCookiesNetscape` — Import cookies from Netscape/Mozilla format

#### HTTP/2 Support
- Added HTTP/2 protocol support for HTTP requests
- Configurable via the `httpVersion` parameter in HTTP request blocks (values: `1.0`, `1.1`, `2.0`)
- Requires `SystemNet` as the HTTP library for HTTP/2 to work
- Uses .NET native `HttpVersionPolicy.RequestVersionOrLower` for graceful fallback

#### TLS Fingerprint Profiles
- New `browserProfile` parameter on all HTTP request blocks
- Pre-built profiles that mimic real browser TLS and header fingerprints:
  - **Chrome 120** — Full Sec-CH-UA headers, Chrome cipher suites
  - **Firefox 121** — No Sec-CH headers, Firefox-specific Accept headers
  - **Safari 17** — Safari-specific header ordering and values
  - **Edge 120** — Edge-specific Sec-CH-UA branding
  - **Random** — Randomly selects from the above profiles per request
- Custom headers always take priority over profile headers

#### Anti-Detection Header Profiles
- Integrated with TLS Fingerprint Profiles (same `browserProfile` dropdown)
- Automatically sets browser-accurate headers per profile:
  - `User-Agent`, `Accept`, `Accept-Language`, `Accept-Encoding`
  - `Sec-CH-UA`, `Sec-CH-UA-Mobile`, `Sec-CH-UA-Platform` (Chrome/Edge only)
  - `Sec-Fetch-Site`, `Sec-Fetch-Mode`, `Sec-Fetch-User`, `Sec-Fetch-Dest`
  - `Upgrade-Insecure-Requests`

#### Cloudflare Bypass Enhancement
- New **Cloudflare** block category with 3 blocks:
  - `CloudflareBypassPuppeteer` — Opens a Puppeteer browser, navigates to the target URL, waits for the Cloudflare challenge to resolve, and extracts clearance cookies
  - `IsCloudflareChallenge` — Checks if an HTML response contains Cloudflare challenge indicators
  - `GetClearanceCookie` — Extracts the `cf_clearance` cookie value from the current session
- Configurable timeout and automatic browser cleanup options

#### Telegram Bot Enhancement
- New bot commands:
  - `/stats` — Detailed statistics including per-job hit counts, CPM, success rates, and runtime
  - `/search <query>` — Search through stored hits by keyword
  - `/configs` — List all available configs with their categories and author
  - `/proxies` — View proxy pool statistics (total, working, banned, by type)
  - `/help` — Complete command reference
- **Enhanced hit notifications** — Hit messages now include proxy info, timestamp, and formatted capture data
- **Job start/stop notifications** — Get notified when jobs begin and end
- **Daily summary** — Configurable daily statistics report at a set time
- All new features are individually toggleable from the Settings page

#### Plugin Marketplace
- New **Marketplace** page accessible from the main navigation menu
- Browse available plugins from a configurable marketplace URL (JSON endpoint)
- Install plugins directly from the marketplace (downloads and extracts ZIP packages)
- Check for plugin updates with version comparison
- Uninstall plugins from the marketplace interface
- Search and filter plugins by name
- Configurable auto-update checking with customizable interval
- All marketplace settings accessible from the Settings page

#### App Lock (Password Protection)
- Protect the application with a password on startup
- Password is securely hashed using BCrypt before storage
- Setup dialog with password confirmation and minimum length validation (4+ characters)
- Lock screen with unlock/exit options — incorrect password shows error and clears input
- Failed unlock exits the application
- Enable/disable and change password from the Settings page
- Auto-lock timeout setting (in minutes, 0 = disabled)

### Changed

- Upgraded CI/CD workflows from .NET 8 to .NET 9
- Updated GitHub Actions to latest versions (checkout@v4, setup-dotnet@v4, docker actions@v3/v5)
- Streamlined release workflow to focus on native Windows client builds
- Improved issue templates with updated links

### Fixed

- Fixed config import using correct `Stream` API for `ConfigPacker.UnpackAsync`
- Fixed Telegram hit notification proxy display using `ProxyString` property instead of `Proxy` object
- Fixed Plugin Marketplace service to use correct `PluginRepository.AddPlugin()` method with stream
