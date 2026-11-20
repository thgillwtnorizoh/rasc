# RASC / P006-C03 — Original Source 1 localization recovery, GC burst coalescing and Classic text correction

**Project / Campaign / Actor / Assignment:** CS:GO Achievements Resurrection (RASC) / P006-C03 / owner-authorized same-chat Work 1 (independent original-source evidence and test expectations) + Work 2 (implementation, Windows regression) / address screenshots showing live art but API IDs, missing descriptions, delayed bulk delivery and incorrect Classic layout.

**Status: VERIFIED CHECKPOINT** for source-inspected original parser failure and development Windows CI; **not** independent second-agent production QA and **not** completed real-game qualification. **P006 subsystem OPEN.**

## Starting SHA / publication / exact tested SHA

- **Starting SHA:** `17b24549a054bfc1cd2f2c9ecc96318cba93e229` — last draft PR #46 head, previous 238-test P006-C02 verified development checkpoint.
- **Published and exactly CI-tested production/QA/evidence SHA:** `fb0527208196a6d67df9c9d9b8c2110d1166bb68`.
- **This checkpoint:** documentation-only commit after CI. See PR #47 branch history for checkpoint publication SHA; no production source changed after tested SHA.
- **Branch/PR:** `fix/p006-c03-localization-burst-progress-gate`, [draft PR #47](https://github.com/thgillwtnorizoh/rasc/pull/47), based on draft #46 → #45 → #44 → #43. No merge/force-push; `main` remains older.

## Objective, supported profile, functional changes

1. **Correct valid real CS:GO language parsing:** Source 1 `csgo_english.txt` contains multi-line quoted values before the achievement section. C# lexer now accepts physical LF/CRLF within quoted values, normalizing line breaks. Achievement `_NAME`/`_DESC` repeated keys are now **last-value-wins**, rather than invalidating the complete file; original contains four deliberately revised duplicate descriptions. Malformed unclosed quotes remain errors.
2. **Surface decoding health:** MainWindow displays persistent `Local CS:GO names READY: <count> complete pairs` or a **NOT READY** reason, separate from rapidly replaced GC status text. Tooltip shows selected game-local resource path and diagnostic, helping distinguish missing/invalid local text from missing catalog or artwork.
3. **Preserve Classic original-style text:** Live Classic always uses headline `Achievement Unlocked!` and the resolved localized achievement **name** in the second text field; the full goal description remains the Modern component content. Preserves existing artwork, typography, slide and audio. No uncited third text field added to Classic's two-slot original-like card.
4. **Atomically coalesce actual GC bursts:** `AchievementMonitor.UnlockBatchDetected` presents an ordered array of all outstanding source-confirmed awards per valid successful reconciliation. MainWindow dispatches them through **one** WPF `Dispatcher.Invoke` and **one** `NotificationPresentationGate` callback, so all same-snapshot events enter the Modern 45ms admission batch together even with slow fullscreen UI processing. Existing per-award `UnlockDetected` subscribers, pending history, silent initial baseline, timestamp tie policy, source isolation and dedup remain intact. Classic still receives original per-award animations.
5. **25/50/75% suggestion groundwork:** `AchievementProgressMilestonePolicy.HighestNewMilestone` is a pure, tested threshold evaluator for a *future verified stat and target*. No live progress is emitted: GT-610 achievement timestamps and original descriptions are not sufficient proof of the exact live numeric counter. No milestone history/persistence introduced or false progress projected. At completion, unlock toast would replace progress; a jump crossing multiple thresholds yields only highest eligible milestone, provided prior valid reading exists.

**Excluded:** original achievement rule reconstruction, broad filesystem scraping, Steam/GT-610 writes, Valve asset redistribution, changes to approved 63 Rare/104 Normal decisions, reshaping accepted Classic/Modern effects, unverified progress auto-awards.

## Evidence / confidence classification

- **CONFIRMED ARCHIVED ORIGINAL file:** SteamTracking/GameTracking-CS2 commit `e01c9b47aa6c4b253f1a5888a5693ececc1d2328`, blob `800b1e5ab3ced926b0a5a58dacdec66461f05dd2`, 2023 `csgo/resource/csgo_english.txt`. Independently scanned all **3,201,409 decoded characters** with separate JS lexer; detected **22 physical newlines inside quoted strings** and **four conflicting repeated achievement `_DESC` keys**. Corrected scan yields **183 complete name/description pairs** (184 unique name IDs); original RASC parser threw before reaching them. Source reference, minimal derived fixture and repeatable test expectations in `research/P006/fixtures/C03-original-source-localization-and-burst.md`. This independently confirms the former failure on 2023 original file, not on user's unknown physical file bytes.
- **CONFIRMED OWNER RUNTIME OBSERVATION:** Four post-C02 in-game screenshots show actual rare/normal historical JPEGs on live toasts but raw identifiers and absent descriptions; one-shot/multi-shot bulk delay reported. Owner's local original files were **not** submitted in this turn. Four PNG/JPEG images are referenced in chat only; artwork is not committed.
- **CONFIRMED ENGINEERING bug:** Classic live overload passed name as headline and full description as second field, losing `Achievement Unlocked!`; GC monitor issued separate per-event synchronous UI dispatcher calls, vulnerable to exceeding the Modern 45ms grouping window.
- **RECONSTRUCTED timing explanation:** Atomic batch removes a specific scheduler timing hazard. The exact prior owner delay may also involve the accepted wave capacity/hold: Modern remains maximum **3 cards**, **300ms entrance + 5000ms hold + 300ms exit**; fourth+ within one burst naturally waits for subsequent waves. This behavior is **not** claimed eliminated.
- **ENGINEERING progress policy:** no original stat binding or source verified. Runtime progress notifications remain NOT IMPLEMENTED.
- **VERIFIED COMPONENT:** development + actual-WPF synthetic smoke, 244 tests.
- **UNVERIFIED ORIGINAL RUNTIME:** player's specific `csgo_english.txt`, full original unlock semantics, physical fullscreen post-fix screenshot/effects and independent QA.

## Tests (exact production source and environment)

**Exact CI-tested SHA:** `fb0527208196a6d67df9c9d9b8c2110d1166bb68`  
**GitHub Windows runner:** `windows-latest`, .NET 10, [successful workflow #44703294922](https://github.com/thgillwtnorizoh/rasc/actions/runs/44703294922).

- `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"` — **244 passed / 0 failed / 0 skipped** (previous 238). Added multiline/duplicate localization fixture, LF/CRLF/unclosed quote fixture, single-batch four-ID GC monitor test, three safe progress threshold tests; no old tests removed.
- `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental` — **PASS**.
- Prior **10/10 WPF real-window** popup/profile/sound/installation smoke scenarios — **PASS**.
- P006 achievement-library real-WPF smoke — **PASS**.
- P006 3+2 named/JPEG/rare-wave real-WPF smoke — **PASS**, with extended live Classic host assertion of **`Achievement Unlocked!` header + localized name**. No extra unlock or render callback needed to drain second wave.
- Original CodePen Chrome reference frame capture — **PASS**; no intentional changes to rare glow or typography geometry.
- `dotnet publish src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/Rasc-Windows-x64 -p:PublishSingleFile=false` — **PASS**.
- Built-in editable `Data/achievement-definitions.v0.2.csv` package assertion — **PASS**, 167 rows.
- [Windows x64 application ZIP artifact **14614231182**](https://github.com/thgillwtnorizoh/rasc/actions/runs/44703294922/artifacts/14614231182).
- **NOT RUN:** real game output post-fix, owner's exact `csgo_english.txt` parse 183 expectation, original 181-image hash QA, independent second-actor production QA, verified per-achievement progression under real source.

## Coverage gained / remaining, discrepancies and blockers

**Gained:** archived-original-language parsing failure independently diagnosed and fixed at Core level, stable ordered duplicate override, visible UI text health, valid Classic heading semantics, atomic per-snapshot event batching, general 25/50/75 pure policy and preserved full Windows regression.

**Remaining:** owner physical runtime validation and confirmed language-pair count; further local-installation difference diagnosis if NOT READY; independently proven exact original 167 Steamworks schema; source-proven cumulative/stat goal mapping for progress and durable milestone dedup; actual live progress variant triggers; independent second-agent QA; owner acceptance of P006.

**Discrepancy:** same-GC-timestamp ordering stays deterministic but cannot establish subsecond original award order. Batch grouping is a latency repair, but capacity-3 still requires a subsequent full wave for 4th+ award. Last-wins localization policy matches archived source revisions but original game's duplicate-token override operation has not been runtime-proven.

## Updated records / reproduction

- `research/P006/fixtures/C03-original-source-localization-and-burst.md`: direct archived original evidence, owner screenshots, expected state/Classic/Modern/progress behavior.
- `src/Rasc.Core/CsgoAchievementLocalization.cs`: multiline quote / repeated-keys recovery.
- `src/Rasc.Core/AchievementMonitor.cs`, `src/Rasc.Desktop/MainWindow.xaml.cs`: one source-confirmed burst + one WPF callback.
- `src/Rasc.Desktop/ClassicToastHost.cs`, `src/Rasc.Desktop/MainWindow.Achievements.cs`, `src/Rasc.Desktop/MainWindow.xaml`: correct headline + visible local-text status.
- `src/Rasc.Core/AchievementProgressMilestonePolicy.cs`: pure non-live threshold groundwork.
- Focused tests in `CsgoAchievementLocalizationTests.cs`, `MonitorTests.cs`, `AchievementProgressMilestonePolicyTests.cs`, `P006LiveToastSmoke.cs`.

**Owner reproduction:** Run win-x64 artifact; select actual GT-610 GC path and start monitoring. Expect a visible `Local CS:GO names READY` line showing valid pair count (historical 2023 reference **183**, but version may differ). On a genuine *new* award Modern should display `The Cleaner` or `Shot With Their Pants Down` with local description and artwork; Classic must show `Achievement Unlocked!` and localized name. Earn 2–3 in one GC snapshot and verify they share one Modern wave. If 4+, verify later waves appear automatically but after the accepted 5.6-second lifecycle. Do not replay already baselined awards to fabricate test data.

**Next bounded task / actor / dependencies:** Owner tests actual local source file; combined Work 1 source/fixture QA and Work 2 any targeted file-version correction; separately source-proof counters for live progress before enabling 25/50/75. Publication remains draft, P006 **OPEN**.

**Subsystem: OPEN | not OWNER-ACCEPTED.**
