# P003-C01 — Modern Steam-style desktop achievement notifications

**Status:** DEVELOPMENT CANDIDATE; NOT VERIFIED until an exact-SHA Windows Actions run passes. NOT independently verified, NOT runtime qualified, NOT owner-accepted.

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
