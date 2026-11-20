# P006-C01 — Read-only GC current earned state and achievement library

**Branch:** `feature/p006-c01-gc-earned-library`, stacked on draft localization PR #44. **Scope:** Current GC state projection and basic browseable list. Not real unlock toast binding yet.

## User interface

Choose a GC source as before, then click **Browse achievements** next to the event-history section. A separate resizable window shows the owner-approved 167 candidate CS:GO achievements, with read-only searchable/filterable rows: **State, Achievement, Curated RASC rarity, Description, Unlock time, API ID**.

- **Unknown:** The selected GC file has **never yielded a valid snapshot** to RASC. Never reported as locked.
- **Locked:** A valid baseline exists and this ID is **absent** from its last committed GC achievement map.
- **Earned:** The API ID **appears** in the last committed GC achievement map; even timestamp zero is awarded state (time unknown).
- **Uncatalogued GC IDs:** Render as extra earned rows with raw IDs and unknown rarity. They never modify or extend the owner-curated 167 catalog.
- **Missing/invalid/unavailable GC file:** View keeps the **last valid** source-specific state, with an explicit source warning; no accidental 0/167 reset.
- **Not monitoring:** Restores app-owned last valid SQLite state, if any. Does **not** create toasts from it.
- **Local text:** When the exact game root's `csgo/resource/csgo_english.txt` is readable, displays its authentic `_NAME/_DESC` tokens. Otherwise display raw API ID and "Description unavailable." No Valve localized strings bundled.
- **Reload files:** Re-reads edited `Data/achievement-definitions.v0.2.csv` and player-local text. Invalid edits produce an error rather than silently substituting a different catalog.

## Core contracts

`UnlockEventStore.GetCurrentState(sourceKey)` is a **read-only** query over existing `sources` and `achievement_state`, requiring no migration. It distinguishes a missing source baseline from valid empty achievement state and retains timestamps as unsigned `uint`. Existing `unlock_history` is unaffected.

`AchievementMonitor.CurrentStateChanged` signals **only after** successful validated GC input and committed SQLite state, and only when the IDs/timestamps actually differ. Missing/invalid files never raise an empty or reset state. Existing `UnlockDetected`, source isolation and dedup remain unchanged.

`AchievementLibraryProjection.Create` is a pure Core join over the 167 approved definitions, optional local CS:GO text and last valid GC achievement map. Missing names do not affect state; matching descriptions alone cannot grant awards. This is suitable for independent testing without WPF or original copyrighted resources.

## What is deliberately not claimed

The 167 imported IDs are owner-approved localized-icon catalog candidates; historical Valve Steamworks active-set and rule versions still require independent source work. No actual game runtime/physical Windows qualification, artwork loader or Classic/Modern live unlock notification binding is part of this checkpoint. The existing toast appearance and preview fixtures are unchanged.

**Test expectations:** [Independent source-side fixture matrix](../../research/P006/fixtures/earned-state-projection.md), focused Core and actual WPF browse/filter smoke tests, preserved whole Windows CI regressions. No game/Steam/GT-610 writes; only existing app-owned `history.db` is modified by the previously authorized monitoring logic.

**Subsystem:** P006 OPEN pending original verification, independent QA, live integration and owner acceptance.
