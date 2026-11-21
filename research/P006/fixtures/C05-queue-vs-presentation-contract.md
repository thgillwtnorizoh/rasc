# P006-C05 — Owner popup queue/presentation separation (Work 1 acceptance contract)

**Project:** RASC / CS:GO Achievements Resurrection.
**Campaign:** P006 / C05 — eliminate repeated off-time and incomplete achievement popups.
**Actor:** Work 1, requirement interpretation and independent expected cases (same chat also performs Work 2 implementation; not an independent second-agent code QA).
**Starting branch:** C04 `fix/p006-c04-modern-late-admission-two-line`; verified starting SHA `68a9f5e8504708d34d5edc153b6e449e6b953bed`.
**Owner authority:** Direct user instruction on 2026-11-21. This record is a **RECONSTRUCTED requirement specification** and **ENGINEERING** timing interpretation, not a CONFIRMED ORIGINAL Steam internal scheduler.

## Acceptance units

1. **Order and presentation are different concerns.** GC receipt and SQLite determine ordered, source-keyed achievement identities. Presentation mode never edits achievements or invents award times.
2. **Classic:** an achievement gets an independent slot when fewer than 3 are occupied. Individual Classic card timing/animation controls when its slot is freed. Queued replacement enters when space is released, rather than waiting for an entire fixed wave. Preserve current source-derived Classic slide (450ms in / 6s hold / 450ms out).
3. **Modern collection:** first confirmed award at RASC source observation time T opens exactly `[T, T+800ms)`. Eligible awards with later source-observed timestamps in that interval join **the same pending wave**, up to 3 total. At exactly T+800ms the group is sealed for presentation. A fourth award creates the next FIFO group; a later new signal outside window makes its own 800ms group. A GC callback that reaches WPF late must NOT change its original source observation timestamp, making the grouping independent of UI load.
4. **Modern presentation:** an admitted wave is **immutable**. If 1 card enters, awards signaled after its sealed collection window may NOT join it even with vacant capacity. All 1–3 cards start together, enter 300ms, hold 5000ms and exit 300ms together. No card individually extends a wave. Next ready FIFO wave starts promptly after previous wave retires and gets its own complete 5.6s presentation duration.
5. **No queued loss:** an award waiting in the FIFO never expires because the presentation clock advanced. After a delayed 12s or 17s WPF callback, the NEXT previously unshown wave must start at that callback time with first frame at its common off-screen origin; it must not be fast-forwarded and retired without ever being shown.
6. **No cross-cohort mixing:** an award from a later first signal must not be bundled with a prior wave when the prior wave finishes. Mixing and order must be traceable to RASC's original receipt times, not the moment a queued visual is finally shown.
7. **Preview distinction:** synthetic preview controls are visual diagnostics, **not a confirmed GC signal**. They may remain immediate to preserve original CodePen/Classic/Modern visual acceptance tests. Only authentic source-confirmed GC awards must obey 800ms collection.
8. **Preserved:** v0.2 manual Rare/Normal policy, local JPEG hash loader, original text, actual GC read-only writer data, first-snapshot silent baseline, owner-approved Classic/Modern geometry, wave audio cues, both existing profiles and core/WPF historical regression suites.

## Independent discriminating fixture cases

| ID | Source signals / UI | Correct expectation |
|---|---|---|
| M0 | IDs A@0ms B@400ms C@799ms | Sealed first Modern wave [A,B,C] ready at +800ms, in exact FIFO |
| M1 | ID D@800ms | NOT in [A,B,C]; creates second group ready +1600ms |
| M2 | IDs A..H@0ms, I@3000ms | FIFO groups [A,B,C], [D,E,F], [G,H], [I]. Last group never merged with prior overflow |
| M3 | A@0 B@600; UI receives both only after 15s stall | Still one [A,B] with historical close T+800; display starts at NOW with full lifetime |
| M4 | One Modern [A] is already visible, later B arrives at +1s | B belongs to a NEW wave; A's 5.6s timing unchanged |
| M5 | Fully queued 3+2 after 800ms collection | First visible 3, second visible 2 after first retires, without an additional GC unlock |
| M6 | Source clock rolls backwards after an earlier signal | Isolate rollback across groups rather than infer a false contiguous 800ms burst |
| M7 | 3 sealed waves waiting, UI callback jumps 16.8s | No unshown wave is skipped/retired; every wave gets its own new presentation clock |
| C0 | Classic has 2 occupied slots, another award arrives | Admit without a Modern wave wait; independent Classic slide/hold applies |
| C1 | Classic has 3 occupied slots and oldest expires | FIFO next begins as slot is freed, independent of the other two |
| R0 | Unknown or duplicate GC IDs | Keep source state truth and event dedup; do not manufacture progress or rarity |

**Visibility limitation:** WPF `Show()` or `Apply()` proves that the renderer submitted state, not that the OS compositor physically displayed it over an exclusive fullscreen game. Controlled Windows smoke + owner actual-game qualification remain mandatory.

**Unavoidable latency:** Fixed 3-card capacity and a 5.6s wave mean an overflow award or a later cohort may wait multiple seconds (three waves require roughly 17.6s from first signal when no interruptions, including initial 800ms). This cannot be removed without dropping achievements, reducing hold time or increasing capacity, none of which the owner authorized. The invariant is predictable FIFO and no *additional* time spent waiting due to a broken clock.

**Known persistence gap:** Current `UnlockEventStore` marks `delivered_ms` after UI enqueue, not after physical on-screen presentation. C05 fixes in-process queue starvation and silent expiry of pending waves, but durable delivery acknowledgements and crash-restart replay are NOT yet verified or implemented; record as a bounded subsequent gate rather than claiming complete reliability.

**P006 subsystem OPEN**, subject to development tests, independent QA, physical owner runtime and explicit acceptance.
