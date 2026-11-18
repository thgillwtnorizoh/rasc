# CS Virtual Achievements (RASC)

An independent Windows companion for displaying and browsing virtual Counter-Strike: Global Offensive achievements without representing them as Steam-published achievements.

**Project status:** P000 research OPEN; P001-C01 native Windows foundation development checkpoint published and build-verified, user-runtime-qualified; independent QA and owner acceptance pending. P002-C01-C04 Classic FPS/DPI completed its 105/105 development CI gate and owner-approved visual design remains preserved. P003-C01 passed 117/117 tests. P003-C02 corrects Modern waves, watermark and rare/progress variants in draft PR #7; the final C02 source passed Windows CI 124/124 (0 build warnings/errors) and self-contained x64 publish at SHA 391582703a8b1cc8f415aa8eca293aeac0187309. P003-C03 foreground-app placement and Modern icon/medal correction passed Windows CI 132/132 at SHA 43e598ad97a25fb4230349e852016f976f1e725b. Runtime/independent QA and owner acceptance remain OPEN.

## Preserved achievement notification renderers

**Versioned cross-project visual reference:** [Steam-style renderer archive v1](docs/rendering-reference/v1/README.md) preserves **Classic** and **Modern** as distinct frozen design contracts (geometry, fonts, color, motion, stack, CodePen rare glow, reference fixtures, source hashes, rights and regression tests). The source lock is pinned to C07 `373d04101d03f51135f2f1b192758efddad12f00`. Owner accepted Classic card appearance and C07 rare-glow appearance; original Valve runtime parity, independent Work 1 QA and PNG redistribution permissions are **not** implied. [Documentation draft PR #30](https://github.com/thgillwtnorizoh/rasc/pull/30).

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


## P003-C02 — Owner-target Modern visual/wave correction

- [Work 2 issue #6](https://github.com/thgillwtnorizoh/rasc/issues/6), [draft PR #7](https://github.com/thgillwtnorizoh/rasc/pull/7), [C02 checkpoint](docs/P003/checkpoints/P003-C02.md), [four owner screenshot evidence records](research/P000/modern/owner-visual-c02.md).
- Up to three notifications form a **single wave**; cards fan from one invisible origin beneath the taskbar and return to it on exit. Later arrivals wait. 1/2/3 stack decorations share a dynamically clipped faint vector watermark. Modern cards have no outer border/status headline. There are separate name+description, rare-preview gold icon, and numeric-progress+bar views.
- Added independent preview wave fixture with 1, 2, 3 or 6 events. The GT-610 provider cannot supply rarity/progress; previews never grant awards or alter history.
- This is a constrained approximate WPF desktop presentation, NOT original stock Steam, not an in-game overlay, and requires owner runtime acceptance and Work 1 independent QA.


## P003-C03 — Foreground-app placement, rare icon and medal correction

- [Work 2 issue #9](https://github.com/thgillwtnorizoh/rasc/issues/9), [draft PR #10](https://github.com/thgillwtnorizoh/rasc/pull/10) based on C02, [C03 checkpoint](docs/P003/checkpoints/P003-C03.md), [owner report](research/P000/modern/owner-visual-c03.md), [Work 1 QA issue #11](https://github.com/thgillwtnorizoh/rasc/issues/11).
- Both Classic and Modern now target the foreground application's visible bottom-right and selected monitor. Windowed apps are confined to usable work area; fullscreen/borderless spans actual monitor. Auto popup size follows foreground app height, while manual percentages still work. Classic card XAML and scheduler remain unchanged.
- Modern removes ordinary blue icon and solid golden rare frames; rare shine remains an approximate animated halo, not the proven original CSS algorithm. Progress cards include the tiny medal before numeric progress. Faint watermark geometry is improved but remains an original-asset-free approximation.
- [Exact-source Windows CI 44118029224](https://github.com/thgillwtnorizoh/rasc/actions/runs/44118029224): 132/132 tests, zero WPF build warnings/errors, self-contained x64 artifact 14343852367.
- **Borderless/fullscreen placement is not equivalent to Steam's injected overlay:** true exclusive fullscreen may hide an unrelated WPF HWND. Actual visual/monitor/DPI/focus QA and owner acceptance are NOT VERIFIED.


## P003-C03-C01 — Fullscreen-only anchoring and visual fidelity correction

[Owner correction](research/P000/modern/owner-c03-c01-correction.md) • [Work 2 issue #12](https://github.com/thgillwtnorizoh/rasc/issues/12) • [draft PR #13](https://github.com/thgillwtnorizoh/rasc/pull/13) • [Work 1 independent QA #14](https://github.com/thgillwtnorizoh/rasc/issues/14)

- Normal desktop/windowed/maximized applications: P002 Classic and C02 Modern taskbar work-area placement/Auto size RESTORED. These popups no longer anchor or autoscale to the foreground app's client rectangle.
- Only a focused fullscreen/borderless monitor-spanning window overrides to its monitor's physical full-screen bottom-right. Borderless runtime qualification pending, WPF visibility over exclusive fullscreen not guaranteed.
- Modern watermark uses the recognizably correct Steam symbol geometry from Simple Icons CC0 (Steam remains a trademark), keeping C02 1/2/3 wave positions. Rare icon uses a rotating conic gold halo behind a stationary mask, not C03 orbiting dots.
- Exact-source [Windows CI run 44123158624](https://github.com/thgillwtnorizoh/rasc/actions/runs/44123158624): 133/133 PASS, 0 warnings/errors, self-contained Windows x64 artifact 14345807588 at 8d3132e54a7086db6a72001629309d305c63ff57. Independent QA and owner Windows acceptance NOT VERIFIED. P003 OPEN.


## P003-C03-C02 — Steam progress glyph and cross-profile crash correction

- [Work 2 issue #15](https://github.com/thgillwtnorizoh/rasc/issues/15), [draft PR #16](https://github.com/thgillwtnorizoh/rasc/pull/16), [engineering checkpoint](docs/P003/checkpoints/P003-C03-C02.md), [source research](research/P000/modern/progress-svg-and-c03-c02.md), [independent Work 1 QA #17](https://github.com/thgillwtnorizoh/rasc/issues/17).
- Owner reported Modern -> Classic preview animation/sequence failure and post-render crash. **Exact crash root cause NOT VERIFIED.** A cross-style presentation gate now prevents simultaneous Classic/Modern HWND presentation while preserving same-style event batches and existing popup lifetimes. Deferred closed-window WPF callbacks are guarded. Unhandled exceptions write a local stack trace at %LOCALAPPDATA%/RASC/last-unhandled-error.txt without swallowing the fault.
- The tiny progress medal is source-informed from the Steam UI Achievement inline SVG; progress fill uses (current-min)/(max-min), while numerical label remains (current/max). A source reference is not an original client runtime comparison. GT-610 source cannot provide live progress or rarity; preview remains display-only.
- Rare gold WPF effect has a reverse 18s star-shaped mask, forward 6s conic light, fixed square outer mask and diffuse gold shadow. The original Valve bitmap mask files and CSS overlay blend are not redistributed or claimed equivalent.
- **Exact Work 2 tested SHA:** 50fdf70fa7851ce07cf49b760d5b7c9ce940d44d. [Windows CI 44137221353](https://github.com/thgillwtnorizoh/rasc/actions/runs/44137221353) **PASS:** 138/138 core tests; 4/4 timed real-WPF-HWND smoke scenarios (Classic-only, idle Modern -> Classic, active Modern wave -> Classic, Modern progress); zero WPF compiler warnings/errors; self-contained Windows x64 [artifact 14352028743](https://github.com/thgillwtnorizoh/rasc/actions/runs/44137221353/artifacts/14352028743).
- Historical intermediate [CI 44136754195](https://github.com/thgillwtnorizoh/rasc/actions/runs/44136754195) FAILED solely at a mistaken developer smoke assertion demanding a Modern HWND from the intentionally idle Modern selection; corrected without deleting the test. This is a recorded TEST HARNESS DISCREPANCY, not confirmed production failure.
- Owner real Windows/CS:GO graphical acceptance, original gold blend fidelity and Work 1 independent QA: **NOT VERIFIED**. The subsystem remains **OPEN**.


## P003-C03-C03 — Modern two-pixel stack-seam correction

- **Owner observed** a two-pixel horizontal interruption between Modern cards (282×134 screenshot, SHA-256 e0cba11f7eec35eefdfd0f2b0604d59f65b2e712bfa13c5c53c35b003be9ded6). [Report/evidence](research/P000/modern/owner-c03-c03-seam-report.md). The DWM cause is not independently proven.
- [Work 2 issue #18](https://github.com/thgillwtnorizoh/rasc/issues/18) / [draft PR #19](https://github.com/thgillwtnorizoh/rasc/pull/19) / [P003-C03-C03 checkpoint](docs/P003/checkpoints/P003-C03-C03.md) / [Work 1 independent QA #20](https://github.com/thgillwtnorizoh/rasc/issues/20).
- Modern cards now render as child controls of **one shared transparent HWND per wave**, instead of separate native windows; the one/two/three-card stack has contiguous 70-unit slots in one 283×210 logical viewport, using the same background, logo, progress medal and rare-shine code. Accepted Classic styling/animation, Modern 300/5s/300 wave schedule, profile switch gate and fullscreen-only anchor remain intact.
- **Exact source development CI**: commit e89e4209a94b3b393df06be59e71b3a8057f9180, [Windows run 44224357909](https://github.com/thgillwtnorizoh/rasc/actions/runs/44224357909) — **148/148 tests**, **5/5 real WPF HWND smoke scenarios**, zero WPF warnings/errors, self-contained Windows x64 [artifact 14391788398](https://github.com/thgillwtnorizoh/rasc/actions/runs/44224357909/artifacts/14391788398).
- **Rare achievement glow remains NOT FINISHED / NOT OWNER-ACCEPTED**: WPF approximation lacks the exact original Steam mask bitmaps and original overlay blend. No claim of original native visual parity. Owner screenshot verification for seamless 1/2/3 cards and rare glow, Work 1 independent QA and CS:GO fullscreen runtime qualification remain **NOT VERIFIED**. P003 OPEN.


## P003-C03-C04 — Continuous Modern wave and CodePen gold (development checkpoint)

- **Owner report:** even after the shared-HWND C03-C03 patch, small light horizontal seams remained between all Modern cards. The new 281×206 screenshot's two bands are at approximately y=67–68 and y=137–138. [C04 owner evidence](research/P000/modern/owner-c03-c04-stack-and-glow.md). The owner explicitly authorized implementation of the [CodePen's Steam Colors / LegendaryAchievement example](https://codepen.io/Dataminer/pen/pooWzpo).
- [Work 2 issue #21](https://github.com/thgillwtnorizoh/rasc/issues/21), [draft PR #22](https://github.com/thgillwtnorizoh/rasc/pull/22), [C04 checkpoint](docs/P003/checkpoints/P003-C03-C04.md), [Work 1 independent QA #23](https://github.com/thgillwtnorizoh/rasc/issues/23).
- The Modern renderer now has one **continuously painted radial backdrop and one shared Steam watermark** across each occupied 1/2/3-card wave, with separately transparent foreground content controls. The decorations follow the moving wave union and preserve the hidden below-screen origin; no arbitrary pixel overlaps. Classic appearance, Modern wave timing/profile-switch and fullscreen-only anchors unchanged.
- Rare gold uses the **CodePen radial** nine-stop colors (not the separate SteamCommunity conic variant), source -10px nested 48/68/88/108 sizing, 18s reverse star-mask/6s forward light, dual gold shadows. The Pen's two embedded alpha-mask PNGs were NOT redistributed: WPF independently reconstructs their shape, so exact visual fidelity is NOT VERIFIED.
- **Exact tested source:** \`1423c7f5142f738d22a4a4f5305f3e156e7c4151\`; [Windows CI 44230546521](https://github.com/thgillwtnorizoh/rasc/actions/runs/44230546521) **PASS**: 162/162 core tests, 5/5 real Windows WPF smoke scenarios including 100/125/150% raster seam checks, zero WPF build warnings/errors and self-contained win-x64 [artifact 14394952402](https://github.com/thgillwtnorizoh/rasc/actions/runs/44230546521/artifacts/14394952402).
- Initial [CI 44230291652](https://github.com/thgillwtnorizoh/rasc/actions/runs/44230291652) FAILED three NEW test expectations (159 passed) that wrongly required a moving card's union to end at the screen edge during animation; cards intentionally extend below the viewport and are clipped at the viewport. Test corrected without changing the scheduler or losing fixtures.
- **Independent QA, owner actual Windows screenshot approval and original/native Valve or CodePen alpha-mask pixel parity remain OPEN.** P003 not owner accepted.


## P003-C03-C05 — Actual PNG masks and visibly animated rare glow

- **Owner evidence:** C04 seamless Modern 1/2/3 stack approved as gapless, but rare gold effect reported invisible. Owner then supplied 64×64 square/star Base64 PNG data and authorized their use. [Research and source notice](research/P000/modern/owner-c03-c05-masks.md) / [asset rights notice](src/Rasc.Desktop/Assets/SteamRareMasks/NOTICE.md).
- [Work 2 issue #24](https://github.com/thgillwtnorizoh/rasc/issues/24), [draft PR #25](https://github.com/thgillwtnorizoh/rasc/pull/25), [C05 checkpoint](docs/P003/checkpoints/P003-C03-C05.md), [independent Work 1 QA #26](https://github.com/thgillwtnorizoh/rasc/issues/26).
- Source masks: square Git blob e81be0ce97e8efbbe663567783f5c7d858da064f (5924B); star 211aca32f4dd2d54515286627d161785f86de3a1 (7327B). The owner-provided mask payloads match the approved CodePen reference and public mirror. WPF now uses exact PNG alpha in 68px outer root and 88px rotating inner mask, preserving 108px radial gold, 6s light/18s reverse star and double shadows.
- [Exact-source Windows CI 44236912219](https://github.com/thgillwtnorizoh/rasc/actions/runs/44236912219) at commit 1e5e17c4c69038d0112daf69d38b8c65b65f6d61: **162/162 tests**, **6/6 real-WPF smoke cases**, zero WPF warnings/errors, Windows x64 self-contained [artifact 14397966899](https://github.com/thgillwtnorizoh/rasc/actions/runs/44236912219/artifacts/14397966899). New WPF raster test proves gold **outside** fixed icon (2060 then 1958 gold-tinted pixels in two captures) and moving light; no image-only or static code assumptions.
- Previous failed intermediate C05 CI runs are preserved in the checkpoint, including a false negative from an incorrectly cropped RenderTargetBitmap. Classic, gapless Modern, progress medal, cross-profile lifecycles, fullscreen-only anchoring and read-only achievements unchanged.
- **Owner CodePen visual acceptance NOT VERIFIED.** Original Steam desktop-client asset/version equivalence and rights to redistribute mask PNGs are NOT VERIFIED. This is a draft experimental owner-test branch/artifact; the owner must clear asset rights or choose an alternate local-import path before merging or publicly releasing. Independent QA NOT RUN. **P003 OPEN.**
