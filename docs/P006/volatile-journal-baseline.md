# P006-C06 — Volatile session-earned baseline and no startup popup replay

Project / Campaign / Actor / Assignment: CS:GO Achievements Resurrection / P006-C06 / Work 2 / drop SQLite replay, implement one-time match-earned baseline.
Starting SHA: `41a3a30e4ea917de44a12ab24c78a86c7940102a` (working P006-C05 stack / PR #49). Actor: Work 2. Authority: owner runtime feedback 2026-11-22.
Status: ENGINEERING CHECKPOINT; exact branch CI and owner runtime not yet verified.

## Reported discrepancy and boundaries
Owner confirms instant toasts now render. Old achievement popups reappear when RASC starts monitoring; root cause: persistent `history.db` pending history was replayed, and preexisting journal records could be treated as new by the monitor. Four grind awards also reappear after GC reset; GC-I04 separately adds a post-reset write fence.
Owner specifically instructs: **discard database**; during match attach/join use achievements.txt only to determine already-earned state, log for new awards; if log is missing use achievements.txt for both.

## Implemented
- Replace `UnlockEventStore` with an in-memory, monitor-session-only ledger. No SQLite writes/reads, migrations or durable pending notifications. Remove SQLite package dependencies and exception references. Existing `history.db` is left untouched; RASC does not read it and users can archive/delete it voluntarily.
- New monitor Start always uses a fresh ledger; first existing journal content is silently acknowledged, with source/session/sequence watermark; no old unlocks are dispatched.
- With journal present: native `match_start` marker (`reason:join|reset`) prompts a one-time `achievements.txt` earned-state read. Subsequent journal unlock records are the ONLY source of new toasts, and existing queues/UI scheduling are preserved. Snapshot is not polled for notifications while journal exists; malformed/unavailable journal fails closed.
- Without journal: snapshot-only legacy mode uses first valid snapshot as silent baseline, subsequent observed changes as notifications. Switching to fallback silently rebaselines.
- The monitor continues to watch the two GC files, recovers missed notifications via periodic 250ms reconciliation and preserves source isolation. No original game files are modified by RASC.
- New test fixtures: startup no replay; second monitor instance starts empty; first match and reset marker parser; one-time snapshot on join; fallback and journal identity, baseline, original monitor regressions.

## Evidence / confidence
RECONSTRUCTED user-observed problem, ENGINEERING implementation. CI run for branch head pending; do not claim acceptance until completed. Independent original 2023 client / owner qualification NOT RUN.
Cross-repo dependency: GC-I04 PR #5 of `thgillwtnorizoh/csgo-gc` emits a `match_start` on actual match join or reset; earlier GC versions produce an empty file and RASC still has startup silence but cannot reliably distinguish a new match for the earned snapshot.
Known unavoidable discrepancy: owner requires GC to clear `achievement_log` immediately at match completion; any unlock added and removed entirely between RASC polls can be lost. Truly lossless delivery would require retention/acknowledgment and owner approval. Tests do not claim this is solved.
No notifications for old awards when starting monitoring mid-match; only entries appended after the initial attach are eligible. No persistent notification history after RASC shutdown; intentionally owner-requested.

## Verification gates
- Exact SHA Windows Core xUnit tests, native WPF real-window smokes, full app package build and artifact upload.
- Real legacy CS:GO local game: monitor restart; join with saved achievements; grind reset and process restart; fresh post-restart earn; new match marker; legacy missing-log fallback.
- Archive old history.db if user wishes; do not delete automatically.
- Document remaining risks and test SHA; preserve all previous source and CI coverage.
Subsystem: OPEN pending owner acceptance.
