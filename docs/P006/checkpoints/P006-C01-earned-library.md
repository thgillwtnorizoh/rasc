# RASC / P006-C01 — Source-isolated GC earned-state and 167-entry achievement library

**Project / Campaign / Actor / Assignment:** CS:GO Achievements Resurrection / P006-C01 / Work 1 requirement fixtures and Work 2 production implementation in the same owner-authorized chat / read-only GC current-state projection and browsable historical achievement catalog.

**Status: VERIFIED CHECKPOINT (development/Core and Windows UI smoke).** Not independent second-actor production QA or full original gameplay equivalent. P006 subsystem OPEN.

## Starting SHA / published SHA / exact tested SHA

- **Starting source:** `04a93c9edf659e33a035c1bea72ecb27cc077513` (last owner-approved v0.2 localization PR #44 head, still a stacked draft and not merged into main).
- **Exact tested source:** `183a8efb5f437b247e9dbf0e03c43817c4505ae7`.
- **Published checkpoint:** this markdown is a documentation-only commit after the tested source; see PR #45 history for publication SHA. No post-test production modifications included.
- **Branch:** `feature/p006-c01-gc-earned-library`, [draft PR #45](https://github.com/thgillwtnorizoh/rasc/pull/45) based on `feature/p006-c01-localized-definition-join`. Earlier main remains older; no PR merge or force-push.

## Objective / supported profile / changes and integration

**Read-only source state:**
- `UnlockEventStore.GetCurrentState(sourceKey)` queries existing `sources` and `achievement_state` without schema migration or changing `unlock_history`. Returns `HasValidSnapshot=false` when that source was never valid; previously accepted source-specific data persists across restarts.
- `AchievementMonitor.CurrentStateChanged` reports committed valid snapshots, never missing/invalid/unavailable reads and never redundant unchanged snapshots. This does not alter existing `UnlockDetected` event generation, timestamp dedup or silent first-read baseline.
- Treat GC achievement key presence as source-confirmed EARNED even when unsigned timestamp equals zero; do not invent a real 1970 date for unknown time. A key absent from a known valid snapshot is LOCKED; an unbaselined source is UNKNOWN. Zero-stat values or conditions described in original text **never award**.

**Catalog projection and UI:**
- `AchievementLibraryProjection` joins 167 approved curated rarity v0.2 API IDs, optional player-local original `csgo_english.txt` name/description pairs and the source-keyed current achievement map. Extra unknown GC-awarded IDs appear as separately flagged unclassified rows, without corrupting the fixed 167 catalog.
- `AchievementLibraryWindow.xaml(.cs)` adds a real WPF resizable 167+ row grid (DataGrid virtualization), search and Earned/Locked/Unknown filters, coherent title/description/raw-ID fallback, curated rarity and trustworthy local unlock timestamp. Accessible through the `Browse achievements` button in `MainWindow`. Manual `Reload files` re-reads edited definitions and owner-local text.
- `MainWindow.Achievements.cs` restores source-specific app-owned SQLite state even when not actively monitoring, shows stale/source errors while retaining prior valid state, and refreshes only after committed state changes. No live-to-toast metadata routing changes; Classic/Modern notifications remain unchanged.

**Exclusions:** no original 2023 Steamworks schema proof, no per-rule derivation, no automatic awards, no Valve names/icons bundled in Git, no local icon resolver, no new GC/Steam write, no real new-event title/icon/rarity toast hookup yet.

## Evidence / confidence classification

- **CONFIRMED SOURCE semantics:** GT-610 v1 writer at `72e694d48485daaa569d218992500e5c724345f6` serializes achievement IDs to unsigned timestamp; current RASC ledger established baselines and previously deduplicated events. No claim that arbitrary stats are exact original rules.
- **OWNER-ACCEPTED ENGINEERING:** P006-RARITY-v0.2 167 candidate ID scoring, 63 Rare / 104 Normal including six below-threshold capstone exceptions. This is not official Steam rarity.
- **RECONSTRUCTED requirement fixtures (same-chat Work 1):** `research/P006/fixtures/earned-state-projection.md` separately records N0/B0/B1/S1/S2/M0/I0/I1/T0/X0/L0/L1/E0/R0/UI original-GC-derived behavioral expectations, with source provenance and synthetic IDs/timestamps, not independent production QA.
- **VERIFIED COMPONENT:** exact tested C# Core+WPF build in Windows CI. Original game runtime **UNVERIFIED**.

## Tests — commands, environment, results, artifacts

**Environment:** GitHub Actions `windows-latest`, .NET 10.  
**Exact tested SHA:** `183a8efb5f437b247e9dbf0e03c43817c4505ae7`.  
**Successful workflow:** [run 44680143831](https://github.com/thgillwtnorizoh/rasc/actions/runs/44680143831).

- `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"`: **231 passed / 0 failed / 0 skipped**. Baseline before this slice 222; eight source/projector cases and one focused monitor valid-commit/corrupt-file state-change case added.
- `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental`: **PASS**.
- Preserved 10 existing real-WPF smoke scenarios: **10/10 PASS**, including Modern rare, Classic, profile transitions, sound/typography, installation recovery.
- New `achievement-library` real-WPF smoke: **PASS**, verifies 167 canonical plus one extra row, two earned / 165 locked, search/filter, stale source label, reload callback. **Uses synthetic data, not owner-local game source**.
- Chrome CodePen reference capture: **PASS** (existing legacy notification regression).
- Self-contained `win-x64` publish and artifact upload: **PASS**, [Windows ZIP artifact 14603754132](https://github.com/thgillwtnorizoh/rasc/actions/runs/44680143831/artifacts/14603754132).
- Published editable CSV assertion: **PASS**, 167 rows in application `Data/`.
- **NOT RUN:** actual owner's CS:GO installation/play session, real historical Steam API schema reconciliation, owner-local original icon decoding, independently controlled second-actor compiled-production QA, actual live earned notification with original text/icon and rare glow.

**No regression tests removed.** Existing P004 queued-wave sound and P005 owner-local readiness remain unresolved human qualification gates; this CI success does not close them.

## Coverage gained / remaining requirements

**Gained:** source-isolated latest valid GC state query, Unknown/Locked/Earned semantics with timestamp-zero and unknown GC-ID handling, 167-entry read-only library UI, local text joins, synthetic source fixtures, real WPF browse/filter smoke, preserved notification regression.

**Remaining:** original historical 167 source/version crosswalk, more original rule fixtures, real owner-local text/icon art paths, source-correct unlock record binding to Classic and Modern notification hosts, owner Windows/runtime testing of actual GC source, formal independent QA, accepted discrepancies and owner P006 acceptance.

## Discrepancies / limitations / blockers

- The 167 definitions remain owner-approved icon/text **candidates**, not authenticated exact shipped Valve Steamworks schema.
- No earlier historical event is replayed as a new unlock toast; changing catalog metadata never creates any achievement or event.
- Runtime user-local resource decode is NOT VERIFIED, and unknown localization falls back to raw API ID without any redistribution.
- Stale persisted source state remains visible if the GC file goes missing or invalid; source health shown in UI, not falsely presented as new live confirmation.
- Visual library and main-button connection were compiled and library real-WPF smoke tested; a physical owner session with real local GC is still required for full runtime qualification.

## Updated records / reproduction steps

- `docs/P006/earned-state-library.md` — user-facing states, UI, Core contract and launch instructions.
- `research/P006/fixtures/earned-state-projection.md` — source-specific independent requirement/edge fixtures.
- New `src/Rasc.Core/AchievementLibraryProjection.cs`, `src/Rasc.Desktop/AchievementLibraryWindow.xaml(.cs)`, `src/Rasc.Desktop/MainWindow.Achievements.cs`, Core/monitor/WPF tests.
- Re-run exact workflow commands on tested SHA above, or use [Windows CI #44680143831](https://github.com/thgillwtnorizoh/rasc/actions/runs/44680143831).

**Next bounded task / actor / dependencies:** W1 confirm 3–5 original API IDs and original text/icon/hash on owner's actual game installation; W2 integrate read-only local achievement JPEG loader and bind **new confirmed** unlocks to Classic and Modern host metadata with current RASC curated Rare/Normal, preserving event dispatch and sound. Stop at independently tested vertical slice or reproducible blocker.

**Subsystem: OPEN** — owner approval and independent verification still required.
