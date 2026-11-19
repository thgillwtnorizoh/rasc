# P004-C02 — Original Steam Modern achievement font metrics, with reversible WPF test

**Project / Campaign / Actor:** RASC / P004-C02 / Work 1-style source research, with Work 2 limited implementation. **Status:** STATIC SOURCE CONFIRMED in archived SteamTracking extraction; WPF and owner visual acceptance **PENDING** at publication. **Original P004 base:** `772d9296457cfc248da51fa39cee97045ce69f6b`; **new development branch:** `research/p004-c02-modern-steam-font-metrics`.

## Research authority and precise claim

SteamTracking repository `SteamTracking/SteamTracking`, **revision** `9f22fbdfd7710a57d59e07b7469c684b8c4cf0d2`, sources:
- [`ClientExtracted/steamui/sp.js`](https://github.com/SteamTracking/SteamTracking/blob/9f22fbdfd7710a57d59e07b7469c684b8c4cf0d2/ClientExtracted/steamui/sp.js)
- [`ClientExtracted/steamui/css/sp.css`](https://github.com/SteamTracking/SteamTracking/blob/9f22fbdfd7710a57d59e07b7469c684b8c4cf0d2/ClientExtracted/steamui/css/sp.css)

**Trace:** `sp.js` module `3285` function `Qe` resolves the achievement notification. If `Ue(location)` is true (short desktop toast), `Qe` constructs `Y` with `title = achievement.strName` and body `AchievementTextSection > DescriptionShort` for a completed achievement, or progress children for an incomplete one. `Y` assigns CSS classes from module `21728` (`ShortTemplate`, `Header`, `Title`, `Body`). Module `21728` maps `ShortTemplate = fntOoeLPSTpmyXGGmgf99`, `Header = _1WuK_iZ6ARkIiptCX5qd7G`, `Title = _18PwvOcpWfW3M8j2-bEPPJ`, `Body = _2jpxEWvo06efD6-NR1cplA`. Module `48248` maps achievement `DescriptionShort = _35FNy1MhQxGVCALPuR6FH3`, which applies overflow/ellipsis only.

**CSS selectors and normalized properties:**
- `.fntOoeLPSTpmyXGGmgf99 ._18PwvOcpWfW3M8j2-bEPPJ`: `font-family: "Motiva Sans", Arial, Helvetica, sans-serif`; `font-style: normal`; `font-size: 11px`; `font-weight: 500`; `white-space: nowrap`; `overflow: hidden`; `text-overflow: ellipsis`; `color: #fff`. No explicit letter-spacing override in this exact rule. Parent `.fnt... ._1WuK_iZ6ARkIiptCX5qd7G` has `height: 14px` and `line-height: 14px`.
- `.fntOoeLPSTpmyXGGmgf99 ._2jpxEWvo06efD6-NR1cplA`: `font-family: "Motiva Sans", Helvetica, sans-serif`; `font-weight: normal` (400); `font-style: normal`; `font-size: 12px` (overriding earlier 16px); `line-height: 16px` (overriding earlier 20px); `letter-spacing: 0`; `color: #b8bcbf`; `height: 16px`, `white-space: nowrap`, `overflow: hidden`, `text-overflow: ellipsis`. The `DescriptionShort` element adds ellipsis/nowrap but no typography override.
- **Source scope:** confirmed extracted *public snapshot*, NOT a hash-match to owner's earlier supplied customized `dogbones.zip` UI build 11229305. Code paths for in-game, gamepad and other toasts differ. Historical original runtime resolution (e.g. whether exact Motiva face is installed and actually used) remains UNVERIFIED.

## Why RASC Motiva can look less faithful than RASC sans-serif

Current accepted RASC P004 Modern title is 12 WPF DIPs / SemiBold 600; description 11.5 DIPs / Normal 400; both use automatic WPF line metrics and natural glyph spacing. **Source Steam CSS** title is **11px / Medium 500** and body **12px / Normal 400 / 16px line-height**. At 100% browser zoom, CSS px and WPF DIPs share nominal 1/96-inch units, but browsers and WPF rasterize and line-position glyphs differently. The owner's observation about Motiva glyph shapes is consistent with this actual font family; do not claim the stylized lowercase `a` alone identifies an exact file/weight.

| Property | RASC accepted original | Steam CSS comparison |
|---|---|---|
| Modern title | user font, 12 DIP, weight 600, automatic line metrics | user font, 11 DIP, weight 500, 14px inherited header line height |
| Modern body/description | user font, 11.5 DIP, weight 400, automatic line metrics | user font, 12 DIP, weight 400, 16px line-height |
| Modern numeric progress | user font, 11 DIP, weight 400, automatic line metrics | user font, 12 DIP, weight 400, 16px body line height (bounded WPF approximation) |
| Letter spacing | natural font metrics | title natural; body CSS explicitly 0; no custom WPF tracking |
| Other visuals | original C07 | unchanged (text only) |

## Implemented *debug* experiment

A new **Modern text metrics** dropdown selects either **RASC accepted original (default)** or **Steam CSS (11/500, 12/400)**. Existing **Popup font** selection stays independent: compare Motiva Sans and Windows sans-serif using identical metrics. The setting is persisted in `notification-customization.json` with backwards-compatible default, so previous users retain the accepted C07 appearance. A change during an existing wave updates only the active TextBlocks, once per change, without restarting animations or playing a cue. Source CSS mode changes text size/weight/line height only; it does not alter rare glow, background, asset, clipping, position, event data, sounds or Classic typography.

**Approximations:** The WPF title's 14px `LineHeight` is derived from its CSS header, not an independently measured title box. WPF `LineStackingStrategy.BlockLineHeight` imitates line-box height but is not Chromium's exact font ascent/descent placement. CSS `letter-spacing: 0` is left as normal WPF glyph advances; we do not construct glyph-by-glyph tracking and destroy kerning. The progress variant may have additional nested markup; exact Steam progress-number typography/cross-browser face resolution must be separately checked. Changed sizes may move apparent glyph ink relative to old fixed WPF coordinates.

## QA and acceptance

1. Run old full P004 178-core/7-WPF suite, C07 gold browser comparisons and Windows x64 publish at exact new SHA; verify no regressions.
2. Verify new unit tests for enum default, old JSON migration and persistence.
3. In real WPF smoke, select Modern > progress > font Arial; switch Steam CSS mode while popup is visible and assert title 11/500/14, progress 12/400/16. Switch back and assert 12/600 and 11/400 + default line layout.
4. Owner compare Modern normal, rare, progress in selected **Motiva Sans**, each text metric profile, and Windows sans-serif baseline, all at identical DPI/sizing. Record screenshot and measured width/height, don't change accepted default until owner explicitly accepts.
5. Work 1 independent source/CSS and Windows QA. Physical font-face selection and native Steam pixel parity remain UNVERIFIED RUNTIME absent observation.

**Subsystem P004 OPEN; C07 accepted visual baseline unchanged; no main merge.**
