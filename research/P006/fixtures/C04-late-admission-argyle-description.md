# P006-C04 / Work 1 — Owner screenshot fixture and independent acceptance criteria

**Campaign:** RASC P006 / P006-C04; **actor:** combined owner-authorized Work 1 source/requirement verification and Work 2 implementation, with expectation evidence kept distinct from production code. This is **not** a second independent compiled-production QA actor.

## Owner-provided 2026-11-20 runtime evidence

The owner supplied four images in this conversation: a tightly cropped **The Argyle Sap** Steam-style achievement reference, and three full-frame legacy CS:GO screenshots after P006-C03.

1. **Modern names, local JPEG icons and descriptions now display**: e.g. *Mercy Rule* / *Blitzkrieg* share one two-card stack and *Shot With Their Pants Down* displays the original description. This validates prior missing-text correction on the owner's real installed game.
2. **Cross-snapshot lag remains**: the game itself reports *Shot With Their Pants Down* and *The Cleaner* together in chat, but the first picture of that sequence shows only *Shot With Their Pants Down* in the Modern popup; later, *The Cleaner* appears alongside *Rite of First Defusal* in a subsequent screenshot. Screenshots do **not** provide GC write timestamps or exact elapsed delay, and cannot alone prove whether they were distinct file writes.
3. **Description layout mismatch**: owner-provided *The Argyle Sap* reference visibly supports two description lines below the title, unlike the existing RASC `TextWrapping.NoWrap` and single-line ellipsis at y=36. The 283×70 card and icon left edge should remain fixed. Design-unit estimates from the supplied cropped reference: **wrapped headline y≈10; description first line y≈26 and second at y≈42, line spacing ≈16**. For short descriptions preserve accepted one-line baseline **headline y=18, description y=36**. These are visual ENGINEERING measurements pending owner pixel/runtime approval, not exact source CSS proof.

## Reproducible root cause and bounded correction

- **CONFIRMED CODE:** P006-C03 `AchievementMonitor.UnlockBatchDetected` passes one source snapshot into one WPF callback, but **each later GC rewrite** enters `ModernToastHost.Enqueue` after the current `_batchWake` has elapsed. `ModernToastScheduler.Enqueue` always puts it in `_waiting`, even when only one or two cards are visible. It waits the current full 5.6-second lifetime. This is a functional policy mismatch with the owner's request for prompt closely spaced awards, not proof of Steam's exact scheduling.
- **OWNER-AUTHORIZED ENGINEERING PROFILE:** allow a source-confirmed LIVE award (not a preview) to enter the active Modern stack **while the first 3 seconds of that wave elapse, before hold exit, and only if fewer than 3 cards are showing and none are already queued**. A later/over-capacity award stays FIFO waiting. This deliberately preserves prior immutable preview-wave tests and old timing for groups already admitted.
- **Seam-safe geometry:** when a newcomer enters, all existing cards smoothly slide up one 70-unit slot while the newcomer rises from below over 300ms. Each card's entry is independent; existing card titles and audio are not replayed, one shared radial backdrop/watermark remains contiguous, and the newest member receives its complete 300ms entrance and 5000ms hold before the stack exits together. A live late join can therefore extend the active stack's duration; do not represent it as original Steam internal timing.
- **Visual text:** measure actual WPF text wrapping under selected font and typography. One line retains y18/y36; 2+ lines are **clamped to 2 lines** with y10/y26, 16-unit lines and terminal ellipsis where truncated. Do not change rare glow, asset sizes, progress variant, sounds or other typography colors/weights.
- **No invented source data:** this change does not generate awards, percentages or Steam global rarity. It does not modify CS:GO or GC files, the owner's curated rarity, the first silent baseline or persisted dedup state.

## Independent test cases

- L0: initial 1-card wave at 0 → enters as before, with first cue once.
- L1: second genuine GC event arrives at 400ms while 1 card is showing → **two** WPF cards immediately, not delayed 5.6 seconds, and first does not re-enter or replay audio. At +150ms positions must be adjacent (no gap).
- L2: third live award at 900ms → **three** cards, all showing by 1200ms; common stack envelope remains contiguous, and no additional cue.
- L3: fourth live award with capacity 3 → waiting FIFO and gets a separate wave after final admitted card's minimum full hold; one new wave cue.
- L4: event at 3100ms, or when earlier overflow is pending → must **not jump FIFO**; no unbounded hold extension.
- L5: immutable legacy preview `Enqueue` and historical 1/2/3-card animation tests unchanged.
- V1: single short description measured under actual font → headline y18, description y36; remains one line.
- V2: reference-length Argyle description → two-line wrapping with y10/y26, 16-unit line height, max 2 lines and ellipsis for overflow.
- V3: font profile changes recalculate wrap without altering card 283×70 geometry.
- WPF: actual visible 1→2→3 cards across distinct enqueue times; 4th appears later without a fifth unlock; first cue once, replacement cue once.
- Regression: preserve all previous WPF and Core tests, 167-row CSV, artifact build, original CodePen rare-glow screenshot gate.

## Verification boundary

The original CS:GO screenshots prove the *symptom* and text/asset improvements. They do **not** include exact GC timestamps or an authoritative original Steam queue implementation. The live late-admission behavior and positions are an explicit owner-requested ENGINEERING correction, verified first with synthetic Core and real-WPF tests, and still require owner physical fullscreen follow-up. The Steam-style reference supplies visual guidance only and does not establish a complete CSS computed layout.

**Subsystem:** P006 OPEN; Work 1 source/fixture register awaits independent physical follow-up.
