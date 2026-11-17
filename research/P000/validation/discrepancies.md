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
| C-D08 | Classic screenshot headline alignment versus archived VGUI label | DISCREPANCY | both owner-supplied screenshots show title/body aligned at near-identical left x; May 2023 resource marks title alignment center and description north-west | implement explicit screenshot-target left alignment, preserve resource center evidence, resolve with build-matched capture |
| C-D09 | Owner-researched Helvetica Regular versus May 2023 FriendsSmall Arial weight800 | DISCREPANCY | owner reports Helvetica Regular without build-matched font provenance; pinned steamscheme.res states Arial 12/800 | prefer Helvetica Regular in owner-target C02 build, Arial fallback, approximate 11.5 WPF DIP; obtain version-matched original evidence before source-equivalence claim |
| C-D10 | Apparent inter-popup gap in owner RASC screenshot | RECONSTRUCTED; GUI QA PENDING | screenshot3 row~83/84 is lower card's gradient, icon begins y98 after original 14px inset; no demonstrated 14px transparent gap | preserve source y14, pixel-grid continuity tests for fractional DPI, qualify actual desktop seam and visual expectations |
| C-D11 | Owner's 3-card top/middle seam persists (middle/bottom flush) | DISCREPANCY, C03 DEVELOPMENT CORRECTION PENDING USER QA | 94 DIP is 117.5 physical pixels at125% DPI; first seam snaps to118 while second seam rounds differently; owner DPI not measured; C03 adds matched pixel-aligned opaque backplates behind unchanged cards | compare owner C03 snapshot/recording at 3 settled cards, 100/125/175% DPI when available; keep visual design frozen |
| C-D12 | Classic custom FPS target and high-resolution scaling | DEVELOPMENT VERIFIED, OWNER GUI QUALIFICATION PENDING | C04 105/105 tests; presets plus custom 15–240 and display-sync, independent monotonic timeline, per-monitor DPI manifest, 1080/1440/2160 logical-size model; source UI never measured on owner 1440p screen | test saved control behavior, custom value, display change, high-DPI card seam and physical FPS differences; no original Steam FPS equivalence asserted |



