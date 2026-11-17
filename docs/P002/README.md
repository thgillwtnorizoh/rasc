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
