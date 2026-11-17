# P003-C03 — Owner's second Modern visual/positioning report (2026-11-17)

**Authority:** owner-operated Windows P003-C02 candidate observation in ChatGPT, referring to earlier four original screenshot references in owner-visual-c02.md. This report did not supply a new Steam client build or source files. Confidence is OWNER-REPORTED, not original source equivalence.

## New evidence / requirements

- **Modern watermark:** relative placement and continuity across 1/2/3 stack states are accepted as consistent. Owner rejects the shape: C02 uses an invented vector made of circles/rod, not original Steam artwork. Improve geometry without moving the existing watermark position. Exact CSS/SVG/PNG still missing.
- **Ordinary icon:** no visible blue framing. **Rare icon:** gold shimmer around image, not a constant yellow border. Owner compared Steam Community rare achievement glow and desktop client visuals and reported that they appear the same. Separate Steam Community CSS contains achievement_glow_container, achievement_glow_rotate, golden conic gradients and masks; reported visual equivalence is NOT conclusive source-code reuse by desktop toast.
- **Progress variant:** small medal/rosette icon immediately LEFT of numeric progress (19/50), as visible in screenshot four. Bar and distinct progress layout remain.
- **Shared placement:** BOTH Classic and Modern must anchor at foreground application's **visible rightmost/bottom corner**, automatically scale with current app/screen, and follow its monitor. Fullscreen and borderless fullscreen windows span display, so popups use display edge rather than always landing above desktop taskbar. Windowed apps anchor within visible window bounds clipped to usable monitor work area. Reanchoring must not reset timing.

## C03 interpretation and open questions

- Implement shared Win32 foreground HWND/monitor probing (GetForegroundWindow, monitor info, DWM visible bounds, effective monitor DPI) and native physical-pixel WPF positioning. No injected hooks or code in foreground game process.
- Recheck placement every 250ms during hold as well as animation frames. Preserve Classic artwork/typography/gradient.
- True exclusive fullscreen can occlude a WPF topmost window; owner must distinguish exclusive from borderless.
- Vector watermark/glow/medal are independently drawn WPF approximations, not original assets. No source-derived rarity classification from GT-610, thus rare/progress previews are presentation-only.
- Mixed-DPI multi-monitor, taskbar sides, foreground switch/minimize, WPF native movement and exact shimmer require owner GUI qualification.

## Acceptance fixtures

1. Compare Classic before/after with identical previews; only anchor/scale should change, not accepted icon/text/seam geometry.
2. Windowed primary and secondary, small/large, maximized desktop, borderless fullscreen CS:GO, exclusive fullscreen CS:GO (classify occlusion).
3. Move/resize foreground app across 100/150% monitors, taskbar bottom/side. Verify right edge, bottom, scale.
4. Modern 1/2/3 wave with unchanged logo continuity, corrected shape, no blue/gold frame, rare shimmer, medal preceding progress number.
5. Previews do not modify GC or SQLite; all earlier tests preserved.

**P003 remains OPEN; no owner acceptance implied.**
