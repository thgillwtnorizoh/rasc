# P003-C01 — Modern Steam-style desktop achievement notifications

**Status:** VERIFIED DEVELOPMENT CHECKPOINT — exact-source Windows CI passed at 2acebb8c43df58841fbf309fcaf8272f7d3fecf0 (117/117 tests, 0 WPF compiler warnings/errors, self-contained x64 artifact). NOT independently verified, NOT runtime qualified, NOT owner-accepted. See [checkpoint](checkpoints/P003-C01.md).

**Starting main:** 425444dc7440163ec8b3f74ee0ad9c360d5896fd.
**Actor:** Work 2 implementation. **Campaign:** P003 — Modern desktop notifications. **Research authority:** [P000 modern reference](../../research/P000/modern/reference.md), [renderer-neutral contract](../../research/P000/comparison/renderer-contract.md).

## Implemented support profile

- Independent read-only WPF desktop notification presenter; events enter through the existing monitor callback. Never modifies the GC file, Steam, or SQLite event decisions.
- New Classic / Modern / Off selector, persisted separately in %LOCALAPPDATA%\RASC\notification-profile.txt. Defaults to Classic when absent or invalid. Switching affects NEW events; already-displayed toasts finish in their original selected renderer.
- Separate click-through, non-activating, topmost window per modern toast, placed in the primary monitor work area. Uses WPF Window.Left/Top moves as a functional adaptation of the supplied UI's per-window MoveTo control.
- Normal achievement window: width 283 logical units and a **provisional** achievement card height 70. 300ms entrance, default 5000ms steady interval, 300ms exit; up to three active windows, FIFO remainder. A monotonic scheduler separates timing from WPF frame callbacks. Classic's existing design/timings are unchanged.
- Reuses FPS and popup-size preferences for both styles; target FPS does not alter achievement hold duration. Display-only previews for unlock/progress never grant achievements.
- Placeholder icon/text and unlicensed-safe radial background. This is an approximation, not original artwork or pixel-exact Steam.

## Deliberately unsupported / not verified

- Reference DesktopToastContainer height 70 is not established for every achievement window at runtime. Exact text layout, type-specific sizing, display-alone rules, alternate one-toast mode, server notification delay, user sound settings and audible sound asset routing are not implemented.
- Actual stock-client equivalence, behavior under GameOverlay/fullscreen, per-monitor DPI, multi-monitor, hover, frame pacing and device-pixel position require original evidence and owner-operated Windows testing.
- Queue/order and bottom-right placement where reference does not prove exact runtime behavior are ENGINEERING decisions, not CONFIRMED ORIGINAL.
- Never claim that the absence of sound means the original notification is silent; registry sound depends on client settings.

## Reproduction and acceptance gates

From the tested source commit, run:

- dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release
- dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental
- dotnet publish src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/Rasc-Windows-x64

Owner runtime checklist:
1. Start monitoring GT-610 achievements.txt and verify that existing awards baseline silently.
2. Select Modern, use Preview Unlock and Preview Progress. Confirm they do not touch GC/SQLite.
3. Generate at least six local previews rapidly: max three active, queued remainder, 300ms movement and ~5000ms hold, safe taskbar anchoring.
4. Switch to Classic and confirm that accepted Classic styling/animation remains unchanged, then Off and confirm newly received events make no popups.
5. Change FPS and popup size while Modern is visible; check windows remain bounded and no timer resets occur.
6. Observe a genuine new unlock; verify exactly one notification, correct history row, and no duplicate after restart.
7. Check behavior under scaling, monitor changes, normal desktop and fullscreen/borderless gameplay; report visible failures.

**Stop condition:** only report VERIFIED DEVELOPMENT CHECKPOINT after exact-source automated CI/build success. Human acceptance and Work 1 independent QA are separate gates. Preserve the full P002 suite.


## C02 superseding owner-target behavior (separate from historical C01 record)

**P003-C02** replaces C01's provisional independent-slot admission and generic notification contents with owner-observed wave semantics, three distinct card variants, and a stack-aware faint watermark. See [C02 checkpoint](checkpoints/P003-C02.md) and [owner screenshot evidence](../../research/P000/modern/owner-visual-c02.md). C01's 117/117 test result remains historical, not C02 runtime visual proof.

Use the **Modern wave fixture** (1, 2, 3, 6) for reproducible stack/queue preview. **Preview Rare** and **Preview Progress** are display-only. Actual rarity and progress remain unknown from GT-610. Selected style affects only future arrivals. Owner and independent QA remain required.


### Latest C02 exact-source development gate

