# P006-C02 — Owner Windows runtime observations and independent fixture expectations

## Observed owner evidence (2026-11-20)

Four owner-provided legacy CS:GO gameplay screenshots demonstrate the **pre-correction** live RASC notification overlay, not fixture previews. The game visually announces achievements in its own chat while RASC overlays generic star icons and raw API IDs.

- Screenshot 1 (Connector): three Modern cards show `FAST_ROUND_WIN`, `KILL_ENEMY_RELOADING`, `UNSTOPPABLE_FORCE` as identifiers, with dark placeholder star, no visible original achievement name or description. Game chat reports “Shot With Their Pants Down” for a newly earned achievement.
- Screenshot 2 (Bombsite B): two cards display `LAST_PLAYER_ALIVE` and `WIN_BOMB_DEFUSE` as IDs with placeholders. Game chat shows new earned achievements around bomb defuse. This is not an independently verified frame-by-frame notification schedule.
- Screenshot 3 (Bombsite B): a single `KILL_ENEMY_TEAM` raw-ID card while multiple game achievements are shown in chat.
- Screenshot 4 (T Ramp): a single `KILLING_SPREE` raw-ID card. The owner explicitly reports a three-award burst where only one popup appears and a remaining popup does not appear until a later achievement. Static screenshots cannot independently prove exact delivery timing; treat the owner's timing report as a **runtime observation** requiring a discriminating test.

The material does not reveal the actual `csgo_english.txt` bytes, the exact local JPEG bytes/hashes, GC snapshot timestamps, source event IDs, or whether user-installed launch binaries were from a specific RASC PR commit. **Do not infer any of those from the images.** The anime overlay is a separate on-screen element not owned by RASC; do not treat it as notification artwork.

## Source-inspected defects

1. **CONFIRMED ENGINEERING integration gap:** `MainWindow.Start_Click` previously called `_classicToasts.Enqueue(item)` or `_modernToasts.Enqueue(item)`, which delegated to placeholder text/blank description, a generic star and non-rare appearance. The P006 catalog and localization parser existed only behind the separate achievement-library UI.
2. **CONFIRMED ENGINEERING unstable order:** prior `UnlockEventStore.AcceptSnapshot` sorted new achievements alphabetically by API ID; `GetPending` ordered same-observation-second rows by SHA-based `event_id`. Neither preserves known ascending GC unlock time or insertion sequence.
3. **RECONSTRUCTED fullscreen timing risk:** `ModernToastHost` used a hold-wake timer through the five-second hold, but relied upon ongoing WPF `CompositionTarget.Rendering` (or anchor/display triggers/new enqueue) for wave retirement after exit motion. While fullscreen CS:GO owns the foreground, callbacks can be delayed. A static screenshot cannot prove this is the *only* cause of observed backlog. Discriminating test must suppress Rendering and anchor callbacks and verify 3+2 cards complete without any additional event.

## Independent expected behavior, fixed fixtures

- S0: first valid GC snapshot with existing awards silently baselines; no popup. This is tested by existing history/monitor suites, and must remain unchanged.
- B1: subsequent valid snapshot adds five source-confirmed IDs, distinct positive second timestamps `50,51,51,52` and one unknown timestamp `0`; delivery must use known ascending seconds, exact-ID stable tie break, then unknown-time row. Pending SQL must preserve insertion order. Where timestamps tie or are zero, actual original subsecond order **cannot be established**; stable deterministic tie order is an engineering rule, not original proof.
- B2: Modern host accepts 3, then another 2 while first wave is active. With normal render and anchor callbacks experimentally disabled, the timer must advance the 5.6-second first wave and expose the next 2 **without a sixth unlock**; titles and descriptions must be those of independently supplied synthetic fixture metadata.
- A0: English local name/description pair and matching local JPEG SHA-256: pass original-local name, complete description and image to Classic/Modern; use owner-selected v0.2 rarity to activate existing Modern legendary glow.
- A1: missing/corrupt/text-only/hashed-mismatch original files: raw ID / empty description / generic icon fallback; no false Valve name/artwork or invented rare marker. Do not write to original CS:GO, GT-610 or Steam.
- Q0: queued live event payload is resolved **at arrival**, so editing the local metadata file later does not mutate a toast already queued. Silent first snapshot and event replay/dedup unaffected.

## Classification and remaining proof

- **CONFIRMED OWNER OBSERVATION:** screenshots and owner account of visible layout/order delays.
- **CONFIRMED CODE DEFECT:** explicit host placeholder path and hashed/alphabetical ordering.
- **RECONSTRUCTED RUNTIME HYPOTHESIS:** compositor-dependent stalled wave. Test with no additional event and Rendering suppression is required, plus owner physical post-fix validation.
- **ENGINEERING CHANGE:** deadline-driven scheduler, read-only owner-local JPEG loader, v0.2 curated rarity text/art routing.
- **NOT VERIFIED ORIGINAL:** exact unlocked-achievement rules, original Steam rarity, original 167 active Steam schema, physical owner post-fix game qualification.

Keep the distinction between original runtime evidence and synthetic tests; no independent second-agent production QA is claimed by a combined Work 1/Work 2 execution.
