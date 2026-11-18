# Portable recreation and regression plan — renderer archive v1

These are **fidelity requirements and fixture recipes**, not instructions to copy proprietary Steam executables or patch Steam.

## A. New project import checklist
1. Identify which **profile** is requested (Classic or Modern normal desktop); state client/build, window context, OS, DPI, target font availability and icon source. Don't use the shared word "Steam" to merge unrelated generations.
2. Check out `373d04101d03f51135f2f1b192758efddad12f00`; run `python docs/rendering-reference/v1/verify-snapshot.py`. Copy *specification values and independently permitted code*; **do not automatically redistribute** PNG masks, original Steam resource files, logo/achievement art, archived uploads or user-client JS/CSS. Preserve asset provenance and licenses.
3. Build a **renderer-neutral preview input** (unique ID, display text, icon, unlock/progress subtype, optional rarity and progress counters). Keep event acquisition/GC/SQLite separate: no writes to the game, no Steam award APIs, no fake official unlock notifications.
4. Reproduce exact source geometry in one authoritative coordinate system, with explicitly declared design-unit→physical-pixel mapping. Capture window/client/viewport bounds at every layer.
5. Reproduce transitions with monotonic time and the *appropriate profile-specific queue*, then multi-card occlusion/clipping. Render 1,2,3-card cases before any glow iteration.
6. For Modern glow, start from the **source CodePen radial gradient + two masks + two shadows**. Validate actual alpha-mask loading and mapping against element boxes. A screenshot of only the two shadows can look persuasive but is **invalid reference evidence**.
7. Preserve input fixtures and deterministic phase snapshots. Record exact timestamps, screenshot tool, DPI, monitor scale, background, face icon, font/fallback, resource hashes, and engine/rendering backend. Compare visual captures only after matching these variables.
8. Record independent discrepancies; don't silently "fit" alpha, gradients, timings or sizes to one picture. A development test ≠ independent source/runtimes equivalence.

## B. Acceptance and test matrix

| Case | Profile(s) | Required observable result | Automated or independent? |
| --- | --- | --- | --- |
| CL-U | Classic | 240×94 appearance, icon x14 y14 64²; title/body x88 left, source colors, text wrap, no border | screenshot + font environment |
| CL-P | Classic | Progress wording and owner reference line wrap; preview is nonpersistent | screenshot + event safety |
| CL-1/2/3 | Classic | flush right/bottom, sequential push/occlusion, no gaps; every visible pixel opaque | scheduler unit + real WPF |
| CL-4 | Classic | queued events beyond capacity; serial exit; correct survivor order | scheduler unit |
| CL-DPI | Classic | 100/125/150/175/200% real display + Auto/manual size; seamless top/middle & middle/bottom | real OS screenshot **NOT RUN** |
| MO-U | Modern | 283×70 text/icon, radial shared backdrop, watermark once | real GUI |
| MO-P | Modern | 19/50 medal+bar, appropriate clamped progress fraction, no source inventions | core + GUI |
| MO-1/2/3 | Modern | one 283×210 HWND, contiguous shared-wave decoration and bottom origin, no DWM seams | core + real GUI |
| MO-6 | Modern | bounded batches, waves serialized, no overlap between profile windows | core + real GUI |
| MO-R | Modern | icon48, masks 68/88, radial light108, original dual shadows, 6s/18s continuous gold | WPF pixel and geometry smoke |
| MO-CH | Modern | Chromium reference phase900 vs2000 differs and is not no-glow/no-masks control | headless browser screenshot |
| MO-DPI | Modern | physical 100/125/150% and fullscreen/borderless target; synthetic RTB alone insufficient | independent owner hardware **NOT RUN** |
| X-CTX | Both | popup never claims official award, steals focus, changes achievements/history on preview, or interferes with Steam files | event safety + runtime |

Existing [C07 Windows workflow](https://github.com/thgillwtnorizoh/rasc/blob/373d04101d03f51135f2f1b192758efddad12f00/.github/workflows/windows.yml) implements six WPF scenario names plus core 162 tests. New project should add its own independent differential captures, not just import RASC assertions.

## C. Commands on Windows (.NET 10, WPF)
```powershell
python docs/rendering-reference/v1/verify-snapshot.py
dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj --configuration Release
dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --no-incremental
foreach ($scenario in @("modern-rare", "classic-only", "modern-idle-classic", "modern-stack-two", "modern-classic", "modern-progress")) {
    dotnet run --project tests/Rasc.Desktop.Smoke/Rasc.Desktop.Smoke.csproj --configuration Release -- $scenario
    if ($LASTEXITCODE -ne 0) { throw "GUI smoke failed: $scenario" }
}
dotnet publish src/Rasc.Desktop/Rasc.Desktop.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=false
```

Renderer-hash check is **platform-independent Python 3 standard library**. WPF compile/GUI requires Windows; don't reinterpret `NOT RUN` on Linux as passing Windows GUI QA.

## D. Last-mile owner comparison protocol
Capture the current RASC reference and a recreated renderer for the **same card**, same preview text, same icon/background, same monitor DPI and same paused time. Compare full layout, icon halo clipping, hard-edge/warm pixel count, font metrics, watermark placement, cross-card seam and entrance/exit visibility. Use actual OS-level screen captures for anti-aliasing and fractional DPI. Preserve both before/after frames, change log and rejection cases. Submit to **separate Work 1** for independent assertions; owner explicitly decides visual acceptance.

**Avoid regressions:** C06 rectangular golden rays were fixed by absolute alpha-mask mapping, not lowering source brightness; Classic visible no-border/Helvetica owner design was accepted despite genuine May 2023 resource conflicts; Modern joined waves need one composited background/watermark across all visible cards.
