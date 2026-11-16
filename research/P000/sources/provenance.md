# P000-C01 source provenance

## Classic source

- Original reference: SteamTracking commit [`90999d0307ac7f2b92f805e8f3a5d159421eaf8a`](https://github.com/SteamTracking/SteamTracking/commit/90999d0307ac7f2b92f805e8f3a5d159421eaf8a).
- Windows client manifest: [`ClientManifest/steam_client_win32`](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientManifest/steam_client_win32), manifest version **1685488080**, Git blob `bec8dc95aef3960995282de478c3e195a7f366ac`.
- Uploaded package `bins_win32.zip.552301e7ebae1bbd0c5a141020798635551c0433`: **56,674,412 bytes**, SHA-256 `4dc2f39f6f129ac165c726291f79307d6f96a39fcf4ecf659a989358e8c1caca`, **63 ZIP entries**, archive CRC good. SHA-256 exactly matches `bins_win32.sha2` in the pinned manifest. CONFIRMED ORIGINAL *for this archive and manifest*, not proof of historical runtime behavior.
- Original extracted layout: [`ClientExtracted/friends/AchievementNotification.res`](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/AchievementNotification.res) — blob `9d31daab82a3e7c735eaac4bd4e163c83b5e0a72`.
- Original styles: [`ClientExtracted/resource/styles/steam.styles`](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/styles/steam.styles) — blob `d5f0d0470141cd69dfd73de6e49a15668ab631ca`.
- Overlay style: [`ClientExtracted/resource/styles/gameoverlay.styles`](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/styles/gameoverlay.styles) — blob `4bdabb1f02a04f628dbfd444e3392d33fd0f02e0`.
- Original font scheme: [`ClientExtracted/resource/steamscheme.res`](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/steamscheme.res) — blob `8a0c9ba944867f1a5b4e5a2d9879cd4926729524`.
- Original English strings: [`ClientExtracted/friends/trackerui_english.txt`](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/trackerui_english.txt) — blob `6ba29e5f6367702889d22b3ca99dab27d4eb9094`.

All six text source files were re-fetched from the exact pinned commit during this execution; checked contents include the manifest version, geometry, notification settings, font and localization. Native evidence comes from matching archived DLL bytes. Do not treat the Git blob SHA-1 as a substitute for SHA-256.

## Modern supplied archive

- Owner-supplied `dogbones.zip`: SHA-256 `fce957aab0785add653c084757a1ff8f7d7faa504b41f3deec698050480d3887`; **74,544,452 bytes**, **908 ZIP entries**, archive CRC good.
- `steamui/changelist.txt` is `11229305`, also represented by `CLSTAMP` in `steamui/index.html`. This identifies the packaged UI bundle **not an independently certified Steam release version/date**.
- Relevant contents: `steamui/sp.js` (16,224,005 decoded bytes in this ZIP), `steamui/css/sp.css` (2,798,959 bytes), `steamui/sounds/desktop_toast_default.wav`, `steamui/sounds/deck_ui_achievement_toast.wav`, overlay DLLs and `gameoverlayui.dll`.
- Customized-client boundary: `steamui/skin.css` (542,983 bytes) and `steamui/faceit_stats.css` (5,002 bytes) are **suspected external additions**. Their paths did not occur in the contemporary public SteamTracking `master` tree at the time checked, but that comparison is NOT version-matched and cannot prove that other supplied files are unmodified. No direct selector targeting the recovered desktop-toast hashed CSS class was found in `skin.css`; indirect overrides/injection are unexcluded.
- Modern statements should be labeled **CONFIRMED IN SUPPLIED CLIENT**, not *Valve-stock original*, until independently version-matched.

## Reproduction

Run `python research/P000/tools/verify_archives.py --classic "<classic ZIP path>" --modern "<dogbones.zip path>"`. This verifies the exact offline bytes and selected static markers without extracting to disk or executing binaries. Read the pinned historical text sources at the links above for the VGUI values.

**Never** commit the uploaded ZIPs, proprietary DLLs, WAVs, images, or extracted original resource files to this repo.
