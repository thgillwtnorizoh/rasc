# P006 / L001 — Historical CS:GO localization and local VPK/VTF achievement icon discovery

**Project/Campaign/Actor:** RASC / P006-C01 / Headquarters source gap research, for Work 1 #40 and Work 2 #41.  
**Status:** VERIFIED RESEARCH FINDING for archived original localization **only**; owner-local VPK entry list and icon decoding **NOT YET VERIFIED**, and no achievement API-ID↔token or criteria equivalence claimed.  
**Owner observation date:** 2026-11-19.

## Discovery and independently checked provenance

Owner found legacy Source 1 CS:GO localized text under `<CSGO-root>/csgo/resource/csgo_<language>.txt`. English contains:
- `SURVIVE_GRENADE_NAME` -> Shrapnelproof; `SURVIVE_GRENADE_DESC` -> Take 80 points of damage from enemy grenades and still survive the round.
- `WIN_KNIFE_FIGHTS_LOW_NAME` -> Make the Cut; `..._DESC` -> Win a knife fight.
- `WIN_KNIFE_FIGHTS_HIGH_NAME` -> The Bleeding Edge; `..._DESC` -> Win 100 knife fights.
- `KILLED_DEFUSER_WITH_GRENADE_NAME` -> Defuse This!; `..._DESC` -> Kill the defuser with an HE grenade.
- `SURVIVED_HEADSHOT_DUE_TO_HELMET_NAME` -> Safety First; `..._DESC` -> Survive a shot to the head because you had the good sense to wear a helmet (Competitive Mode only).

**Direct independent corroboration**: read SteamTracking [historical `csgo/resource/csgo_english.txt` as of 2023-04-25](https://github.com/SteamTracking/GameTracking-CS2/blob/e01c9b47aa6c4b253f1a5888a5693ececc1d2328/csgo/resource/csgo_english.txt). Repo `SteamTracking/GameTracking-CS2` previously tracked legacy Source 1 CSGO and contains older history even though today's repository name is GameTracking-CS2. Historical git commit `e01c9b47aa6c4b253f1a5888a5693ececc1d2328`, original file Git blob SHA-1 `800b1e5ab3ced926b0a5a58dacdec66461f05dd2` (3,203,808 source bytes). All FIVE owner quotation pairs found verbatim in archived source by reading exact blob.

**Exploratory metadata counts (not achievement counts):** A broad case-sensitive regex matching `"([A-Z0-9_]+)_(NAME|DESC)"\s*"([^"\r\n]*)"` found **185 distinct token-base groups**, **183 groups with both NAME and DESC**. The source contains unused/cut and potentially non-achievement tokens, so **these numbers must NOT be treated as active original achievement totals**, and cannot replace the historical target 167. Also, presence of a displayed English requirement does not establish an implementable event trigger.

**Crosswalk requirement:** Work 1 must independently identify the actual API names of the archived Steamworks achievement schema/legacy game, the exact active set, removed entries, and matching category, goal, icon reference and progress stat. A base localization token may coincide with an API ID but there is NO blanket proof of identity. Only map if IDs verified by original source or independent direct behavior. Wrong or unknown IDs show safe raw fallback; a missing locale key must not create an award.

## Owner-local Source 1 icon location and verified tooling

Owner reports icon assets inside their installed game's `csgo/pak01_dir.vpk` archive (the owner typed a different spelling, so exact file path/case remains to be established), internal `materials/vgui/achievements/`. The Source 1 archive is commonly split into `pak01_dir.vpk` (index) and neighboring `pak01_000.vpk`, etc (data chunks); reading only the index may not provide full icon bytes. The VPK may contain `.vmt` material declarations referring to `.vtf` textures; the textures are encoded images, not bytecode and do not need code disassembly.

Independent tools supported by their upstream documentation:
- [craftablescience/VPKEdit](https://github.com/craftablescience/VPKEdit): MIT licensed; Source 1 VPK browse, preview VTF texture, extraction, standalone `vpkeditcli` [CLI guide](https://vpkedit.com/vpkeditcli-tutorial/).
- [Sky-rym/VTFEdit-Reloaded](https://github.com/Sky-rym/VTFEdit-Reloaded): Source 1 VTF preview and `vtfcmd.exe -exportformat png` conversion (read project's `README.md` for options).
- [craftablescience/sourcepp](https://github.com/craftablescience/sourcepp): MIT C++ package/texture parsers `vpkpp` and `vtfpp`, bindings of varying completeness for C and C#; candidate for RASC integrated local on-demand asset resolution, **not yet tested**. Dependency compatibility and Windows clean deployment must be separately proven.

**Safe manual asset inspection** (from owner-installed game, without writes to the live game directory; verify actual paths and tools' `--help` first):

```powershell
vpkeditcli.exe "C:\Path\To\CSGO\csgo\pak01_dir.vpk" --file-tree
vpkeditcli.exe "C:\Path\To\CSGO\csgo\pak01_dir.vpk" --extract "materials/vgui/achievements/" --output "C:\RASC-Local-Research\achievements"
vtfcmd.exe -folder "C:\RASC-Local-Research\achievements\*.vtf" -output "C:\RASC-Local-Research\png" -recurse -exportformat png
```

Tool flags supported by upstream docs; actual user fixture and exact output path for directory extraction NOT VERIFIED. For CLI converter, adjust `-folder` to the location of extracted `.vtf` files and perform conversion to a private workspace. Avoid extracting full VPK unnecessarily. Do not commit extracted Valve icons, VTFs or complete locale files to a public repository; hash, source path, expected dimensions and a neutral internal test icon should be used in CI instead.

## Integration recommendation and acceptance gates

**Bounded Stage 2 plan:** (1) independent real-ID active catalog crosswalk; (2) local read-only localization file selection by locale/encoding (support actual BOM and original escape handling, no arbitrary game writes); (3) local-only VPK/VTF asset reader for icon display (on-demand cache only with explicit owner permission, safe path validation/format limits); (4) live GC-confirmed unlock maps to name/description/icon; (5) library shows locked/unlocked and unknown progress; (6) Work 1 independently checks real installed CS:GO on exact Windows build. Icons are optional in first slice; authoritative names and state may land before VPK reader if decoder/dependency is blocked.

**Critical discrepancy:** The Source 1 game locale can contain extra/cut achievement candidates beyond the shipped active Steam achievement set; neither a `_NAME/_DESC` pair nor VTF presence independently proves the achievement is official/earnable. `csgo_gc/achievements.txt` remains authoritative for actual observed unlock stamps, not for complete definitions or criteria. Do not auto-unlock from text descriptions.

**Tests performed this research step:** Git archive localization direct inspection and five matching token pairs PASS; regex token-base counts 185/183 reproduced from exact blob. **NOT RUN:** owner game files, vpkeditcli, VTFCmd, SourcePP, exact VPK image decoding, RASC tests/build, Steam runtime achievement matching. Existing P005 exact source CI remains 198/198 core and 10/10 WPF at `e84b452a54c4df1319f03492485e67ddc7b5a6c8`; no claims about this research-only document. Work 1 #40 and Work 2 #41 comments record new dependencies. P006 subsystem OPEN.
