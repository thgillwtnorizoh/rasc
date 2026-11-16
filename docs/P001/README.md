# P001-C01 — Native Windows achievement monitor foundation

**Actor:** Work 2 — development implementation. **Assignment:** [Issue #2](https://github.com/thgillwtnorizoh/rasc/issues/2). **Status:** VERIFIED DEVELOPMENT CHECKPOINT, not owner-accepted, not independently verified or human-runtime-qualified.

## Quick start (Windows 10/11 x64)

1. Download [Rasc-P001-Windows-x64](https://github.com/thgillwtnorizoh/rasc/actions/runs/43888156224/artifacts/14236435399), extract to a separate folder and launch `Rasc.Desktop.exe`. The artifact is a self-contained .NET 10 build; keep its adjacent files together. Unsigned development build, not a polished installer.
2. Browse to the actual GT-610 `csgo_gc/achievements.txt` used by your standalone CS:GO installation. This is a manually selected path; the tool does **not** auto-discover AppID 4465480.
3. Click **Start monitoring**. The first successfully parsed file is treated as a **silent baseline**, so it does not falsely announce already-earned achievements. After the game writes new awards, their raw IDs appear in the list. The page is an app-owned event history, not the full Steam achievement library.
4. Click **Stop** before changing paths. The app can be shut down normally; it stops the monitor before closing the SQLite ledger.

### Local app data

- Read-only input: the chosen `achievements.txt`. It is never written or renamed by RASC.
- App-owned output: `%LOCALAPPDATA%\RASC\history.db` (SQLite history/baselines) and `last-source.txt` (selected path).
- The app never publishes achievements through Steam. No game or Steam process modification/injection.
- All old Steam/modern Steam assets and sound files are **not** bundled. P002 rendering work remains unapproved.

## Implementation contracts

`src/Rasc.Core` — a separately testable .NET 10 class library:
- `GcKeyValuesParser`: GT-610 local version-one text KeyValues; top-level sections; 2 MiB bounds, strict UTF-8, signed int stats, IEEE-754 float bit-patterns, unsigned timestamp seconds and duplicate-key validation.
- `GcFileSource`: read-only file access allowing atomic replacement (`FileShare.Delete`); returns valid, missing, invalid and unavailable states separately. Transient failures never become an empty committed snapshot.
- `UnlockEventStore`: own SQLite transactions, source-specific first-run baseline, current award-state checkpoint, deterministic event IDs, persisted pending notifications, at-least-once accepted-event dispatch; same (source, achievement, timestamp) cannot create repeated history.
- `AchievementMonitor`: FileSystemWatcher as a low-latency wake signal plus a periodic reconciliation pass (~1 second). Successful event-consumer return marks a pending event delivered; a crash may cause redelivery before acknowledgment.
- `Rasc.Desktop`: native WPF source selector, monitoring status and recent unlock IDs. The event list is development tooling, not a Steam popup.

### Important semantics

- When `achievements.txt` is missing, unreadable or syntactically invalid, retain last committed state. The app does not create synthetic awards or regress progress.
- A legitimate *valid* snapshot omitting an award removes that award from current state; reappearance with its **same** recorded unlock timestamp stays deduplicated, and reappearance with a **new** timestamp can be a new event. This is an explicit ENGINEERING policy, not a claimed original Valve rule.
- This provider has no durable transient `IndicateAchievementProgress` stream. No unsupported progress popup or partial completion data are invented.
- Initial seeding ignores historic awards for popups. The raw stats file may already include many achievements that will not appear in the app's event-history list; a future catalog browser can list them separately.
- This app is not guaranteed to appear above games in exclusive fullscreen. No in-game overlay / popup renderer yet.

## Build / test / reproduce

Prerequisites for source build: .NET 10 SDK on Windows. From repository root:

```powershell
dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj -c Release
dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj -c Release
dotnet publish src/Rasc.Desktop/Rasc.Desktop.csproj -c Release -r win-x64 --self-contained true -o artifacts/Rasc-Windows-x64 -p:PublishSingleFile=false
```

CI runs these commands on `windows-latest` using SDK 10.0.404 and uploads a Windows x64 artifact. `Microsoft.Data.Sqlite` 10.0.0 plus an explicit SQLitePCLRaw bundle 2.1.14 avoids the vulnerable 2.1.11 native dependency; NU1903 is not suppressed.

### What P001-C01 does not claim

No human-launched GUI testing, no native GT-610 runtime observation, no clean-install qualification, no independent Work 1 QA, no Steam-style toasts or sound, no exact achievement names/icons/catalog, no standalone source auto-discovery, and no portability proof beyond the successful CI self-contained publish.

See [checkpoint](checkpoints/P001-C01.md) for exact tested SHA and [P000 contracts](../../research/P000/comparison/renderer-contract.md) for future renderer requirements.
