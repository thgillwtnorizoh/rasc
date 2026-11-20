# P006 — Read-only local CS:GO achievement text import

**Scope of this slice:** Join the owner-approved, editable `P006-RARITY-v0.2` 167-ID catalog to authentic names/descriptions from the player's existing Source 1 CS:GO resource file. **No Valve text is committed. No Steam/GC data is written. No achievements are awarded by this feature.**

## Usage from RASC Core

```csharp
using Rasc.Core;

// The parent directory containing csgo.exe, or the inner csgo/ content folder.
string gameRoot = @"C:\Games\Counter-Strike Global Offensive";
var definitions = CuratedAchievementCatalog.LoadDefault();
var read = CsgoAchievementLocalization.LoadFromGameRoot(gameRoot, "english");

if (read.Status == ReadStatus.Valid && read.Catalog is not null)
{
    var entries = CsgoAchievementDefinitionJoin.Join(definitions, read.Catalog);
    // 167 candidate entries, in the v0.2 order; names/descriptions null when missing.
    var sample = entries.Single(x => x.Definition.ApiId == "KILL_ENEMY_HIGH");
    Console.WriteLine(sample.Name ?? sample.Definition.ApiId); // presentation fallback only
}
else
{
    Console.WriteLine($"Local CS:GO text unavailable: {read.Status}: {read.Detail}");
}
```

The runtime file path is `<gameRoot>/csgo/resource/csgo_english.txt`, or `<csgoContentRoot>/resource/csgo_english.txt` if selecting that directory. For `language="spanish"`, read the matching `csgo_spanish.txt` only within the same selected game directory. No recursive or arbitrary-drive scans; only lowercase ASCII locale strings accepted to prevent path traversal.

**Encoding:** strict UTF-16LE or UTF-16BE with BOM (original Source 1), plus UTF-8 with or without BOM (edited copies), bounded to 8 MiB compressed bytes / 4 Mi decoded characters. Unsupported malformed source is reported as `Invalid` without replacing previously valid state. Absent English is `Missing`; unreadable file is `Unavailable`. Locale missing -> explicitly marked English fallback; locale partial -> prefer complete English pair for that achievement. Present but invalid preferred locale -> `Invalid`, not silent fallback.

**Parser:** source-style quoted KeyValues strings under the `Tokens` block, comments, escaped quotes/backslashes/newlines, non-achievement tokens, conditional annotations and nested irrelevant objects. Conflicting duplicate achievement tokens are rejected rather than arbitrarily picking one value. A name or description alone is not a complete display pair. Source may include cut/unused entries; the join iterates the curated 167 IDs, never localization candidates.

## Data boundaries

- `CuratedAchievementCatalog`: 167 candidate API IDs, owner-curated rarity and relative icon filenames/hashes only, packaged and editable separately.
- `CsgoAchievementLocalization`: read-only name/description pairs from selected owner's game installation, no bundled Valve strings.
- `CsgoAchievementDefinitionJoin`: 167-entry projection combining two sources. A missing pair remains null/unknown; the UI can show the raw API ID later.
- `GcKeyValuesParser`: separate trusted parser for valid GT-610 local award timestamps. This localized join does **not** read or modify GC state, and does not guess awards based on statistics.

**Evidence:** [Work 1 independent five-ID fixture crosswalk](../../research/P006/fixtures/localized-gc-first-slice.md) uses the user's owner-local `rasc-icon-audit.csv` and code-checked GT-610 writer. Synthetic test strings are clearly labeled; original Valve text/artwork is never checked into the repository.

**Caveats:** Initial owner-local source inventory matches 167 curated icon/text candidates but an independently authenticated 2023 Steamworks schema crosswalk is still missing. Direct owner-local runtime qualification, full gameplay-original rule recovery and the actual GC earned-state library/toast pipeline are later steps. P006 remains OPEN.
