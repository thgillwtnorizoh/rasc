# RASC P006-C04 — Modern prompt late admission and two-line original-reference text

**Project / Campaign / Actor / Assignment:** CS:GO Achievements Resurrection / P006-C04 / combined owner-authorized Work 1 source/fixture evidence and Work 2 production and test development / owner follow-up to live achievement timing and Modern description layout.

**Status: VERIFIED CHECKPOINT** — Windows development Core, real-WPF synthetic qualification and full retained regression passed. Real owner CS:GO fullscreen post-fix and independent second-actor QA are **NOT VERIFIED**. **P006 subsystem OPEN.**

## Starting / published / tested SHA

- **Starting branch and SHA:** `fix/p006-c03-localization-burst-progress-gate` at `7e2f1b5b5cd397bfc277ed0a0a08498df52e9dda` (C03 validated names/local JPEGs, historical English text and atomic GC snapshot batching).
- **Exact source tested and published in Windows CI:** `bc71cadedae8628dec027e6b3db080686c303aad` on `fix/p006-c04-modern-late-admission-two-line`.
- **Post-test Work 1 fixture documentation-only SHA:** `4fccfa711842729c514902b838ae88213ccfaf43`; this checkpoint is a further documentation-only commit after source verification. Look up PR #48 commits for its final SHA.
- **Draft PR:** [#48](https://github.com/thgillwtnorizoh/rasc/pull/48) stacked on draft #47 (and upstream #46/#45/#44/#43). No main merge, force-push or source rewriting.

## Objective, supported profile and functional integration

**Observed problem 1: late popups although a 1- or 2-card wave still had space.**

- **Confirmed code cause:** a later GC file write after initial 45ms Modern batch entered `ModernToastScheduler.Enqueue`, which always queued it behind the active full 5.6s wave **regardless of current card count**. This is separate from C03's single-snapshot atomic batch correction and accounts for the behavior expected in the owner's screenshot sequence (*Shot With Their Pants Down*, *The Cleaner*, later *Rite of First Defusal*). The screenshots lack clocked GC write traces; they support the symptom, not the exact original write intervals.
- **New production behavior (ENGINEERING):** `ModernToastScheduler.TryAdmitLiveDuringHold` permits **source-confirmed live GC** events arriving in distinct reconciliations to enter an existing stack during a bounded **3-second window** from first-wave admission, only while **fewer than three cards** are visible, before wave exit, and with no previously queued FIFO award. Original immutable-wave preview and scheduler `Enqueue` contract remains unchanged.
- **Visually coherent movement:** the new card rises from the same bottom origin over **300ms**; earlier visible cards move up one slot smoothly while remaining visible, preserving the contiguous shared gradient/watermark geometry. Existing cards do not re-enter or trigger duplicate audio. A live late card receives its full 300ms entrance and 5000ms hold; the common wave expiry is extended to guarantee that. A fourth card or out-of-window arrival waits FIFO for an automatic next wave and one new cue. The possible extension of old cards' residence time is explicitly an **owner-requested engineering tradeoff**, not an assertion of Steam's original algorithm.
- **Fullscreen resilience:** a **ThreadPool 250ms watchdog**, armed only while a Modern wave or its initial batch is pending, posts bounded UI work through the dispatcher. It protects against an idle/foreground WPF `DispatcherTimer` or `CompositionTarget.Rendering` wake being suppressed. It cannot execute WPF on the worker thread, cannot unfreeze a permanently blocked dispatcher, and does not decide whether any achievement is earned. Existing precise WPF deadline timer remains primary; watchdog is an independently clocked fallback. Under a permanently stopped UI dispatcher, no WPF toast can progress.

**Observed problem 2: every Modern description truncated to a single line.**

- Original-reference screenshot in this chat: Steam-style *The Argyle Sap* two-line text; the owner requested different vertical layout from short one-line cards.
- `ModernToastCard` now **measures actual WPF wrapping** for the selected font family and typography profile (not a fixed character count). Short single line preserves the previous owner-accepted headline **y18** / description **y36**. Two-or-more line content wraps to at most **two 16-DIP lines**, with headline **y10** / description **y26**, height 32 and existing character-ellipsis trimming. Original 283×70 overall card, icon slot, colors, font choices, rare glow, title widths, watermark, sound and normal/rare policy remain unchanged.
- Changing font/profile recalculates wrapping. The numeric progress toast remains unaffected; no live progress threshold logic is activated by this task.

**Explicit exclusions:** no Valve asset redistribution, GC/Steam writes, automatic awards or changed original achievement rule interpretation; no altered rarity v0.2 values, catalog rows, fixed three-card capacity, main branch or prior historical test suites.

## Evidence and confidence classification

- **CONFIRMED OWNER RUNTIME OBSERVATION:** supplied current game screenshots show restored real titles, descriptions and icons, but *The Cleaner* from the *Shot With Their Pants Down* game-chat burst is observed appearing with a later *Rite of First Defusal* toast. Owner's *The Argyle Sap* cropped reference has two visible description lines.
- **CONFIRMED PRODUCTION CODE CONSTRAINT:** C03's `ModernToastHost.Enqueue` and `ModernToastScheduler` always queued out-of-window events behind any active wave even when a live stack had spare capacity; `ModernToastCard` used `TextWrapping.NoWrap` and only one text line. These are independently inspectable code facts.
- **RECONSTRUCTED BEHAVIOR:** later GC file writes may be separated enough to miss the initial 45ms batch; exact owner GC event timestamps and dispatcher health remain unrecorded.
- **ENGINEERING CHANGE:** 3s late admission, smooth contiguous shifting, full later-item hold, two-line measured card layout, auxiliary wall-clock watchdog; no claim of reproducing every original Steam scheduling detail.
- **VERIFIED COMPONENT:** 246 Core, 10 legacy real-WPF smoke, two earlier P006 smoke tests and new real-WPF late admission and measured typography scenario at exact source below.
- **UNVERIFIED RUNTIME:** physical game owner post-fix, exact original Steam text raster/compositor equivalence, UI response under permanently blocked dispatcher, independently conducted production QA.

[Work 1 fixture and visual/behavior acceptance register](../../research/P006/fixtures/C04-late-admission-argyle-description.md) remains distinct from tested code.

## Tests / exact environment / CI artifacts

**Exact CI run:** [GitHub Actions Windows workflow 44723494051](https://github.com/thgillwtnorizoh/rasc/actions/runs/44723494051), runner `windows-latest`, .NET 10, head SHA `bc71cadedae8628dec027e6b3db080686c303aad`, overall **SUCCESS**.

- `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"`: **246 passed / 0 failed / 0 skipped**. The preserved 244 historical tests remain; two new focused tests prove staggered live admission, 70-unit contiguous geometry, cue non-replay, capacity-3 FIFO, 3-second cutoff and unchanged baseline preview-wave behavior.
- `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental`: **PASS**.
- Existing **10/10** real-WPF scenario gate: **PASS** (profile transitions, Modern rare, Classic, typography, progress preview, wave sound and installation).
- Existing P006 achievement-library real-WPF smoke: **PASS**.
- Existing P006 original-local named/JPEG 3+2-wave smoke: **PASS**.
- **New real-WPF `p006-late-admission` smoke: PASS.** Simulates separate live award UI callbacks at about 0, 1, 1.45 seconds; 1→2→3 visible cards and no repeated first cue, fourth forced into FIFO next wave. Verifies next wave and one new cue **without fifth award**, even after removing ordinary WPF `DispatcherTimer` handlers and rendering subscriptions during hold (watchdog fallback), as well as actual short y18/y36 and two-line y10/y26 positions, height32, wrap/truncation and font-change reflow.
- Chrome original CodePen rare-glow reference screenshot: **PASS**; no intended rare visual changes.
- Self-contained Windows x64 publish: **PASS**; editable 167-row v0.2 CSV verification **PASS**.
- **Windows release artifact:** [Rasc-Windows-x64.zip, ID 14623042227](https://github.com/thgillwtnorizoh/rasc/actions/runs/44723494051/artifacts/14623042227).
- Chrome/rare visual evidence artifact: [ID 14623516175](https://github.com/thgillwtnorizoh/rasc/actions/runs/44723494051/artifacts/14623516175).
- **NOT RUN:** actual owner game with new code, live GC capture with reliable per-update timing, independent second-actor compiled QA, physical screenshot pixel-perfect comparison against Argyle Sap on Windows.

**Historical tests were not deleted.** Core and WPF tests verified source at the exact tested SHA, not the documentation-only commits following it.

## Coverage gained / remaining requirements

**Gained:** separate-source-file live timing no longer automatically forces every later award to wait a full wave if room remains; bounded late admissions preserve exactly 3 max and FIFO, independent watchdog prevents WPF timer/render stalls in synthetic reproduction, two-line descriptions with distinct vertical layout and actual font-aware detection.

**Remaining:** owner Windows original-game verification of the specific lag and typography, independent runtime QA and screenshot comparison, original full 167 shipped achievement schema/rules, real stat-to-progress mapping and dedup for 25/50/75 milestone suggestions. Actual Steam compositor/runtime timing equivalence is outside this slice.

## Discrepancies / limitations / blockers

- The 3-second late admission window and extra dwell for previously visible toasts are deliberate engineering choices, not archived Steam facts. Events after 3s or overflow beyond three still queue; expected.
- The app's primary/backup timers still need a pumping WPF dispatcher. Physical exclusive fullscreen on the owner's computer could reveal further environmental differences.
- The user-provided reference does not independently establish exact original CSS computed positions. Pixel equivalence and truncation require owner review.
- The C03 historical 167 localization schema and user-curated rarity are unchanged; full official original award-rule correctness remains unverified.

## Updated records / reproduction / next task

- `src/Rasc.Core/ModernToastScheduler.cs`: late-arrival admission, per-member independent entry positions, bounded hold extension.
- `src/Rasc.Desktop/ModernToastHost.cs`: live-only admission option, independent wake fallback.
- `src/Rasc.Desktop/ModernToastCard.cs`: font-measured one/two-line presentation.
- `tests/Rasc.Core.Tests/ModernToastWaveTests.cs`: 2 new edge/behavior tests.
- `tests/Rasc.Desktop.Smoke/P006LateAdmissionSmoke.cs`, `tests/Rasc.Desktop.Smoke/Program.cs`, `.github/workflows/windows.yml`: real-WPF test and CI gate.
- `research/P006/fixtures/C04-late-admission-argyle-description.md`: owner evidence and independent acceptance fixtures.
- **Reproduce:** run exact CI above or execute `dotnet run --project tests/Rasc.Desktop.Smoke/Rasc.Desktop.Smoke.csproj --configuration Release -- p006-late-admission` on Windows after building; fixture is entirely synthetic.
- **Next bounded task / actor / dependencies:** owner runs new x64 archive against actual CS:GO with GC monitoring and captures awards from different source writes plus a long two-line description; Work 1 independently reviews screenshot/GC timestamp evidence, Work 2 corrects only measured remaining discrepancies, without broad research or redefining rarity. Keep draft PR open pending owner confirmation.

**Subsystem: OPEN — not OWNER-ACCEPTED.**
