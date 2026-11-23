# P006-C11 — Progress eligibility and unlock priority checkpoint

Project / Campaign / Actor / Assignment:
CS:GO Achievements Resurrection / P006 / Work 2 — Reconstruction Engineering /
P006-C11 — progress mapping, source-baselined milestone pipeline and priority.

Status: ENGINEERING IMPLEMENTATION — **UNVERIFIED BUILD / OPEN** until exact-source
Windows CI completes. No claim of original Steam runtime equivalence.

Starting SHA: `839b6839a009561e6fb50f678268f3db9dde29e4`
(C10 Classic single-owner branch).
Published SHA / Exact tested SHA: to be completed after GitHub Actions;
no passing CI result is asserted by this record.

Objective / Supported profile / Changes:
- 167-entry source-gated classification (86 scalar / 9 composite / 72 unlock-only);
  4 trace correlations recorded without targets or activation.
- Optional validated read-only GC snapshot-to-progress pipeline, with initial
  silent baseline, rollover suppression, highest milestone crossing, per-source
  and per-achievement dedup, earned suppression.
- Priority across shared cross-style queue, Classic waiting admissions,
  Modern not-yet-presented wave groups, preserving active cards and unrelated FIFO.
- Classic/Modern progress UI already existed and remains compatible.
- Additional xUnit coverage for mapping integrity, guard failures,
  counter updates, source isolation and queue cancellation.

Evidence / Confidence classification:
- ENGINEERING: static classification, eligibility gates, cancellation semantics.
- RECONSTRUCTED: progress visual fixture and 25/50/75 presentation rules.
- USER-PROVIDED GC TRACE CORRELATION (not independently revalidated):
  394 stat writes, 10 unlock calls, 0 progress API attempts.
- UNVERIFIED RUNTIME: real GC values and native Steam popup fidelity.

Tests:
- Source tests added; commands expected in Windows CI:
  `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release`
  `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental`
  Existing WPF smoke and `dotnet publish` retained.
- **NOT RUN / NOT VERIFIED at initial publication**: exact-source CI,
  human desktop/fullscreen validation, independent QA.
- Do not invent pass/fail counts before run completion.

Coverage gained:
Mapping registry, optional stat producer, cross-profile / renderer-queue
unlock priority, focused regression fixtures.
Remaining requirements:
exact versioned stat+goal mapping evidence for all 86 candidates;
nine composite producer specifications; original GC progress API behavior;
runtime testing with changing stat snapshots; source-specific persistence policy.

Discrepancies / Limits / Blockers:
No trusted original progress API signal exists; all live mappings disabled by default.
Modern/Classic previews are synthetic UI fixtures only. Later merge target is
C10/active P006 development, **not the stale default `main`**.

Updated records:
`docs/P006/progress-contract.md`,
`src/Rasc.Core/Data/achievement-progress-mappings.v0.1.csv`,
tests, workflow checks, this checkpoint and PR #56.

Reproduction:
Review PR #56; run the full Windows workflow on exact head.
For synthetic unit fixtures, construct a `VERIFIED:fixture` scalar mapping
with an integer stat and target; baseline at 24, observe 25/50/75 and unlock.

Next bounded task / Actor / Dependencies:
Work 1 independently verify target values/stat names and earned behavior;
Work 2 qualify actual runtime with verified mappings, preserve original
source timing and crash regressions; owner final acceptance.

Subsystem: **OPEN**.
