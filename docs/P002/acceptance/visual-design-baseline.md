# P002 Classic Visual Design — Owner-accepted Baseline (2026-11-17)

**Project:** RASC / **Campaign:** P002 Classic Steam-style notifications / **Authority:** Owner in conversation, 2026-11-17. Owner reported: "the popup GUI is very well done ... we will take this design as the final one, unless we change something later on." This explicitly accepts the current **visual design**, not every component of the subsystem.

## Accepted design surface (C02 owner observation)
- Classic 240×94 nominal WPF profile with original-style dark gradient; current visible finish has no explicit frame stroke.
- Helvetica Regular owner-target appearance and current 11.5 DIP sizing/12.5 line-height candidate; accepted as **appearance**, not confirmation that May 2023 Steam binaries shipped that exact scheme.
- Current left-aligned headline/description, wording and natural wrapped text, placement and 64×64 placeholder icon geometry.
- Overall contiguous, bottom-right presentation style and visual proportions. No redesign or font substitution unless owner explicitly revises this baseline.

## Accepted design does NOT imply
- Approval of the **three-stack top/middle seam**: still discrepant; middle/bottom appears flush.
- Qualification of actual per-frame slide smoothness, refresh/FPS, original interpolation, taskbar placement across displays or nonactivation/click-through under every game mode.
- Original Valve-font/source/runtime fidelity; archived May 2023 FriendsSmall Arial 12/800 still conflicts with owner Helvetica Regular research.
- P002 feature completion, owner acceptance of subsystem, independent QA, sound/icons/metadata or modern renderer completion.

## Change control and engineering implementation
P002-C01-C03 authorized corrective implementation must **not change ClassicToastCard.xaml** (except if explicitly requested later): only fix actual physical pixel seam/occlusion and update cadence, preserving C02 accepted appearance. Current P002 stage remains **OPEN / visual baseline accepted** under UEP-001. Future style changes require explicit owner instruction; avoid gratuitous font/layout experiments.

**Evidence classification:** USER-OBSERVED RUNTIME + OWNER-ACCEPTED VISUAL DESIGN; not INDEPENDENTLY VERIFIED and not original-RUNTIME-PARITY ACCEPTED. Attribution supported by owner statement; image references and font discrepancies remain in `docs/P002/fixtures/owner-classic-screenshot-calibration.md` and `research/P000/validation/discrepancies.md`.
