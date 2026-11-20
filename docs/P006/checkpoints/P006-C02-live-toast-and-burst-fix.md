# RASC P006-C02 — Live original-local toast metadata and fullscreen-safe Modern backlog

**Project / Campaign / Actor / Assignment:** CS:GO Achievements Resurrection (RASC) / P006-C02 / Work 1 observational evidence and test expectations + Work 2 implementation (same owner-authorized conversation) / Correct owner-reported live popup raw IDs, missing artwork/description, burst ordering and delayed queued wave.

**Status: VERIFIED CHECKPOINT** (development/Core + Windows real-WPF synthetic integration). **Original owner-local live runtime requalification NOT VERIFIED.** P006 subsystem **OPEN**.

## Starting SHA / published SHA / exact tested SHA

- **Starting SHA:** `fe493a65b0ebed1744c4f1d4c4125f0f61eee441` — previous 231-core-test P006 earned-state/library draft PR #45 checkpoint, still not merged.
- **Exact tested implementation SHA:** `d4dd3b126f362dfce147bbda482492ef753adff7` — successful GitHub Windows CI.
- **Source fixture record published after tested SHA:** `59546156fec7e624dde93f1b5c9cf14cb4927bc6`, documentation only.
- **Publication:** this checkpoint is another documentation-only follow-on commit; see PR #46 history for published SHA.
- **Branch:** `fix/p006-c02-live-toasts-and-wave-wake`; [draft PR #46](https://github.com/thgillwtnorizoh/rasc/pull/46), stacked on unmerged #45/#44/#43. No main merge/force-push.

## Objective / supported profile / functional changes

1. **Live name and description:** `MainWindow.Start_Click` now loads the installed `csgo/resource/csgo_english.txt` through the existing read-only `CsgoAchievementLocalization` before subscribing to GC events. Instead of calling the raw-ID placeholder host overloads, both Classic/Modern receive a frozen-at-event-arrival `ResolvedAchievementToast` with exactly matched complete name/description from original owner-local text; if absent, show API ID with blank description, no invented strings. The library's explicit Reload also refreshes live metadata.
2. **Local original icon:** `CsgoAchievementIconReader` reads the exact expected JPEG basename under `csgo/materials/panorama/images/icons/achievements/`, with `csgo/resource/flash/images/achievements/` fallback, **read-only** with 1 MiB bound and expected v0.2 SHA-256. A mismatch, missing file, invalid header/footer, corrupt decoded image or unavailable path retains the existing neutral star. RASC never bundles Valve art bytes. `AchievementToastArtwork` decodes verified bytes in memory for both existing Classic and Modern icon-face geometry, including preserving the Modern CodePen rare glow wrapper.
3. **Curated v0.2 Rare:** read-only RASC owner-approved `63 Rare/104 Normal` policy selects Modern normal or golden rare presentation, and is explicitly **not** Valve/Steam global rarity. Original unlock conditions and GT-610 stats unaffected. Classic original art and names become visible; no undocumented gold styling added to Classic.
4. **Real order:** `UnlockEventStore.AcceptSnapshot` now sorts newly confirmed GC awards by positive source `unlock_seconds` ascending, then identifier deterministic tie break; zero timestamp (unknown actual time) follows known timestamps. SQLite pending rows are read by `observed_ms,rowid` rather than hashed `event_id`, preserving insertion order across persisted at-least-once delivery. Recent event history reads reverse insertion order for same observation millisecond. **Same-second award sequence cannot be reconstructed exactly** because GT-610 retains whole seconds, not subsecond order; alphabetical ties are an explicit engineering fallback.
5. **Modern backlog completion:** `ModernToastScheduler.NextTransitionDeadline` supplies entrance/hold/exit deadlines. `ModernToastHost` keeps a one-shot-like DispatcherTimer at each boundary even when WPF Rendering is paused by fullscreen foreground; repeated render calls do not postpone that timer. A queued second/third wave advances when the first finishes **without needing another unlock event**. Existing wave size 3, visual timing (300ms in + 5000ms hold + 300ms out), stack geometry, audio cue behavior and dedup remain unchanged.

## Evidence / confidence classification

- **CONFIRMED OWNER RUNTIME OBSERVATION:** four in-game screenshots show API-ID names `FAST_ROUND_WIN`, `KILL_ENEMY_RELOADING`, `UNSTOPPABLE_FORCE`, `LAST_PLAYER_ALIVE`, `WIN_BOMB_DEFUSE`, `KILL_ENEMY_TEAM`, `KILLING_SPREE`, generic star placeholders and blank descriptions; user reports a three-event burst resulting in one card until a later award. Static images alone do not establish exact sequencing/timestamps. See `research/P006/fixtures/owner-runtime-toast-discrepancy.md`.
- **CONFIRMED CODE DEFECT:** live `Enqueue(item)` host overload never consulted imported names/rarity/icons, and alphabetical/hashed ordering did not reflect known GC timestamps.
- **RECONSTRUCTED TIMING HYPOTHESIS:** WPF Rendering starvation during fullscreen can delay completion of an active wave; independent transition deadline removes that scheduling dependency, but **owner's exact earlier stall source has not been captured with timestamped runtime traces**.
- **OWNER-ACCEPTED ENGINEERING policy:** RASC-RARITY-v0.2, fixed 167 candidates, curated Normal/Rare; original Steam rarity NOT VERIFIED.
- **VERIFIED COMPONENT:** synthetic C# and actual Windows-WPF tests, full retained regressions.
- **UNVERIFIED RUNTIME:** post-fix owner CS:GO installation physical gameplay, actual `csgo_english.txt` parse count, actual image hash matches and visually observed result.

## Tests and environment

**Exact tested source SHA:** `d4dd3b126f362dfce147bbda482492ef753adff7`  
**Windows runner:** GitHub `windows-latest` / .NET 10; [workflow 44696901666](https://github.com/thgillwtnorizoh/rasc/actions/runs/44696901666), **SUCCESS**.

- `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"`: **238/238 passed, 0 failed, 0 skipped**. +7 tests over 231 baseline: live curated rarity and localization, unknown/missing safe fallback, SHA-verified Panorama→Scaleform JPEG fallback and file-size rejection, timestamp-order persistent event delivery and deadline-drained five-card backlog.
- `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental`: **PASS**.
- Existing **10/10** WPF real-window smoke scenarios: **PASS**, including Modern rare, Classic, wave, queued sound, font/sound and installation recovery.
- Existing P006 library WPF smoke: **PASS**.
- Added P006 live-format **real-WPF** synthetic 3+2 queue smoke: **PASS**. Verified FIFO named titles/descriptions, actual synthetic JPEG visible in Modern faces and Classic renderer, Rare golden wrapper for flagged event. Suppressed CompositionTarget Rendering + anchor updates throughout, and observed second two cards appear automatically without a sixth award.
- Existing Chrome CodePen reference capture: **PASS**.
- Published self-contained Windows x64 release: **PASS**; [Windows application artifact ID 14610509824](https://github.com/thgillwtnorizoh/rasc/actions/runs/44696901666/artifacts/14610509824).
- Published built-in/editable 167-row CSV check: **PASS**.
- **NOT RUN:** owner physical Windows after this fix, original installed Source 1 localized file/art 181 JPEGs and Steamworks full historical schema, independent second-actor production QA, actual game-triggered Rare and Normal smoke, performance under actual fullscreen GC load.

**Historical tests preserved.** No existing legacy suites removed.

## Coverage gained / remaining requirements

**Gained:** end-to-end new-GC-award-to-resolved-original-local-metadata-to-Classic/Modern toast route, verified path/hash local JPEG decoding, owner-curated rarity for Modern, FIFO source-second ordering and fullscreen-safe bounded wave timer, comprehensive synthetic production and WPF test gates.

**Remaining:** real local source file runtime test and icon match count, original 167-ID shipped Steam schema validation, other award-rule fixtures, original gallery/review consistency, independent compiled QA, user review of updated actual CS:GO screenshots, owner acceptance. Unknown timestamps/tied seconds cannot establish original subsecond chronology.

## Discrepancies / limitations / blockers

- No additional Steam files or Valve art may be redistributed; app relies on the player's installed local achievement resource files. If files are absent/hash differs from owner audit, placeholder remains by design. English text parser may need correction if a version-specific genuine file differs from synthetic tests; report it rather than fabricating text.
- The screenshot cannot prove the exact original queue bug; deadline timing addresses a plausible and independently testable starvation mechanism.
- This does not retroactively replay historical/baselined awards: only newly confirmed GC additions enter live notification hosts, following existing at-least-once event store policy.
- No entire P006 subsystem acceptance or separate Work 1 QA; a verified checkpoint is not owner acceptance.

## Updated records / reproduction steps

- `research/P006/fixtures/owner-runtime-toast-discrepancy.md` — independent original screenshot observations and discriminating expected cases.
- `src/Rasc.Core/CsgoAchievementIconReader.cs`; `src/Rasc.Desktop/AchievementToastArtwork.cs`; existing Core/Classic/Modern/Monitor/MainWindow integration; unit and WPF smoke tests.
- To reproduce CI on exact SHA, run commands above plus `dotnet run --project tests/Rasc.Desktop.Smoke/Rasc.Desktop.Smoke.csproj --configuration Release -- p006-live-toasts` on real Windows. The test uses only synthetic images/text and cannot prove user-local original art.
- For owner physical QA: use the build artifact, verify local `csgo/resource/csgo_english.txt` and both icon directories, select the correct CS:GO installation in RASC, start monitoring, earn a *new* GC award (first snapshot is silent), check original name+description+art, RASC Rare/Normal variant, and earn more than three close together. Verify all waves finish without more awards.

**Next bounded task / Actor / dependencies:** Owner runtime qualification with screenshots and source-file diagnostics if needed, then combined Work 1 independent review and Work 2 narrowed correctness fixes. Do not revise rarity policy or change original unlock conditions to mask source failures.

**Subsystem: OPEN | not owner-accepted.**
