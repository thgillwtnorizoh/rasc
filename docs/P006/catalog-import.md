# P006 — Curated achievement definition import (v0.2)

**Owner decision:** P006-RARITY-v0.2 is the accepted, editable RASC rarity policy. It is an **ENGINEERING** classification, **not** original Valve/Steam global rarity, an original achievement award rule, or a proven recovered historical Steam schema.

## Simple database, one file

`src/Rasc.Core/Data/achievement-definitions.v0.2.csv` contains **167 candidate IDs** from the owner-local icon/localization audit and the approved v0.2 scoring workbook. Expected initial split: **63 RARE / 104 NORMAL**. This CSV has no bundled Valve icons, names or descriptions; the last two remain an independent read-only game-localization requirement for a later slice. The original shipped catalog/API-ID crosswalk is still **not independently complete**.

The same CSV is (1) embedded inside `Rasc.Core.dll` so startup needs no extra files and (2) copied to an editable `Data/` folder on build/publish. It can be loaded with:

```csharp
var catalog = CuratedAchievementCatalog.LoadDefault(); // editable Data CSV if present; otherwise embedded
var definition = catalog.Find("KILL_ENEMY_HIGH");       // null for unknown IDs
var rarity = definition?.Rarity;                       // Rare, Normal, or unknown
```

The database intentionally stores *only metadata*: API ID, expected relative JPEG filename, owner-local JPEG SHA-256, five 0–5 scores, series-capstone YES/NO and optional owner rarity override. **No local image path is trusted from CSV.** A later asset resolver must anchor these names under the verified game installation directory, check hash/file size and decode safely. No game paths, Steam accounts, unlock flags or achievement progress are in this CSV.

### Editing without rebuilding

Open the published `Data/achievement-definitions.v0.2.csv` in a plain text editor. Keep the header and API IDs intact unless evidence supports a real identity correction. For a judgment change:
- Change one or more of `m,c,s,o,t` (integers 0–5), or `series_capstone` (`YES`/`NO`). The normal/rare classification automatically recalculates with threshold **17/25** and capstone exception.
- Or set the last `rarity_override` column to `RARE` or `NORMAL` to preserve the factor scores while overruling the classification. Leave it blank for the default rule.
- Reopen/reload the catalog to apply changes. Invalid syntax, duplicate IDs, bad factor scores, unexpected columns or unsafe filenames raise `FormatException` rather than silently changing the catalog; restoring the shipped file resets defaults.

For local experiments, `CuratedAchievementCatalog.LoadDefault(pathToCsv)` accepts an **explicit** file path; a missing explicitly selected file raises `FileNotFoundException`. Any edit of this file is an application-owned modification, never a write to CS:GO, Steam or GT-610 data.

The CSV is intentionally plain (no free-text values or CSV-quoted fields) to minimize parser complexity. Preserve uppercase IDs, lowercase `.jpg` basenames and 64-character uppercase/lowercase hexadecimal icon hashes. Header: `api_id,icon_filename,icon_sha256,m,c,s,o,t,series_capstone,rarity_override`. Comments begin with `#`.

## Source, evidence and what is still unverified

**Owner-supplied source:** `RASC-P006-curated-rarity-v0.2.csv`, SHA-256 `1e33bca82450b3e6fc98c51708f995a90ab2e351aa3c62d342b6bfc09db22d4b`; 167 distinct IDs, all source totals equal their five factor sums. 12 marked capstones, six below 17 which qualify through the exception. Independent source counts are 63 rare/104 normal. `rasc-icon-audit.csv`, SHA-256 `8ba3a297a69362695750454fac68681396da0c12cb9732811322df85a14f0127`, records owner-local icons but is not a shipped-artwork authority.

User independently observed **181 JPEGs each** under:
- `csgo/materials/panorama/images/icons/achievements/`
- `csgo/resource/flash/images/achievements/`

The user reports identical filenames and **zero SHA-256 mismatches** between corresponding files, not a verified physical collection present on CI. The earlier `materials/vgui/achievements/` VPK content was **CS:S artwork** and must not be used for CS:GO icons. Do not import copyrighted image bytes or full game localization into Git.

The actual official legacy 167-ID Steamworks crosswalk, per-achievement conditions/categories and original icon relationship require independent version-labelled research. Approved *rarity* is independent of that question; this loader does **not** award achievements or assert local GC unlocks. The single modern AppID 730 `PLAY_CS2` schema found in 2026 is not the historical source.

## Next integration boundary

Work 1: independently verify first 3–5 historical API IDs and original game conditions, including mismatch handling. Work 2: compose source-confirmed current GC earned states with this metadata, hydrate authentic text from owner-selected local `csgo_<language>.txt` and JPEG assets read-only, and route confirmed new events through existing Classic/Modern toasts. Preserve silent first snapshot, deduplication and no progress/award inference.

**Acceptance boundary for this slice:** one editable catalog + embedded fallback + strict validation + development tests. No GUI, toast, GC or physical Windows behavior is accepted by the database import itself. **P006 subsystem remains OPEN.**