**VERIFIED DEVELOPMENT CHECKPOINT:** [Windows Actions CI 44111688453](https://github.com/thgillwtnorizoh/rasc/actions/runs/44111688453) at source commit **391582703a8b1cc8f415aa8eca293aeac0187309** — 124 tests passed, 0 failed/skipped, WPF Release compiled without warnings/errors, self-contained Windows x64 artifact [14340258031](https://github.com/thgillwtnorizoh/rasc/actions/runs/44111688453/artifacts/14340258031). This includes the C02 one-click 1/2/3/6 wave fixtures.

**Independent QA:** [Work 1 issue #8](https://github.com/thgillwtnorizoh/rasc/issues/8), NOT RUN. **Owner actual Windows visual/gameplay qualification:** NOT RUN. P003 remains OPEN.


## C03 — Active foreground application and visual corrections

[Development PR #10](https://github.com/thgillwtnorizoh/rasc/pull/10) / [C03 checkpoint](checkpoints/P003-C03.md) supersede C02's primary-work-area-only popup position. Both profiles use read-only foreground Win32 visible-window/monitor geometry and native pixel placement, reanchoring during holds. Fullscreen-sized foreground windows use the entire display edge; ordinary windowed apps use visible bounds clipped to work area. Automatic scale is tied to foreground app height.

Modern changes: correct C02's implausible manually sketched logo with improved mechanical geometry (still not original asset-equivalent), remove blue icon frame, animate diffuse rare gold instead of solid yellow frame, add miniature progress medal left of (current/max). Existing 1/2/3 wave rules, visual preview fixtures and read-only history are preserved.

[Windows CI 44118029224](https://github.com/thgillwtnorizoh/rasc/actions/runs/44118029224) passed 132/132 and native x64 publish at source 43e598ad97a25fb4230349e852016f976f1e725b. [QA assignment #11](https://github.com/thgillwtnorizoh/rasc/issues/11). Physical visibility over **true exclusive fullscreen** remains unsupported/unverified for WPF; borderless fullscreen and mixed-DPI monitors need owner runtime checks.


## C03-C01 — Correction superseding C03's mistaken any-window anchoring

C03's normal foreground application positioning was a CONFIRMED OWNER REGRESSION, not an approved feature. [PR #13](https://github.com/thgillwtnorizoh/rasc/pull/13) restores P002/C02 desktop work-area position and size for BOTH profiles in ordinary desktop/windowed/maximized cases. ONLY a focused monitor-covering fullscreen or borderless app moves the anchor to its physical monitor bottom-right. Auto size never follows an ordinary app.

Modern symbol is now the actual recognizable Steam mark geometry (Simple Icons CC0 vector, Valve trademark); its 1/2/3 wave position is unchanged. Rare glow is continuous rotating conic gold with a stationary square halo mask, rather than five orbiting points. Original desktop-toast CSS reuse and screenshot fidelity remain unverified.

[Checkpoint](checkpoints/P003-C03-C01.md), [owner evidence](../../research/P000/modern/owner-c03-c01-correction.md), [Work 1 QA #14](https://github.com/thgillwtnorizoh/rasc/issues/14). [CI 44123158624](https://github.com/thgillwtnorizoh/rasc/actions/runs/44123158624): 133/133, 0 WPF warnings/errors, Windows x64 publish PASS at exact source 8d3132e54a7086db6a72001629309d305c63ff57.


## C03-C02 — Serialized renderer handoff and source-derived progress

[Issue #15](https://github.com/thgillwtnorizoh/rasc/issues/15) / [draft PR #16](https://github.com/thgillwtnorizoh/rasc/pull/16) / [checkpoint](checkpoints/P003-C03-C02.md) / [reference record](../../research/P000/modern/progress-svg-and-c03-c02.md). Earlier C03-C01 desktop/fullscreen-only positioning is preserved.

Changing Classic/Modern affects **new** notifications; a currently showing old-style notification finishes before the new style gets its own HWND. Same-style arrivals continue into that style's scheduler immediately. Distinct styles never have on-screen HWNDs concurrently. The effect is not guaranteed original Steam behavior; it is an explicit RASC reliability policy prompted by an owner-observed crash.

The progress medal uses an offline WPF reconstruction of the Steam UI's Achievement 36x36 SVG icon; progress fraction is minimum-relative. Rare light uses source-inspired masks/6s/18s animation but not the original proprietary PNGs or overlay compositor.

[Exact-source Windows CI 44137221353](https://github.com/thgillwtnorizoh/rasc/actions/runs/44137221353): 138/138 core PASS, 4/4 WPF HWND smoke PASS, WPF Release 0 warnings/errors, self-contained x64 artifact 14352028743 PASS at source SHA 50fdf70fa7851ce07cf49b760d5b7c9ce940d44d. [Independent QA #17](https://github.com/thgillwtnorizoh/rasc/issues/17) and owner real Windows validation remain NOT RUN.


## C03-C03 — One composited Modern stack, no inter-card native HWND seams

The owner supplied a two-card screenshot with a two-pixel horizontal gray strip at the card boundary, and reported a corresponding defect between lower cards in 3-card stacks. The previous scheduler already had exact contiguous 70-unit slots; the Modern host nonetheless created one transparent topmost WPF HWND per card, allowing compositor seams. [Visual evidence](../../research/P000/modern/owner-c03-c03-seam-report.md) / [issue #18](https://github.com/thgillwtnorizoh/rasc/issues/18).

The new architecture places all Modern cards in ONE nonactivating topmost window with a 283×210 design-unit viewport. Empty slots are fully transparent. Cards preserve their original backgrounds, watermark, rare and progress variants; all share the same bottom starting point, 70-unit adjacent settled positions and unchanged Modern scheduling. The accepted Classic renderer and the fullscreen-only anchor are unchanged.

[Draft PR #19](https://github.com/thgillwtnorizoh/rasc/pull/19) passed [Windows CI 44224357909](https://github.com/thgillwtnorizoh/rasc/actions/runs/44224357909) at exact tested source e89e4209a94b3b393df06be59e71b3a8057f9180: 148/148 core tests, five real WPF smoke scenarios, zero WPF compiler warnings/errors, win-x64 self-contained artifact 14391788398. [C03-C03 checkpoint](checkpoints/P003-C03-C03.md); [Work 1 independent QA #20](https://github.com/thgillwtnorizoh/rasc/issues/20).

**Important:** the rare gold animation itself is NOT finished; Valve's original PNG masks, blend mode and actual desktop-toast visual appearance remain unverified. Work 1 independent QA and owner Windows screenshot qualification are not yet complete. P003 OPEN.