| M-D01 | Modern archive stock equivalence | NOT VERIFIED | supplied build includes suspected customized CSS | compare same UI build against known untouched originals |
| M-D02 | Modern precise icon/title layout | PARTIAL | JS/CSS module found, deeper React component untraced | trace resolved achievement toast component |
| M-D03 | Exact sound mode and user preference | PARTIAL | two WAVs and mode conditional, registry setting | trace imports and actual settings/per-context audio |
| M-D04 | Modern animation appearance in game | UNVERIFIED RUNTIME | frame-state code read; no capture | 60+ FPS runtime video, actual window scale and hook behavior |
| M-D05 | Generalized lifetime claim | CORRECTED | default 5s does not imply universal 5.6s | distinguish type override/resolved callbacks/delays |
| M-D06 | Modern stacking rules across types | PARTIAL | cap 3/1 with display-alone guards | test burst by notification type and overlay mode |
| M-D07 | Modern C01 independent admissions and card content | OWNER-REPORTED DISCREPANCY; C02 development correction | four owner screenshots and explicit shared below-taskbar waves/no status text; C01 contradicted | C02 grouped wave tests and owner Windows capture; preserve first failed CI |
| M-D08 | Shared modern faint watermark across 1/2/3 cards | RECONSTRUCTED; EXACT CSS NOT VERIFIED | P000 records top/middle/bottom Steam logo positions; owner screenshots; original dogbones.zip unavailable now | WPF shared clipped vector approximation; re-examine hashed CSS and owner capture |
| M-D09 | Rare achievement gold icon shine | OWNER-REPORTED; TOAST SOURCE NOT VERIFIED | related Steam Community achievement glow CSS exists; toast reuse unproven | don't infer live rarity from timestamps; trace original achievement React/CSS and metadata |
| M-D10 | Modern numeric-progress variant | OWNER-REPORTED; LIVE PROVIDER UNSUPPORTED | owner shows progress numbers/bar; GC saves no progress callback | display-only progress fixture; future reliable event source |
| M-D11 | Modern wave admission and taskbar hiding | ENGINEERING; RUNTIME NOT VERIFIED | shared under-taskbar origin and waited arrivals; screenshots cannot distinguish whole-wave vs first-item barrier | owner 1/2/3/6 frame capture and actual taskbar test |
| M-D12 | Incorrect C02 Steam watermark geometry | OWNER-REPORTED DISCREPANCY; C03 development adjustment | owner confirms stack-aware placement but rejects circle/rod shape; exact original image unavailable | C03 improved vector wheel/rod/hub only; compare against source-matched logo and owner 1/2/3 screenshots |
| M-D13 | Modern ordinary/rare icon framing | OWNER-REPORTED DISCREPANCY; C03 development adjustment | no blue ordinary image border; rare is animated gold halo, not solid yellow border; Community animation visually same per owner | C03 removes solid frames, adds rotating highlights; source reuse/raster not yet verified |
| M-D14 | Missing progress medal before numeric | OWNER-REPORTED DISCREPANCY; C03 development adjustment | owner screenshot shows tiny icon left of 19/50 text | WPF medal added as vector; compare actual icon dimensions/alignment on Windows |
| M-D15 | Foreground app / fullscreen anchor for BOTH profiles | OWNER-REPORTED DISCREPANCY; C03 development adjustment, RUNTIME NOT VERIFIED | previous C02 anchored to primary work area; owner wants right-bottom foreground app, display edge for fullscreen | Win32 monitor/window bounds + PMv2 DPI in C03; QA windowed, borderless, mixed-DPI, taskbar sides, foreground changes |
| M-D17 | C03 anchored inside every normal window | CONFIRMED OWNER REGRESSION; C03-C01 development correction | owner saw tiny popups inside ordinary active application; fullscreen only was intended | restore P002/C02 desktop work-area/auto size for normal/maximized; only fullscreen monitor override; owner runtime QA pending |
| M-D18 | C03 Steam watermark was inaccurate geometry | CONFIRMED OWNER DISCREPANCY; source-geometry correction | C03 invented wheel/rod; Simple Icons CC0 genuine Steam mark used with unchanged 1/2/3 placement | compare real original toast opacity and raster with screenshots |
| M-D19 | C03 rare gold glow style was wrong | CONFIRMED OWNER DISCREPANCY; revised development reconstruction | five-dot orbit replaced with 6s/18s rotating conic gold under fixed square mask; original desktop source reuse still unproven | owner visual test and targeted original CSS/mask analysis |
| M-D16 | True exclusive fullscreen WPF occlusion | UNVERIFIED RUNTIME / PLATFORM LIMIT | a topmost desktop WPF window is not an injected overlay and can be hidden by exclusive game compositor | owner check exclusive vs borderless; separate system design/owner authorization if full exclusive coverage required |
| P-D01 | Original Steam binaries/assets licensing | OPEN | copyrighted proprietary input | reference only metadata/strings/constants, do not redistribute original assets |
| Q-D01 | Independent QA | DEFERRED | one-time single-operator exception | separate independent review at later acceptance gate |

## Scope coverage

- **Verified source-based:** old manifest/ZIP identity, old layout/colors/fonts/settings/strings; modern ZIP identity/build, desktop popup/window states and constants, default notification duration, capacity code, achievement registration and WAV presence.
- **Partially specified:** original audio branch condition, event callback end-to-end, actual stacking arbitration/geometry under concurrent bursts, modified CSS influence.
- **Unverified:** visual fidelity, live notification playback, exclusive fullscreen, original game input/network side effects, independent reviewer, clean modern release parity.
- **Excluded:** production app, GC data provider, CS:GO achievement catalog, original Steam execution.

**Independent QA is not accidentally satisfied by a second pass from the same operator.** No user acceptance is implied by publication.
