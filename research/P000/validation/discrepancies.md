# P000-C01 — evidence, coverage and discrepancy register

| ID | Topic | Status | Evidence / risk | Bounded resolution |
|---|---|---|---|---|
| C-D01 | Classic smooth-glide preview unlike Steam | DISCREPANCY | owner observation; preview used invented ease | exclude preview as authoritative; inspect native interpolation or real 60+ FPS capture |
| C-D02 | Exact classic stacking offset/arbitration | UNVERIFIED RUNTIME | `StackSize=3` and background-queue comment only | targeted notification manager disassembly or burst capture |
| C-D03 | Classic icon / sound | NOT VERIFIED | layout specifies image panel, no asset mapping | identify game-specific icon source and sound callbacks |
| C-D04 | Physical-unit scaling/clipping | NOT VERIFIED | nominal y74+h23 exceeds panel h94 | runtime window capture and resolution scale test |
| C-D05 | Classic border vs owner visual correction | DISCREPANCY | P000 original style suggests procedural one-unit edges; owner reports accepted popup has no visible border or icon-frame stroke | implement owner-target no visible stroke, preserve original-resource claim, compare actual runtime capture |
| C-D06 | Full 3-slot arbitration | NOT VERIFIED | owner reports top sinks into second and new one rises; original source mentions background queuing, but immediate eviction vs normal expiry is not established | use queued/expiration policy provisionally and seek discriminating capture |
| C-D07 | Text font metrics and baseline | DISCREPANCY | owner rejects WPF first pass size, font, text positions; VGUI FriendsSmall Arial tall12/weight800 does not directly establish WPF glyph bounds | capture/measure original and calibrate typography independently, no arbitrary fitting |

| M-D01 | Modern archive stock equivalence | NOT VERIFIED | supplied build includes suspected customized CSS | compare same UI build against known untouched originals |
| M-D02 | Modern precise icon/title layout | PARTIAL | JS/CSS module found, deeper React component untraced | trace resolved achievement toast component |
| M-D03 | Exact sound mode and user preference | PARTIAL | two WAVs and mode conditional, registry setting | trace imports and actual settings/per-context audio |
| M-D04 | Modern animation appearance in game | UNVERIFIED RUNTIME | frame-state code read; no capture | 60+ FPS runtime video, actual window scale and hook behavior |
| M-D05 | Generalized lifetime claim | CORRECTED | default 5s does not imply universal 5.6s | distinguish type override/resolved callbacks/delays |
| M-D06 | Modern stacking rules across types | PARTIAL | cap 3/1 with display-alone guards | test burst by notification type and overlay mode |
| P-D01 | Original Steam binaries/assets licensing | OPEN | copyrighted proprietary input | reference only metadata/strings/constants, do not redistribute original assets |
| Q-D01 | Independent QA | DEFERRED | one-time single-operator exception | separate independent review at later acceptance gate |

## Scope coverage

- **Verified source-based:** old manifest/ZIP identity, old layout/colors/fonts/settings/strings; modern ZIP identity/build, desktop popup/window states and constants, default notification duration, capacity code, achievement registration and WAV presence.
- **Partially specified:** original audio branch condition, event callback end-to-end, actual stacking arbitration/geometry under concurrent bursts, modified CSS influence.
- **Unverified:** visual fidelity, live notification playback, exclusive fullscreen, original game input/network side effects, independent reviewer, clean modern release parity.
- **Excluded:** production app, GC data provider, CS:GO achievement catalog, original Steam execution.

**Independent QA is not accidentally satisfied by a second pass from the same operator.** No user acceptance is implied by publication.
