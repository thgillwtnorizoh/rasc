# P004 — popup font comparison and notification audio choices

## P004-C02 — source-derived Modern text comparison

**Verified Work 2 development checkpoint** (184/184 core PASS, 8/8 Windows WPF PASS at exact production SHA `0d954dc35c12f681911bc1b618fc0417da44053a`, [CI #44433832499](https://github.com/thgillwtnorizoh/rasc/actions/runs/44433832499)). Draft [PR #33](https://github.com/thgillwtnorizoh/rasc/pull/33) extends this guide with a separately selectable **Modern text metrics** dropdown. Keep the previously accepted RASC visual baseline as default. Steam CSS Standard uses name 12/400 and description 12/300 with 16px line height; BasicUI uses 12/500 and 12/400. The font family chooser is independent. See the [source-by-source typography audit](C02-source-typography.md) for exact CSS/JS module mapping and provenance. Source-informed is not pixel-identical WPF or owner acceptance.


**Project:** CS:GO Achievements Resurrection / RASC · **Status:** VERIFIED WORK 2 DEVELOPMENT CHECKPOINT ([Windows CI 44341243839](https://github.com/thgillwtnorizoh/rasc/actions/runs/44341243839), 178 core PASS, 7 real WPF PASS), owner audible and font GUI approval pending · **Starting production baseline:** `373d04101d03f51135f2f1b192758efddad12f00` (P003-C07). **Owner scope:** initial popup visual renders accepted; font and sound customization are NEW features. No change to accepted colors, icon, mask glow, wave, DPI positioning, or source observer.

## 1. Font comparison

The main window's **Popup font** dropdown applies to BOTH notification styles (Classic headline/body and Modern headline/description/progress text) without editing the icon glyph, shadow, watermark, medal or background. Choices:

| Choice | Behavior | Original-source confidence |
|---|---|---|
| **Original RASC design** (default) | Classic retains accepted `Helvetica, Arial`; Modern inherits Windows WPF UI font | RASC visual baseline, **not** proven native font |
| **Motiva Sans** | Uses installed Motiva Sans; Arial fallback otherwise. Missing font is reported in the main window | Widely used in Steam's wider UI; **specific Modern achievement popup family NOT VERIFIED** |
| **Arial** | Native Windows Arial family | Diagnostic comparison |
| **Helvetica** | Uses installed Helvetica, with Arial fallback if missing | Classic owner-target selection; historic May 2023 resource specifies different font |
| **sans-serif** | WPF's default Windows UI font (`SystemFonts.MessageFontFamily`) | Generic CSS-like debug option, not a particular Steam webfont |

**Fonts are NOT bundled or installed by this project.** A Motiva/Helvetica selection that falls back is invalid as evidence that the original used that font. Check installed fonts before capture and record resulting actual face, OS scaling and font raster metrics. Font changes are in-session and persistent (`%LOCALAPPDATA%\RASC\notification-customization.json`), and restyle text in any visible cards without restarting the toast timeline. `ClassicToastCard.xaml` accepted baseline is unchanged.

## 2. Notification sounds

The main window's **Notification sound** dropdown selects:

- **Steam desktop (default):** `desktop_toast_default.wav` from the owner's upload; 44.1kHz stereo 16-bit PCM, **0.634195s** (SHA-256 `c03fc4796f78001b8d9e279c053d859b6e68ca97bdcb4bc657e45af3d17a3ef7`).
- **Big Picture achievement:** `deck_ui_achievement_toast.wav`; 44.1kHz stereo 16-bit PCM, **1.634580s** (SHA-256 `f96a1ab8ff7668b3663a465749e517be98d54515b2dd314ccc291fbf3645c22a`).
- **Custom:** user-selected local `.wav`, `.mp3` or `.wma` (up to 16MB); actual playback requires a working Windows Media Foundation/DirectShow codec. The selected file must remain at its saved path.
- **Off:** no sound, optional silent mode.

**Important rights boundary:** these two uploaded WAVs have not been given a redistribution license. The public repository and self-contained Windows CI binary DO NOT include the WAV bytes. Instead click **Import Steam WAV...** and browse to each user's copy. The application verifies the exact SHA-256, copies atomically into the user's `%LOCALAPPDATA%\RASC\sounds\`, and never modifies the original file or Steam installation. Import each preset separately. This is not `SteamClient` audio playback.

**Alternative offline owner pack:** extract the supplied private `RASC-P004-Owner-Sound-Presets.zip` beside `Rasc.Desktop.exe`, yielding `Assets/NotificationSounds/desktop_toast_default.wav` and `Assets/NotificationSounds/deck_ui_achievement_toast.wav`. The application checks both hashes before playback. If the private pack is unavailable, use the Import button with the originally supplied WAVs.

**Test selected sound** plays one cue without earning an award. Actual notification sound is triggered exactly at **Classic card entry** or once at the beginning of a **Modern wave**, never during queued cross-profile admission, on every WPF frame, or for history rehydration. Silent/unavailable preset never crashes or suppresses the popup. Up to eight transient playback objects are retained and cleaned up on media end/error or app shutdown. Sound volume follows Windows output settings; no independent mixer or Steam client APIs.

## 3. Verification matrix

1. Select Modern, **Arial**, Preview Progress. Compare text to baseline. Switch to **Helvetica**, **Motiva Sans**, **sans-serif**, Original, and verify visible text changes (or missing family warning); confirm the 283×70 geometry and C07 halo remain unchanged.
2. Select Classic and repeat for headline/body. Preserve original default Helvetica styling; no changes to the 240×94 XAML appearance with **Original** selected.
3. Import each WAV and use **Test selected sound**. Verify the desktop cue sounds shorter than Big Picture. Select Custom and play a user WAV, MP3 or WMA; select Off and verify silence. Corrupt/wrong WAVs must be rejected without overwriting imported presets.
4. Verify sound occurs on one actual Classic arrival and only once per Modern admitted wave of up to three cards. Queue across profile changes; there should be no sound before visual admission or on every animation frame.
5. Restart RASC and confirm choices persisted. Move/delete custom file, verify warning/no crash. Test app closure during sound and settings modifications.
6. Perform physical owner GUI + audible sound tests at normal and high DPI; WPF/CI tests do not prove exact native original font raster, physical audio output timing, or original Valve equivalence.

## 4. Implementation and historic authority

- `src/Rasc.Core/NotificationCustomizationSettings.cs`: enum profiles, independent settings store and validated WAV import resolver.
- `src/Rasc.Desktop/{PopupFontResolver,NotificationSoundPlayback}.cs`: system-font lookup and bounded WPF MediaPlayer.
- `src/Rasc.Desktop/MainWindow.xaml[.cs]`: preferences, paths, import and test controls.
- `src/Rasc.Desktop/{ClassicToastHost,ModernToastHost,ModernToastCard,ModernToastWindow,ClassicToastCard.xaml.cs}`: text-only font propagation and entrance-bound sound cues.
- `tests/Rasc.Core.Tests/NotificationCustomizationTests.cs` and WPF smoke `font-sound-settings`.
- `research/P000/modern/reference.md` confirms separate desktop and Deck sound asset names in supplied build 11229305, but had **not** established the exact actual Modern toast font face. General Steam Motiva usage cannot substitute for a target-specific CSS resolution check.
- All P002/P003 visual source, rare masks, animations and historical tests remain intact. Sound chosen for a Modern wave once is an **ENGINEERING policy**; not asserted as exact Valve callback sequencing.

**Confidence:** media files CONFIRMED USER-SUPPLIED (local SHA, duration, PCM); accepted visual baseline USER-APPROVED; source-specific font remains UNVERIFIED; implementation development CI is tracked in [checkpoint](checkpoints/P004-C01.md); physical audio output/renderer typography Work 1 **NOT VERIFIED**.

**Subsytems:** P002 OPEN; P003 OPEN; P004 OPEN until verified and owner acceptance.
