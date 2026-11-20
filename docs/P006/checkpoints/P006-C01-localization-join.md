# RASC — P006-C01 localized definition import and source-fixture checkpoint

**Project / Campaign / Actor / Assignment:** CS:GO Achievements Resurrection (RASC) / P006-C01 / combined owner-authorized Work 1 (original-source fixture authority) and Work 2 (production Core parser, join, tests) / **localized text join after curated database import**.

**Status: VERIFIED CHECKPOINT** for Core read-only localization importer and associated regression CI. **NOT** independent second-actor compiled QA, original 2023 Steamworks schema, first real GC-to-toast integration, owner-local original language-file execution or completed P006.

## Git source boundaries

- **Starting SHA:** `b2fd77930b73ddcce7e9cb87eb87a28b000297e5` — verified parent PR #43, owner-approved 167-entry v0.2 curated rarity importer. Earlier main remains `425444dc7440163ec8b3f74ee0ad9c360d5896fd`, but these changes are on unmerged stacked draft PR branches.
- **Published implementation SHA / exact Windows CI-tested SHA:** `62836363fdef47df33aa93cb98a7ce71dcaa088a`.
- **Documentation checkpoint published after tested source:** this file; use the PR's updated branch head/commit history for the documentation-only SHA.
- **Draft PR #44:** https://github.com/thgillwtnorizoh/rasc/pull/44, based on `feature/p006-c01-curated-definition-import`. No force-push, main changes or merge.

## Objective / supported profile / changes

- `src/Rasc.Core/CsgoAchievementLocalization.cs`: read-only loader for `<gameRoot>/csgo/resource/csgo_<language>.txt` or selected `<csgoContentRoot>/resource/csgo_<language>.txt`, original Source 1 UTF-16LE/BE BOM, strict UTF-8 support; bounds checks; `Tokens` KeyValues parser for quoted key/value, comments, escaped characters, nested unrelated sections and conditional suffixes. Conflicting duplicate achievement tokens rejected.
- Language fallback: if a requested optional language is absent, report `PreferredFileMissing=true` and use English. If a requested pair is partial, use the **complete** English pair, not mixed-language strings. Invalid existing requested language -> `ReadStatus.Invalid`, never silently replaced. English missing -> `Missing`; IO inaccessible -> `Unavailable`.
- `CsgoAchievementDefinitionJoin`: joins local names/descriptions to each of the 167 owner-approved metadata entries by **exact** API ID. Unknown text remains null. No expansion to cut or unused localized achievements; no uncited unlock-rule derivation, rarity changes, GC/Steam writes, or UI modification.
- `tests/Rasc.Core.Tests/CsgoAchievementLocalizationTests.cs`: synthetic KeyValues fixture tests covering catalog join, locale fallback, UTF16 LE/BE and UTF8 BOM, file read-only preservation, missing/corrupt source, path traversal, comments/escapes, duplicate conflicts and GT-610 v1 source-authoritative award example.
- `research/P006/fixtures/localized-gc-first-slice.md`: Work 1 independently source-selected five IDs and S0/S1/S2 **synthetic** GT-610 snapshot expectations. `docs/P006/localized-definition-join.md`: usage and contract.

**Explicit exclusions:** no original Valve text/image redistribution; no full original shipped 167-ID Steamworks confirmation; no local icon bytes or real image decoding; no live GC monitor/SQLite/GUI/Classic/Modern toast hookup yet. Current reward timestamps and award logic remain the independent GT-610 `GcKeyValuesParser` / `AchievementMonitor` subsystem.

## Evidence and confidence

