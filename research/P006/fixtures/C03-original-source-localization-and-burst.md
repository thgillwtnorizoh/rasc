# P006-C03 — Independent original-source localization and owner runtime QA

**Actor:** Work 1 source research / independent requirement derivation (same owner-authorized combined execution; **not** second-agent production QA).
**Campaign:** P006 — CS:GO achievement resurrection; C03 — English strings + bulk-burst follow-up.
**Owner evidence:** Four new in-game screenshots (2026-11-20) show authentic local artwork now visible, but `UNSTOPPABLE_FORCE`, `KILL_ENEMY_TEAM`, `LAST_PLAYER_ALIVE`, `WIN_BOMB_DEFUSE`, `KILL_ENEMY_RELOADING` and `GIVE_DAMAGE_LOW` still displayed as raw API IDs and no descriptions; user reports bulk delay and Classic headline wrong. These are owner runtime observations; screenshots alone do not provide GC milliseconds or actual source file bytes.

## Independent original-source analysis

**CONFIRMED ARCHIVED ORIGINAL FILE:** SteamTracking/GameTracking-CS2 2023-04-25 commit `e01c9b47aa6c4b253f1a5888a5693ececc1d2328`, `csgo/resource/csgo_english.txt`, blob SHA-1 `800b1e5ab3ced926b0a5a58dacdec66461f05dd2`. Fetched via public GitHub blobs API and scanned with an **independent JavaScript KeyValues scanner**, not the RASC C# parser.

- Historical decoded text length: **3,201,409 characters**.
- Number of physical newline characters inside quoted string values: **22**. First encountered early in `Cstrike_TitlesTXT_Alias_Not_Avail`, far before the achievement section.
- **Original parser failure:** old RASC `CsgoAchievementLocalization.KeyValuesTokens.Read()` throws `FormatException` at the first physical newline *inside* a quoted value, so no achievement text is returned, despite complete name/description keys later in the same file.
- **Second independent parser failure:** **four deliberately conflicting duplicate `_DESC` definitions** occur later: `GUN_GAME_FIRST_THING_FIRST_DESC`, `GUN_GAME_TARGET_SECURED_DESC`, `LOSSLESS_EXTERMINATION_DESC`, `FLAWLESS_VICTORY_DESC`. Rejecting conflicts makes this historically valid file unreadable even after fixing newlines. The later definition is intended as the replacement text in the sequential file; RASC now applies last-wins. This is an ENGINEERING decoding policy based on order, not original API behavior proven with a live engine trace.
- Independent scan after allowing quoted physical newlines and applying last-wins: **30,982 token pairs, 184 distinct `_NAME` ID candidates and 183 complete nonempty `_NAME`/`_DESC` achievement pairs**. Sample source-confirmed entries include `KILL_ENEMY_RELOADING` -> `Shot With Their Pants Down`, `KILL_ENEMY_TEAM` -> `The Cleaner`, `WIN_BOMB_DEFUSE` -> `Rite of First Defusal`.
- No copyrighted whole-file text or images checked into RASC. Focused tests use **synthetic** multiline and repeated-key fixtures; developer can compare against the historical blob by SHA.

## Independent behavioral expectations

- L1: unmodified 2023 English KeyValues with literal newline + later duplicate achievement descriptions parses to 183 complete pairs, never all raw IDs.
- L2: later repeated `_DESC` determines the displayed description; malformed unclosed quotation remains invalid.
- L3: invalid/missing user-local text is shown **visibly as Not Ready**, not hidden in a tooltip replaced by poll status; raw IDs remain safe fallback, not fake Valve strings.
- C1: Classic live toast must display fixed header `Achievement Unlocked!` and **achievement name** as the second text field; verbose requirement is a Modern description (Classic has only two text slots).
- B1: 3 or 4 new GC awards in a **single valid file reconciliation** must be delivered in exactly **one** WPF Dispatcher callback and one Modern 45ms admission batch, avoiding artificial 5.6-second delay between the first and second entries of the same burst. Later awards in a full 3-card stack may still wait a normal 5.6-second wave; the owner-accepted 3-card cap/300+5000+300ms animation has **not** been silently revised.
- B2: 1 valid baseline -> zero toast; repeated unchanged GC state -> no replay; exception in batch delivery -> pending events remain in app-owned SQLite for retry.
- P1: 25%, 50%, 75% progress suggestion is accepted **as a design target** only where a GT-610 source counter and exact goal/achievement-ID binding can be independently established. No current owner screenshot or GC unlock timestamps prove those mappings. Avoid invented progress; implement pure milestone policy only and do not enable live progress or persist fake milestone events yet.

**Classification:** CONFIRMED ARCHIVED ORIGINAL localization behavior; CONFIRMED ENGINEERING parser defects and source ordering; OWNER-OBSERVED current runtime UI; RECONSTRUCTED batching root-cause hypothesis; ENGINEERING C03 corrections; **UNVERIFIED ORIGINAL user-local file and post-fix game runtime**.

**Next independent verification:** Test the full version-matched owner-local file from the exact player's installation, compare parser pair counts and source paths, and run burst awards against actual GC file. Keep physical screenshots and CI synthetic tests separate; no original rules or Steam rarity inferred.
