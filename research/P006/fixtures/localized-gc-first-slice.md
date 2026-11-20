# P006-C01 / Work 1 — Independent localized ID and GT-610 fixture authority

**Purpose:** Source-side expectations for the next read-only localization/definition join. This file is independent of the parser implementation; fixture data is **synthetic** and does not claim original unlock gameplay results.

## Sources checked

1. Owner-supplied `rasc-icon-audit.csv` (CSV generated from owner-local legacy CS:GO installation, SHA-256 `8ba3a297a69362695750454fac68681396da0c12cb9732811322df85a14f0127`), containing the original local JPEG basenames, icon hashes and English name/description token matches. Provenance is user-local original resource, not an archived official Steamworks API schema.
2. `GT-610/csgo-gc` `csgo_gc/local_user_stats.cpp` at commit `72e694d48485daaa569d218992500e5c724345f6`, blob SHA-1 `7843454c126b2edb4bb9626611da868c5086666d`: `Store::Save()` writes top-level version `1`, optional `integer_stats`, `float_stats` bit patterns and `achievements` API-ID→unsigned timestamp pairs. `Store::GetAchievement()` reports the key's presence as awarded; stats alone are not authoritative award flags.
3. Pre-CS2 SteamTracking historical `csgo/resource/csgo_english.txt` commit `e01c9b47aa6c4b253f1a5888a5693ececc1d2328`, blob SHA-1 `800b1e5ab3ced926b0a5a58dacdec66461f05dd2`. Earlier research confirmed sample `_NAME`/`_DESC` tokens, but the exact owner-installed resource has **not** been executed in CI.

## Five independently selected catalog/description crosswalk fixtures

| API ID in local audit | Local display name | Evidence of described requirement (paraphrase, not a reconstructed trigger) |
|---|---|---|
| `KILL_ENEMY_HIGH` | God of War | Cumulative 10,000 enemy kills |
| `KILL_WITH_OWN_GUN` | Lost and F0wnd | Kill someone with their own dropped weapon in the current round |
| `UNSTOPPABLE_FORCE` | The Unstoppable Force | Four kills in a single round |
| `IMMOVABLE_OBJECT` | The Immovable Object | Kill a player responsible for four teammates' deaths that round |
| `WIN_ROUNDS_WITHOUT_BUYING` | The Frugal Beret | Win ten consecutive Classic rounds without dying or spending money |

**Source confidence:** **CONFIRMED LOCAL AUDIT** names/descriptions and icon base matching, **OWNER-ACCEPTED ENGINEERING** rarity, not independent official award-rule verification.

**Non-admitted entry:** `BOMB_DEFUSE_NEEDED_KIT` also has a local name/description and image but was excluded from the owner-approved 167 candidate rows. The join must not create a canonical achievement simply because extra localization exists.

## Independent synthetic GT-610 fixtures (test values are not original observations)

**S0: first valid observation.** Input includes:
- `version=1`
- `integer_stats.total_kills=10000`
- `achievements.UNSTOPPABLE_FORCE=1790000000`
- `achievements.KILL_WITH_OWN_GUN=1790000060`
- `achievements.NOT_IN_CATALOG=1790000100`

Expected: GT-610 parser produces three awarded IDs. Exactly two are present in the curated catalog. `KILL_ENEMY_HIGH` remains **unawarded** even with `total_kills=10000`. The first observation of existing awards remains a **silent baseline** under the established monitor/event-store contract; joining metadata is never a trigger.

**S1: subsequent verified snapshot.** Keep S0 achievements and add `IMMOVABLE_OBJECT=1790000300`. Expected: if the persisted monitor baseline is S0, precisely one new award event for `IMMOVABLE_OBJECT`; no extra event for the previously recorded IDs; no fabricated `KILL_ENEMY_HIGH` award.

**S2: missing, malformed or unavailable file.** Expected: no new awards and no resetting previously valid current state; a view must show unknown/unavailable when no valid baseline exists. Localized names are not authoritative state.

**Source localization scenarios:**
- English UTF-16LE BOM, complete paired `<API>_NAME` and `<API>_DESC` keys -> display one coherent pair.
- Preferred locale missing -> use English and flag fallback; preferred locale partial -> use complete English pair rather than mix languages; preferred locale present but invalid -> report invalid.
- Missing English pair -> unresolved name/description; do not invent text from the API ID.
- Cut/extra localized pair not in approved catalog -> no catalog row and no automatic unlock.

## Acceptance and remaining gaps

Work 2 may ship read-only parser/join + synthetic fixture tests. Independent original runtime/2023 exact API-ID mapping and the physical owner's CS:GO `csgo_english.txt` remain **NOT VERIFIED** here. Actual owned game resources, credentials and copyrighted text are never copied into GitHub.

**Next:** independent CI test of parser and fixture cases; then read-only GC state projection and authentic live-toasts after separate acceptance. This is not proof of overall P006 completion.
