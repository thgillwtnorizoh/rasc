# Modern Steam toast — supplied UI build `11229305`

**Authority:** exact hashed owner-supplied `dogbones.zip`, specifically `steamui/sp.js`, `steamui/css/sp.css`, `steamui/index.html` and sound resources. **Confidence:** CONFIRMED IN SUPPLIED CLIENT. Stock equivalence UNVERIFIED.

## Desktop popup construction

The JS module exporting `DesktopToastPopup`, `DesktopToastBackground` and `DesktopToastContainer` maps to hashed CSS classes. Desktop toast code specifies `he=283` (popup width) and creates a distinct composited, initially hidden, borderless browser/window, using `SteamClient.Window.MoveTo`, `ShowWindow`, and `HideWindow`. It creates a portal containing the popup background and notification content. The **283px** wrapper width is confirmed; `DesktopToastContainer` has CSS `width:283px;height:70px`. However, the popup window's height is obtained from a toast-type-dependent height helper (`m.xW(notification)`), so **70px must not be asserted as every achievement's runtime window height**.

Desktop background CSS for the popup uses a radial gradient from `#23262E` to `#0E141B`. A second blue-tinted `::before` layer appears on hover; original CSS also sets relative Steam-logo/background positioning for top/middle/bottom stack entries. Do not infer final device-pixel measurements without DPI/window scale checking.

## Timing and positioning (desktop JS state machine)

The desktop state machine explicitly defines `NotShown=0, FadingIn=1, Showing=2, FadingOut=3, Finished=4`. Its constants are `K=300` ms (entrance), `V=300` ms (exit), `he=283`, with `X=0`; `performance.now()` drives state transitions on animation frames. Desktop screen positions come from the usable monitor rectangle, insets, selected corner, measured toast heights, and fraction complete. Movement uses `Math.ceil(height*fraction)` and `Math.floor(height*(1-fraction))` for per-toast stacking offsets, then actual `MoveTo` calls. In other words **this is NOT simply CSS smooth sliding**. The code keeps the target popup's last X/Y coordinates to suppress redundant moves.

The default notification duration constant in notification registration is `G=5*1e3` milliseconds. It is assigned as `nToastDurationMS=typeOverride||G`; individual types, resolution callbacks, settings, game modes or focus may alter effective behavior. **Do not report a guaranteed 5.6-second on-screen lifetime for every achievement**: entrance, steady interval, exit, scheduling delays and resolution rules vary.

Normal desktop selection is `St=ct?1:3` where `ct` is a mode predicate; three active toast slots are allowed normally and one in the alternate mode. The selection loop uses `PopNextToastNotification` and guards `BAnyToastDisplayAlone`/`BNextToastDisplayAlone` before filling capacity. A FIFO-like array is evidenced by `m_rgNotificationToasts.push` and `shift`. All types are **not guaranteed** to stack together: some display alone, are suppressed, or are held for additional data. The 3-second pending server-toast delay (`Te=3*1e3`) is a *conditional* server-data path, **not a general three-second delay for local achievements**.

## Achievement classification and sound

Achievement notification registry uses `showToast:Ot("achievement_notification_toast")`, `playSound:pe`, `sound:P.PN.ToastAchievement`, and `bEnableInReducedUI:true`. The application also exposes `achievement_notification_sound` settings. Sound mapping uses a mode conditional for `ToastAchievement`, with two distinct bundled sound assets: `sounds/desktop_toast_default.wav` and `sounds/deck_ui_achievement_toast.wav`. The precise symbol-to-mode correspondence should be rechecked against imported module IDs before claiming it is universal across fullscreen/overlay contexts. **No sound is unconditional**.

## Customized source caveat

`steamui/skin.css` and `steamui/faceit_stats.css` are plausible external modifications; stock status of the remaining JS/CSS is not proved. No **direct** occurrence of desktop popup class `_1OOQPJx0nCNP9ME9toCmV7` was found in `skin.css`, but this does not exclude generic inherited, injected or runtime overrides. A version-matched unmodified build comparison and frame capture remain pending.

## Important source observations requiring follow-up

- The animation state-machine fraction appears linear at the coordinate calculation level; whether window/compositor transition behavior, frame scheduling or easing alters perception is **UNVERIFIED RUNTIME**.
- In-game and VR/gamepad notifications have different code paths; don't collapse them into the desktop profile.
- The native `gameoverlayui.dll` and render hooks were supplied, but no full binary-disassembly pass or runtime qualification is claimed for this C01 checkpoint.
