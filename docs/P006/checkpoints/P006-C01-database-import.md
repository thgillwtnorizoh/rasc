# RASC / P006-C01 — Curated achievement definition database import checkpoint

**Project / Campaign / Actor / Assignment:** CS:GO Achievements Resurrection (RASC) / P006-C01 / combined owner-authorized Work 1 source reconciliation and Work 2 implementation / first versioned database import; [PR #43](https://github.com/thgillwtnorizoh/rasc/pull/43).

**Status:** **VERIFIED CHECKPOINT** — source-data parity, development core tests and Windows CI. **NOT** historical unlock-rule validation, completed P006, owner physical Windows qualification, or independent second-actor production QA.

**Starting SHA:** `1f6817db0431eae926a90cf70cd8080fc474f3c8` (latest P006 source research PR #42 parent; `main` remains older).  
**Published production source / exact Windows CI-tested SHA:** `4b480612c5753e8f89b42282b561b8818440bc80`.  
**Checkpoint publication:** added after the tested source commit; see GitHub file history/PR head for its documentation-only commit SHA.  
**PR branch:** `feature/p006-c01-curated-definition-import`; unmerged draft PR #43. No force-push and no main-branch changes.

## Objective / supported profile / functional changes and integrations

1. Import the owner-accepted **P006-RARITY-v0.2** as one editable, versioned `src/Rasc.Core/Data/achievement-definitions.v0.2.csv`, no original copyrighted Valve text/images.
2. Add `CuratedAchievementCatalog` and `CuratedAchievementDefinition` with read-only lookup by exact ID, five 0–5 score factors, threshold **17**, capstone exception, per-row optional owner `RARE`/`NORMAL` override, safe image filename/hash metadata, and clearly labeled rarity basis. Unknown IDs stay null/unknown.
3. Embed the same CSV for zero-setup fallback and copy it to `Data/` in output/publish for modification without rebuilding. Invalid explicitly selected CSV is rejected rather than silently ignored; no Steam/GC resources are written.
4. Add unit tests for baseline counts and owner examples, 16/17 boundary, capstones, overrides, duplicate/invalid IDs, unsafe icon filename, invalid score, corrupt/missing CSV, BOM/comment/CRLF and no text/award logic.
5. Correct superseded claim that `materials/vgui/achievements/` held authentic CS:GO icons; owner identifies those as CS:S, while authentic loose CS:GO JPEGs exist in the Panorama and Scaleform locations.

**Excluded:** no original Steam API schema recovery, no rule reconstruction, no current-state SQLite/UI changes, no real event toast changes, no bundled Valve localization/JPEGs, no achievements auto-awarded.

## Evidence / confidence classification

- **Owner-accepted ENGINEERING judgment:** v0.2 rarity policy, original owner input `RASC-P006-curated-rarity-v0.2.csv` SHA-256 `1e33bca82450b3e6fc98c51708f995a90ab2e351aa3c62d342b6bfc09db22d4b`. **This is not historical Steam global rarity.**
- **INDEPENDENT SOURCE-SIDE DATA CHECK (not independent production QA):** parsed supplied CSV independently of .NET loader and compared all 167 IDs, filenames, icon hashes, five factors and capstone flags against the committed CSV. **0 differences**. Source classification **63 rare / 104 normal**, 12 capstones including six score-below-17 exceptions. All source score totals equal factor sums.
- **Owner-local source audit:** `rasc-icon-audit.csv` SHA-256 `8ba3a297a69362695750454fac68681396da0c12cb9732811322df85a14f0127`. Independently compared in this execution: **167/167** v0.2 entries have identical ID, English name, description, JPEG filename and SHA-256 in the local audit; the other localized pairs are `BOMB_DEFUSE_NEEDED_KIT`, `BREAK_PROPS`, `GUN_GAME_SELECT_SUICIDE_WITH_KNIFE` (historically cut/unreleased candidates). This does not prove the original official Steamworks ID set.
- **VERIFIED COMPONENT (development build/test):** GitHub Windows CI verified loader and all preserved Windows regression gates. **Original gameplay runtime parity UNVERIFIED.**

## Tests

**Exact tested SHA:** `4b480612c5753e8f89b42282b561b8818440bc80`  
**Environment:** GitHub Actions `windows-latest`, .NET 10, [successful run #44651289758](https://github.com/thgillwtnorizoh/rasc/actions/runs/44651289758), workflow run for tested head.

- `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"`: **207 passed / 0 failed / 0 skipped**.
- `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental`: **PASS**.
- Explicit embedded-resource fallback assertion: **PASS**, 167 definitions with 63 rare / 104 normal.\n- Existing WPF real-window profile smoke: **10/10 invocation gates PASS**, covering rare glow, classic/modern transitions, stack, progress, sound/settings and installation recovery. These are regression gates, not direct achievement-library tests.
- Chrome CodePen reference capture and PNG upload: **PASS**.\n- Published-package contract: **PASS**, `artifacts/Rasc-Windows-x64/Data/achievement-definitions.v0.2.csv` exists with exactly 167 data rows (editable without recompiling).
- `dotnet publish src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/Rasc-Windows-x64 -p:PublishSingleFile=false`: **PASS**. [Windows artifact ID 14590223832](https://github.com/thgillwtnorizoh/rasc/actions/runs/44651289758/artifacts/14590223832).
- **NOT RUN:** local .NET SDK in this container (unavailable); direct owner Windows/game runtime; separate independent tester of compiled production; original 2023 CS:GO rule/Steam-schema verification. CI emitted GitHub action Node 20 deprecation notices, not a .NET failure.

## Coverage gained / remaining

**Gained:** complete versioned *owner-curated* 167-row rarity metadata candidate set; deterministic classification, editor-friendly overrides, source integrity checks and tested loader. **Remaining:** original-Steam historical 167-ID provenance, full official names/categories/rules, local read-only string/image loader, GC earned-state library projection and actual Classic/Modern real-unlock event integration.

## Discrepancies / limitations / blockers

- Candidate IDs originate from localized image names and accepted v0.2 workbook; **not independently verified as the complete shipped original Steam schema**.
- Real Valve artwork/localization remains local to the user's installation and is not published; the full original text/icon pipeline is out of scope.
- The UI does not consume this database yet; only the Core API and built-in/editable data import exist.
- Owner has accepted rarity **v0.2** as mutable policy, not this implementation or the entire P006 subsystem. Historic P004 sound/publish and experimental mask rights limitations persist.

**Updated records / reproduction:** `docs/P006/catalog-import.md`, `research/P006/catalog/rarity-v0.2-source-validation.md`, correction in `research/P006/catalog/localization-vpk-discovery.md`, source CSV, C# loader and tests. Re-run the above Windows CI commands at the exact tested commit; review PR #43 and its artifact.

**Next bounded task / actor / dependencies:** combined W1 source-ID/localization fixture authority then W2 read-only local `csgo_<language>.txt` join and first 3–5 verified GC earned-state records; no guesses and no new art distribution. Stop at verified vertical slice or reproducible blocker.

**Subsystem:** **OPEN** — owner approval still required for production adoption.