- **CONFIRMED OWNER-LOCAL AUDIT (not official Steam schema):** user-supplied `rasc-icon-audit.csv` SHA-256 `8ba3a297a69362695750454fac68681396da0c12cb9732811322df85a14f0127`; 170 local icon/name/description pairs; all 167 owner-approved candidate IDs match its image and text records. Non-admitted extra examples `BOMB_DEFUSE_NEEDED_KIT`, `BREAK_PROPS`, `GUN_GAME_SELECT_SUICIDE_WITH_KNIFE` are not auto-admitted.
- **SOURCE-CONFIRMED WRITER FORMAT:** `GT-610/csgo-gc` commit `72e694d48485daaa569d218992500e5c724345f6` / `local_user_stats.cpp` blob `7843454c126b2edb4bb9626611da868c5086666d` confirms `version=1`, stat sections and API-ID→unsigned timestamp achievement section. Source fixture proves no arbitrary award from `total_kills`.
- **INDEPENDENT SOURCE EXPECTATIONS (same conversation, not separate-agent production QA):** five named fixtures in `research/P006/fixtures/localized-gc-first-slice.md`, all data synthetic or copied as identifier-only, not representing real in-game tests.
- **ENGINEERING IMPLEMENTATION:** C# parser, fallback, null-safe join.
- **VERIFIED COMPONENT:** core build/test plus Windows regression and artifact at source SHA shown below.
- **UNVERIFIED RUNTIME:** true user-installation `csgo_english.txt` decoding/complete 167 text coverage, actual icon display, original game award behavior.

## Tests and published Windows evidence

**First CI attempt:** https://github.com/thgillwtnorizoh/rasc/actions/runs/44677067505 — **FAILED** at new parser Core compilation: several KeyValues token accesses incorrectly used nullable `.Value` after null-coalescing had already unwrapped the value. No runtime test occurred in that failed attempt. Fixed in commit `62836363fdef47df33aa93cb98a7ce71dcaa088a`; error preserved for regression history.

**Successful exact CI:** https://github.com/thgillwtnorizoh/rasc/actions/runs/44677210004 (GitHub Windows `windows-latest`, .NET 10).
- `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"` — **222 passed, 0 failed, 0 skipped** (prior baseline 207).
- `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental` — **PASS**.
- 10 existing WPF real-window smoke invocations — **PASS**; these do not exercise new source text in a real CS:GO install.
- Chrome source-reference frame capture — **PASS**; existing rare-glow parity context, not localization QA.
- `dotnet publish src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/Rasc-Windows-x64 -p:PublishSingleFile=false` — **PASS**.
- Published v0.2 editable CSV check — **PASS**, 167 rows preserved.
- Windows x64 ZIP artifact ID **14602454411**: https://github.com/thgillwtnorizoh/rasc/actions/runs/44677210004/artifacts/14602454411

**NOT RUN:** owner installed game resource file on physical Windows machine, headless tests with original copyrighted localization, actual live awarded achievements in UI, separate independent actor regression.

## Coverage gained / remaining / discrepancies

**Gained:** Source-independent functional English/preferred-language loader, strict and read-only error contracts, nullable metadata join for the full 167 candidate IDs, first five source-selected fixture IDs and synthetic GT-610-state authority test, 15 added xUnit tests and full preserved CI regression.

**Remaining:** verify actual 2023 version-matched canonical Steam ID set and historic unlock rules; evaluate parser on user's physical `csgo_english.txt` and alternate locale; connect selected game root from P005 UI; resolve artwork read-only; project current GC snapshot to locked/earned/unknown and bind real Classic/Modern notifications with no false replay. Actual mainline changes are stacked draft PRs and are not merged. Existing P004/P005 owner runtime/rights caveats remain.

**Updated records / reproduction steps:**
- `docs/P006/localized-definition-join.md`: library usage, path and failure policy.
- `research/P006/fixtures/localized-gc-first-slice.md`: five original audit references, GT-610 writer and synthetic fixture expectations.
- Reproduce at exact tested SHA with CI commands above; use synthetic fixtures only for development QA.

**Next bounded task / actor / dependencies:** Work 1 independently confirm 3–5 exact original IDs and ground GC-award rules; Work 2 read-only current snapshot projection and WPF achievement library, then actual named toast integration under separate verified gate. Original era source authority and local owner runtime fixture required before claiming original equivalence.

**Subsystem:** **OPEN** pending integrated functionality, independent QA and owner approval.
