# P002-C01-C01 — Classic Steam Notification Stack and Occluded-Slide Contract

**Project:** CS Virtual Achievements (RASC) · **Campaign:** P002 Classic Notification Rendering · **Actor:** Headquarters specification / Work 2 future implementation · **Owner authority:** direct reported behavior and visual correction · **Status:** IMPLEMENTATION CONTRACT v1, pending production correction and visual qualification · **Date:** 2026-11-17.

**Starting verified main:** `b582786ce6adf0519511f90ec3230db56e3fd969`. **Previous exact development-tested renderer:** `5701aefa75cf672c28f97764a2e66ac216f6bd35` (32/32 Windows development tests); the old renderer does **not** conform to this contract. This document changes requirements, not executable implementation. P000 and P001 remain OPEN; no independent QA or owner acceptance is inferred.

## 1. Authority and precedence

Three separate layers must not be conflated:

1. **OWNER-OBSERVED TARGET (acceptance authority for this correction):** flush contiguous right/bottom stack; every arrival comes up from taskbar and pushes existing cards upward; no alpha fading; downward removal into the immediately lower surface (card or taskbar), with descending leading edge completely occluded; no visible card/frame border; current typography/placement incorrect.
2. **CONFIRMED ORIGINAL SOURCE CONFIGURATION (static, not runtime proof):** May 2023 `AchievementNotification.res`, `steam.styles`, `steamscheme.res`, localization. Nominal 240x94 VGUI panel, 64x64 icon, title and description rectangles, `FriendsSmall` Arial tall12/weight800, `BottomRight`, vertical slide, `FadeInTime=.45`, `DisplayTime=6`, `FadeOutTime=.45`, `StackSize=3`. These `Fade*` **configuration names do not establish observed alpha blending**. Prior P000 style analysis also indicates procedural one-unit edges: this conflicts with the owner's **no visible border** report, so retain that as an unresolved visual discrepancy, not an altered original fact.
3. **ENGINEERING (provisional until target comparison):** WPF shared compositor, work-area scaling, chosen easing function, timing interpretation, serialized transition arbitration, and handling taskbars in non-bottom positions.

Do not call the original runtime animation or typography faithfully reconstructed merely because the source constants and these target rules are implemented.

## 2. Geometry and coordinate system

**Target profile:** one Windows desktop work area with a **bottom-docked taskbar**. Other taskbar orientations, auto-hide, multiple monitors, exclusive fullscreen and DPI coordinate conversion require separate qualification; never silently infer equivalence.

- Card nominal dimensions: `W=240`, `H=94` in **VGUI reference units**. Render in logical units under a measured VGUI-to-screen scale `s`; treating WPF DIPs as identical is provisional. For the initial 100%-scale profile use `s=1` while separately testing physical sizing.
- Work-area right edge `R`, bottom edge `B`. **No outer margin**: right edge of every card = `R`; bottom edge of the lowest settled card = `B`. Thus `x=R-W` and settled vertical coordinate for slot `k` = `B-(k+1)H`.
- Slot numbering: `k=0` bottom/newest, `k=1` above, `k=2` top/oldest. Cards form a **contiguous column**: slot `k+1` bottom = slot `k` top. `gap=0`, `edgeMargin=0`.
- Visual viewport: width `W`, height `3H`, lower edge `B`, right edge `R`. Clip all card content to this viewport. Transparent pixels outside the card outlines must not intercept input.
- For each card, the **visible region** is a rectangular subset of its original card surface, not an independently moving alpha-faded free-standing window.

## 3. Motion and state machine

**Data:** persistent achievement ledger emits a unique event ID; the presentation queue receives it at most once. Historical `GetRecent` rows and first-run baseline **never** enter the toast queue. Preview is explicitly non-persistent.

**Presentation states:** `Queued -> Entering -> Holding -> Exiting -> Removed`; the existing event history and source file are not modified by any presentation transition. A card has `Opacity=1.0` whenever a pixel is visible, with **NO animated opacity**, regardless of the legacy `FadeInTime` or `FadeOutTime` label.

**Time:** source-configured `T_enter=450ms`, `T_hold=6000ms` after entrance completes, `T_exit=450ms` as **initial timing interpretation**. Human visual speed/easing remains **UNVERIFIED**. Every transition accepts a monotonic clock and normalized progress `p=F(elapsed/duration)`, `p in [0,1]`. `F(0)=0`, `F(1)=1`, monotonic. **Do not describe an arbitrary easing curve as Valve's original**. First development pass may use linear `F(u)=u` with an explicit engineering label; visual acceptance remains blocked pending owner comparison.

### A. Arrival when settled count is 0, 1 or 2

Every new notification follows exactly this animation, even if there are zero or just one other card:

- New card begins **entirely below** the work-area/taskbar boundary: `y_new(0)=B`.
- It rises to bottom settled slot: `y_new(p)=B-H*p`.
- Every existing settled card moves **upward together** by one full card height: `y_existing_i(p)=y_existing_i(0)-H*p`.
- New card content is clipped to `y < B`. At `p=0` no part appears above the taskbar; at `p=1` it fully occupies `[B-H,B)`. The sliding edge outside the work area must **not** be rendered on top of the taskbar.
- The previous bottom card becomes slot 1, previous slot 1 becomes slot 2. The incoming card becomes slot 0 (newest). No gap or floating overlap after transition.

