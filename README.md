# CS Virtual Achievements (RASC)

An independent Windows companion for displaying and browsing virtual Counter-Strike: Global Offensive achievements without representing them as Steam-published achievements.

**Project status:** P000 research OPEN; P001-C01 native Windows foundation development checkpoint published and build-verified, user-runtime-qualified; independent QA and owner acceptance pending. P002-C01-C04 Classic FPS/DPI completed its 105/105 development CI gate and owner-approved visual design remains preserved. P003-C01 Modern normal desktop renderer and profile selector passed Windows CI 117/117 at implementation SHA 2acebb8c43df58841fbf309fcaf8272f7d3fecf0; Modern runtime/independent QA and subsystem owner acceptance remain OPEN.

## Scope and provenance

- Initial integration target: standalone CS:GO (Steam AppID `4465480`) using the GT-610 `csgo-gc` local achievements provider.
- Initially read-only: observe `csgo_gc/achievements.txt` and derive only progress justified by documented game statistics and achievement criteria.
- Notification reference profiles: **Steam Classic (May 2023 VGUI)** and **modern Steam**. Their original rendering mechanisms and behavior must be documented separately.
- Preserve distinctions among CONFIRMED ORIGINAL, RECONSTRUCTED, VERIFIED COMPONENT, INDEPENDENTLY VERIFIED, ENGINEERING, UNVERIFIED RUNTIME and DISCREPANCY.
- Research assets and proprietary Steam binaries remain in their original permitted locations; reference them by manifest, original URL and hash rather than committing redistributable copies.
- External/customized client resources are not automatically evidence of unmodified Valve behavior.

## Organization

- **Headquarters:** scope, evidence registers, review and proposed acceptance.
- **Work 1:** original-behavior research, independent fixtures and QA.
- **Work 2:** production implementation and development tests *only when separately authorized*.
- **Owner:** decides acceptance. A research checkpoint is not subsystem acceptance.

## Campaigns

- **P000:** Steam notification visual and runtime reference investigation. **OPEN**.
  - **P000-C01:** consolidate, independently verify, and publish classic/modern notification reference contracts, their provenance and discrepancies.
