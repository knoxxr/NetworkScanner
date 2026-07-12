# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A desktop network scanner: pings an IP range, records each host's alive state, open TCP ports, MAC/vendor, and hostname, saves results to CSV, optionally uploads via FTP, and flags open ports that match a reserved/prohibited (backdoor) port list. Two UIs share one scan engine.

## Build / test / run

```bash
# Full solution build — only works on Windows (includes the WPF project)
dotnet build NetworkScanner.sln

# On macOS/Linux the WPF project can't run, but you can still compile-check the whole solution:
dotnet build NetworkScanner.sln -p:EnableWindowsTargeting=true

# Run the cross-platform Avalonia app (Windows/macOS/Linux)
dotnet run --project NetworkScanner.Avalonia

# Tests (xUnit) — Core-only, runs on any OS
dotnet test NetworkScanner.Tests/NetworkScanner.Tests.csproj

# A single test
dotnet test NetworkScanner.Tests/NetworkScanner.Tests.csproj --filter "FullyQualifiedName~ScanEngineTests.ComputeIPCount_RangeCrossingSubnetBoundary_CountsCorrectly"
```

Note on testability: `NetworkScanner.Tests` references **only** `NetworkScanner.Core`. The WPF project targets `net8.0-windows10.0.20348.0` with `UseWPF=true`, so anything depending on it cannot execute on non-Windows (the Windows Desktop runtime doesn't exist there). Keep test-worthy logic in Core.

## Architecture

The important structural fact: **all real logic lives in `NetworkScanner.Core` (a plain `net8.0` library with no UI dependency). The two UIs are thin adapters over it.**

- **`NetworkScanner.Core`** — scan engine, settings/CSV/ini persistence, IP-range math, OS-specific helpers. Shared by both UIs and exercised by the tests.
- **`NetworkScanner`** — Windows-only **WPF** UI (`net8.0-windows...`, `UseWPF=true`).
- **`NetworkScanner.Avalonia`** — cross-platform **Avalonia** UI (`net8.0`).
- **`NetworkScanner.Tests`** — xUnit, references Core only.
- **`OUIConvertor`** — standalone WPF utility that regenerates `ouiinfo.ini` (MAC OUI → vendor table); not part of the app runtime.
- **`SetupNetworkScanner`** — legacy WPF `.vdproj` installer; not built by `dotnet` (MSBuild warns and skips it).

### How the UI talks to the engine

`ScanEngine` (in Core) is the center. A UI creates one, passing:
- the observable `IPInfoList` it binds its grid to,
- an `OUIInfo` (loaded vendor table),
- an **`IScanConfigProvider`** — the decoupling seam. The engine never reads UI controls; it calls this interface for port lists, FTP creds, scan ranges, system name, etc. In both apps the main window implements it (WPF `MainNetworkScanner`, Avalonia `MainWindow`), delegating to the settings views.

The engine reports everything through events; UI handlers marshal them to the UI thread:
`Message`, `ProgressMaxChanged`, `ProgressChanged`, `ResultsSummaryChanged`, `ItemsRefreshNeeded`, `ScanStarted`, `ScanFinished`.

`Start*` methods (`StartRefreshAllRange`, `StartSchedulingScan`, `StartCheck*PortList`) are guarded by `TryBeginScan` (lock + `CancellationTokenSource`) so only one scan runs at a time, and wrapped in `RunTracked` which raises `ScanStarted`/`ScanFinished`. `ScanningStop()` cancels the token — the "취소" button.

### Concurrency model (read before touching the scan loop)

`DoScanAllRange` runs IPs through `Parallel.ForEachAsync` (up to `ScanParallelism` = 64) using async ping/port-check. Because results arrive on many worker threads:
- `_items` writes are serialized under `_itemsLock`, exposed as **`ScanEngine.ItemsSyncRoot`**.
- `ItemsRefreshNeeded` is fired **outside** that lock — firing it inside while a UI handler needs the same lock deadlocks.
- Per-IP config (prohibited ports, port-checking flag) is prefetched once before the loop, not read from `Config` per IP (that would marshal to the UI thread 64× concurrently).
- WPF calls `BindingOperations.EnableCollectionSynchronization(_IPInfoList, engine.ItemsSyncRoot)`; Avalonia rebuilds its grid from a snapshot copied under that lock. Both UIs use non-blocking dispatch (`BeginInvoke` / `Post`) and coalesce rapid refresh requests.

`IPInfo` does **not** implement `INotifyPropertyChanged` — property edits to existing rows are reflected only when the UI is told to refresh (hence `ItemsRefreshNeeded`).

### Cross-platform seams (Core)

OS branches live behind small helpers so the rest of Core stays portable:
- `ArpResolver` — Windows `SendARP` P/Invoke vs. Unix `arp` command parsing (Unix needs a prior ping to populate the ARP cache; the scan always pings first).
- `CredentialProtector` — DPAPI (Windows) / Keychain `security` (macOS) / libsecret `secret-tool` (Linux), falling back to Base64 obfuscation when unavailable.
- `PingTester` — logs an ICMP-permission hint once when raw-socket access is denied (common on Linux/macOS).
- `IPRangeUtil` — treats IPv4 as `uint32` so ranges spanning subnet boundaries (e.g. `10.0.1.250`–`10.0.2.10`) count/iterate correctly. Do not revert to last-octet math.
- `LocalNetworkInfo` — detects the active interface's subnet; `AppSettingsStore.LoadScanRanges` seeds it as the default range when none are configured.

## Runtime data files

`ouiinfo.ini`, `reservedports.ini`, `prohibitports.ini` live in the WPF project and are linked into the Avalonia project (copied to output, not duplicated). `setting.ini` and `iprange.ini` are written at runtime in the working directory — do not commit them (a scan or app launch will regenerate them).

## Making changes across both UIs

A UI-facing change almost always needs mirroring in **both** WPF (`NetworkScanner/UC*.xaml[.cs]`, `MainNetworkScanner.xaml[.cs]`) and Avalonia (`NetworkScanner.Avalonia/Views/*.axaml[.cs]`, `MainWindow.axaml[.cs]`). Prefer pushing shared logic into Core so it's tested once and both UIs stay thin.

## Releases

Pushing a `vMAJOR.MINOR.PATCH` git tag triggers `.github/workflows/release.yml`, which publishes and Velopack-packages the **Avalonia** app on each of Windows/macOS/Linux runners and attaches the installers to a GitHub Release. `dotnet publish -p:Version=<tag>` stamps the real version. Both UIs show `ver. Major.Minor.Build` in the sidebar. Installers are unsigned — expect Gatekeeper/SmartScreen/AV false-positive warnings (documented in README).
