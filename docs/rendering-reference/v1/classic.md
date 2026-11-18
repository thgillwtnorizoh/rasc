# Classic achievement notification — frozen visual profile v1

**Scope:** RASC WPF owner-target Classic card and occluded vertical stack. **Exact source lock:** [`373d04101d03f51135f2f1b192758efddad12f00`](https://github.com/thgillwtnorizoh/rasc/tree/373d04101d03f51135f2f1b192758efddad12f00). **Acceptance:** owner accepted **card appearance** on 2026-11-17; no blanket P002, physical-DPI, Valve runtime or independent-QA acceptance.

## A. Two authorities that must stay distinct
- **CONFIRMED ORIGINAL STATIC CONFIG, May 2023 VGUI:** [AchievementNotification.res](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/AchievementNotification.res), [steam.styles](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/styles/steam.styles), [steamscheme.res](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/steamscheme.res), [P000 explanation](../../../research/P000/classic/reference.md). **Original font** `FriendsSmall` is Arial, `tall=12`, `weight=800`; title alignment `center`, procedural border reported. Source `FadeInTime`/ `FadeOutTime` names do **not** prove opacity fading in runtime.
- **OWNER-ACCEPTED RASC VISUAL TARGET:** [owner acceptance](../../../docs/P002/acceptance/visual-design-baseline.md), [screenshot calibration](../../../docs/P002/fixtures/owner-classic-screenshot-calibration.md). **Helvetica Regular target**, left-aligned title and body, no visible frame stroke. Helvetica is not guaranteed installed; actual WPF has Arial fallback. This intentionally conflicts with the pinned original font/alignment and is **NOT original Valve-font proof**.

## B. Card geometry, layers and text

Source file: [`src/Rasc.Desktop/ClassicToastCard.xaml`](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Desktop/ClassicToastCard.xaml). Dimensions are **reference WPF DIPs**, not universal physical pixels.

| Element | x | y | w | h | Exact v1 style |
| --- | ---: | ---: | ---: | ---: | --- |
| Card root/canvas | 0 | 0 | 240 | 94 | `ClipToBounds=true`, `SnapsToDevicePixels`; no visible border |
| Background | 0 | 0 | 240 | 94 | top `#2A2E33` at 0; bottom `#121A2A` at 0.851 and 1; vertical linear gradient |
| Darkened region | 1 | 74 | 238 | 23 | solid `#121A2A`; clipped by 94-high root even though region nominally extends to y97 |
| Icon placeholder | 14 | 14 | 64 | 64 | `#343434` square, centered `★` (Arial bold 28, `#B4B4B4`); **not licensed game icon art** |
| Headline | 88 | 25 | 144 | 28 | `#C8D0DC`; `Helvetica, Arial`, 11.5 DIP, Normal, **left**, Wrap, line-height 12.5 |
| Description | 88 | 53 | 144 | 28 | `#B4B4B4`; `Helvetica, Arial`, 11.5 DIP, Normal, **left**, Wrap, line-height 12.5 |

WPF uses `TextFormattingMode=Display`, `TextRenderingMode=Grayscale`, `TextTrimming=None`, `LineStackingStrategy=BlockLineHeight`. Do not swap these casually when doing a future screenshot comparison. No source/preview icon should be mistaken for a true CS:GO achievement icon catalog.

## C. Motion and compositing contract

[Core scheduler](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Core/ClassicToastScheduler.cs), [WPF compositor](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Desktop/ClassicToastStackWindow.xaml.cs), [owner motion contract](../../../docs/P002/contracts/P002-C01-C01-classic-stack-motion.md).

- **One shared 240×282 viewport**, bottom-right of primary work area (or fullscreen monitor under bounded condition), zero inter-card gap and zero outer inset. Maximum **three** settled cards; newest/bottom is slot 0, oldest/top slot 2. No separate per-card HWND.
- Serialized `Enter(450ms) -> Hold(6000ms) -> Exit(450ms)`, **visible opacity exactly 1**, no alpha fades. Motions use a monotonic clock and linear fraction in current engineering implementation; do not ascribe this easing to native Steam.
- **Entry:** new card rises from immediately below taskbar edge while all settled cards shift up by exactly one 94-DIP card. Existing cards are never teleported. At top capacity, additional entries queue.
- **Exit:** departing card sinks down into the immediate lower card (or taskbar). The lower surface **occludes** the descending leading edge; survivors above compact toward the bottom. No transparent strip, new margin, or exposed bottom edge.
- **Physical-pixel seam prevention:** `ClassicToastPixelGrid.Map` quantizes origins and clip boundaries on the **same physical grid** at OS DPI × popup-size factor. The compositor adds a color-matched opaque backplate only for subpixel coverage; it must not change the approved card art. Preserve card z order (lower covers upper).
- **Presentation cadence:** WPF `CompositionTarget.Rendering` while sliding, sleep during static hold. Configurable average applied visual update target auto or 15–240 FPS, not hardware presentation FPS. Do not tie movement duration to callback count.
- **Sizing:** design stays fixed at 240×94, uniformly scaled. Auto uses primary display **logical height / 1080**, clamped to 1–2× and work-area fit; manual 100/125/150/175/200/250%. OS DPI is already included in WPF transform calculations. Settings file `%LOCALAPPDATA%\RASC\appearance.json`. Additional-monitor and real DPI behavior remain unverified.

## D. Portable golden fixtures
1. **Unlock:** headline `Achievement Unlocked!`, description `Someone Set Up Us The Bomb`, expected target line break before `Bomb` for owner sample. Validate wrapping at identical font availability and scale.
2. **Progress (preview-only):** headline `Achievement Progress`; body `Paranatural Collection (20,010/100,000)`, owner sample wraps before numeric fraction. GT-610 does **not** provide a persistent progress callback just because a preview exists.
3. **Burst:** 1, 2, 3, then 4+ rapidly queued IDs; confirm max three, newest bottom, gapless settled and moving stack, fully occluded exits, serial arbitration and no alpha fade.
4. **DPI:** 100%, 125%, 150%, 175%, 200% plus manual/auto size; check top/middle and middle/bottom seams in **actual Windows screenshots** (simulation/unit tests alone are insufficient).

**Evidence:** owner upload `image(1).png` 275×102 SHA-256 `ac36c86f025e5f33140a8632274b563caa343c60d6b9a3814559b63df37150b9`; owner upload `image(2).png` 410×162 SHA-256 `106e31ce9961176bbd8727f12da01b8d4bc955b55618d74faf3a48b3d08f9c94`. They were **not committed or newly redistributed** in this archive.

## E. Unresolved compatibility gaps
C-D05 source procedural border vs owner no visible stroke; C-D08 original centered vs owner left-aligned title; C-D09 archived Arial 12/800 vs user-target Helvetica Regular; exact native easing, font raster, original context, progress callbacks, fullscreen/secondary monitors, physical refresh rate, and Work 1 QA remain open. **Keep the differences explicit, not "corrected" by changing source history.**
