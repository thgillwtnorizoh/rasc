# P006-C11 — Achievement progress mapping and unlock priority

**Profile:** CS:GO legacy, RASC standalone read-only companion. **Subsystem: OPEN.**
**Status:** ENGINEERING / provisional classification, not independently verified Valve progress-notification behavior.
**Implementation baseline:** C10 `839b6839a009561e6fb50f678268f3db9dde29e4`.
**Rule:** `P006-PROGRESS-PRIORITY-01`.

## Eligibility is not a popup signal

`achievement-progress-mappings.v0.1.csv` is an owner-editable, embedded-fallback
registry with exactly the same 167 IDs as the curated rarity catalog:

| Class | Entries | Default production behavior |
| --- | ---: | --- |
| `SCALAR_CANDIDATE` | 86 | Disabled until exact stat, goal, evidence and game profile are verified |
| `COMPOSITE_CANDIDATE` | 9 | Unlock-only until subgoal producer is independently established |
| `UNLOCK_ONLY` | 72 | Unlock notifications only |

All shipped `live_enabled` flags are **NO**. The four GC trace correlations
(`KILL_ENEMY_LOW`→`total_kills`, `GIVE_DAMAGE_LOW`→`total_damage_done`,
`KILL_ENEMY_NEGEV`→`total_kills_negev`, `KILL_ENEMY_M249`→`total_kills_m249`)
are recorded as `TRACE_ONLY`, with deliberately **empty** goals and game-version profiles.
Their source correlation is not sufficient to activate progress.

To enable a mapping, independent/owner evidence must establish the *exact* stat ID,
positive numerical goal (at least 2), monotonic semantics, applicable CS:GO game
profile and support for the achievement's true award rule. Document that provenance
in `evidence` with a `VERIFIED:` prefix and set `live_enabled=YES`.
This prefix is a local operator attestation and **does not perform automated
external evidence verification**; do not use it to bypass research. Invalid or
partial enabled records fail closed. The complete 167-ID set must match the
installed curated catalog. No generated achievement or Steam award is persisted.

## Runtime producer

With zero verified mappings, the additional stat polling path remains OFF.
When mappings are explicitly enabled, the monitor independently reads the
validated `achievements.txt` snapshot (read-only) at its normal reconciliation
cadence, even when an `achievement_log` journal is present. This supplementary
snapshot is **never** fed to the earned-state unlock delta detector in journal
mode. The journal continues to be authoritative for observed live unlocks.

For each `(source key, achievement ID)`:
1. Initial valid stat reading establishes a silent baseline.
2. A counter decrease or missing/invalid stat cannot generate progress;
   a subsequent valid counter is evaluated against its own prior reading.
3. A rising counter before the goal crosses 25%, 50%, or 75%. Crossing
   several in one observation generates only the **highest** milestone.
4. Already emitted milestones are deduplicated for the process session.
5. Value at/above the goal does not generate progress; the normal unlock
   detector controls the unlock card.
6. Existing earned IDs in a valid snapshot and all newly accepted journal
   unlocks immediately suppress subsequent progress for that ID.
7. Progress is a synthetic local presentation event; it is **not** a
   `SetStat`, `StoreStats`, or Steam/GC `IndicateAchievementProgress` callback.

The current observed journal format provides unlock/match-start records;
we have **no observed native GC progress API attempts**. The owner's trace
summary reports 394 accepted stat writes, 10 achievement calls, zero
progress API attempts. No unobserved call is inferred.

## Priority / queue invariants

**Accepted unlock for achievement A:**
- cancels A's progress entries waiting for cross-profile dispatch;
- cancels A's progress in Classic scheduler's waiting admissions;
- removes A's progress from unpresented Modern wave groups, preserving
  FIFO order and original collection deadlines for surviving awards;
- permanently rejects later A progress for the current monitoring session;
- leaves unrelated achievements and cards already admitted to a renderer untouched;
- never rewrites the source GC, earned history, or the renderer's animation clock.

"Presented" means handed to the Classic entrance motion or to a sealed
Modern scheduler wave. A card already presented at that boundary continues
through its natural animation even if its achievement unlocks.

Modern still collects an 800 ms source-time wave (up to three cards);
Classic retains 450 ms entrance + 6 s hold + 450 ms exit and up to three
visible slots. Existing sound ownership, rare glow, text, full-screen
placement, and C10 single-owner card content are preserved.

## Limitations and acceptance gates

- CSV entries are **engineering hypotheses**, not a verified Valve list.
- Manual authoring of enabled mappings does not establish original behavior.
- No composite producer exists; no accepted versioned stat→goal matrix
  exists yet. As shipped, **no live progress card is emitted by default**.
- Counter sources depend on the local GT-610 snapshot being available and
  updated; an unavailable or malformed file cannot prove progress.
- All progress state is process-local. No replaying old milestones on restart.
- Independent source verification, real CS:GO runtime and fullscreen WPF
  qualification remain **NOT VERIFIED**.
- Acceptance requires passing full Windows CI, independent QA, owner approval,
  and verified source targets. C11 is not subsystem completion.