### B. Removal at any position

A departing card `D` always **slides vertically downward**, by exactly one card height, toward the immediately supporting surface:

- If `D` is in slot 0, its supporting surface is the taskbar/work-area boundary `B`. Move from `y_D(0)=B-H` to `y_D(1)=B`; clip to `y < B`. Its lower leading edge must **never** be visible on the taskbar.
- If `D` is in slot `k>=1`, its supporting surface is the top edge of the card immediately below. Move `y_D(p)=y_D(0)+H*p`. The **below card occludes D**. Render only the part of D strictly above the lower card's top edge; *no descending lower edge, seam or strip may show through the lower card*.
- If there are cards above D, move those survivors down by one slot in a coordinated compaction transition, so after removal there is never a hole in the settled column. Their relative FIFO order must remain unchanged.
- At `p=1`, D is completely invisible and removed from the rendering tree, never merely opacity-zero but still interactive. Resting survivors snap exactly to contiguous integer slot coordinates without jitter.

### C. Capacity = 3 and a fourth event

- Max **3 fully visible settled cards**. The fourth and later incoming events wait in a FIFO presentation queue when all 3 slots are occupied; the May 2023 resource mentions background queuing.
- When the full stack is ready to make room (normally on expiry), **top/oldest (slot 2) slides down into the middle card (slot 1) and disappears behind it**, per removal rule B. This is *not* an exit out the top of the viewport.
- Then the next queued event **rises from the taskbar into slot 0**, pushing remaining two cards up to slots 1 and 2 per arrival rule A.
- **No instantaneous replacement, fade, pop-out or fourth fully visible card.**
- **Open arbitration question:** whether historical Steam immediately evicts on a new event at capacity or waits for normal hold expiration is not settled by owner observation. The baseline implementation **retains FIFO waiting / normal expiration** based on the P000 configuration comment. Record this as a distinct acceptance gap; do not assert historical equivalence. A different owner-observed sequence requires updating this clause explicitly.

### D. Concurrency and interruption

- Process additions/removals as a **serialized ordered transition queue** for the baseline implementation; never teleport between slot positions or start two contradictory transforms on the same card.
- Events received in one GC snapshot preserve stable deterministic order. Each arrival gets a distinct bottom-up motion; do not animate three same-timestamp arrivals all into separate slots at once.
- Holding expiry is evaluated on a monotonic clock. When an item expires while another transition is active, queue the removal until the current transition finishes. No gaps, duplicate toasts, negative slot indices, or card teleporting.
- Presentation shutdown cancels transitions, detaches child visuals and must not delay normal app exit. Restarts may display history but **must not** replay previously delivered events as new popups. Preserve source/SQLite integrity.

## 4. Composition and occlusion implementation

**Required architectural direction:** compose all active cards in a **single shared, transparent, nonactivating, click-through host surface** spanning the 3-card viewport. Do not retain one freely floating, independently animated top-level window per card unless an equivalent clipping proof is produced. Give cards an explicit z/clip ordering so:

- The taskbar/work-area boundary masks the incoming/outgoing bottom edge.
- On downward exit into another card, the **lower card is the occluder** for the departing card; the entire leading edge is hidden behind it.
- Stationary and moving content never leaks through the occluder, and no one-pixel seams appear during scaling/animation.
- New arrivals always emerge from **below** the boundary and do not fade into view.
- Host never activates/steals focus, traps mouse clicks, presents as a taskbar button, or survives main-app shutdown.

## 5. Style / typography correction gate

**Hard requirement:** **no visible outer border/frame or icon-border stroke** in the accepted target. Remove current WPF `BorderBrush=#43536A` and `#62748A` strokes. The P000 original-style source suggests procedural edges, so preserve **C-D05: style-source-versus-owner-visual discrepancy**, pending an actual comparison. Do not quietly rewrite that source evidence.

**Typography is not accepted yet.** Current WPF Arial `FontSize=12` / `ExtraBold` and fixed rectangles were **rejected by owner observation**. Recover and calibrate the actual visual text baseline, font family/weight, glyph height, line breaks, vertical alignment, and horizontal alignment using original reference capture or independently traceable native metrics.

Source anchors to preserve as **reference, not yet accepted WPF pixel coordinates**:

| Component | Source nominal anchor | Confidence / action |
|---|---|---|
| Title | x88, y25, w144, h28; center, wrap | Confirm font metric conversion and baseline; owner says current WPF placement is wrong |
| Description | x88, y53, w144, h28; north-west, wrap | Confirm y-offset and line metrics; raw achievement IDs are provisional fallback |
| Icon | x14, y14, 64x64 | Source geometry; actual achievement artwork unresolved |
| Font | FriendsSmall / Arial / tall12 / weight800 | VGUI units, **not** an established WPF `FontSize` or exact weight mapping |
| Colors | Title #C8D0DC; body #B4B4B4; background #2A2E33 to #121A2A | Source-based; raster and gamma/canvas treatment not qualified |

