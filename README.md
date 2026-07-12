# NetworkScanner

A desktop application that scans a given IP range to check each host's liveness (Ping), open TCP ports, MAC address / vendor, and hostname, then saves the results to CSV and optionally uploads them via FTP. It also flags suspicious open ports by matching them against known reserved and backdoor (trojan) port lists, and can run scans automatically on an hourly schedule.

It ships two UIs — a Windows-only **WPF** build and a cross-platform **Avalonia** build (Windows / macOS / Linux) — both sharing the same `NetworkScanner.Core` scan engine. The UI is available in **English (default) and Korean**, selectable in Settings.

## Download

To try it without building, grab the file for your OS from the **[Releases page](../../releases/latest)**.

| OS | File |
|---|---|
| Windows | `NetworkScanner-win-Setup.exe` (installer) or `NetworkScanner-win-Portable.zip` (unzip and run) |
| macOS | `NetworkScanner-osx-Setup.pkg` (installer) or `NetworkScanner-osx-Portable.zip` (unzip and run the `.app`) |
| Linux | `NetworkScanner.AppImage` — make it executable (`chmod +x NetworkScanner.AppImage`) and run |

The `*.nupkg`, `RELEASES*`, `assets.*.json`, and `releases.*.json` files you may also see in the Release assets are internal files for Velopack's auto-update feed — you don't need to download them.

New versions are published by pushing a `v*.*.*` git tag: `.github/workflows/release.yml` packages the app on each of the three OSes and attaches the installers to a Release automatically.

## Table of contents

