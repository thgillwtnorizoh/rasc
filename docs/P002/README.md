# P002-C01 — First Steam Classic desktop toast

**Campaign:** P002 — Notification Renderers. **Actor:** Work 2 development + same-operator development tests (NOT independent QA). **Assignment:** [Issue #3](https://github.com/thgillwtnorizoh/rasc/issues/3). **Status:** VERIFIED DEVELOPMENT CHECKPOINT; original-runtime fidelity, human Windows execution and Work 1 independence remain unverified.

## Quick Windows test

1. Download [Rasc-P002-Classic-Windows-x64](https://github.com/thgillwtnorizoh/rasc/actions/runs/44016161175/artifacts/14298872135). Extract the self-contained app to its own folder and start `Rasc.Desktop.exe`. Windows x64; unsigned development build, no installer.
2. Click **Preview Classic**, **without starting monitoring**, to show a clearly labeled test notification. It does NOT change the game file, grant any achievement or add to SQLite history.
3. To test real unlocks, select the actual GT-610 `csgo_gc/achievements.txt` and click **Start monitoring** with **Classic popups** checked. Earn a **new** achievement. An approximate classic notification should appear at the lower right of the **primary monitor's desktop work area**. The event also enters the established SQLite history.
4. Uncheck **Classic popups** to leave monitoring/history active while suppressing new popups. Preview Classic always performs a separate, explicit local test.
5. Existing achievement rows reloaded from `history.db` when Start is clicked are history **only**; they are not new awards and do not schedule popups. First observation of a new source is the silent baseline.
6. Close the app while popups are active; no toast window should keep the process running. Existing `%LOCALAPPDATA%\RASC\history.db` should be preserved.

## Scope, source contract and engineering approximations

| Property | Source authority | P002-C01 behavior |
|---|---|---|
| Client style | May 2023 VGUI `AchievementNotification.res` / `steam.styles` | independent topmost WPF desktop window, **not a Steam injection or native overlay** |
| Panel | nominal 240×94 VGUI units | 240×94 WPF DIPs; physical-scaling parity NOT VERIFIED |
| Icon geometry | x14/y14, 64×64 image; frame x13/y13, 66×66 | exact geometry, **nonproprietary ★ placeholder**, no Valve icons |
| Title/description | x88/w144; title y25/h28; description y53/h28 | `Achievement Unlocked!` and raw GC ID fallback; localized actual display name NOT AVAILABLE |
| Background | `DialogBG #2A2E33` at top → `ClientBG #121A2A` at y80 | source-based gradient stop + darkened region; border raster/clipping provisional |
| Type | FriendsSmall Arial tall12 weight800; colors #C8D0DC and #B4B4B4 | mapped to WPF Arial size12/ExtraBold; font metric parity NOT VERIFIED |
| Time | FadeIn 450ms / Display 6000ms / FadeOut 450ms | deterministic pure timeline, 6900ms total; visually sampled by dispatcher ~33ms |
| Concurrency | StackSize 3, queue after limit | at most 3; overflow FIFO using deterministic slot indices; exact original arbitration UNKNOWN |
| Motion | SlideDirection Vertical; native interpolation unknown | **ENGINEERING: linear 12-DIP vertical travel** on enter/exit; NOT original-verified and must be corrected using captures/disassembly |
| Placement | Windows BottomRight | **ENGINEERING:** primary work area, 16-DIP edge, 6-DIP card gap; native stack offset UNKNOWN |
| Input | original notification behavior unverified | WPF borderless nonactivating desktop window with Windows extended click-through flags; requires user runtime qualification |
| Audio | classic asset/callback not verified | NOT IMPLEMENTED |
| Fullscreen/overlay | original overlay uses separate UI path | NOT SUPPORTED / NOT GUARANTEED (especially exclusive fullscreen) |

**Original evidence:** [P000 Classic reference](../../research/P000/classic/reference.md) and [discrepancy register](../../research/P000/validation/discrepancies.md). Never compare P002's provisional linear motion to historic Steam as if it were confirmed original. The raw achievement ID is not a user-facing original localized achievement name.

## Architecture

- `src/Rasc.Core/ClassicToastScheduler.cs`: pure monotonic-clock timeline, three active slots, FIFO pending events, de-duplication while the same event is pending/active, testable phase/opacity and provisional vertical offset. Never mutates SQLite or the GC source.
- `src/Rasc.Desktop/ClassicToastWindow.xaml[.cs]`: native WPF 240×94 no-activation click-through window and source-based style approximation.
- `src/Rasc.Desktop/ClassicToastHost.cs`: 33ms renderer tick on UI dispatcher, independent child windows in primary work area, close and clean up at end of phase or application shutdown.
- `src/Rasc.Desktop/MainWindow.xaml[.cs]`: preview and checkbox; the *new-event* `AchievementMonitor.UnlockDetected` callback alone can enqueue an in-game toast. History rehydration uses `GetRecent` without enqueuing.
- The existing first-run silent baseline, SQLite dedup, status/error handling and read-only provider remain unchanged.

### CI and tests

[CI run 44016161175](https://github.com/thgillwtnorizoh/rasc/actions/runs/44016161175) executed on GitHub `windows-latest` with .NET 10 SDK:
```powershell
dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"
dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental
dotnet publish src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/Rasc-Windows-x64 -p:PublishSingleFile=false
```
**32 passed, 0 failed, 0 skipped. WPF build PASS, x64 publish PASS, artifact upload PASS.** Development tests cover scheduling clock boundaries, 3/6-event bursts, FIFO, duplicate suppression, event-source integration and historical ledger preservation. No Windows GUI interaction was executed or independently tested.

## Requested manual qualification

First test Preview Classic outside the game: capture a screenshot and describe position, text wrapping, appearance and whether it interferes with desktop input. Then optionally trigger a genuine new in-game unlock in borderless mode and report whether the popup appears above it. Record any deviations in Issue #3. Do not reset the RASC database merely to make old achievements reappear; preview is safer. If a real event arrives while checkbox is unchecked, it still persists in history but is intentionally not replayed later as a popup.

## Remaining and stop conditions

No historical easing, exact stacking offsets, actual icons/metadata or audio, multi-monitor anchoring, original asset redistribution, frame-accurate native raster, exclusive-fullscreen presentation or Steam overlay integration. **P002-C01 is NOT owner-accepted and NOT independently verified.** After manual GUI qualification, target C-D01/C-D02 and metadata/icon provenance before asserting fidelity. Modern P002-C02 remains separately scoped.
