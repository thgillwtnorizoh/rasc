# P003-C03-C06 — CodePen LegendaryAchievement visual parity investigation

**Date:** 2026-11-18. **Owner authority:** previous RASC C05 user screenshot image(10).png (280×68 RGBA); approved reference screenshot image(20261118-043935).png (170×123 RGBA); user authorized implementation and instructed that the original [Dataminer Steam Achievement Animated Glow CodePen](https://codepen.io/Dataminer/pen/pooWzpo) HTML/CSS be the preferential visual source.

**Starting branch:** reconstruction/p003-c03-c05-user-mask-glow-visibility @ 9f873cc044ea739e94d008117050c51e8d626f3a (prior C05 Windows CI 44237885339 PASS). main remains 425444dc7440163ec8b3f74ee0ad9c360d5896fd.

## Screenshot differences

- The C05 screenshot shows a sharp yellow-orange sunburst clipped along the left/bottom of the 70px Modern popup. The reference screenshot shows a soft, fine amber halo around an otherwise plain 48px square with much less saturated rays.
- For context only, a pixel sampling over non-icon regions of the two uploaded frames counted 1611 warm (R>G+8, G>B+10) and 488 bright (R>160) pixels in the C05 screenshot, versus 331 warm and zero bright under the same thresholds in the CodePen image. These are **non-synchronized screenshots with different backgrounds and crop geometry**, NOT a binding numerical parity target. Original image pixels are NOT checked into the repo.
- Historical C05 WPF smoke independently established visible moving rare gold with exact user-supplied PNG alpha masks. The problem is **overbright/high-contrast compositing**, not missing masks.

## Relevant original CodePen source

HTML: one .AchievementIconWrapper.LegendaryAchievement with child .AchievementIconGlowContainerRoot > .AchievementIconGlowContainer > .AchievementIconGlow, followed by sibling .fakeIcon.

CSS: wrapper is **48x48**, no overflow:hidden; three glow levels use position:absolute and top/right/bottom/left **-10px**, producing 68/88/108 units. Outer square PNG mask (100% sizing), inner star PNG mask (100%), star rotates **reverse 18s linear**, light **forward 6s linear**. Gold radial gradient has original stops 0/6/10/26/35/40/60/82/100% and alpha/color combinations (255,201,109,.178) and #ffb84e; do not modify/source-fit arbitrary stops. .fakeIcon uses background **#222** and TWO box-shadows ON THE SAME ELEMENT:
- 0px 0px 2px 1px rgba(255,184,78,0.6)
- 0px 0px 16px 1px rgba(255,217,78,0.4)

See \`tests/reference/legendary-codepen.html\`: self-contained CSS/HTML reference fixture, PNG masks loaded from their existing experimental RASC resource locations. This file is for browser-versus-WPF comparison only. Original pen remains authoritative.

## Source discrepancy confirmed in previous RASC C05

- RASC's CreateIcon had a 54x54 WPF canvas (rather than 48px reference wrapper) with a separately positioned 48px image.
- Two **separate opaque** 48x48 WPF Border elements each used DropShadowEffect, which does not reproduce CSS single-element double shadows or the source's 1px spread. Effects were drawn on separate opaque WPF surfaces.
- RASC's masked 68px glow root explicitly set ClipToBounds=true despite no CSS overflow:hidden. The enclosing Modern card still requires clipping to its 283x70 accepted surface.
- The WPF gradient stops/mask PNG source and 6s/18s rotation were correct in principle. Their original-client parity remains UNVERIFIED.

## C06 engineering decisions

- Use a **single 48px Canvas icon wrapper** at logical card position (10,11). The root 68px mask begins at x=0, preventing the previous arbitrary hard cut across the leftmost masked column. Icon text/content remains at x=69; accepted card geometry remains 283x70 and unchanged Modern/Classic schedules.
- Remove explicit root ClipToBounds=true. Let the original square alpha PNG define the root mask. Outer card ClipToBounds remains necessary for popup lifecycle/bounds.
- Replace two independent opaque Border DropShadowEffect instances with ONE transparent precomposed 128x128 Pbgra32 image behind the actual icon. Build the two CSS shadows using a 1px-spread 50px square source and Gaussian blur sigma=radius/2 (2px->sigma1;16px->sigma8) with near (0.6 amber) composited over far (0.4 gold). **Sigma mapping and WPF/browser gamma differences are engineering approximations**, not claimed exact Chromium CSS raster. Preserve the same source values; no invented brightness multiplier or borders.
- Preserve user-approved original RASC icon artwork (dark face and gold star) in actual notification. CodePen #222 blank fakeIcon is included in the browser reference fixture, not substituted for production achievement art.
- Export two timed WPF rare frames as CI artifact for human visual comparison, plus source/geometry/actual rendered gold/animation assertions. Existing 1/2/3-card seamless background and cross-profile tests MUST remain green.

## Confidence / release boundary

CONFIRMED SOURCE: CodePen HTML/CSS and WPF C05 prior source divergence.
OWNER-OBSERVED: screenshot overbrightness/harsh clipped rays relative to CodePen.
ENGINEERING: Gaussian sigma mapping, one-layer shadow raster, 1px wrapper placement adjustment.
VERIFIED COMPONENT: pending actual exact-SHA Windows CI at document creation.
INDEPENDENTLY VERIFIED: NOT RUN.
UNVERIFIED RUNTIME: owner's screen-level phase-synchronized comparison, full-screen/borderless, mixed-DPI.
RIGHTS: owner-provided square/star PNG masks remain experimental draft assets with unresolved Valve/CodePen redistribution licensing. Do not merge to main or publicly release until owner reviews rights and visual acceptance.

**Subsystem P003 OPEN.**