Do **not** invent final typography offsets/sizes from the complaint alone. A font/text fixture must be captured, compared and owner-qualified before marking this gate PASS. Keep distinct compatibility parameters, not unexplained magic offsets.

## 6. Mandatory acceptance matrix

1. **R01 Empty to 1:** one card rises from the taskbar and settles flush right/bottom; at entrance start it is invisible below B; no opacity fade.
2. **R02 1 to 2:** second rises from taskbar; first moves up exactly H; cards share edge with zero gap at rest.
3. **R03 2 to 3:** third rises from taskbar; both earlier cards rise exactly H, maintaining event order; no gap.
4. **R04 3 + new event:** fourth queues; when eligible, top descends *behind middle*, disappears, then fourth rises from B and pushes remaining two up. Never more than 3 visibly settled cards.
5. **R05 Exit bottom:** card descends into taskbar, with lower leading edge entirely clipped; above survivors compact down without gap.
6. **R06 Exit middle:** card descends behind card below; upper survivor compacts down; no exposed edge/seam.
7. **R07 Exit top:** top card descends behind middle; no top-of-viewport outward exit, no visible descending leading edge.
8. **R08 Burst 6+:** deterministic FIFO and cap=3; every arrival from taskbar; departure transition obeys support-surface occlusion.
9. **R09 No fade:** every visible card pixel uses original artwork alpha only; transition never alters card alpha/opacity.
10. **R10 Edge alignment:** right edge = R, bottom edge = B, neighboring cards are contiguous at all stable frames; no 16-DIP edge margin or 6-DIP gap.
11. **R11 Border:** no visible outer/card/icon-border stroke; source discrepancy explicitly documented.
12. **R12 Text:** test single-line, two-line, long and non-ASCII title/body layouts against original-grounded visual fixture; **NOT VERIFIED until original comparison and owner qualification**.
13. **R13 History/preview:** first-run baseline, rehydration, duplicate monitoring, preview click and restart do not create false awards or new duplicate toasts; preview is never written to GC/SQLite.
14. **R14 Native windows:** click-through, focus/nonactivation, monitor work-area placement, app close while animating, and no orphan window; Windows GUI screenshots/video needed.
15. **R15 Existing regressions:** preserve at least prior 32 passing development tests; add deterministic virtual-time scheduler tests and pixel/clip assertions. CI must build self-contained Windows x64 artifact.

**Evidence gates:** source inspection + scheduler unit tests = **VERIFIED COMPONENT (development)**; live screenshots/recordings plus owner review = **USER-OBSERVED RUNTIME**; distinct Work 1 expectations/tests = **INDEPENDENTLY VERIFIED** only if actually performed; **OWNER-ACCEPTED** requires explicit owner approval.

## 7. Implementation exclusions and stop conditions

Do not develop Modern Steam rendering, original sound, proprietary icon redistribution, Steam/game injection, exclusive fullscreen support, or unrelated UI redesign in this correction. The first deliverable is **P002-C01-C01 Classic stack-motion and style correction**, not blanket original fidelity.

A correction checkpoint must give exact starting/produced/tested Git SHAs; Windows CI commands/counts/artifact; implemented state and occlusion behavior; R01-R15 results; current discrepancy register; NOT RUN markers; and next bounded task. Stop at a verified implementation or reproducible blocker. Keep P002 OPEN until user visual qualification and formal owner acceptance.

### Source and contradiction register

- [P000 Classic May 2023 resource reference](../../../research/P000/classic/reference.md): dimensions, font and nominal configuration; original runtime motion unverified.
- [P000 Renderer contract](../../../research/P000/comparison/renderer-contract.md): source-level geometry/time, 3-stack limit, queue expectation.
- [P000 Discrepancies](../../../research/P000/validation/discrepancies.md): C-D01 motion, C-D02 stacking, C-D03 icon/audio, C-D04 scaling/clipping, Q-D01 independent QA.
- **C-D05 (NEW):** no visible card/icon border in owner's target versus procedural original style edges seen in static resource; requires visual evidence and owner resolution.
- **C-D06 (NEW):** FIFO queue/expiry versus immediate eviction at 3 remains unverified; default to queued behavior until discriminating capture.
- **C-D07 (NEW):** original text size/baseline versus WPF Arial/tall mapping remains unverified and rejected in first preview; do not hardcode arbitrary replacements and call it fixed.

## 8. Owner-supplied screenshot reference addendum

Two owner-uploaded examples (one "Achievement Unlocked!" and one "Achievement Progress") have now been measured and indexed in [owner screenshot calibration](../fixtures/owner-classic-screenshot-calibration.md). The larger screenshot's normalized text position closely agrees with the original VGUI layout anchors (`LabelTitle x88/y25`, `LabelDescription x88/y53`); this does **not** resolve WPF glyph metrics or visual fidelity. Add both named unlock/progress **preview-only** text fixtures to R12. Their original exact Steam build/provenance and rasterization remain unverified. The progress fixture must not be interpreted as a durable GT-610 achievement-progress event. No user image or proprietary icon artwork is redistributed.
