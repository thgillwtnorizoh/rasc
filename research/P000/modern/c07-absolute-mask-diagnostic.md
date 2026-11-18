# P003-C03-C07 — CSS mask-box mapping diagnostic

**Date:** 2026-11-18. **Actor:** Work 2. **Starting exact tested C06 SHA:** `fcc53932f33f5b769381ff52181cec1525a33f6d`. Main still `425444dc7440163ec8b3f74ee0ad9c360d5896fd`.

## Evidence and bounded hypothesis
Original owner-approved CodePen uses source square/star alpha masks and `mask-size:100%` on separate 68/88px boxes. The supplied square PNG has near-zero alpha at its outer edge (x0,y32=1/255; x32,y0=9/255). Yet the actual C06 Windows frame shows vivid gold at the 68px rectangle's edge. C06 WPF uses a relative ImageBrush Viewport and Viewbox against nested, larger rotating descendants. **DISCREPANCY:** the glowing border is disproportionately bright. **ENGINEERING hypothesis, NOT VERIFIED cause:** relative mask coordinates are mapped to painted bounds instead of element boxes.

Fix candidate: explicitly map 64x64 source PNG pixels to absolute 68x68 and 88x88 mask-box viewports. Preserve source gradient, icon art, rotation timing, root CSS-mask-box clip, both CSS shadow layers, gapless card background/watermark, Classic and all other features. Extend native-WPF smoke to assert absolute mask geometry. Chrome diagnostic screenshots at fixed 900/2000ms compare source fixture (blank #222 icon, #1D2025 background). Original C06 frames have a different background and icon; raw pixel comparisons must normalize crop/phase first.

CI history: 44305139439 FAILED browser screenshot step (162/162 core + six real WPF passed); 44305657001 FAILED browser screenshot step (same WPF passes), because direct PowerShell Chrome GUI launch returned before screenshot creation. Diagnostic fix now uses Start-Process -Wait -PassThru; source mask PNGs preserved in second failed run artifact.

**Gates:** exact SHA, Windows WPF build, 162 core regression, six WPF scenarios, browser render, DPI 100/125/150%, seamless one/two/three, x64 publish, separate Work 1 independent QA, owner Windows comparison NOT VERIFIED until tested. Asset rights UNRESOLVED. No main merge or public release. P003 OPEN.

## Exact first verified C07 candidate and browser caveat

Windows CI [44306680450](https://github.com/thgillwtnorizoh/rasc/actions/runs/44306680450) at **f882870c0b569c0e0a87ecd899605b7d86f42fa2**: **162/162 core pass, 6/6 native WPF smoke pass, zero WPF build errors, self-contained x64 publication pass**. Source mask-box mapping reduced 900ms outside-icon bright warm pixels (R>160 and R>B+10) from 836 in prior C06 screenshot to 103; pixel x0,y35 RGB changed from (136,107,58) to (37,38,36). This supports a **VERIFIED COMPONENT** raster fix, NOT the entire modern halo's equivalence. Diff screenshot: 283x70 C06/C07 frames in GitHub Actions artifacts.

Chrome 900ms and 2000ms screenshots produced **identical pixels** at this first CI attempt, so the supposed phase-matched browser render is **NOT VERIFIED** as a discriminating animated reference. A new browser diagnostic captures source CSS with masked glow hidden and with masks removed to test image loading and masking. Do not use zero browser change as evidence of animation parity. DPI-specific 125/150% and owner Windows comparison remain NOT RUN.