- **P001:** native Windows foundation. **AUTHORIZED / DEVELOPMENT CHECKPOINT** — [Issue #2](https://github.com/thgillwtnorizoh/rasc/issues/2); development tests and Windows build passed, independent QA and owner acceptance pending.
- **P002:** Classic first renderer **P002-C01**, slide correction **C01**, font calibration **C02**, display-paced/seam correction **C03**, and display preferences/DPI scaling **C04** published, [Issue #3](https://github.com/thgillwtnorizoh/rasc/issues/3). Its exact original animation/font raster remains **UNVERIFIED**, despite passing CI; modern normal desktop profile implemented as a P003 development checkpoint; runtime and exact original parity unverified.

Research and checkpoint records belong under `research/P000/` when verified. Maintain recovery notes in-repository. Do not run old Steam client binaries or interact with the installed Steam client as part of static reference analysis.

## Related references

- [Original May 2023 SteamTracking manifest](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientManifest/steam_client_win32), version `1685488080`.
- [Original classic notification layout](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/AchievementNotification.res).
- [Original classic styles](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/styles/steam.styles).
- [GT-610 community Game Coordinator](https://github.com/GT-610/csgo-gc).

All original-behavior claims must retain exact authority and confidence; do not substitute a visual preview for runtime verification.

## First Windows development artifact

- [P001-C01 guide](docs/P001/README.md) — architecture, build/run instructions and supported profile.
- [P001-C01 checkpoint](docs/P001/checkpoints/P001-C01.md) — exact tested SHA, Windows CI artifact, testing and limitations.
- [Windows Actions artifact (self-contained x64)](https://github.com/thgillwtnorizoh/rasc/actions/runs/43906938776/artifacts/14246535435) — development build; owner observed live unlocks and tested shutdown.

The app does not modify your CS:GO files or grant Steam achievements. Select the GT-610 `csgo_gc/achievements.txt` path manually. First observation of existing awards establishes a silent baseline; subsequent observed changes appear in the event history. A corrected Classic-style desktop popup has been implemented for new events, but no original Steam overlay equivalence or owner runtime acceptance is claimed; the Modern normal desktop development candidate is available via P003-C01 (no original Steam overlay equivalence).

**Latest P001 runtime note:** Owner confirmed six live unlocks, then a restart without duplicate event rows. Owner reported an earlier ~5-second exit; the revised build was user-qualified with a prompt visible close and 24.3ms internally while actively monitoring. The cause of the old delay remains unknown. P001-D01 is RESOLVED for owner-observed runtime (not independent QA). See [P001-C01-C02 owner evidence](docs/P001/checkpoints/P001-C01-C02.md).

## P002 — Classic desktop notification (development, owner-target slide correction)

- [**Latest corrected Windows x64 artifact**](https://github.com/thgillwtnorizoh/rasc/actions/runs/44050154729/artifacts/14313207463) — self-contained WPF app, **48/48 automated tests**, native build and x64 publish passing. **Preview Unlock** and **Preview Progress** are source-independent and never write GC stats or SQLite history.
- [Current P002 guide](docs/P002/README.md); [P002-C01 original development checkpoint](docs/P002/checkpoints/P002-C01.md); [P002-C01-C01 slide checkpoint](docs/P002/checkpoints/P002-C01-C01.md); [P002-C01-C02 font checkpoint](docs/P002/checkpoints/P002-C01-C02.md); [P002-C01-C03 render/seam checkpoint](docs/P002/checkpoints/P002-C01-C03.md); [P002-C01-C04 FPS/DPI checkpoint](docs/P002/checkpoints/P002-C01-C04.md); [owner-accepted visual baseline](docs/P002/acceptance/visual-design-baseline.md); [owner image calibration](docs/P002/fixtures/owner-classic-screenshot-calibration.md).
- Classic source reference: `research/P000/classic/reference.md`. The corrected popup uses one clipped stack host, intended flush placement, **no alpha fade or visible strokes**, and user-target motion; **owner-target Helvetica Regular at 11.5 WPF DIPs with Arial fallback, not native-proven**; the source-vintage font conflict, original easing/overflow, exact raster and in-game overlay remain unverified.
- Existing achievements restored in the history list do **not** trigger new toasts; only newly observed unlock events may do so.

P000/P001/P002/P003 subsystems remain **OPEN** without owner acceptance or independent Work 1 QA. Do not report a WPF build as human GUI qualification.

## P002-C01-C04 — Adjustable animation FPS and DPI-aware sizing

**Current development build:** [Windows x64 artifact 14313207463](https://github.com/thgillwtnorizoh/rasc/actions/runs/44050154729/artifacts/14313207463) / [CI 44050154729](https://github.com/thgillwtnorizoh/rasc/actions/runs/44050154729), **105/105 automated tests** and self-contained Windows x64 publication passed. Exact code SHA `66c3f4559b59d6db763b4b72a0969c42926627b6`. [Full checkpoint](docs/P002/checkpoints/P002-C01-C04.md).

**Settings:** Animation FPS is **Display-synced** (default) or any integer from **15 through 240**, including presets 30/60/90/120/144/165/240. FPS is an *average visual update target* sampled from WPF's real render callbacks, not physical monitor FPS or a change to 450ms slide/6000ms hold. Popup Size offers **Auto** (default; resolution-aware 1080p baseline with normal Windows DPI scaling) or 100–250% manual choices. Preferences persist at `%LOCALAPPDATA%\RASC\appearance.json`, and can be changed while toasts are visible. `app.manifest` requests PerMonitorV2 DPI awareness. Primary screen/work-area presentation only; additional monitors and actual pixel fidelity need human qualification. The accepted C02 card XAML/artwork/typography are unchanged.

The owner reported C03 animation **123.3 WPF Rendering callbacks/sec**, longest gap **36.90ms** across seven segments; not actual presented FPS. P002 Classic is a **development-complete candidate for these two requested features**, subject to the owner's native Windows tests and independent QA. No new original Steam fidelity or subsystem acceptance is claimed.


## P003-C01 — Modern normal-desktop notification implementation

- [P003 guide](docs/P003/README.md) and [exact-SHA checkpoint](docs/P003/checkpoints/P003-C01.md).
- [Development PR #4](https://github.com/thgillwtnorizoh/rasc/pull/4).
- Verified test implementation commit: 2acebb8c43df58841fbf309fcaf8272f7d3fecf0, [Windows Actions run 44058257118](https://github.com/thgillwtnorizoh/rasc/actions/runs/44058257118): **117/117 tests**, **0 WPF build warnings/errors**, self-contained Windows x64 artifact uploaded.
- Select **Classic / Modern / Off**; stored separately from existing Classic appearance preferences. Normal desktop Modern profile uses per-toast nonactivating WPF windows, a source-informed 283px width, provisionally 70px height, 300ms entrance/exit, 5s hold, 3 active slots plus FIFO. Classic source unchanged.
- Modern visuals are a documented placeholder reconstruction, not Steam assets; in-game overlay, sound, special-mode queues, original CSS/font metrics, multi-monitor and owner Windows runtime qualification are **NOT VERIFIED**.
