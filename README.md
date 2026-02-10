# ProjectBullet

A powerful, extensible automation and testing toolkit built with .NET 9 and WPF. ProjectBullet provides a rich set of tools for HTTP request automation, credential testing, proxy management, and more — all through an intuitive native Windows interface.

## Features

### Core Functionality
- **Multi-Run Job Engine** — Execute configs against large data sets with parallel processing, customizable bot counts, and real-time statistics (CPM, hits, progress)
- **LoliCode / LoliScript** — Write automation scripts using a simple, domain-specific language with visual block-based editing or raw code
- **Config System** — Create, import, export, and manage automation configs with built-in metadata, categories, and password protection (.opk / .pbc formats)
- **Wordlist Management** — Import and manage large wordlists with support for multiple data types and slicing
- **Hit Storage** — Automatically store, search, export, and deduplicate hits with capture data

### HTTP & Networking
- **HTTP/2 Support** — Native HTTP/2 protocol support via .NET SystemNet library, configurable per-request
- **TLS Fingerprint Profiles** — Mimic real browser TLS fingerprints (Chrome 120, Firefox 121, Safari 17, Edge 120) to avoid detection
- **Anti-Detection Header Profiles** — Automatically apply browser-accurate headers including `Sec-CH-UA`, `Sec-Fetch-*`, and proper `Accept` headers per browser profile
- **Proxy Support** — HTTP, SOCKS4, SOCKS4a, and SOCKS5 proxy support with automatic rotation, health checking, and proxy sources
- **Cloudflare Bypass** — Built-in Puppeteer-based Cloudflare challenge solver with configurable timeout and cookie extraction
- **Cookie Management Blocks** — Save, load, clear, get, set, delete cookies; export/import in Netscape format

### Automation Blocks
- **HTTP Requests** — GET, POST, PUT, DELETE, PATCH, multipart, raw, and basic auth with full header control
- **Puppeteer Browser Automation** — Headless/headful Chrome control for JavaScript-heavy sites
- **String Functions** — Comprehensive string manipulation (regex, replace, split, substring, encoding, hashing)
- **Crypto** — AES, RSA, HMAC, SHA, MD5, Base64, JWT, and more — optimized with .NET 9 static HashData APIs
- **Captcha Solving** — Integration with popular captcha solving services
- **Interop** — Execute external programs, PowerShell scripts, and system commands

### User Interface
- **Config Favorites & Pinning** — Star your most-used configs to keep them at the top of the list
- **Drag & Drop Config Import** — Drop `.opk` or `.pbc` files directly onto the config list to import
- **Customizable Themes** — Full color customization for backgrounds, text, buttons, and status indicators
- **Background Images** — Set custom background images with opacity control
- **Job Monitor** — Real-time monitoring of all running jobs with live statistics
- **Built-in Debugger** — Step through configs with variable inspection and breakpoints

### Marketplace
- **Full API Integration** — Browse, search, download, upload, and manage configs/plugins via `projectbullet.eros.sh/api`
- **HWID-Based Authentication** — Automatic device identification with optional username/password registration
- **Persistent Login** — Users stay logged in across app restarts; auth tokens are validated on startup
- **Browser Login** — Generate one-time login links to authenticate on the web panel without re-entering credentials
- **Config Download** — Download configs directly to `UserData/Configs/` folder for immediate use
- **Auto-Sync** — Automatically download your shared configs when you log in
- **Publish from Editor** — Publish or update configs to the marketplace directly from the config editor page
- **Search & Filter** — Filter by category (config/plugin), sort by newest/oldest/downloads/name, full-text search with pagination
- **User Avatars** — Display user avatars from the marketplace server in the items list
- **Update Own Items** — Update button appears only for your own marketplace items

### Integrations
- **Telegram Bot** — Control ProjectBullet remotely via Telegram:
  - `/start`, `/help` — Bot info and command list
  - `/status` — View running job statuses
  - `/stats` — Detailed statistics (total hits, CPM, success rates)
  - `/configs` — List available configs
  - `/proxies` — Proxy pool statistics
  - `/search <query>` — Search through hits
  - Hit notifications with enhanced format (proxy, capture, timestamp)
  - Job start/stop notifications
  - Daily summary reports at configurable times
- **Telegram Webhook Relay** — Receive Telegram bot updates through a server-side relay for users without static IPs:
  - One-click webhook setup from PB Settings
  - Server queues incoming Telegram updates
  - App polls the relay server every 2 seconds
  - Automatic message acknowledgment
  - Copyable webhook URL in settings
  - Seamless switching between long polling and webhook modes
- **Remote Configs** — Load configs from remote endpoints

### Captcha Solving
- **Multi-Provider Support** — Integrations with popular captcha solving services
- **12ws (wssolver.net)** — New captcha provider block for configs; calls `wssolver.net/token` API and returns the solved token as text
- **solvertr (solver.tr)** — New captcha provider block for configs; calls `solver.tr` API and returns the solved token as json

