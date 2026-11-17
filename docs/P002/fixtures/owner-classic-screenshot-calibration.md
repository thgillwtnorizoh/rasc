# P002-C01-C01 — Owner-supplied classic screenshot calibration (two fixtures)

**Project:** RASC · **Campaign:** P002 Classic rendering · **Actor:** HQ evidence analysis · **Date:** 2026-11-17 · **Authority:** two image files directly uploaded by owner, represented by cryptographic checksums and measured geometry. Owner described them as official references; their exact Steam build/version, original capture dimensions, cropping, resampling and DPI are **NOT INDEPENDENTLY VERIFIED**. Do not relabel them as `CONFIRMED ORIGINAL` binary/resource evidence without provenance.

## Input identity / custody

| Owner upload | SHA-256 | Native uploaded dimensions | Content |
|---|---|---|---|
| `image(1).png` | `ac36c86f025e5f33140a8632274b563caa343c60d6b9a3814559b63df37150b9` | 275×102 RGBA | "Achievement Unlocked!" / "Someone Set Up Us The" + "Bomb", colored rectangular icon |
| `image(2).png` | `106e31ce9961176bbd8727f12da01b8d4bc955b55618d74faf3a48b3d08f9c94` | 410×162 RGBA | "Achievement Progress" / "Paranatural Collection" + "(20,010/100,000)", grayscale square placeholder-style icon |

These are **external, user-provided reference fixtures**. Do not copy/upload/repackage the screenshots or icon artwork into the redistributable RASC application without verified distribution rights. Image hashes provide identity, not provenance or redistribution license.

## Visible pixel measurements

All rectangles below are **approximate uploaded-image pixel coordinates, not original VGUI coordinates**. Source image resampling/antialiasing means glyph ink bounding boxes need not equal text control boxes. Text boxes derived from row differences vs sampled background (RGB deviation >25), split by visually established text line bands. Icon bounds are approximate visual observations.

| Feature | `image(1).png` 275×102 | `image(2).png` 410×162 |
|---|---|---|
| Icon visible extent | x ≈9–86, y≈6–84 | x≈19–130, y≈21–133 |
| Headline text ink | x98–238, y22–32 | x148–341, y44–62 |
| Description line 1 ink | x99–246, y56–68 | x149–337, y93–107 |
| Description line 2 ink | x99–134, y70–80 | x148–295, y114–132 |
| Description content | `Someone Set Up Us The` + `Bomb` | `Paranatural Collection` + `(20,010/100,000)` |
| Background | dark vertical gradient, no *obvious visually contrasting* outer stroke | dark vertical gradient, no *obvious visually contrasting* outer stroke |

**Precision warning:** do not derive absolute font size, baseline, true panel border, DPI or exact crop from these blurry images. Neither screenshot is a captured overlay/time series; neither proves entrance/exit animation timing or compositor z ordering.

## Source comparison — prioritize larger progress image

May 2023 Valve-tracked original archived resource [AchievementNotification.res](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/AchievementNotification.res) provides 240×94 nominal VGUI panel, `LabelTitle` x88,y25,w144,h28 and `LabelDescription` x88,y53,w144,h28; `AchievementIcon` x14,y14,64×64, with `FriendsSmall` from `steamscheme.res` (Arial tall12 weight800).

Using normalized positions rather than assuming screenshot pixels are VGUI pixels:

| Coordinate | Original source ratio | Screenshot2 observed ratio | Difference |
|---|---:|---:|---:|
| Headline ink left / panel width | x88/240 ≈ **36.67%** (source *control* left) | x148/410 ≈ **36.10%** (ink left) | -0.57 percentage points |
| Headline ink top / panel height | y25/94 ≈ **26.60%** | y44/162 ≈ **27.16%** | +0.56 pp |
| Description ink top / panel height | y53/94 ≈ **56.38%** | y93/162 ≈ **57.41%** | +1.03 pp |
| Icon square width / panel width | 64/240 ≈ **26.67%** | visually ~112/410 ≈ **27.32%** | +0.65 pp |
| Icon square top / panel height | y14/94 ≈ **14.89%** | y21/162 ≈ **12.96%** | -1.93 pp (possible image crop/version difference) |

**Interpretation:** text control anchors from the source are well supported *proportionally* by the larger image. The previously reported WPF typography/placement failure does **not** justify changing the source x88/y25/y53 arbitrarily. First calibrate WPF font family metrics, em-to-VGUI glyph height, baseline, alignment, wrapping, transform scale and ClearType/grayscale antialiasing. Then compare screenshot output at comparable normalized dimensions. Icon leading-edge differences require separate investigation; do not fit them by hand.

The smaller unlock screenshot appears cropped or transformed relative to the larger one (different effective horizontal/vertical scales). Its stronger evidence is **word wrapping**, not absolute panel scale.

## Required fixtures for correction

1. **Unlock sample:** source headline `Achievement Unlocked!`, body `Someone Set Up Us The Bomb`; ensure source-compatible wrap with `Bomb` on second line. Icon may be a clearly labeled generic placeholder in distributable binary, not the supplied copyrighted art.
2. **Progress sample:** source headline `Achievement Progress`, body `Paranatural Collection (20,010/100,000)`; ensure line break before numeric fraction under the source-shaped text rectangle. Progress is **preview/test data only** until an independently supported provider emits progress events: GT-610 `achievements.txt` does not persist `IndicateAchievementProgress` callbacks.
3. Both samples preserve title/body *hierarchy*: first line headline and optional 2-line description, left text column visually aligned. No focus-grabbing window, margin/gap/border, alpha fade or visible clipped descending edge.
4. Future visual QA must preserve actual screenshot dimensions, capture method, system DPI, profile, text/string selection and color comparison. A human side-by-side picture and optical font metrics are required; static tests do not establish pixel parity.

## Evidence classification and discrepancies

- **OWNER-SUPPLIED VISUAL REFERENCE:** both screenshots and pixel measurements, not independent original binary provenance.
- **CONFIRMED ORIGINAL SOURCE:** pinned May 2023 `AchievementNotification.res`, `steamscheme.res`, `steam.styles` defines layout/color/font defaults; no runtime playback proven.
- **RECONSTRUCTED:** headline/control positions approximately consistent between larger screenshot and source at normalized scale.
- **DISCREPANCY C-D05:** owner reports no visible border but static original notification style has procedural 1-unit edge painting. Blurry images show no *clearly visible* border, not proof of zero native edge pixels. Implement no visible stroke per target, retain discrepancy.
- **DISCREPANCY C-D07:** WPF first prototype text font/metrics rejected; now two named screenshot targets exist, but no calibrated parity yet.
- **UNVERIFIED:** same build/version between screenshots and May 2023 reference, exact font rasterization, physical DPI, anti-aliasing, motion and stack behavior.

## Reproduction and next task

With original screenshots in owner-provided chat attachments, compare SHA-256 and inspect the measurements above. Do **not** use OCR as a typography acceptance oracle. Work 2 next: add explicit Unlock and Progress **preview-only fixtures** (no file writes), use the original nominal text rectangles as *starting source parameters*, generate WPF screenshot at consistent DPI and inspect baseline/glyph size/wrap independently. Keep the P002-C01-C01 motion contract and independent QA boundary unchanged.
