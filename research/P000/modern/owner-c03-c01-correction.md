# P003-C03-C01 — Corrected fullscreen-only anchor and asset references

**Owner report:** November 17, 2026, after testing C03 Windows build. This supersedes the **scope** of the previous C03 instruction; it does not erase the original historical checkpoint.

## C03 misunderstanding and corrected requirements

- **DISCREPANCY:** C03 placed and autoscaled popups using ANY focused application's visible rectangle. Owner reported that normal windowed applications displayed tiny achievements INSIDE their own client area.
- **CORRECTED OWNER CONTRACT:** Without a focused fullscreen/borderless-fullscreen application, BOTH Classic and Modern must use their exact **pre-C03 bottom-right desktop work-area anchor and size behavior**, without regard to which ordinary window currently has focus. No auto scaling to ordinary app height. The taskbar reserves the previous space as before C03.
- **FULLSCREEN EXCEPTION ONLY:** If a focused visible HWND covers its full associated monitor, switch resting bottom/right anchor to that monitor's **physical full screen edge**, not its taskbar work area. Fullscreen size may derive from the monitor's logical dimensions. Do not confuse a normally maximized work-area window with fullscreen. Revert to desktop behavior immediately when no fullscreen app is focused.
- Keep original Classic card artwork, timing, clipping, and C02 Modern wave timeline unchanged. This is an anchor correction, not an animation redesign.

## Visual evidence

- C02/C03 Steam watermark *relative logo placement* across 1, 2 and 3 stacked cards accepted, but owner found the actual *silhouette* badly wrong. C03 was a hand-drawn wheel/rod, not Valve's symbol.
- C03 rare-icon glow had the correct architectural idea (animated halo around the icon), but not the right appearance: five circulating dots are not the Steam-style gold shimmer.
- No new screenshots accompanied this particular report.

## Reference-derived corrective implementation decisions

- **Steam symbol geometry:** Simple Icons, icons/steam.svg, https://github.com/simple-icons/simple-icons/blob/develop/icons/steam.svg . Source is a CC0 icon project; the Steam brand mark is Valve's trademark. We embed the geometric path without bundling client binaries. This is source-matched symbol **shape**, not proof of original Steam desktop toast asset opacity/size; retain owner's accepted 170-unit bounding box and C02 wave-specific placement.
- **Rare glow:** Steam app-details stylesheet tracked at https://gist.github.com/Faffotron/6ef1181ec3eb8961078ac39f990d45f7 documents a rotating repeating-conic gold gradient, 6-second forward spin, and related ~18-second reverse outer rotation. Owner reports that the Community rare-achievement glow appears identical to the Steam client one. **Direct reuse in the original desktop-toasts code remains NOT VERIFIED.** WPF reconstructs the angular stops and dual motion with a fixed square halo mask and diffuse light around the icon; blend mode is approximate, not CSS compositor-equivalent.
- **No injected overlay:** true exclusive fullscreen might occlude a topmost WPF HWND even when correctly placed. Borderless/fullscreen-window behavior must be tested by owner on actual CS:GO; do not claim runtime parity.

## Required development and independent checks

1. Four focus states: ordinary windowed app, ordinary maximized desktop app, focused true borderless/fullscreen app, and no valid foreground (fallback). Confirm only the fullscreen case triggers physical monitor edge placement.
2. Classic's normal desktop scale and its original taskbar baseline must match P002 exactly; Modern's normal desktop scale and baseline must match P003-C02.
3. On changing foreground from fullscreen to ordinary app (and back), the original time-based scheduler continues without restarting or duplicating unlocks.
4. 1/2/3 wave logo remains continuous across stack cards, now with recognizable Steam silhouette. Verify rare icon's gold is an animated halo rather than fixed border or rotating squares.
5. Real DPI, multi-monitor, borderless/exclusive game and screenshot comparison: **UNVERIFIED RUNTIME** until owner qualification.

**Coverage:** corrected contract captured. **Subsystem:** P003 OPEN; independence boundary remains.
