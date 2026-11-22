# RASC P006-C07 — Classic fullscreen slide watchdog and clipping regression

Project / Campaign / Actor / Assignment: CS:GO Achievements Resurrection / P006-C07 / Work 2 / Classic overlay partially outside screen in user-observed screenshot.
Starting SHA: `87b5ff2cee94f44a0200b48e80ad960c96665407` (P006-C06 merged into P006-C05 working branch, PR #49).
Integration branch: `fix/p006-c07-classic-fullscreen-animation-watchdog`.
Published SHA / exact tested SHA: inspect PR #52 head and Actions; NOT YET VERIFIED at publication.
Status: ENGINEERING IMPLEMENTED; FULL CI and original fullscreen runtime NOT RUN at creation.

## Source and symptom
Owner's November 22, 2026 uploaded CS:GO 1920x1080 screenshot `image(20261122-134237).png` shows the Classic Achievement Unlocked! notification clipped by the lower-right screen edge: only the upper part of the card is visible, while chat lists newly earned awards. It is consistent with an incomplete 450ms entrance, but a single static frame does not prove whether it was caught during normal animation or remained frozen. Source examination found `ClassicToastHost` moves the card through `CompositionTarget.Rendering` and has a 250ms anchor timer that **only repositions the host**, not the animation. On fullscreen WPF rendering starvation, the card's stored Canvas.Y can therefore lag while the Stopwatch continues.

## Bounded engineering change
- Preserve source-derived 240×94 Classic card, 282 viewport, 450ms slide/6s hold/450ms slide-out, no alpha, icons, text, origin and clipping rules.
- Add a 16ms DispatcherTimer fallback **only during Classic transitions**; if no composition callback has occurred for >=40ms, advance the same monotonic scheduler and existing FPS gate. Healthy WPF rendering remains primary, so fallback does not cap healthy display or start duplicate awards.
- Disable the timer during holds and on host disposal. Preserve Modern renderer entirely.
- Add a real WPF smoke scenario `classic-render-stall` that suppresses composition callbacks intentionally, waits past entrance, asserts the card settles at Y=188 with full 94-unit clip and host bottom does not overrun desktop work area, and asserts final retirement. Run it in Windows Actions alongside existing Classic/Modern regression suite.
- Existing `classic-only` check solely validated HWND presence/lifetime; the new assertion discriminates the owner's partial-offscreen symptom and prevents a false-positive pass with a frozen animation.

## Tests and evidence
Windows CI: `.github/workflows/windows.yml` full core tests, all WPF smoke cases (including new Classic starvation), x64 publish/artifacts; exact workflow SHA and pass counts must be recorded.
Local physical legacy CS:GO fullscreen, windowed and alt-tab test: **NOT RUN**. Single screenshot cannot establish actual duration of clipping, so original-client confirmation remains necessary. WPF smoke cannot prove exclusive-fullscreen presentability or GPU compositor frame delivery.
Confidence: OWNER-OBSERVED SCREENSHOT; ENGINEERING (render starvation watchdog); UNVERIFIED RUNTIME (root-cause and full screen).
Exclusions: achievement ingestion/queue, GC stats reset, Steam overlay, Modern waves, visual typography and icon layout.
Discrepancies: screenshot may have caught a normal transitional frame; true exclusive fullscreen or overlay priority can still affect WPF drawing despite timer.
Coverage gained: rendering-starvation independent animation completion plus full clip and bottom-edge assertions.
Next bounded task / Actor: Work 2 evaluate CI, fix tests, merge only on green; Owner/Work 1 capture steady-state at ~1 second and verify real CS:GO fullscreen after deployment.
Subsystem: OPEN — owner acceptance pending.
