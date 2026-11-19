# P004-C02 — Source-based Modern achievement typography audit

**Date:** 2026-11-19. **Scope:** Modern desktop achievement notification **name and description** text only. Objective: explain why an owner's explicitly identified Motiva Sans face looks denser/wrong in the current WPF rendering, whereas Windows `sans-serif` has visually closer weight/area despite different glyph morphology. No speculative screenshot fitting and no rewriting the approved Classic/C07 backgrounds, icon, glow, timing, DPI or sounds.

## Provenance: exact recovered declarations

Primary accessible **SteamTracking public Steam UI snapshot**:
- [steamui/sp.js at 7857b7d4634423f6343942d07bf53ff6913a8b75](https://github.com/SteamTracking/SteamTracking/blob/7857b7d4634423f6343942d07bf53ff6913a8b75/ClientExtracted/steamui/sp.js); Git blob `7c22f46e19f4a34983006c381aefd32357e95a9c`.
- [steamui/css/sp.css at same commit](https://github.com/SteamTracking/SteamTracking/blob/7857b7d4634423f6343942d07bf53ff6913a8b75/ClientExtracted/steamui/css/sp.css); Git blob `8f867b6980e95d529dd530e9a1e3e385846effd4`.
- `steamui/changelist.txt` and `CLSTAMP` at this public snapshot report **11229305**. The previously archived **owner-supplied dogbones.zip** has the same UI build label 11229305, with raw archive SHA-256 `fce957aab0785add653c084757a1ff8f7d7faa504b41f3deec698050480d3887`, documented in `research/P000/sources/provenance.md`.
- The public `sp.js`/`sp.css` were **reformatted** by SteamTracking with `oxfmt` on 2026-11-09 (`0c9db805944d3b38bc135486431d424a1c6c3487`), hence public blob lengths differ from the owner's raw archived files. **Build-label match is NOT a byte-for-byte identity proof**. The owner's `skin.css` was suspected customized, and an indirect override cannot be excluded without a complete version-matched override audit.

JavaScript CSS module 75883 / `Ie()` maps these source class names:
```text
AchievementTextSection = mJ3Nu2ja1CSqf_qU1GD_b
Name                   = _1juF1NbXeVbg06v1506lgi
Desc                   = _2zm3GcVDieR0YsBNy6jQmX
NoWrap                 = _2ghro44p-iyeXJ1MFbHadQ
```
The achievement rendering function in `sp.js` actually wraps its name/description with `AchievementTextSection`, and uses `Name` and `Desc` children. This mapping excludes unrelated generic `StandardNotificationHeader.Title` declarations, which use other sizes/weights and are not necessarily the achievement's own name. Progress is a distinct nested component whose numeric label is excluded from this bounded calibration.

The relevant `sp.css` declarations, whitespace normalized (each CSS value preserved):
```css
.mJ3Nu2ja1CSqf_qU1GD_b ._1juF1NbXeVbg06v1506lgi {
  color: #fff; font-weight: 400; font-size: 12px;
  min-width: 0; line-height: 16px;
}
.BasicUI .mJ3Nu2ja1CSqf_qU1GD_b ._1juF1NbXeVbg06v1506lgi {
  font-weight: 500;
}
.mJ3Nu2ja1CSqf_qU1GD_b ._2zm3GcVDieR0YsBNy6jQmX {
  flex-grow: 1 1 auto; font-weight: 300; color: #8b929a;
  font-size: 12px; line-height: 16px;
}
.BasicUI .mJ3Nu2ja1CSqf_qU1GD_b ._2zm3GcVDieR0YsBNy6jQmX {
  color: #b8bcbf; font-weight: 400;
}
body {
  font-family: "Motiva Sans", Arial, Helvetica, sans-serif;
}
```

**CSS inheritance/override caution:** These are the **declarations**, not a complete browser `getComputedStyle()` trace on a live notification. In particular, presence/priority of `BasicUI`, a possible other font face or version/customization override, and effective rasterization remain runtime questions. Name/description have no direct explicit `letter-spacing` in these rules: do not invent 0.03em tracking from unrelated notification elements. Popup color is included in test profiles as a direct source value, but determining the best screenshot match is an owner visual gate.

## Reconstructed comparison profiles (NOT replacing accepted RASC)

| Profile | Name size / weight / color | Description size / weight / color | Line height | Confidence |
|---|---|---|---|---|
| **RASC accepted** (DEFAULT) | 12 DIP / 600 / `#D3D8DF` | 11.5 DIP / 400 / `#8C96A4` | WPF default | VERIFIED EXISTING COMPONENT |
| **Steam CSS standard** | 12 CSS px / 400 / `#FFF` | 12 CSS px / 300 / `#8B929A` | 16 CSS px | CONFIRMED SOURCE DECLARATIONS / WPF adaptation |
| **Steam CSS BasicUI** | 12 CSS px / 500 / `#FFF` | 12 CSS px / 400 / `#B8BCBF` | 16 CSS px | CONFIRMED SOURCE OVERRIDE / WPF adaptation |

CSS px are mapped to WPF DIPs by engineering convention at reference `96 DPI`; actual scaled layout and pixel raster can differ. WPF `FontWeights.Light/Normal/Medium/SemiBold` represent 300/400/500/600 but **do not guarantee** exact CSS variable-font face resolution or browser hinting/subpixel antialiasing. We **do not redistribute Motiva Sans font files**. The selected family is independent and may fall back without a font installed; compare only after verifying actual installed face.

For existing RASC 283×70 notifications, we deliberately **keep name/description X/Y location, text clipping and width at 204 DIP, progress number at prior RASC metrics, and all non-text rendering**. CSS also includes a 4px left margin and a separate flex template; moving all content would exceed the bounded typography experiment and require geometry/owner authorization. No new fractional fitting factors are introduced.

## Reproduction and acceptance checks

1. At startup select **Modern**, **Motiva Sans** (installed), **RASC approved**, preview `Champion`; save a screenshot at a recorded OS DPI and popup size.
2. On the same fixture with no other changes, select **Steam CSS standard**, then **Steam CSS BasicUI**. Observe the glyph hook and overall darkness/weight/text width/height; toggle Motiva Sans versus Windows `sans-serif` at the **same selected metrics**.
3. Restore **RASC approved** and verify exact previous name/desc metrics. Verify old settings files lacking `ModernTypography` automatically load this default. Flip to Classic and verify the selector causes **no Classic change**.
4. Test progress, rare, and 1–3-card stacks; previous colors/halo/background and 300ms/5000ms/300ms animation unchanged. Complete historical 178 baseline xUnit checks (plus new tests) and all original WPF smoke scenarios, and capture at multiple DPI. Verify no audio/profile/GC/SQLite side effects.
5. Work 1 independently review original JS/CSS associations, exact CSS specificity/mode inheritance, color and font weight fields, WPF test semantics and physical owner visual comparison before owner acceptance.

**UNVERIFIED RUNTIME:** Actual fonts installed in Steam client, physical font hinting under CEF versus WPF, exact original mode path for achievement, original pixel-level screenshot, original Valve stock equivalence. Screenshots + exact face/OS DPI and original archive scan are the remaining evidence needs. **P004 OPEN; no automatic owner acceptance.**
