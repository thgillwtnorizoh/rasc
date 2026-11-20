# P006 / Work 1 — Independent current-state projection fixtures

**Authority:** Source-derived GT-610 v1 file contract at `GT-610/csgo-gc` commit `72e694d48485daaa569d218992500e5c724345f6` (`local_user_stats.cpp` blob `7843454c126b2edb4bb9626611da868c5086666d`) and previous RASC `UnlockEventStore` ledger contract. All sample IDs and UTC times below are **synthetic expectations**, not original game award observations.

## State semantics independently required

| Case | Input or transition | Required response |
|---|---|---|
| N0 | No source key ever successfully committed | 167 UNKNOWN; **not** 167 locked and not reported as 0/167 earned |
| B0 | First valid GT-610 snapshot, empty `achievements` | 167 LOCKED, 0 EARNED, no notification/history rows |
| B1 | First valid snapshot has `UNSTOPPABLE_FORCE=1790000000`, `KILL_WITH_OWN_GUN=0` | 2 EARNED, 165 LOCKED; both timestamps preserved; zero timestamp must not manufacture 1970 as a known unlock date; **no** initial toast |
| S1 | Repeated exact snapshot B1 | Same states; no new history event; no redundant state update |
| S2 | After B1, add `IMMOVABLE_OBJECT=1790000300` | 3 EARNED, 164 LOCKED; only the newly inserted ID creates one new event |
| M0 | Source file Missing, Invalid, Unavailable | **Retain last valid** persisted B1/S2 state; show source-warning or stale status; never zero it |
| I0 | Different GC path with no baseline | UNKNOWN for that source; never inherit other source's earned state |
| I1 | Other GC path with its own valid empty baseline | 167 LOCKED only for the other source; independent event ledger |
| T0 | GC `total_kills=10000` but no `KILL_ENEMY_HIGH` key | `KILL_ENEMY_HIGH` LOCKED under a valid baseline; stats do not grant awards |
| X0 | GC `NOT_IN_CURATED_CATALOG=1790000100` | Show raw ID and timestamp in an **extra source** row; no original title or curated rarity invented |
| L0 | Local text supplies `UNSTOPPABLE_FORCE_NAME`+`_DESC` | Visible original-local name/description, same state/rarity |
| L1 | Local text missing or malformed | Fallback API ID and description unavailable; GC earned state must still load |
| E0 | Valid earned ID with unsigned time `4294967295` | Source value retained (valid uint), no overflow on read/restart |
| R0 | Reopen existing app-owned SQLite after B1 | Rehydrate accurate current state and history without popup replay |
| UI | Open library on Windows without a valid source | 167 UNKNOWN rows, searchable/filterable, no unintended GC file write |

**Original/behavior confidence:** The `achievements` map is GC-confirmed awarded state, not original rule explanation. These are **RECONSTRUCTED requirement fixtures** from direct writer and prior storage semantics. They do not certify the complete original 167-entry Steamworks historical schema.

**QA separation:** Work 2 must implement independent source-keyed SQLite read-only projection and avoid destructive migration; independent Work 1 verifies production at exact SHA later. For this user-authorized combined actor execution, this research record is an expectation separate from implementation code but does not count as independent second-agent code QA.

**Regression:** Preserve event ledger, first-snapshot silent baseline, duplicate timestamp dedup, previous history, read-status fault handling, GUI responsiveness, 10 existing WPF smoke scenarios and no unnecessary GC/Steam file writes.
