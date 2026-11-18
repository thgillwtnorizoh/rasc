# RASC Steam-style achievement renderers — preservation pack v1

**Date:** 2026-11-18 · **Owner:** RASC project owner · **Actor:** Work 2, documentation only · **Baseline source commit:** [`373d04101d03f51135f2f1b192758efddad12f00`](https://github.com/thgillwtnorizoh/rasc/commit/373d04101d03f51135f2f1b192758efddad12f00).

This is a **frozen, versioned engineering reference** for reproducing the TWO distinct owner-approved visual targets of RASC elsewhere, without repeating the reverse-engineering or the C05–C07 rare-glow debugging campaign. It is not a claim of bit-perfect Valve client output, permission to redistribute source graphics, or acceptance of P002/P003 as complete systems.

| Reference | Target | Owner decision | Reimplementation source |
| --- | --- | --- | --- |
| [Classic v1](classic.md) | May 2023-style dark VGUI achievement panel, with owner-requested visual typography/placement | Classic **card visual design** accepted 2026-11-17; later functionality and original runtime equivalence not thereby accepted | `ClassicToastCard.xaml`, `ClassicToastScheduler.cs`, `ClassicToastStackWindow.xaml[.cs]` |
| [Modern v1](modern.md) | Normal modern Steam desktop achievement toast: regular, progress, rare, 1–3-card gapless wave | C07 **rare golden-glow appearance** accepted 2026-11-18; earlier Modern wave/progress choices preserved; subsystem acceptance still open | `ModernToastCard.cs`, `ModernToastWindow.cs`, `ModernToastScheduler.cs`, `LegendaryGlowSpec.cs` |

### Entry points
- [Classic profile](classic.md): original VGUI vs owner-target differences, exact XAML color/typography, motion/clip/DPI.
- [Modern profile](modern.md): shared wave, watermark, normal/progress/rare geometry, animation and CSS mask-box parity correction.
- [Evidence and rights](evidence.md): immutable source pointers, original reference images and hashes, Chrome/WPF snapshots, limitations and licensing.
- [Porting, fixtures and regression matrix](porting.md): how to rebuild in another GUI engine without regressing visual behavior or faking achievements.
- [Locked manifest](manifest.json) and [offline hash checker](verify-snapshot.py): identify the **exact baseline source and embedded reference fixture**. Run `python docs/rendering-reference/v1/verify-snapshot.py` from any directory after checking out the source SHA.
- [UEP checkpoint](checkpoint.md): publication status; this documentation is not independent QA.

### Baseline and change control
1. Check out **exact SHA `373d04101d03f51135f2f1b192758efddad12f00`** before reimplementation. The docs branch only adds reference material; all rendering source paths in the manifest refer to that immutable baseline.
2. Never collapse Classic (240×94 VGUI-derived, occluded serial stack) and Modern (283×70 achievement, wave-level radial background, masks) into one design with different colors.
3. Separate **CONFIRMED ORIGINAL STATIC SOURCE** (SteamTracking P000 Classic; supplied modern UI build) from **OWNER-ACCEPTED VISUAL TARGET** (RASC), **ENGINEERING** (WPF compositor, 45ms batching, Gaussian CSS approximation), and **UNVERIFIED** (original Steam runtime, fonts, monitor compositor, source licenses).
4. Preserve fixtures, names, source hashes, rendering phase, background, DPI, icon, and capture method. Screenshots without that context are **not** sufficient for pixel acceptance.
5. A new visual correction starts **v2**, not a silent overwrite of this v1 archive. Do not alter working renderers merely to make this archive self-contained.

### Rights and safety
The two owner-supplied CodePen PNG alpha masks are contained in the **experimental baseline branch**, and the browser HTML includes their embedded data URLs. Their redistribution rights and identification with a particular Valve build are **unresolved**. Keep the archive as source-level reference pending rights review; do not assume masks, Valve artwork, Steam marks, user reference screenshots, or downloaded UI bundles have free redistribution rights. Preserve existing source-only references and hashes instead of adding duplicate binaries. This project never changes Steam/CS:GO achievements or injects into the game.
