# P003-C03-C02 — Steam medal, minimum-relative progress and switch crash evidence

**Date:** 2026-11-18. **Owner evidence:** Modern to Classic transition followed by Classic preview leads to incorrect sequence/animation and crash after rendering; Classic from startup works. No original crash stack was supplied.

## Source-specific progress research

Third-party readable Steam UI snapshot: https://github.com/ricewind012/steam-ui-unobfuscated/tree/3c658f496c3bc713eabd1ebb57c91b4659b83591 (2026-06-20; original client's matching build NOT VERIFIED).

- In src/chunk~2dcc5aaf7/11374.js, achievement notification function rt reads min_progress(), current_progress() and max_progress(). Function it renders an l.Achievement React SVG icon immediately before the numeric (current/max) label, using the standard icon/title components.
- Function at calculates the filled percentage as (current - min) / (max - min) * 100 and clamps below 0. RASC clamps both ends for viewport safety (ENGINEERING). The GT-610 persistence file cannot supply live progress callbacks.
- In src/chunk~2dcc5aaf7/35488.js, Achievement is an INLINE SVG with 36x36 viewBox: a rosette-star with a hollow center and two ribbon tails. It is not a PNG, font glyph, or unrelated Steam Timeline steam_ribbon asset.
- Confidence: CONFIRMED IN PUBLIC STEAM UI SNAPSHOT. Version alignment with owner's screenshot remains NOT VERIFIED; WPF compatibility vector is not a bundled Steam client binary.

## Rare glow reference

SteamTracking steamcommunity.com/public/css/skin_1/profilev2.css has 18-second reverse rotation with achievement_star_mask.png, an outer achievement_square_mask.png, conic gold gradient and 6-second forward light rotation, overlay blend, and reduced-motion behavior. Owner reports that Community and client glow look alike. **Direct code reuse in desktop toasts NOT VERIFIED.** RASC uses newly constructed WPF star/square masks, interpolated conic gold stops and diffuse shadow; no Valve mask PNG redistributed.

## Profile transition discrepancy

- OWNER-REPORTED: Modern-to-Classic preview wrong sequence/animation, then crash; Classic-only control passes. No exception or dump provided.
- CONFIRMED IN C03-C01 SOURCE: MainWindow changed the selected profile only; Classic and Modern hosts independently owned timers, CompositionTarget.Rendering and WPF windows, with no cross-style admission barrier.
- ROOT CAUSE NOT PROVEN: overlapping windows/animators or callbacks after Close are plausible hypotheses, NOT confirmed.
- ENGINEERING C03-C02: new NotificationPresentationGate preserves same-style admission and FIFO, but holds different-style arrival until the outgoing host's real windows and scheduler are idle. Stale SourceInitialized/DPI callbacks check closed state. Uncaught exceptions now log to local last-unhandled-error.txt without suppressing failure.
- A new WPF smoke executable exercises Classic-only, Modern wave -> Classic, and Modern progress. This only proves those sequences when actually executed with visible HWNDs; it does not establish pixel-perfect rendering, owner acceptance or exclusive fullscreen equivalence.

## Required evidence

Preserve all Classic and Modern historical tests, no preview writes to GC/SQLite, run .NET core tests, WPF Release build, self-contained x64, actual GUI-smoke lifecycle scenarios, separate Work 1 independent QA and owner Windows check. Label unavailable runtime NOT VERIFIED.

**Subsystem P003: OPEN.**
