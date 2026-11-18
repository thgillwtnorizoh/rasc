# P003-C03-C03 — Modern stacked-card seam observation

**Owner screenshot** (this conversation, November 18, 2026): two cards, 282×134 PNG, SHA-256 e0cba11f7eec35eefdfd0f2b0604d59f65b2e712bfa13c5c53c35b003be9ded6. Screenshot not committed.

At the boundary between "Interstellar" and "NEW_ACHIEVEMENT_NAME_0_4", two full-width unusually light rows separate the otherwise dark backgrounds. At x=0, RGB rows 66..70: (17,22,30), (69,72,79), (73,77,83), (22,27,35), (22,27,35). The owner reports an equivalent defect between the second and third cards.

C03-C02 source inspection confirms scheduler slot spacing is exact 70 design units. ModernToastHost nevertheless used one separate transparent topmost WPF window (HWND) PER CARD. Independent DWM/window composition and physical pixel rounding are possible causes of the seam, but Windows compositor root cause is not yet experimentally established. Classify as OWNER-OBSERVED DISCREPANCY and RECONSTRUCTED hypothesis.

C03-C03 ENGINEERING correction: one transparent 283×210-design-unit WPF window contains all 1/2/3 card views with one shared coordinate system and clipping. Each card preserves the existing gradient, source-informed medal, rare glow, typography, 1/2/3 stack-dependent watermark and slide phases. At rest, three cards occupy logical y=0,70,140; two occupy y=70,140, one occupies y=140. All cards enter from hidden y=210 under the selected app/desktop bottom boundary. This removes inter-card HWND borders rather than hiding the defect with an arbitrary pixel overlap.

Required tests: pure geometry for 1/2/3 cards, 75/100/125/150/200% common scale; actual WPF smoke asserts one HWND containing 1/2/3 cards with contiguous settled positions; preserve Modern→Classic switching and Classic-only baselines, timing/queue regressions, and fullscreen-only override. Owner must independently verify screenshot seam removal at resting and motion phases and varied DPI.

Rare achievement glow is separate and **NOT FINISHED**: current star/square masks are approximate drawn WPF shapes, original Steam CSS overlay blending and Valve mask PNG raster are not verified. This seam checkpoint does not qualify visual glow acceptance. Independent QA, physical WPF pixels and owner CS:GO runtime remain NOT VERIFIED. P003 OPEN.