- [Features](#features)
- [Project structure](#project-structure)
- [Requirements](#requirements)
- [Build and run](#build-and-run)
- [Tests](#tests)
- [Packaging for distribution](#packaging-for-distribution)
- [Runtime configuration / data files](#runtime-configuration--data-files)
- [Known limitations (cross-platform)](#known-limitations-cross-platform)

## Features

- Scan a specified IP range: liveness (Ping), round-trip time, open ports, MAC address / vendor, and hostname
- Parallel host discovery with a TCP fallback for hosts that block ICMP
- Reserved-port and risky (backdoor) port matching
- Service/banner identification, OS-family guess (from TTL), and vendor-based device classification
- Change detection between scans (new/offline hosts, MAC changes, newly opened ports, risky ports) with security alerts
- Save/load results to CSV, HTML/JSON report export, and FTP upload
- Hourly scheduling and a continuous-monitoring mode (rescan every N minutes)
- CIDR range input, per-column sort, search/alive-only filter, and Wake-on-LAN
- English / Korean UI (default English)

## Project structure

| Project | Description |
|---|---|
| `NetworkScanner/` | WPF UI (**Windows only**) |
| `NetworkScanner.Avalonia/` | Avalonia UI (**cross-platform: Windows / macOS / Linux**) |
| `NetworkScanner.Core/` | Shared, UI-independent logic library (`net8.0`, platform-agnostic) |
| `NetworkScanner.Tests/` | xUnit unit tests for `NetworkScanner.Core` |
| `OUIConvertor/` | Standalone utility that converts the raw IEEE OUI (vendor) text into `ouiinfo.ini` (WPF, Windows only, run separately) |
| `SetupNetworkScanner/` | Legacy Visual Studio installer project (.vdproj) for the WPF build |

`NetworkScanner.Core` owns: the scan engine (`ScanEngine`), IP-range math (`IPRangeUtil`), settings load/save (`AppSettingsStore`), FTP upload (`FTPService`, FluentFTP-based), OUI/vendor lookup (`OUIInfo`), ARP lookup (`ArpResolver`), reference-port loading (`PortReferenceLoader`), cross-platform logging (`AppLogger`), credential protection (`CredentialProtector`), and localization (`Localization`).

### Why Core is a separate project

The WPF project (`UseWPF=true`) depends on the Windows Desktop runtime, so anything that references it can only run on Windows. Extracting the UI-independent parts (scan logic, settings persistence, reference-port loading, etc.) into `NetworkScanner.Core` means:

- unit tests run on any OS,
- both the WPF and Avalonia UIs share the same `ScanEngine` without duplicating logic,
- and platform-specific pieces — ARP lookup (`ArpResolver`), logging (`AppLogger`), credential protection (`CredentialProtector`) — handle the Windows/macOS/Linux branching internally, so neither UI needs its own code.

## Requirements

- .NET 8 SDK
- `NetworkScanner` (WPF) and `SetupNetworkScanner` (vdproj) **build/run on Windows only**.
- `NetworkScanner.Avalonia`, `NetworkScanner.Core`, and `NetworkScanner.Tests` build and run on Windows / macOS / Linux.

## Build and run

```bash
# Build the whole solution (Windows — includes WPF)
dotnet build NetworkScanner.sln

# On macOS/Linux, to compile-check the whole solution including the WPF project
# (compile only — the WPF app cannot run there)
dotnet build NetworkScanner.sln -p:EnableWindowsTargeting=true

# Run the Avalonia app (Windows / macOS / Linux)
cd NetworkScanner.Avalonia
dotnet run
```

## Tests

```bash
dotnet test NetworkScanner.Tests/NetworkScanner.Tests.csproj
```

The tests reference only `NetworkScanner.Core`, so they run on non-Windows environments too.

## Packaging for distribution

### Single self-contained executable (Avalonia)

```bash
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r win-x64   --self-contained true
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r osx-arm64 --self-contained true
dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r linux-x64 --self-contained true
```

### Installer packages ([Velopack](https://velopack.io))

The `vpk` CLI wraps the publish output into per-OS installers (a setup `.exe` on Windows, `.app`/`.pkg`/`.zip` on macOS, an AppImage-family package on Linux). `vpk` can only build a package **for the OS it runs on** (no cross-packaging), so each package must be built on its own OS — the commands below were used on macOS to produce and verify the real `.app`/`.pkg`.

```bash
dotnet tool install -g vpk   # once
export PATH="$PATH:$HOME/.dotnet/tools"

dotnet publish NetworkScanner.Avalonia/NetworkScanner.Avalonia.csproj -c Release -r osx-arm64 --self-contained true -o publish/osx-arm64

vpk pack --packId NetworkScanner --packVersion 1.0.0 \
  --packDir publish/osx-arm64 --mainExe NetworkScanner.Avalonia \
  --packTitle "Network Scanner" --packAuthors SMIC \
  --runtime osx-arm64 --outputDir release-packages/osx-arm64
```

On Windows, add `--mainExe NetworkScanner.Avalonia.exe` and `--icon NetworkScanner/img/Kyo-Tux-Delikate-Network.ico` to produce an installer with the app icon.

There are two CI workflows with different purposes:

- `.github/workflows/build-and-test.yml` — on every push/PR, verifies that tests, build, and packaging still succeed and stores the output as workflow artifacts (expire after 90 days, GitHub sign-in required).
- `.github/workflows/release.yml` — on a `v*.*.*` tag push, runs publish + `vpk pack` on the Windows/macOS/Linux runners and attaches the resulting installers to the [Releases page](../../releases) so anyone can download them directly.

The WPF build's legacy `.vdproj` (`SetupNetworkScanner/`) is kept as-is, but new distribution should use the Avalonia build + Velopack packages.

## Runtime configuration / data files

The following files live in the application's working directory (some are created automatically on first run).

| File | Purpose |
|---|---|
| `iprange.ini` | List of IP ranges to scan (auto-seeded with the local subnet on first run) |
| `setting.ini` | Settings: FTP account (password stored encrypted — DPAPI on Windows, Keychain on macOS, libsecret on Linux), schedule hours, port list, language, etc. |
| `annotations.ini` | User-entered name/note per IP, re-applied on later scans |
| `reservedports.ini` | Reference list of reserved (well-known) ports |
| `prohibitports.ini` | List of risky ports known to be used by backdoors/trojans |
| `ouiinfo.ini` | MAC OUI → vendor mapping (IEEE public data converted via `OUIConvertor`) |

`NetworkScanner.Avalonia` links the three reference-data files (`ouiinfo.ini` / `reservedports.ini` / `prohibitports.ini`) and the icon images directly from the `NetworkScanner/` project at build time (no duplicate copies). The bundled reference files are resolved next to the executable, so the app finds them regardless of the current working directory.

## Known limitations (cross-platform)

- **ARP (MAC address) lookup**: uses the `SendARP` API on Windows and parses the system `arp` command output on macOS/Linux. On Unix-like systems the lookup only works after the ARP cache has been populated by a ping to that IP (the scan always pings first, so normal use is fine).
- **FTP password protection**: stored in the OS credential store — DPAPI on Windows, Keychain (`security`) on macOS, libsecret (`secret-tool`) on Linux. Where those tools are unavailable (e.g. `secret-tool` not installed on Linux), it falls back to simple Base64 obfuscation and logs a warning.
- **ICMP Ping permission**: depending on the Linux/macOS setup, ICMP ping may be restricted for a normal user. When a permission problem is detected, the affected host is treated as no-response (Timeout) and a guidance message is logged once so the scan is not interrupted. If needed:
  - Linux: grant just the ping capability with `sudo setcap cap_net_raw+ep <path-to-executable>`, or run with `sudo`
  - macOS: usually works without extra permissions; if restricted, run with `sudo`
- **Unsigned installer warnings (macOS/Windows)**: the Release installers are not yet Apple/Microsoft code-signed or notarized (each requires a paid developer certificate). The following OS warnings on first launch are therefore expected and do **not** indicate malware.
  - **macOS**: if you see `"NetworkScanner-osx-Setup.pkg" can't be opened`, **right-click (Control-click) → Open** in Finder, then **Open** again in the dialog. Alternatively run `xattr -d com.apple.quarantine NetworkScanner-osx-Setup.pkg` in Terminal, then open it.
  - **Windows**: if the "Windows protected your PC" (SmartScreen) screen appears, click **More info** → **Run anyway**.
- **Antivirus false positive (e.g. AhnLab V3) — installer quarantined/deleted**: because the unsigned installer bundles an auto-update mechanism (Velopack), some antivirus products mistake it for a suspicious pattern and quarantine it without scanning. It is not actual malware; you can:
  - restore the file from V3's quarantine and add it to the scan exclusions,
  - download the **Portable (zip)** build (no installer / auto-update behavior), which is less likely to trigger a false positive (see the [Download](#download) table above),
  - report the false positive through AhnLab's false-positive report page so future builds are recognized correctly.
  - The real fix is a paid code-signing certificate, which is deferred for now on cost/priority grounds.
