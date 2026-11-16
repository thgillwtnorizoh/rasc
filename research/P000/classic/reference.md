# Classic Steam achievement notification — May 2023 VGUI reference

**Authority:** original May 2023 [achievement layout](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/AchievementNotification.res), [steam.styles](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/styles/steam.styles), [steamscheme.res](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/steamscheme.res), [localization](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/trackerui_english.txt), and manifest-matched DLLs. **Classification:** CONFIRMED ORIGINAL resource values; runtime interpolation **UNVERIFIED**.

## Geometry

All measurements are nominal VGUI resource coordinates, **not guaranteed physical screen pixels**.

| Element | x | y | width | height | Notes |
|---|---:|---:|---:|---:|---|
| `AchievementNotification` | 0 | 0 | 240 | 94 | `style=notification` |
| `AchievementIcon` | 14 | 14 | 64 | 64 | `ImagePanel`, `scaleImage=0` |
| `IconBorder` | 13 | 13 | 66 | 66 | `Panel`, `paintbackground=0` |
| `LabelTitle` | 88 | 25 | 144 | 28 | centered, wrapped |
| `LabelDescription` | 88 | 53 | 144 | 28 | north-west, wrapped |
| `DarkenedRegion` | 1 | 74 | 238 | 23 | `fillcolor=ClientBG`, `zpos=-1` |

**Boundary note:** the dark region's nominal bottom (y=97) extends beyond the panel height (94); clipping or parent sizing is not proven by this resource alone. Do not 'fix' the source measurement silently.

## Visuals

`Notification` style draws the background procedurally: gradient from `DialogBG` at y0 to `ClientBG` at y0+80. It draws one-unit left/top/right/bottom borders, with right and bottom accent gradients; **no single background image asset is required by this style**. Full exact rasterization remains unverified.

- `DialogBG`: RGBA 42,46,51,255 (`#2A2E33`)
- `ClientBG`: 18,26,42,255 (`#121A2A`)
- `Highlight5`: 24,53,82,255 (`#183552`)
- `AchievementPopup.TitleColor`: `Text` = 200,208,220,255 (`#C8D0DC`)
- `AchievementPopup.DescriptionColor`: 180,180,180,255 (`#B4B4B4`)
- Labels use `FriendsSmall` font: `Arial`, `tall=12`, `weight=800` (VGUI schema values; not yet calibrated against CSS points/pixels).
- Individual achievement icon artwork is **not** embedded in the layout; separately resolved 64×64 image. Original CS:GO icon provenance remains open.

## Notification configuration

`Notifications.PanelPosition=BottomRight` on Windows, `SlideDirection=Vertical`, `FadeInTime=0.45`, `FadeOutTime=0.45`, `DisplayTime=6.0`, `StackSize=3`. The stack setting's own original comment states background queuing after three panels. Nominal non-interrupted interval is 6.9 seconds; this is **not** a frame-exact empirical measurement. A separate `gameoverlay.styles` includes `steam.styles` and a BottomRight notification-position entry; actual overlay-specific override semantics are not independently traced.

### Localization variants

English keys: unlock headline `Achievement Unlocked!`; body `%achievement%`. Progress headline `Achievement Progress`; body `%achievement% (%cur%/%max%)`. Do not claim all progress notifications are durably represented by the GT-610 provider.

### Static native markers

ZIP `bin/friendsui.dll`: `CAchievementNotification`, `friends/AchievementNotification.res`, `AchievementPopup.TitleColor`. `bin/vgui2_s.dll`: `CNotificationManager`, `Notifications.FadeInTime`, `Notifications.StackSize`. `bin/gameoverlayui.dll`: `overlay_achievement_item.layout`. Presence of strings is evidence of linkage/feature names, **not a full native call-graph proof**.

## Discrepancy

The earlier interactive preview used a smooth slide easing and invented offset/staggering. Owner explicitly observed that it was unlike classic Steam movement. Classify as **DISCREPANCY C-D01**: no preview animation is accepted as a visual fixture. Exact easing, panel spacing, stacking arbitration, event callbacks, audio and live overlay behavior need targeted static/runtime qualification.
