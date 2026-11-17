# P002-C01-C03 — Current Classic render-paced motion / seam-check build

**Current Windows development:** [CI run 44046616395](https://github.com/thgillwtnorizoh/rasc/actions/runs/44046616395), [self-contained Windows x64 artifact](https://github.com/thgillwtnorizoh/rasc/actions/runs/44046616395/artifacts/14312275576). Exact tested source `984db970f0c6ac9880333606da8195a7d2b57543`. **56 passed / 0 failed / 0 skipped; WPF build + publish + artifact upload PASS**.

**Owner decision (2026-11-17):** Existing C02 card visual design is **OWNER-ACCEPTED VISUAL BASELINE**: Helvetica Regular target, text placement/wrap, icon placeholder geometry, background color and borders. This is **not owner acceptance of P002 functionality**, and is not original Steam fidelity proof. Source: [visual-design baseline](acceptance/visual-design-baseline.md). C03 does **not** change the approved card XAML or styling.

**Remaining correction:** On a 3-card stack the owner still observed a seam at **top–middle**, while **middle–bottom** was flush. A 94-DIP card equals 117.5 physical pixels at 125% scaling (164.5 at 175%); independently snapping the next origin to 118 (or 165) can expose a half-pixel gap only at that junction. This is a **specific reproduction hypothesis**, not verified cause on the owner's actual PC. C03 adds color-matched pixel-snapped underlays behind cards, leaving the accepted text/icons and natural gradient intact. The underlay extends only to the shared screen-pixel boundary and follows the lower card's occlusion. No layout margin/spacing changed.

**Animation rate:** prior C02 `DispatcherTimer.Interval=16ms` nominal 62.5 updates/sec, ~28 updates per 450ms slide; actual physical FPS never measured. C03 uses WPF `CompositionTarget.Rendering` callbacks **only while sliding**, so update cadence follows WPF render opportunities rather than a 16ms timer; 60Hz≈27, 120Hz≈54, 144Hz≈65 theoretical updates/450ms (not guaranteed actual). During static six-second holds a deadline-based `DispatcherTimer` sleeps until the next expiry; no need to redraw 60/120 times per second. All durations, path/easing, FIFO max3 and no-opacity-change behavior retained.

**Low-cost diagnostics:** after closing RASC, inspect `%LOCALAPPDATA%\RASC\last-classic-animation.txt`. It reports animation segments, unique `CompositionTarget.Rendering` callbacks, sampled callback intervals, estimated callback rate and longest interval. **This is NOT proof of displayed FPS**, since WPF could drop frames after the callback; timestamps exclude idle holds. It never captures achievement names or external data, and does not affect SQLite or GC.

### Owner runtime check

1. Unzip the new artifact and run `Rasc.Desktop.exe`. Open **Preview Unlock** three times quickly enough to build a stable 3 stack. Check specifically the **top/middle edge**, then the **middle/bottom edge**, at rest and during push/exit, and compare against C02. Check whether the seam is transparent or simply the original gradient transition.
2. Observe slide smoothness and scrolling/dropouts with multiple previews. Optionally capture 60/120fps video (actual acquisition may differ from refresh) and record monitor refresh + Windows scaling.
3. Close RASC and share `last-classic-animation.txt` if useful; this can reveal callback cadence and stalls. No need to earn or reset CS:GO achievements; previews are isolated from the GC and SQLite history.
4. Verify exit responsiveness, click-through and no orphan window. Real in-game overlay, multi-monitor/DPI and original Steam interpolation are **NOT VERIFIED**.

**Current status:** VERIFIED DEVELOPMENT CHECKPOINT, pending native Windows owner requalification and independent Work 1 QA. [UEP C03 checkpoint](checkpoints/P002-C01-C03.md). P000/P001/P002 remain OPEN; P002's **visual design only** is accepted.

---

## Historical guides (C02 and earlier, superseded for latest artifact)

# P002-C01-C02 — Latest Classic font/seam calibration build

**Current status:** VERIFIED DEVELOPMENT CHECKPOINT (Windows CI **48/48**), **NOT** independently verified or owner GUI-qualified. P002 remains OPEN. [Full C02 checkpoint](checkpoints/P002-C01-C02.md).

**Latest exact tested production:** `67a6c7bbef1ade46e204da8d8b38e48cf05bd52b`. [Windows CI](https://github.com/thgillwtnorizoh/rasc/actions/runs/44031682814) · [Windows x64 artifact ZIP](https://github.com/thgillwtnorizoh/rasc/actions/runs/44031682814/artifacts/14305836935).

### What changed since C01-C01

- **Owner target Helvetica Regular** for both headline and body (Windows will fall back to Arial if Helvetica is unavailable), `FontSize=11.5` WPF DIPs and `LineHeight=12.5` DIPs, **Normal** weight instead of prior bold. These sizes are estimates from reference screenshot glyph proportions, not independent Steam font proof. Original pinned May 2023 `FriendsSmall` specifies Arial/tall12/weight800; contradiction **C-D09** remains open.
- **Preview Unlock:** `Achievement Unlocked!` should occupy **one** headline line; `Someone Set Up Us The Bomb` should wrap to two body lines.
- **Preview Progress:** `Achievement Progress` should occupy **one** headline line; body naturally wraps `Paranatural Collection (20,010/100,000)` into two lines. This is a visual-only preview; no durable progress events are created.
- **Apparent gap:** owner's 228×166 screenshot contains a lower-card gradient at row~83 and 14px blank icon/top inset before y~98; this is **within** the card rather than a proven 14px transparent margin. Original source icon y14 is preserved. An integer physical-pixel origin/clip mapper adds 13 parameterized DPI tests to prevent *actual* subpixel seams at 100–200% scaling; WPF physical screen QA remains pending.
- No change to full-height slide, three-card FIFO, source read-only monitor, persistent SQLite history or no-fade policy.

### What to test

Extract the latest ZIP, start `Rasc.Desktop.exe`, then use **Preview Unlock** and **Preview Progress** without monitoring to compare one-line headlines and two-line body against your reference uploads. Trigger two previews close together and inspect the area between cards carefully: does it show a **transparent line exposing the desktop**, or merely the lower card's darker-to-lighter gradient and original 14px top padding? If possible include a screenshot and your Windows DPI/display scale. Never reset the GC file to force test events.

**Do not copy screenshots or proprietary Helvetica/Steam fonts into the repository/application**; only measurements and provenance notes are retained.

---

## Historical C01-C01 instructions (superseded by the latest C02 build)

# P002-C01-C01 — Owner-target Classic desktop notification correction

**Campaign:** P002 — Classic notification rendering. **Actor:** Work 2 development and own regression, NOT independent Work 1 QA. **Assignment:** [Issue #3](https://github.com/thgillwtnorizoh/rasc/issues/3). **Status:** **VERIFIED DEVELOPMENT CHECKPOINT**: source compiled, tests pass, artifact published; user GUI qualification and source-perfect visual equivalence **NOT VERIFIED**.

## Latest Windows build

- **Exact source tested:** `9d6bbe8c1b0a76019fe5a2b4941c5196e5ff6cb0`. [Windows CI run 44020123840](https://github.com/thgillwtnorizoh/rasc/actions/runs/44020123840) — 35/35 automated tests, WPF build PASS, self-contained x64 publish PASS, artifact upload PASS.
- [Download corrected Windows x64 artifact](https://github.com/thgillwtnorizoh/rasc/actions/runs/44020123840/artifacts/14300597403) and extract entire ZIP to a folder; launch `Rasc.Desktop.exe`. Unsigned development build, Windows 10/11 x64, .NET 10 self-contained.
- Keep `%LOCALAPPDATA%\RASC\history.db` intact. Neither preview modifies the GT-610 `achievements.txt` file, SQLite history or Steam.

## Targeted owner visual qualification

1. **Preview Unlock** WITHOUT starting monitoring. Inspect title left alignment, font weight/glyph size, two-line text wrapping at "Someone Set Up Us The" / "Bomb", square icon placeholder, borderlessness, background gradient, flush right/taskbar placement, and entrance from behind the taskbar.
2. **Preview Progress** WITHOUT starting monitoring. Inspect "Achievement Progress", body "Paranatural Collection" plus second line "(20,010/100,000)". This is a visual fixture **only**; the GT-610 file has no durable progress callback stream.
3. While the first preview is still visible, click **Preview Unlock** again one or two times. Verify each new card rises from the taskbar and pushes the previous cards upward with **no gap**. No alpha fade must appear.
4. Queue four or more previews quickly. With three settled, further events wait; at normal expiry, the top/oldest card should sink **down behind the middle** before the next rises from the taskbar. The top must never disappear upward beyond the viewport.
5. Let a single card time out: it must sink into the taskbar, its moving bottom edge hidden. Check desktop mouse/focus unaffected.
6. Close RASC mid-animation: it should shut promptly without leaving a floating popup or destroying the database.
7. For real unlocks: select actual GT-610 `csgo_gc/achievements.txt`, enable **Classic popups**, Start monitoring and earn a new achievement. **Past history rows are not replayed as popups**. If checkbox is unchecked at receipt, event remains durable in history without delayed visual replay.

For visual feedback, please record OS DPI scale, taskbar docking/orientation, monitor details, whether RASC was run alone or above CS:GO, and ideally screenshot or 60fps recording. Do not reset or edit the game's achievements to force previews; dedicated preview controls are safer.

## Source-based profile versus engineering choices

| Attribute | Source authority / observed target | Current implementation |
|---|---|---|
| Panel | Original VGUI 240×94 nominal | 240×94 WPF DIP card; same physical pixels unverified |
| Icon | Original x14/y14, 64×64 icon, frame66 | 64×64 **unframed placeholder**, no original asset redistributed |
| Text | Source x88/y25 title, x88/y53 description, width144, heights28 | WPF positions and 12pt-like size; both **Arial ExtraBold**, grayscale display formatting, screenshot-target **LEFT** alignment |
| Title alignment | Original May 2023 resource explicitly **CENTER**, owner's two screenshots look **LEFT** | LEFT **owner visual target**, disagreement **C-D08 unresolved** |
| Background | Gradient #2A2E33→#121A2A and bottom dark fill | Approximate source-based WPF gradient; original pixel raster/clipping unverified |
| Border | Original style procedural one-unit edges; owner asks no visible strokes | No deliberate card or icon border stroke; contradiction **C-D05** preserved |
| Stack | Owner target zero gaps and margins, bottom newest | One 240×282 transparent viewport, flush primary **desktop work area** bottom-right; only bottom taskbar currently targeted |
| Arrive | Owner target always rises from taskbar, pushes older cards up one height | Full-height 94-DIP upward translation, 450ms, **no alpha fade**, all arrivals serialized |
| Exit | Owner target sinks behind below card or taskbar; moving lower edge never visible | Downward 94-DIP translation, per-card clip and lower-card z occlusion, 450ms, no alpha fade |
| Hold | May 2023 DisplayTime=6 | 6000ms after each card's entrance completes |
| Capacity | May 2023 StackSize=3, background queue after full | FIFO, max3; full-stack waits for old card's expiry; immediate eviction remains original-runtime unknown |
| Native motion | Steam config calls timings `Fade*` but also `SlideDirection=Vertical`; owner observed no opacity changes | Linear progress `p` as **ENGINEERING**; interpolation fidelity not proven |
| Input & host | Original overlay/runtime unverified | One click-through nonactivating topmost WPF host with Win32 extended styles; GUI verification pending |
| Audio / actual names | Original assets/callbacks not verified; GT-610 file contains raw IDs | No audio, real events display raw ID fallback; original/localized metadata and icons not yet supported |

**Specific source/sample records:** [P000 Classic resource reference](../../research/P000/classic/reference.md), [Owner screenshot calibration](fixtures/owner-classic-screenshot-calibration.md), [P002-C01-C01 motion contract](contracts/P002-C01-C01-classic-stack-motion.md), [P000 discrepancy register](../../research/P000/validation/discrepancies.md). Owner supplied two references attributed to original Steam; exact build, crop, resampling and display scale are not independently established. No attached copyrighted image is packaged into RASC.

## Architecture and retained data safety

- `src/Rasc.Core/ClassicToastScheduler.cs`: monotonic virtual-time serialized 3-card lifecycle with FIFO waiting and per-card 450/6000/450ms timing; physical Y/clip rectangles and **constant opacity1**. Long `Advance` jumps resolve intermediate transitions at historical deadlines for deterministic tests.
- `src/Rasc.Desktop/ClassicToastStackWindow.xaml[.cs]`: shared transparent 240×282 WPF composition window, snap-to-pixel and lower-card z-order occlusion, click-through/nonactivation flags.
- `src/Rasc.Desktop/ClassicToastCard.xaml[.cs]`: 240×94 card style and text; no visible border strokes.
- `src/Rasc.Desktop/ClassicToastHost.cs`: 16ms UI dispatcher timer, live card visual creation/removal, deterministic content mapping, preview fixtures and window shutdown.
- `src/Rasc.Desktop/MainWindow.xaml[.cs]`: preview controls, popups checkbox and new GC unlock callback. `GetRecent` history restoration is **not** an event source and cannot schedule popups.
- Existing `UnlockEventStore` SQLite atomic baseline/dedup and source read-only behavior unchanged. Preview does not touch ledger or source.

## CI/regression evidence

[Windows Actions 44020123840](https://github.com/thgillwtnorizoh/rasc/actions/runs/44020123840), `windows-latest`, .NET 10:
```powershell
dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release --logger "trx;LogFileName=rasc.trx"
dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental
dotnet publish src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/Rasc-Windows-x64 -p:PublishSingleFile=false
```
**35 passed, 0 failed, 0 skipped. WPF build and self-contained publish PASS.** The final run cleared the two new xUnit analyzer warnings. Earlier intermediate scheduler-only commit failed WPF compile because its old presenter depended on a removed frame property; this was corrected by the integrated shared-host commit, which built successfully before the final clean run.

**NOT RUN:** actual Windows popup rendering/screenshot, original Steam runtime comparison, human focus/click-through check, DPI/multi-monitor and taskbar orientation, exclusive fullscreen, sound, genuine new CS:GO popup with new build, independent Work 1 QA.

## Status and next gate

Read [checkpoint](checkpoints/P002-C01-C01.md). R01–R10 deterministic behaviors supported by tests; R11 static border-source review, R12 fonts and screenshot match **NOT ACCEPTED**, R13 ledger integration passed, R14 manual Windows runtime **NOT RUN**, R15 Windows CI passed. P002 OPEN, pending owner visual qualification, independent QA and explicit owner acceptance. Modern P002-C02 still out of scope.
