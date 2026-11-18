# P003-C03-C04 — Owner's accepted glow reference and remaining Modern boundaries

**Date:** 2026-11-18. **Owner screenshot:** newly uploaded `image(9).png`, 281×206 RGBA, SHA-256 `721b573d7587beef10c4aaa1d080f5bfadc4bd527fbc1d9c4da80e7367c83ff0`. The screenshot is not reproduced in the source repository.

## Owner-reported requirements

- Modern still exhibits tiny internal horizontal lines at **every** adjacent-card boundary even AFTER C03-C03 unified the native HWND. Classic never shows this defect.
- The reference for the rare gold glow is Dataminer's **Steam Colors / LegendaryAchievement** demo at https://codepen.io/Dataminer/pen/pooWzpo — user explicitly approved implementing this look.
- Do not change accepted Classic visuals, medal glyph, wave timing, profile switching, fullscreen-only anchor, GT-610 reader or unlock history.

## Screenshot evidence

The visible interrupted horizontal bands are near screenshot rows 67–68 and 137–138 (70-pixel separation), running nearly across the full 281-pixel screenshot width. The original screenshot is RGBA but the line is **not a wholly transparent region**: at x=0, rows 66–70 are approximately RGB (17,22,30), (26,29,37), (22,27,35), (22,27,35), (24,27,35). The short bright jump repeats at the next card boundary. Claiming the compositor made fully transparent holes is therefore not supported. A per-card alpha/gradient/reset seam is a stronger explanation.

## Traced source defect

C03-C03 ModernToastWindow already contained all cards in **one HWND**, but each ModernToastCard still painted its OWN 283×70 radial background and independently clipped another copy of the 170-unit Steam watermark. The WPF surface was shared, but its backgrounds/decorations were still per-card. This is a confirmed source structure; whether its rasterization alone explains every screenshot pixel remains a testable inference.

**C03-C04 correction:** leave UserControls as transparent content layers; move ONE radial background and ONE Steam watermark onto a dynamically sized continuous decoration canvas in the shared window. The occupied union exactly follows the moving wave (1/2/3 cards); no fixed 1-2px overlap, separator repainting, or unrelated changes to scheduler math. Add actual WPF bitmap sampling at internal boundaries, including fractional DPI.

## Approved CodePen gold reference

Source (reviewed 2026-11-18): https://codepen.io/Dataminer/pen/pooWzpo, **LegendaryAchievement** section.

- `AchievementIconWrapper`: 48×48 CSS px.
- Root glow, inner mask container and gradient layer each inset -10px, yielding 68/88/108-unit nested canvases.
- `AchievementIconGlowContainer`: reverse linear 18-second rotation.
- `AchievementIconGlow`: forward linear 6-second rotation.
- Gradient is **RADIAL** (not conic), with stops at 0/6/10/26/35/40/60/82/100 percent. Source colors: faint `rgba(255,201,109,.178)`, transparent at 6%, bright `#ffb84e` at 26/40/82%.
- Two shadow layers on static 48px icon: `0 0 2px 1px rgba(255,184,78,.6)` and `0 0 16px 1px rgba(255,217,78,.4)`.
- Two embedded 64×64 PNG alpha masks are part of the CodePen. Their ownership and redistribution terms are not established. **Do not bundle or copy them.** Instead recreate analogous square/star masks procedurally with source-derived size, alpha-layout intent and explicit visual limitations.
- This pen's **radial** recipe differs from the Steam Community stylesheet's separately confirmed repeating **conic** recipe. The user chose the Pen as the desired visual target; do not silently interchange them or claim native Steam code reuse.

## Evidence confidence and limitations

- OWNER-OBSERVED: new screenshot line and chosen CodePen appearance.
- CONFIRMED IN C03-C03 PRODUCTION SOURCE: per-card backgrounds and clipped watermark despite one HWND.
- CONFIRMED IN REFERENCE CODE: CSS dimension, radial stops, 6/18s animation and dual box-shadow.
- ENGINEERING: continuous-wave union decoration and procedural mask approximation, WPF drop-shadow/gradient raster matching.
- INDEPENDENTLY VERIFIED: NOT RUN until Work 1 checks new exact source.
- UNVERIFIED RUNTIME: user 1/2/3-card screenshots and actual CodePen visual comparison on Windows.
- System boundary: no Steam/CS:GO hooks, injection, patching, downloaded original mask redistribution or invented rarity metadata.
