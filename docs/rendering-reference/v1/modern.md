# Modern achievement notification — frozen visual profile v1

**Scope:** RASC normal desktop achievement toast **unlock, progress, rare** and 1–3-card shared wave. **Exact source lock:** [`373d04101d03f51135f2f1b192758efddad12f00`](https://github.com/thgillwtnorizoh/rasc/tree/373d04101d03f51135f2f1b192758efddad12f00). **Acceptance:** C07 rare golden appearance was owner-approved on 2026-11-18; full P003 and original Steam client parity remain OPEN.

## A. Provenance and target
- **CONFIRMED IN OWNER-SUPPLIED MODERN CLIENT UI BUILD `11229305`**, not guaranteed stock: `dogbones.zip` / `steamui/sp.js`, `steamui/css/sp.css`, `steamui/index.html`, described in [P000 modern reference](../../../research/P000/modern/reference.md). Original JS desktop path uses per-toast windows/MoveTo, default 300ms transitions, 5000ms default duration and up-to-3 capacity subject to modes/guards; **RASC owner-target uses one HWND instead to avoid visible compositing seams**.
- **GOLD EFFECT REFERENCE** is Dataminer's [Steam Colors / LegendaryAchievement CodePen](https://codepen.io/Dataminer/pen/pooWzpo), NOT the unrelated SteamCommunity conic-gradient implementation and **not independently confirmed as Valve-original client CSS**. The CodePen reference is [`tests/reference/legendary-codepen.html`](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/tests/reference/legendary-codepen.html) with embedded two PNG data URLs. **Reference HTML's blank `#222` fake icon is not RASC's actual icon**.

## B. Base achievement card and gapless wave

| Item | Owner-target v1 |
| --- | --- |
| Achievement card | **283 × 70** logical units, independent content clipped to card envelope; *other notification types can have other heights* |
| Common transparent host | **283 × 210** one nonactivating, topmost, click-through WPF window for up to 3 cards |
| Settled slots | 0 bottom at y140, 1 middle at y70, 2 top at y0; **no spacing, border or independent window DWM gap** |
| Backdrop | Exactly **one radial gradient across occupied wave**, center/origin (0.48, 0.26), radii X=.85,Y=1.1, `#23262E` to `#0E141B` |
| Steam watermark | Exactly one wave-level mark; CC0 simple-icons vector geometry, displayed 170×170 at x143, centered vertically in occupied wave, opacity .09, fill RGB(118,134,154) |
| Normal/rare icon wrapper | 48×48 at x10,y11, contents not clipped at wrapper; actual face RGB(13,16,22), ordinary `★` RGB(179,196,222), rare `★` RGB(255,216,70), font 23; placeholders, not original achievement art |
| Headline | x69,y18 for unlock; x69,y13 for progress; width204; size12, SemiBold, `#D3D8DF`, ellipsis, no wrap |
| Unlock description | x69,y36; width204; size11.5, RGB(140,150,164), ellipsis, no wrap |
| Progress number | x84,y31; size11, RGB(157,168,182); `(current/max)` |
| Progress medal | source-informed rosette path with 36×36 source drawing scaled to 12×12 at x69,y33; not an external Valve asset |
| Progress track/fill | x69,y49, 202×8, corner radius4; track RGB(54,65,78), fill RGB(24,148,245); fraction `clamp((current-min)/(max-min),0,1)` |

Relevant [`ModernToastCard.cs`](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Desktop/ModernToastCard.cs), [`ModernToastWindow.cs`](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Desktop/ModernToastWindow.cs), [`ModernStackDecorationLayout.cs`](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Core/ModernStackDecorationLayout.cs), [progress representation](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Core/ModernToastContent.cs).

### Motion
[ModernToastScheduler](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Core/ModernToastScheduler.cs): at idle, collect one bounded wave (at most three) after a **45ms ENGINEERING event-coalescing delay**. Each item starts at the **same below-taskbar y origin**, rises as a group over **300ms**, holds for **5000ms**, then returns to that same origin over **300ms**; excess events wait for the wave to finish. Linear fraction based on monotonic time. Normal client UI source itself may allow other admission/display-alone cases; do not claim this policy matches all Valve notification contexts. Shared backdrop occupies the **union of moving card rectangles** and is one continuous painted surface. No extra anti-seam strip or 1–2px guessed overlap.

### C07 rare gold halo — DO NOT replace with generic sunburst
[`LegendaryGlowSpec.cs`](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Core/LegendaryGlowSpec.cs); [mask implementation](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Desktop/ModernRareMaskAssets.cs); [shadow implementation](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/src/Rasc.Desktop/ModernCssBoxShadow.cs).

- Real icon **48×48**. Outer **68×68** masked square is offset -10,-10; interior **88×88** rotating star mask another -10,-10; **108×108** radial light another -10,-10.
- Source: **64×64 owner-supplied alpha PNGs** `achievement_square_mask.png` and `achievement_star_mask.png`. Each must be sized to its OWN MASK ELEMENT BOX: CSS `mask-size:100%`; in WPF `ImageBrush.ViewboxUnits=Absolute`, `Viewbox=(0,0,64,64)`, `ViewportUnits=Absolute`, outer `Viewport=(0,0,68,68)`, star `Viewport=(0,0,88,88)`, Stretch=Fill. **Never map relative to enlarged/rotated painted child bounds.**
- Outer root **clips to its 68×68 element box** in addition to its opacity mask; rotating star mask uses reverse **18s / -360°** loop, radial light forward **6s / +360°** loop; mask PNG alpha creates soft edges.
- Gold is a **RADIAL**, not conic, ellipse farthest-corner style gradient; center=(.5,.5), WPF radii `sqrt(.5)`. Exact stops are given as `offset : alpha,red,green,blue` (8-bit channels):

```text
0.00:  45,255,201,109
0.06:   0,255,201,109
0.10:  45,255,201,109
0.26: 255,255,184, 78
0.35:  45,255,201,109
0.40: 255,255,184, 78
0.60:  45,255,201,109
0.82: 255,255,184, 78
1.00:  45,255,201,109
```

- Exactly **two CSS icon shadows on one real face**: `0 0 2px 1px rgba(255,184,78,.6)` then `0 0 16px 1px rgba(255,217,78,.4)`. The WPF approximation precomposes both into ONE transparent premultiplied `Pbgra32` 128×128 bitmap (icon origin40, source48; spread1; Gaussian sigma=CSS blur/2; near over far), rather than two opaque border/effect plates. Browser blur kernel and gamma are **not proven pixel-identical**.
- Position wrapper x10,y11; halo box x0..68 and visible icon x10..58 inside the 283×70 card clip. Preserve owner-approved right-side headline/description and root clip. C06's *relative brush mapping* produced harsh rectangular rays even though source PNG boundaries were low-alpha; C07's **absolute mask-box mapping** removed the bug without changing gold stop brightness.
- Keep the reference's blank `#222` icon and different background **separate** from the RASC dark star/icon and actual card background for side-by-side QA. Browser 900/2000ms frames verify moving masks, **not exact synchrony with WPF animation**.

## C. Fixtures & traps
- Preview regular: `Champion`, `Win 100 games.`.
- Preview rare: `Interstellar`, `Accumulate one mile of distance.`.
- Preview progress: `Arrest Warrant`, `(19/50)`; medal, bar and selected content positions.
- Preview wave: 1,2,3 and 6 events; verify batches, shared continuous background/one watermark, no seams and no content beyond card clip.
- Gold at nominal 900/2000ms; Chromium reference must differ across phase and differ from **no-glow** control. Include **no-masks** control to rule out shadow-only fixture. Screenshot background/icon/phase/DPI must match before numerical pixel diff.
- C06 comparative diagnostic at near 900ms: outside-icon bright-warm pixels **836 → 103** at C07; left edge x0,y35 RGB **(136,107,58) → (37,38,36)**. These measurements describe one existing WPF comparison, **not universal acceptance thresholds** or original Valve pixels.
- If rebuilding in CSS/canvas/WebGPU, apply masks to defined element bounding boxes rather than overflowing descendant paint extents; preserve clipped soft alpha. If rebuilding in WPF, copy semantics of `ModernRareMaskAssets.AlphaBrush(source, maskExtent)` exactly.
- Avoid: per-card HWNDs; one gradient and logo per card; conic substitute; detached orbiting points; hard rectangle/polygon masks; two opaque WPF shadow plates; missing PNG `file://` assets (silent shadow-only captures); uncontrolled DPI/phase or guessed exposure tweaks.

## D. Acceptance boundary
**Owner visually accepted** final C07 shiny result, not the assertion that the pinned modern UI build, Dataminer's CodePen or WPF are byte-identical. Mask redistribution and Valve-client provenance unresolved; independent Work 1 QA, original runtime and physical monitor raster not verified. [C07 diagnostic](../../../research/P000/modern/c07-absolute-mask-diagnostic.md), [C07 checkpoint](../../../docs/P003/checkpoints/P003-C03-C07.md), [CI 44310193628](https://github.com/thgillwtnorizoh/rasc/actions/runs/44310193628).
