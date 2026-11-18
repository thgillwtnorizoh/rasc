# Steam Rare Achievement Alpha Mask References — Rights and Provenance

The two 64×64 PNG images in this experimental Work 2 branch were explicitly supplied as Base64 data URLs by the RASC project owner on 18 November 2026, in a request to apply them to the Steam-Modern rare notification glow. Their exact byte payloads were cross-located in the publicly available CodePen reproduction *Steam Achievement Animated Glow* by Dataminer, https://codepen.io/Dataminer/pen/pooWzpo (and a public GitHub mirror). They are referenced in that CodePen's "Steam Colors" / LegendaryAchievement effect.

The owner identified the files as Valve's achievement_square_mask.png and achievement_star_mask.png. Matching the CodePen and names does **not** independently establish byte-for-byte authenticity with a particular Valve client version, nor does it grant a license to redistribute the Valve artwork or the CodePen author's work.

**Rights and release gate:** These files are included solely in this draft corrective branch and its owner-test Windows CI build. Attribution and licensing are unresolved, so the project owner must decide on permission/replacement before accepting, merging to main or redistributing the final product. Do not imply Valve/Steam endorsement, attach a permissive software license to these images or incorporate them into an official release without that review.

No Steam client executable/assets are accessed at runtime; these small resources are embedded in the experimental RASC WPF build for deterministic offline rendering. No network calls, patching, process injection or achievement-state mutations are introduced.

If redistribution is not approved, move to a user-local mask import workflow (or independently authored replacement masks) before release while preserving the same runtime interface and correctness tests.
