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
