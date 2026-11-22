# RASC P006 — Journal-first, snapshot-fallback integration

Project / Campaign / Actor / Assignment: CS:GO Achievements Resurrection / P006-JOURNAL / Work 2 / live GC journal consumer and fallback.
Starting SHA: `08023d22a8052b42f88cbb2a4384f4ac18ba7125` (P006-C05 working branch; currently open PR #49, not older main).
Published SHA: inspect PR head. Exact tested SHA: **NOT RUN** when written.
Status: ENGINEERING IMPLEMENTED; CI AND OWNER CLIENT RUNTIME PENDING.

## Goal and behavior
- When `csgo_gc/achievement_log` exists, RASC consumes newline-terminated JSONL v1 unlock events; an empty file (including post-match truncation) is valid and does NOT fall back to snapshot-derived notifications.
- When the journal is actually absent, preserve prior `csgo_gc/achievements.txt` first-snapshot baseline and delta detection.
- Invalid/unavailable journal data is fail-closed; never silently switch to snapshot-derived notifications on parse errors.
- The GC can create the journal on joining a match and truncate it at match end. RASC polls and watches both files. A partial trailing JSON record is ignored until completed; complete records are validated for version, (session, sequence) identity and unlock fields.
- Insert event history in the app-owned SQLite ledger keyed by source + journal session + sequence, not second-precision unlock timestamps. On repeated polls/restarts do not duplicate history or popup events.
- Keep current achievement library synchronized with validated persisted achievements.txt and the active live journal; the existing UI queue receives one source-confirmed batch. Snapshot updates are silent while journal exists.
- Read-only access to original game files; never create or truncate GC journal from RASC.
- Discovery accepts a journal-only GC installation (with csgo.exe and csgo_gc folder); browsing either valid path maps to stable achievements.txt source identity.

## Source contract and tests
Production: `GcAchievementJournal.cs`, `GcFileSource.cs`, `AchievementMonitor.cs`, `UnlockEventStore.cs`, installation/browse UI updates.
Development fixtures: journal missing/empty/partial, genuine unlock vs progress/store, invalid/duplicate identities, truncate/reuse, session restart idempotence, snapshot-only fallback, malformed fail-closed, and journal/snapshot no-double-award integration. Historical tests preserved.
CI: existing `.github/workflows/windows.yml` Windows production build and development tests. **NOT RUN** at document creation; inspect exact PR SHA and workflow result before claiming coverage.

## Discrepancies, risks and limits
An end-of-match truncation by GC can occur between RASC polls. Awards created and removed entirely inside that interval cannot be recovered from the journal; a confirmed lossless handoff would require an acknowledgment/retention protocol and owner approval. RASC does not infer synthetic award signals from stale stats while journal exists. Game event semantics (`player_connect_full`, `round_start`, `cs_win_panel_match`, `cs_match_end_restart`, `cs_game_disconnected`) remain UNVERIFIED RUNTIME against final 2023 client.
Existing history.db is preserved; journal event IDs use a distinct identity from historical snapshot IDs.
No changes to modern/classic wave scheduler, font, native achievement rules or Steam account.
Cross-repo dependency: GC-I03 PR #4 `thgillwtnorizoh/csgo-gc` requires its own CI and owner runtime gate. Enable GC `achievement_log` option explicitly.
Next bounded task: inspect and fix CI at exact PR head; independent physical CS:GO + RASC smoke for join-empty → unlock → match-end-empty → next-match → reset; owner accepts subsystem only after verification.
Subsystem: OPEN.