### Security
- **App Lock** — Protect the application with a password (BCrypt hashed) on startup
- **Config Encryption** — Encrypt configs with password protection for secure sharing
- **Plugin Sandboxing** — Plugins run within the application's managed environment

## Requirements

- **Operating System**: Windows 10/11 (x64, x86, or ARM64)
- **Runtime**: [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)

## Installation

### From Release (Recommended)
1. Download the latest release from [Releases](https://github.com/eros1sh/ProjectBullet/releases)
2. Extract the ZIP archive to a folder of your choice
3. Make sure [.NET 9.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) is installed
4. Run `ProjectBullet.Native.exe`

### Build from Source
```bash
# Clone the repository
git clone https://github.com/eros1sh/ProjectBullet.git
cd ProjectBullet

# Build
dotnet build ProjectBullet.sln -c Release

# Or publish for a specific platform
dotnet publish ProjectBullet.Native -c Release -r win-x64 -o ./publish
```

## Project Structure

```
ProjectBullet/
├── RuriLib/                          # Core automation library
│   ├── Blocks/                       # Automation blocks (HTTP, Puppeteer, Crypto, etc.)
│   ├── Functions/                    # HTTP client factory, crypto, parsing
│   ├── Models/                       # Configs, jobs, hits, proxies, settings
│   └── Services/                     # Plugin repository, settings services
├── RuriLib.Http/                     # Custom HTTP client library
├── RuriLib.Proxies/                  # Proxy client library (HTTP/SOCKS4/5)
├── RuriLib.Parallelization/          # Parallel execution engine
├── ProjectBullet.Core/               # Application core (EF Core, repositories, services)
├── ProjectBullet.Native/             # WPF desktop application
│   ├── Views/                        # XAML pages and dialogs
│   ├── ViewModels/                   # MVVM view models
│   └── Helpers/                      # Utilities and converters
├── ProjectBullet.Telegram/           # Telegram bot + webhook relay polling
├── ProjectBullet.Native.Updater/     # Auto-updater for the native client
└── ProjectBullet.Console/            # Console runner
```

## Configuration

All user data is stored in the `UserData/` folder:

| Folder | Contents |
|--------|----------|
| `UserData/Configs/` | Automation configs |
| `UserData/Wordlists/` | Imported wordlists |
| `UserData/Plugins/` | Installed plugins |
| `UserData/Logs/` | Job execution logs |
| `UserData/Hits/` | Stored hits database |

### Settings
Application settings are accessible from the **Settings** page and are persisted to `UserData/ProjectBulletSettings.json`:

- **General** — Default config section, bot count behavior, logging
- **Customization** — Colors, themes, background images, sounds
- **Telegram** — Bot token, chat ID, notification preferences, daily summaries, webhook relay setup
- **App Lock** — Enable/disable, set password, auto-lock timeout
- **Marketplace** — Authenticated user display, persistent login state

## Tech Stack

- **.NET 9.0** — Runtime and framework
- **WPF** — Windows desktop UI with MVVM pattern
- **Entity Framework Core 9.0.3** — Data persistence with SQLite
- **Roslyn 5.0.0** — C# runtime scripting and compilation
- **Jint 4.5.0** — JavaScript (ES2024) execution engine
- **IronPython 3.4.1** — Python 3 scripting support
- **MahApps.Metro** — Modern Windows UI controls and themes
- **AvalonEdit** — Code editor with syntax highlighting
- **PuppeteerSharp** — Headless Chrome automation
- **Selenium 4.40.0** — WebDriver browser automation
- **Telegram.Bot** — Telegram Bot API client
- **MailKit 4.14.1** — SMTP/POP3/IMAP email protocols
- **SSH.NET 2025.1.0** — SSH protocol support
- **FluentFTP 53.0.2** — FTP protocol support
- **BCrypt.Net** — Password hashing
- **LiveCharts** — Real-time charting

### Performance Optimizations
- **Static HashData APIs** — All hash/HMAC computations (MD5, SHA1, SHA256, SHA384, SHA512) use .NET 9 zero-allocation static `HashData()` methods instead of `Create()`+`ComputeHash()` pattern
- **FrozenDictionary** — Type-mapping lookups in block descriptors use `System.Collections.Frozen` for faster read-only dictionary access
- **Fast Line Counting** — `FileDataPool` uses buffered byte-scanning with `ArrayPool<byte>` for wordlist line counting instead of LINQ `Count()`, significantly faster for large files
- **Span-Based Operations** — Leverages `Span<byte>` and `ReadOnlySpan` across crypto and data processing paths

## CI/CD

GitHub Actions workflows with manual `workflow_dispatch` trigger support:

- **Build + Release** — Triggered on push to `master` with `[build]` in commit message, or manually via "Run workflow"
- **Build + Release | Staging** — Same for `staging` branch with prerelease tagging
- **Run Tests** — Automated test execution on push/PR to `master` and `staging`
- **Docker Build** — Multi-platform Docker image build and push

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m "Add my feature"`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

Please use the [issue templates](.github/ISSUE_TEMPLATE/) for bug reports and feature requests.

## License

This project is open source. See the repository for license details.
