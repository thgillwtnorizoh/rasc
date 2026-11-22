# RASC P006-C10 — Classic popup single-owner lifecycle consolidation

Project / Campaign / Actor / Assignment: CS:GO Achievements Resurrection / P006-C10 / Work 2 — Reconstruction Engineering / owner-authorized Classic lifecycle consolidation.
Starting branch: `fix/p006-c05-queue-wave-lifecycle`.
Verified starting SHA: `58e30684578a69f8e6b61df0403245170c772145`.
Feature branch: `refactor/p006-c10-classic-single-owner`.
Published SHA / exact tested SHA: see PR #55 and Actions; **NOT VERIFIED at initial publication**.
Status: ENGINEERING IMPLEMENTATION / OPEN, pending exact-source CI, independent QA and owner's actual CS:GO fullscreen qualification.
Authority: owner explicitly authorized P006-C10 consolidation after repeated Classic crash, with UEP-001 Revision 2.1 requirements.

## Goal, cause, and design boundary
Owner observed six simultaneous achievement unlocks and repeated Classic halfway-popup→sound→fatal app crash. P006-C09 independently verified WPF nested Tick guard but still held three lifecycle representations: scheduler-owned `UnlockEvent`, host-owned `_text` dictionary, WPF-owned `_cards` dictionary. This forces timing-sensitive synchronization and makes another missing-content crash structurally possible if a future update violates that synchronization.

Refactor **only Classic** into a single authoritative scheduler-owned card. Its card carries `UnlockEvent`, cloned immutable title/description/icon content, slide/hold/exit state and deadline. The host is a visual consumer, not a second achievement metadata store. No changes to GC, journal source, earned-state library, Modern wave scheduling, achievement mapping, CS:GO originals, font assets or sound files. Prior historical Classic timing and geometry remain exact owner target, not proof of native Steam interpolation.

## Functional integration
- `ClassicToastScheduler.Enqueue(evt, ClassicToastContent)` atomically accepts complete metadata. Waiting and settled cards each retain full metadata until completion. Source icon byte arrays are cloned at admission and again when materializing the WPF bitmap, never shared mutably.
- `AdvancePresentation(monotonicTime)` is the **only** mutating scheduler update and publishes one complete read-only transaction: card frames with content + legacy unchanged geometry/clip/opacity, ordered one-shot transitions (Started, Settled, ExitStarted, Retired), animation/idle state, next hold deadline. Legacy `Advance` delegates to it for test/API compatibility. Frames remain safe after a card retires.
- Classic host deletes `_text`, the old second ID→content lifetime. It only caches live WPF visual controls, renders snapshot frames, and retires visual controls absent from the snapshot. It does not mutate scheduler in the render loop. Existing P006-C09 reentry guard serializes `Tick()`; callback/timer/resize requests defer to the next pass.
- Sound cue emitted once from scheduler `Started` transition **only if that achievement has a materialized visual in that transaction**, not on WPF control construction, not each animation callback or while queued. This preserves normal first-card audio and prevents repeated cue during WPF churn. Exact accelerated clock jumps suppress cues for cards never painted.
- Exact Classic constants unchanged: max 3 cards, 240×94 DIP each, 282 DIP viewport, 450ms entrance/exit, 6sec hold, no fade, original bottom-edge + stacked occlusion geometry, fonts and audio choices. Modern controller and wave gating left unchanged.
- `ClassicToastContent` read-only title/description + private cloned icon; `ClassicToastFrame.Content` snapshots provide the entire render data. Old hand-authored geometry fixtures may retain null Content for compatibility, but scheduler-created production frames cannot.
- P006-C07 fullscreen stalled-render watchdog and P006-C08 local bounded log capture preserved.

## Focused tests and regression evidence
- New pure Core tests cover immutable snapshot surviving source data mutation, scheduler retirement and clearing; exact-once Started/Settled/Retired transitions; read-only frame lists; duplicate suppression/re-admission; no missing render metadata; large monotonic clock jump; stress 1,3,6,20,100 rapid simultaneous achievements with capped 3 visible and exactly-once audible admissions.
- New real WPF scenario `classic-transactional-effects` uses six rapid previews, simulated monotonic time, duplicate frame requests and a real HWND/card tree to assert exactly six sound callbacks, max 3 rendered card elements, full cleanup, no content-invariant logs. Existing `classic-reentrant-six-burst`, `classic-render-stall`, Classic-only HWND, Modern and library/P006 wave smokes remain and continue to run.
- Test commands: existing Windows GitHub Actions `.github/workflows/windows.yml` core xUnit, WPF real-window smoke, Windows x64 build/package/upload. Exact head SHA, counts and artifacts **NOT RUN/NOT VERIFIED at initial checkpoint**. Historical failures preserved, no test removal.

## Confidence/discrepancy
ENGINEERING/RECONSTRUCTED: direct source ownership cleanup and updated deterministic acceptance tests. CONFIRMED APP RUNTIME: earlier user-provided C09 `KeyNotFoundException` on missing `_text` after six-award batch. P006-C10 itself is NOT VERIFIED in physical legacy CS:GO, WPF dispatcher tests do not prove fullscreen GPU presentation. Owner decides acceptance.
Limitations: Visual cache remains a separate WPF UI implementation (not authoritative state); frame data stays immutable. Native compositor starvation, taskbar DPI and lost journal events are separate qualified boundaries. If `_playCue` itself throws, this refactor does not modify the notification sound exception policy.
Security/privacy: no new raw log or original game file writes; no unsafe native instrumentation.

## Updated records / reproduction
This checkpoint; PR #55; Windows Actions run once complete. Source and tests are in the same feature commit. On Windows build: start Classic monitor in legacy fullscreen CS:GO, earn six achievements quickly, inspect no freeze or crash; check per-award cue count and 3-card cap, mixed DPI and alt-tab; examine `%LOCALAPPDATA%\\RASC\\logs\\rasc-*.log` only if necessary and redact private stack paths before sharing.
Next bounded task / Actor / Dependencies: Work 2 exact-source CI analysis/merge once green; Work 1 independently verify snapshot invariants and no change to Classic visual/timing; owner perform actual fullscreen qualification. 
Subsystem: OPEN — OWNER-ACCEPTED not claimed.
