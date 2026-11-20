# P006 / Work 1 — Independent source-data reconciliation for curated rarity v0.2

**Role:** Work 1 *source-side checking* while the same conversation is also authorized to perform Work 2. This does **not** substitute for independent production code/runtime QA.

**Owner-approved input:** `RASC-P006-curated-rarity-v0.2.csv`, SHA-256 `1e33bca82450b3e6fc98c51708f995a90ab2e351aa3c62d342b6bfc09db22d4b`. The owner accepted v0.2 ratings as editable engineering judgments. This is **not** an archived Steam official rarity snapshot.

**Comparison:** Read the exact owner-supplied CSV independently of the C# loader and compare all candidate ID, icon basename, icon SHA-256, M/C/S/O/T factor and capstone values to `src/Rasc.Core/Data/achievement-definitions.v0.2.csv` in the P006 import branch. Recalculate `M+C+S+O+T` and class `RARE` when score >= 17 or capstone YES.

| Check | Result |
|---|---|
| Source candidate rows | 167 |
| Distinct source API IDs | 167 |
| Committed CSV candidate rows | 167 |
| API IDs cross-matched | 167 |
| Source-to-commit differences (IDs, 5 scores, icon filename, hash, capstone) | **0** |
| Expected Rare / Normal | **63 / 104** |
| Series capstones | 12 |
| Below-threshold capstone exceptions | 6 |
| Source total-score vs 5 factor sums | 0 mismatches |
| Unsupported historical Steam original-definition crosswalk | **NOT VERIFIED** |

Spot-check expected examples **from owner input** (not implementation-derived):
- `KILL_ENEMY_HIGH` (God of War): score 14, capstone YES, RARE.
- `KILL_WITH_OWN_GUN` (Lost and F0wnd): score 19, RARE.
- `UNSTOPPABLE_FORCE` (The Unstoppable Force): score 18, RARE.
- `IMMOVABLE_OBJECT` (The Immovable Object): score 18, RARE.
- `WIN_ROUNDS_WITHOUT_BUYING` (The Frugal Beret): score 22, RARE.

**Separation of concerns:** These scores represent the owner's chosen rarity policy. No image/text presence alone certifies Steam achievement IDs, historically active entries, original unlock conditions or GC trigger behavior. The local `csgo_english.txt` strings and 181 JPEG source images remain external copyrighted resources.

**Not run:** Windows .NET build, C# catalog tests, actual CS:GO/GC runtime, formal second-actor independent production QA. A CI result is required before declaring the code checkpoint VERIFIED COMPONENT.
