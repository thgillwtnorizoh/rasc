# RASC / P006-C05 — Separated achievement FIFO and popup presentation timing

**Project / Campaign / Actor / Assignment:** CS:GO Achievements Resurrection / P006-C05 / combined owner-authorized Work 1 acceptance fixtures and Work 2 reconstruction implementation, Windows tests and publication in the same chat / owner-specified Classic independent slots and Modern fixed 800 ms immutable waves with no unseen queue expiry.

**Status:** **VERIFIED CHECKPOINT** for Core and real-WPF synthetic Windows delivery. **P006 subsystem OPEN**; owner physical in-game acceptance and independent second-actor compiled production QA **NOT VERIFIED**.

## Starting SHA / Published SHA / Exact tested SHA

- **Starting branch:** `fix/p006-c04-modern-late-admission-two-line`.
- **Starting SHA:** `68a9f5e8504708d34d5edc153b6e449e6b953bed` (owner's observed C04 branch).
- **Exact tested production SHA:** `61f1433d877edcda628fe93d3c9d625bf1f8173a`.
- **Work 1 acceptance fixture published after tested SHA:** `78d88818b1392ed6c3e5a549b0efd29a90e91c42`, documentation only.
- **This checkpoint publication SHA:** this is an additional documentation-only commit after tested source. See PR #49 commit history for exact published SHA.
- **Branch / draft PR:** `fix/p006-c05-queue-wave-lifecycle`, [PR #49](https://github.com/thgillwtnorizoh/rasc/pull/49), stacked on unmerged draft #48/#47/#46/#45/#44/#43 and earlier dependencies. `main` remains `425444dc7440163ec8b3f74ee0ad9c360d5896fd`. No merges, force-push or prior test deletion.

## Objective / supported profile / changes and integrations

Owner's direct requested model: **Queue decides achievements/order; popup mode decides appearance and expiry.**

**Modern — production source-confirmed events:**
- New `ModernAchievementWaveQueue` owns FIFO and dedup of `UnlockEvent`s independently from WPF animation. The immutable first observation `ObservedAt` from the GC source reconciliation opens a **fixed [T,T+800ms) collection window**, even when the UI thread receives the event much later. The group contains at most three achievements; overflow starts another FIFO group; different later arrival cohorts cannot be silently combined when a waiting wave finally appears. Abnormal backward source-clock signals are isolated rather than merging across time discontinuities. Waiting entries have **no expiration**.
- `ModernToastHost` uses this queue exclusively for genuine GC events. Once a group is ready and the presenter idle, it passes the whole group into `ModernToastScheduler.BeginSealedWave` at the actual UI admission time. Active waves are **immutable even with one card**: C04 late-admission 3-second path and C03 45ms production collection were removed. All wave cards share 300ms entrance, 5000ms hold and 300ms exit; a later signal does not extend an older wave. Overflow next wave begins when the previous wave is retired and has its own **full 5.6s** duration.
- `ModernToastScheduler.Advance` no longer loops through old queued wave deadlines to catch up to wall-clock time. If WPF is delayed substantially, the previously active wave can retire, but only **one** queued replacement may enter, starting NOW, so additional unseen waves cannot be skipped.
- Host preserves the 250ms backup wake and precise WPF transition timer; both request UI work on the dispatcher, neither changes source award truth. Local icons, historic English names/descriptions, curated 63 Rare / 104 Normal classification, font and two-line Argyle-style descriptions and existing sound cues remain unchanged.
- Pure *synthetic previews* are not source-confirmed awards and use the archived immediate visual fixture path, so baseline WPF geometry, icons, sound and CodePen regressions remain compatible. This is an intentional distinction from live 800ms batching.

**Classic:** Existing independently expiring 3-slot Classic presenter is retained. It admits FIFO replacements when slots become available, without Modern's 800ms collection or shared wave lifecycle. Classic's existing 450ms slide in + 6s hold + 450ms slide out, stable text and artwork are unchanged; its independent long-dispatcher-stall qualification is a remaining gate, not claimed solved here.

**Scope exclusions:** no original achievement rule reconstruction, dynamic Steam rarity, live 25/50/75% progress generation, Valve art/text redistribution, GC/Steam writer changes, SQLite data migration, user game modification or arbitrary visual timing changes.

## Evidence / confidence classification

- **OWNER-DIRECTED ENGINEERING acceptance:** 2026-11-21 direct instruction: distinct FIFO vs modal timing, Classic independent slot up to 3; Modern first signal 800ms collection, three max, immutable common waves and no old awards mixed with new awards. Full independent expectations: `research/P006/fixtures/C05-queue-vs-presentation-contract.md`.
- **CONFIRMED PRIOR CODE DEFECT:** C04 Modern scheduler retroactively expired multiple queued unseen waves inside one `Advance(now)` loop, and C04 live host let later GC events join the active wave for up to 3 seconds, contradicting new owner requirements.
- **VERIFIED COMPONENT (development):** production Core + WPF smoke on exact source SHA and Windows CI. Not physical gameplay, not independently controlled second-actor production QA.
- **UNVERIFIED ORIGINAL:** authentic Steam internal popup queue/wave duration; owner intentionally chose 800ms as engineering estimate, not recovered Valve constant.
- **DISCREPANCY:** accepted 3-card cap and 5.6s wave guarantee mean overflow must wait, sometimes into subsequent gameplay rounds. This is unavoidable without dropping awards, reducing the show period, or raising capacity; the new design removes *extra* latency and cross-cohort mixing, not this physical lower bound.
- **Known unfinished reliability boundary:** existing `UnlockEventStore.MarkDelivered` records delivery on UI dispatch, not proven visual completion. Crash/restart between queue admission and showing can still lose a pending presentation; durable presentation acknowledgment is a future bounded task. A WPF `Show()` call is also not proof of physical fullscreen compositor display.

## Tests — exact source, commands, environment, pass/fail counts

**Exact tested source:** `61f1433d877edcda628fe93d3c9d625bf1f8173a`. **Environment:** GitHub Actions `windows-latest`, .NET 10.
**Entire workflow:** [Windows CI run 44850718715](https://github.com/thgillwtnorizoh/rasc/actions/runs/44850718715), **SUCCESS**.

- `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"` — **251 passed / 0 failed / 0 skipped**. +5 over previous 246, including 800ms [0,799,800] boundary, capacity overflow/different cohorts, 15s delayed UI with original receipt time, abnormal backwards clock rollback isolation and duplicate ID handling. Old historical Modern tests preserved, with a few assertions explicitly amended for owner's new no-unseen-expiry acceptance; no test suite deleted.
- `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental` — **PASS**.
- Existing **10/10 real-WPF** profile, Classic, Modern rare, two-card visual, progress-preview, font/sound, CSS typography, queued sound and installation recovery — **PASS**.
- Existing P006 current-state/achievement-library real-WPF smoke — **PASS**.
- P006 real-WPF *3+2 live awards* smoke — **PASS** with fixed 800ms original signal collector, two separate waves, original-local synthetic icons/text/curated rare, no sixth award.
- Repurposed P006 real-WPF source-cohort/wave smoke — **PASS**: one card starts after 800ms; later +1.45s and +1.65s source awards do **not** join it but form a second 2-card wave; still later +2.5s award forms a third wave; all three present automatically without any fifth signal, with WPF timer/render callbacks suppressed and exactly one cue per wave. Argyle two-line/short-line typography still tested.
- Chrome original CodePen reference frame capture — **PASS**; no modification to golden rare glow, compositor visual asset path or source CSS fixture.
- Self-contained `win-x64` publish — **PASS**, [Windows ZIP artifact 14685436175](https://github.com/thgillwtnorizoh/rasc/actions/runs/44850718715/artifacts/14685436175), artifact SHA-256 `cb8041be5655a32ca29812b0c91aa4d6a6853b4a51534bb881fea3853c689b16` as supplied by GitHub Actions.
- Built-in, editable `Data/achievement-definitions.v0.2.csv` shipping check — **PASS**, 167 rows.
- **NOT RUN:** real owner post-C05 CS:GO gameplay, GC source write timing capture, physical onscreen frame visibility qualification, Classic full 12s stall case under real game, independent second-actor QA, durable post-render acknowledgment integration.

## Coverage gained / remaining requirements

**Coverage gained:** clean separation of source event FIFO + collection from immutable wave animation; 800ms RASC signal-cohort boundary, capacity-3 and cross-cohort isolation, no live mid-wave admission, stable group audio/FIFO, long-UI-stall queued-wave non-expiration, Windows visual regression retention.

**Remaining:** independent physical fullscreen and timestamped source-to-frame QA; final source event-to-display diagnostics; durable presentation acknowledgments/at-least-once crash recovery; original rule/Steamworks historical 167-ID verification; progression source binding before real milestone notifications; owner acceptance.

## Discrepancies, limitations, blockers and repro

- The first eligible Modern popup is intentionally withheld **800ms** to allow same-cohort awards. This is the only collection delay before free-slot presentation; `FileSystemWatcher` plus 1s reconcile and a busy UI can contribute additional observed latency.
- A 4th+ award necessarily waits. The only way to provide less latency than capacity permits is owner-approved smaller hold, more slots or event dropping; none were enacted.
- A permanent WPF UI deadlock cannot be repaired by a ThreadPool wake that posts to that same dispatcher; physical Windows/fullscreen testing still required.
- No changes to original Steam/GC file writes, no fabricated rarity or unlocks, no automatic replay of baselined awards.
- Evidence: `research/P006/fixtures/C05-queue-vs-presentation-contract.md`.
- Reproduce with CI commands above, `dotnet run --project tests/Rasc.Desktop.Smoke/Rasc.Desktop.Smoke.csproj --configuration Release -- p006-live-toasts` and `-- p006-late-admission` on Windows. Both latter named historical smoke entrypoints now verify the new sealed wave policy.
- Owner physical QA: start monitoring the actual game and earn 1–3 achievements within an 800ms RASC detection window, then an extra award after 800ms. Check that only first group appears together, second never joins the active wave and later appears in its own wave, with no unexplained skip or loss. Capture time-correlated GC data/screenshots before claiming the real bug solved.

**Next bounded task / actor / dependencies:** Owner fullscreen run and Work 1 independent timestamped source→UI observation. If events are still visibly partial, Work 2 instrument source/queue/first-frame/retirement transitions and address *presentation acknowledgment*, not more arbitrary timer changes. Keep P006-C05 draft until independently evaluated.

**Subsystem: OPEN | OWNER-ACCEPTED: NO.**
