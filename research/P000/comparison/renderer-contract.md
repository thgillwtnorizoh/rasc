# P000-C01 — renderer-neutral contracts and fixture plan

These contracts specify **what to preserve**, not how WPF must be engineered.

| Contract | Classic May 2023 | Modern supplied build |
|---|---|---|
| Basic geometry | panel 240×94 VGUI units, icon 64×64 | desktop 283px popup width; window height type-dependent |
| Background | procedural vertical gradient and four edges | radial gradient and optional blue hover layer |
| Typography | Arial FriendsSmall 12/800 | hashed CSS modules; achievement-specific resolved typography needs follow-up |
| Motion | vertical slide configured; 450ms in/450ms out | desktop per-window MoveTo with 300ms phase in/out |
| Steady interval | 6.0s configured | default 5000ms; type-dependent override/resolution |
| Capacity | 3 before background queuing (config) | normal 3; alternative mode 1; display-alone guards |
| Audio | unverified | sound assets and ToastAchievement routing confirmed in supplied bundle |
| Event text | unlock and progress variants localized | achievement notification registry located; content variants need further mapping |
| Rendering context | VGUI/client/overlay path unresolved | desktop multiwindow, gamepad/VR distinct |
| External mods | historical pinned resources | user-supplied modern customization must be separated |

## Event/API-independent model (ENGINEERING, not Valve behavior)

Future application should accept an immutable achievement-event record with an event ID, originating source, event time, game/app identifier, achievement key, optional title/description/icon reference, event kind (unlock, progress), and optional current/maximum. Preserve known/unknown fields and provenance. Deduplicate **upstream** of renderer profiles; never let a visual profile invent events. Queue policy belongs to profile, not GC persistence.

Suggested interfaces/concepts for later design only: `IAchievementEventSource`, `AchievementEventReconciler`, `INotificationProfile`, `INotificationScheduler`, `IToastPresenter`, and `IAudioCueResolver`. No C# interface signatures or production code committed in C01.

## Fixture matrix for development and eventual independent QA

| Fixture | Original expected behavior | Current status |
|---|---|---|
| C1: one unlock | classic VGUI unlock headline/body, 240×94 layout; configured 0.45/6/0.45 | source verified; runtime unverified |
| C2: one progress callback | progress headline and `cur/max` body | source verified; provider availability not verified |
| C3: three rapid unlocks | classic visible-stack limit 3; exact spacing/animation unknown | partial |
| C4: six rapid unlocks | classic queues above limit; exact arbitration unknown | partial |
| M1: one desktop unlock | modern popup 283px wide; 300ms phase transitions, per-window MoveTo, default duration category | source verified; visual timing unverified |
| M2: six desktop unlocks | max 3 active normally; FIFO array with display-alone exceptions | partial |
| M3: reduced-capacity desktop | 1 active slot in alternate UI mode; exact predicate user meaning needs trace | partial |
| M4: sound enabled/disabled | source requires user settings and conditional sound route | partial |
| M5: gamepad/VR/in-game | separate presentation paths, no desktop-motion equivalence asserted | unverified |
| X1: missing icon, malformed text, very long title | renderer must be safe and bounded | ENGINEERING proposal only |
| X2: application restart, burst and file-rewrite | durable upstream event reconciliation, no false repopups | future P001; NOT TESTED |
| X3: monitor DPI, borderless/fullscreen | independent window qualification per display mode | UNVERIFIED RUNTIME |

## Stop conditions / anti-overclaim

Do not claim exact original easing, queue spacing, appearance under render-hook injection, unmodified stock modern equivalence, or audio heard in a specific gameplay state until measured. Only an owner may accept these disclosed gaps. Static reference coverage supports a bounded **first implementation**, not full historical rendering fidelity.
