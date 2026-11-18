# Renderer preservation v1 — UEP-001 engineering checkpoint

**Project / Campaign / Actor / Assignment:** RASC / P002 Classic + P003 Modern cross-project reference archival / Work 2 / Owner request 2026-11-18 to preserve both accepted renders for future projects.

**Status:** DOCUMENTATION DELIVERY CANDIDATE. Baseline development evidence independently retrieved; new documentation hash-lock script and new branch CI **NOT RUN at authoring**. This does not close P002 or P003.

**Starting SHA:** `373d04101d03f51135f2f1b192758efddad12f00` (C07 reference); `main` verified `425444dc7440163ec8b3f74ee0ad9c360d5896fd`. **Published SHA:** resolve from `documentation/steam-achievement-renderers-v1` draft PR, because no published commit existed when this draft was composed. **Exact tested production SHA:** `373d04101d03f51135f2f1b192758efddad12f00` in CI [44310193628](https://github.com/thgillwtnorizoh/rasc/actions/runs/44310193628). Subsequent docs publication CI must be reported separately.

## Objective / supported profile / functional changes
Versioned, portable source-grounded archival specification for **both** distinct Steam-style achievement renders. Source-locked manifest and offline verifier; Classic card geometry/fonts/stacks/source conflicts; Modern 1/2/3 shared-wave, progress, rare CSS mask/gradient/shadow semantics; reference fixtures, evidence and legal notices; faithful porting and visual regression protocol. **No production code, CSS glow, UI visual, mask bytes, app source or unit tests modified.** Intended base branch is the exact C07 experimental source, so a draft docs PR need not merge it into `main` or grant image redistribution rights.

## Evidence / classification
- **CONFIRMED ORIGINAL STATIC:** May 2023 SteamTracking resources for Classic; see `research/P000/classic/reference.md`.
- **CONFIRMED IN SUPPLIED BUILD:** modern build 11229305 (may contain mods); `research/P000/modern/reference.md`.
- **CONFIRMED PUBLIC REFERENCE:** Dataminer CodePen CSS and owner-supplied reference PNGs; not proven Valve-original.
- **OWNER-ACCEPTED VISUAL:** Classic card appearance 2026-11-17, C07 gold appearance 2026-11-18. Not full subsystem acceptance.
- **VERIFIED DEVELOPMENT COMPONENT:** 162 core tests, 6 real-WPF GUI smoke, WPF Release build and win-x64 publish successful at C07 source 373d.. (CI 44310193628).
- **INDEPENDENTLY VERIFIED:** NOT RUN; Work 1 #29 pending.
- **UNVERIFIED RUNTIME:** original Steam visual parity, physical DPI monitor screenshots, standalone repro in a foreign renderer.
- **RIGHTS:** rare PNG masks and external screenshot imagery unresolved; no added binary copies.

## Tests / commands / results
- Existing test authority: `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release` => 162/162 PASS in historical exact-source Windows CI.
- Existing six real-WPF smoke scenarios and self-contained Windows publication => PASS in CI 44310193628.
- New offline script: `python docs/rendering-reference/v1/verify-snapshot.py` => **NOT RUN at authoring**; must run on branch and verify 31 frozen baseline files.
- New docs branch WPF CI => **NOT RUN at authoring**, not needed to infer that unchanged source continues to work, but should be checked before any further acceptance.
- No owner native monitor test or new Work 1 QA run.

## Coverage gained / remaining
Gained durable geometry/color/timing/mask-source specifications, exact code references, Git SHA content locks, non-duplicated rights notices, reproducible fixture matrix and explicit future reimplementation method. Remaining durable screenshot custody: owner must privately save expiring GitHub Actions screenshot/build ZIPs and externally supplied classic images; do not public-commit third-party copies absent rights review. New project must perform independent visual/runtime QA for its target engine.

## Discrepancies / limitations / blockers
Classic owner design vs pinned source font, alignment and border; Modern supplied build customizations and CodePen-to-client provenance; CSS/WPF shadow/compositor and cross-DPI differences; third-party asset rights; original Steam/CS:GO runtime unverified. No full P002/P003 acceptance claimed.

## Updated records / reproduction
`docs/rendering-reference/v1/{README,classic,modern,evidence,porting,manifest.json,verify-snapshot.py,checkpoint.md}`. Reproduce with source SHA checkout, hash checker, Windows commands and real screenshots specified in `porting.md`.

**Next bounded task / Actor / Dependencies:** Docs author verify pinned manifest and branch publication; Work 1 independently confirm baseline/fixtures without rewriting production; owner archive expiring external artifact ZIPs, resolve licensing and decide P002/P003 acceptance separately.

**Subsystem: P002 OPEN; P003 OPEN.** Visual acceptance is not subsystem acceptance.

## Post-publication verification addendum — 2026-11-18

This append-only addendum supersedes the provisional **NOT RUN at authoring** fields above; those fields describe the pre-publication snapshot.

- First documentation publication SHA: `2f612c8bbb29c70e85f6c359d1d3b8b892179b9d`; dedicated hash-lock workflow addition SHA: `3bb0f8aa06cda340b35069b67aa4d59eed3c4411`.
- **VERIFIED COMPONENT — offline archive verification:** [GitHub Actions 44325478259](https://github.com/thgillwtnorizoh/rasc/actions/runs/44325478259) **SUCCESS**, Linux Python 3, `python3 docs/rendering-reference/v1/verify-snapshot.py`: **31 Git blob identities checked, 0 mismatched** at SHA `3bb0f8aa06cda340b35069b67aa4d59eed3c4411`. Independent Work 1 QA still NOT RUN.
- **VERIFIED DEVELOPMENT REGRESSION:** [Windows CI 44325478048](https://github.com/thgillwtnorizoh/rasc/actions/runs/44325478048) Windows job **SUCCESS** at the same SHA; core tests, WPF build, all six actual WPF smoke, Chromium reference, x64 publish and artifact upload steps all successful. The immutable production baseline is unmodified.
- GitHub recursive-tree independent metadata comparison checked **31/31 manifest entries** against exact C07 SHA before publication; no source discrepancy. RASC user-accepted visuals and rights limitations remain unchanged.
- This addendum is a Markdown-only follow-up commit after the cited verified SHA. **The new follow-up commit's own CI is NOT VERIFIED at authoring**, and must be distinguished from the two passing runs above.

**Status after the cited source-locked runs: VERIFIED DOCUMENTATION CHECKPOINT** for baseline identity and unchanged development functionality. **Work 1 independent acceptance still OPEN; P002 OPEN; P003 OPEN.**
