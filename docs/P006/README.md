# P006 — Stage 2: Actual original CS:GO achievements resurrection

**Campaign owner:** RASC Headquarters. **Stage 1:** user declared project milestone COMPLETED on 2026-11-19. **Stage 2:** owner authorized "make actual achievements." This is milestone-level acceptance only; P004 queued-wave sound **physical owner playback NOT VERIFIED**, P005 local discovery **physical owner runtime NOT VERIFIED**, and experimental P003 rare mask licensing unresolved. Do not erase these facts or infer individual subsystem/PR merge acceptance.

**Project:** https://github.com/thgillwtnorizoh/rasc  
**Starting production baseline to verify:** `e84b452a54c4df1319f03492485e67ddc7b5a6c8` (P005-C01 exact CI-pass, draft PR #38). At kickoff `main` remains `425444dc7440163ec8b3f74ee0ad9c360d5896fd`, older than unmerged P002–P005 descendants. Keep their draft graph and no force merges.  
**Assignments:** [W1 original catalog research #40](https://github.com/thgillwtnorizoh/rasc/issues/40), [W2 catalog+GC+UI first vertical slice #41](https://github.com/thgillwtnorizoh/rasc/issues/41).

## Objective

RASC must become an **independent, read-only recreation of original CS:GO achievements** with a browsable earned/locked library, authentic names/descriptions/icons where lawfully available, source-confirmed and provenance-labelled progress, and actual newly GC-confirmed unlock notifications. This is **not** Steam achievement publishing and does not alter game/GC/Steam files.

Historical target: reported **167 legacy CS:GO achievements**. This number is supported by older community catalog references and Valve's historical Library screenshot (which shows 10/167), but Work 1 must establish an exact version-matched complete original schema. The current CS2 AppID 730 schema is **not** a trustworthy source for the old 167 legacy entries. The Steamworks API supports separately retrieving achievement API names, localized names/descriptions, icons and unlock time (generic semantics, not proof of actual CS:GO rules).

## Current implementation and gap

- GC writer authority: [GT-610/csgo-gc local_user_stats](https://github.com/GT-610/csgo-gc/blob/72e694d48485daaa569d218992500e5c724345f6/csgo_gc/local_user_stats.cpp). Version-1 `csgo_gc/achievements.txt` has integer stats, float stats and a map of achievement API IDs to unlock timestamps. It does **not** contain human-readable descriptions, icons, or full criteria. A valid saved achievement is source-confirmed as unlocked; inferred criteria/progress require independent original rule evidence.
- Existing RASC: `GcKeyValuesParser` / `GcFileSource` preserve strict read-only valid/missing/invalid/unavailable semantics; `AchievementMonitor` watches read-only; `UnlockEventStore` stores a source-specific first-read silent baseline, current achievement_state and unlock_history in app-owned SQLite. Its public API exposes only historical events; the current complete state has no reader. `ClassicToastHost.Enqueue(evt)` and `ModernToastHost.Enqueue(evt)` use raw internal IDs for actual notifications; standalone previews are different.
- Required first vertical slice: versioned real catalog subset (3–5 independently evidenced original IDs), current GC-earned/locked library view for every verified catalog ID, names/descriptions in both actual Classic/Modern alerts, exact timestamp display and unknown-ID fallback. Preserve all Stage 1 visuals/timing and never replay historical unlocks after catalog load.
- Local data privacy: never download a user's achievement state to servers or send Steam account identifiers; accept only owner-selected local files and rights-cleared resources. Steam/GC installation scanning work from P005 remains bounded. No direct Steam API credentials or game injection.

## Work division and gates

**Work 1 (independent requirements and QA):** Establish actual 2023 legacy ID/name/description/category/icon/criterion provenance, source version, confidence and fixture. Validate first 3–5 IDs and rule compatibility with GT-610's raw keys before Work 2 may present any name as authoritative. Maintain coverage and discrepancy register for the remaining catalog; split into bounded original-research batches. Later independently test exact Work 2 production code and Windows runtime.

**Work 2 (implementation):** Versioned strict catalog loader with unknown-safe lookup; extend read-only current achievement state projection while preserving SQLite history and source isolation; WPF achievement collection and counter; real event title/description binding for both toast profiles; no unsupported progress or auto-award; tests, build, Windows GUI/publish, and reproducible checkpoint. Work 2 must never substitute its own implementation fixtures for Work 1 independent source expectations.

**HQ:** Keep dependency/coverage/discrepancy register, review exact SHA, reconcile requirements and independent QA; owner decides acceptance. Only move from bounded initial catalog to full-set expansion once sample is integrated and verified. Do not merge P004/P005/P006 to main without resolution of proprietary image rights and explicit owner approval.

### Sequential campaigns

| Campaign | Deliverable | Dependency | Stop/acceptance condition |
|---|---|---|---|
| P006-C01 | Authoritative initial IDs plus GC-confirmed earned library and named live unlocks | W1 #40 then W2 #41 | 3–5 real original achievements, true locked/earned state, no phantom events, Windows dev + independent QA |
| P006-C02 | All version-matched original achievement catalog entries and categories | Verified catalog source and rights | Independently verified coverage register accounting for all original IDs (historical target 167), UI handles absent icons |
| P006-C03 | Source-informed progress rules for supported stats achievements | Original per-rule fixtures, GC integer/float stat mapping | Exact thresholds and mode gating; unsupported cases explicitly unknown; no false unlock |
| P006-C04 | Icon/localization pipeline and complete achievement library UX | Versioned originals, asset redistribution permission or local-asset reader | Safe local-only asset resolution, no bundled unlicensed Valve files, UI/platform QA |
| P006-C05 | Gameplay qualification, recovery and final acceptance | Work 1 independent QA + real user Windows/GC comparison | No regressions in Stage 1, known discrepancies accepted, owner approves P006 |

## State/confidence contract

- **GC-CONFIRMED UNLOCK**: key/timestamp appears in a successfully parsed valid `achievements.txt` from the bound source. First startup is shown in library but **never** creates historic popup storm.
- **NOT OBSERVED / LOCKED**: key absent in the latest valid snapshot (after a valid baseline). Do not equate missing/corrupt/unavailable file with 0 achievements. Keep prior validated state or show unknown if no baseline.
- **ORIGINAL RULE-VERIFIED**: exact original source/condition version/threshold verified by Work 1.
- **DERIVED PROGRESS**: numeric prediction from a verified rule and current stats, separately labelled. No automatic Steam or GC unlock commits.
- **UNSUPPORTED / UNKNOWN**: missing catalog mapping, hidden achievement, inaccessible icon, absent stat or ambiguous criterion; render raw ID or neutral placeholder without inventing facts.
- **EXPERIMENTAL PREVIEW**: locally generated test event, never written to GC/Steam/database history. Keep previews isolated.

## Evidence / tools

- Generic Steamworks interface and names/icons/time: https://partner.steamgames.com/doc/api/isteamuserstats
- Historic Valve-era original 167 count: 2019 Valve Steam Library screenshot (10/167), https://steamcdn-a.akamaihd.net/appmgmt/gdc2019/slides/gdc_2019_talk_english.pdf
- GT-610 local storage reference: https://github.com/GT-610/csgo-gc/blob/72e694d48485daaa569d218992500e5c724345f6/csgo_gc/local_user_stats.cpp
- Project starting snapshot and tests: [P005-C01 Windows CI](https://github.com/thgillwtnorizoh/rasc/actions/runs/44468131757) 198 core and 10 WPF passed. Real P004 sound playback and P005 discovery not yet owner qualified.

**Subsystem:** P006 OPEN. This document is a verified planning checkpoint only; no new original catalog items or executable achievement code are being claimed by the Headquarters kickoff.
