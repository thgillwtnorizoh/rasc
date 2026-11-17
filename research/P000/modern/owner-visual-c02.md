# P003-C02 — Owner-supplied Modern screenshot evidence and corrected contract

**Date:** 2026-11-17. **Source:** four images provided by project owner in ChatGPT conversation, plus owner's explicit behavior description. Screenshots are NOT redistributed in the repository. Observations are OWNER-REPORTED / RECONSTRUCTED from captures; do not promote to CONFIRMED IN ORIGINAL CLIENT without source/build provenance.

## Image identification and retained integrity

| Order | Resolution | SHA-256 | Observed case |
|---|---|---|---|
| 1 | 282×206 | c690362ca61f0e70c755995b7239bc525f3336bebbfb727dfd1634bd2851fdc0 | Three stacked ordinary achievements |
| 2 | 279×134 | f4eee0a0278d35182103f1e07243ea71b4d7fa08a70b1b0766d3ef311f49bfeb | Two stacked ordinary achievements |
| 3 | 282×66 | 78934a8a4fac228f37ec037a9063998d6cffc1a525f9544881da020d89cc1eb0 | Single ordinary achievement |
| 4 | 278×62 | 3874248d865fc59333766417c24d9641494515082254b9e6254dd8e4e04766c6 | Progress variant |

Images were uploaded to the active conversation, and may be referenced by the owner without redistributing copyrighted achievement icons or Steam source assets.

## Owner observations and requirements

1. One/two/three visible cards show a subtle Steam watermark which changes by stack count and per-card placement. The P000 reference separately reported top/middle/bottom logo/background positioning in the supplied Steam UI CSS, but original selectors and asset coordinates were not preserved.
2. Normal unlock variant has only achievement name and description (and icon); **no status headline** and **no visible outer card border**.
3. A rare achievement has a distinct golden shimmering frame **around the icon**. Determination of a LIVE achievement's rarity requires reliable source data and cannot be inferred from the local GT-610 timestamp. Provide a preview-only rare fixture.
4. Progress variant has an icon, name, numeric current/maximum and a horizontal fill bar, NOT a description-substitution string. GC provider does NOT persist progress callbacks; preview can exercise appearance without claiming a game event.
5. Cards in one wave travel from one shared point **below the taskbar** to separated stack destinations; exiting cards return to that origin. New events earned while a wave is visible wait until its exit.
6. Owner describes a deck of up to three. Whether a new wave starts after the **first** exiting card or only after the **whole** existing batch has exited is not discriminated by four static screenshots; P003-C02 uses whole-wave admission as an explicit ENGINEERING policy pending owner motion capture.

## Source cross-checks and unresolved claims

- Source-derived P000: DesktopToastContainer width 283px, height 70px, desktop MoveTo, 300ms in/out, default 5000ms registration and 3 normal active slots. Window height remains toast-type-dependent in JS; these screenshots have crops near 70px/card but are NOT exact DPI or physical measurements.
- Separate Steam Community CSS includes achievement_glow_container and achievement_glow_rotate animations, mask images and golden conic gradients; that does NOT establish use by desktop achievement toasts.
- The originally uploaded dogbones.zip UI bundle is not currently accessible to the present executor, so exact CSS selector/layout/reference asset extraction cannot be rerun.
- Changes to layout, font, colors, shape, watermark and rarity shimmer in P003-C02 are owner-target WPF RECONSTRUCTION approximations unless specifically source-derived.
- No original Steam stock-client or in-game/fullscreen runtime comparison has been performed.

## Engineering acceptance tests to preserve

- 1/2/3 waves share identical hidden origin and settle into distinct 70-unit slots.
- A burst of 6 yields two groups of at most 3 in FIFO order; new arrivals do not enter an occupied wave; large time jumps preserve exact scheduling.
- Card surfaces are clipped to primary desktop work area so moving below it never paints over taskbar; pixel-level owner runtime verification remains pending.
- Watermark geometry is continuous across card boundaries, changes according to visible wave size, and does not become an independent watermark on each card.
- Normal unlock, rare unlock, and progress have distinct presentation models; malformed progress is rejected; preview never touches GC or persistent history.
- Legacy Classic appearance, source reconciliation and historical tests are preserved.

**Subsystem: OPEN; source-specific Steam fidelity and owner visual acceptance pending.**
