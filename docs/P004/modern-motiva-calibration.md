# P004-C02 — Modern Motiva Sans typography calibration (experimental, reversible)

**Goal:** Test the owner's observation: the original Modern Steam achievement toast has a Motiva Sans-like hooked lowercase 'a', but RASC's native Motiva Sans option looks heavier/wider than its Segoe UI-based sans-serif fallback.

**Starting stable implementation:** P004-C01 `772d9296457cfc248da51fa39cee97045ce69f6b` (178 core + 7 WPF smoke passed). P003-C07 owner accepted modern glow/geometry retained. This C02 is a **controlled font-only experiment**, **NOT** a silent change to approved default visuals.

## Source inspection (reproducible provenance)

- SteamTracking publicly preserves Steam UI source in [`ClientExtracted/steamui/css/sp.css` at commit `9f22fbdfd7710a57d59e07b7469c684b8c4cf0d2`](https://github.com/SteamTracking/SteamTracking/blob/9f22fbdfd7710a57d59e07b7469c684b8c4cf0d2/ClientExtracted/steamui/css/sp.css). The file SHA1 blob is `8f867b6980e95d529dd530e9a1e3e385846effd4`, ~3.2MB. This is **not** the owner's build-11229305 hashed `dogbones.zip` and the repo has since changed its CSS packing. Do not claim this snapshot is version-matched.
- The archived CSS **explicitly uses** `font-family: "Motiva Sans", Arial, Helvetica, sans-serif` and also `"Motiva Sans", Helvetica, sans-serif`. Examples around CSS byte offset ~2,700,400 have `font-weight: 500; font-size: 11px` for a different UI component; another at ~2,760,462 uses 14px weight 400. **These rules are not yet linked by component/class evidence to the Modern achievement toast.** The visually similar font family is source-supported *elsewhere in Steam*, but the exact Modern achievement title weight/size/line-height remain **NOT VERIFIED**. Do not substitute these arbitrary component numbers as original facts.
- The actual desktop toast background selector `._1OOQPJx0nCNP9ME9toCmV7` and container `.zXrpABNQHpWKgSzqnGlL` appear around byte 2,790,682 and support 283×70 and dark radial styling, but do **not themselves define achievement title font metrics**. CSS inheritance and title/description child component selectors require a linked JSX/JS render-class trace, ideally from the owner's original `dogbones.zip`, which is not present in this session's accessible files. The on-screen text raster, Steam webfont load variant and CEF rendering remain unverified.
- Current RASC source: [ModernToastCard](../../src/Rasc.Desktop/ModernToastCard.cs) baseline 12 DIP/600 title, 11.5 DIP/400 body, 11 DIP/400 progress with default WPF line height and kerning. Font-only choices changed the family but not the metrics. **WPF text uses its own rasterizer, not Chromium's font rasterizer.**

## Debug comparison presets

The **Modern Motiva calibration** dropdown in the main window has five values. The *Baseline* default preserves existing appearance and obeys the existing Popup font selector. **All non-Baseline presets force installed Motiva Sans for Modern text only**, leaving Classic text unchanged. Presets make one change from the prior row; use them in order to isolate effects.

| Preset | Title WPF DIP size | Weight | Body WPF DIP size | Body weight | Meaning |
|---|---:|---:|---:|---:|---|
| Baseline | 12 | 600 | 11.5 | 400 | Previously approved RASC visual; current font selector still controls family |
| A: Motiva 12/400 | 12 | 400 | 11.5 | 400 | Test reducing title weight only, keep size |
| B: Motiva 12/500 | 12 | 500 | 11.5 | 400 | Test 500 vs 400 at same size |
| C: Motiva 11/500 | 11 | 500 | 11.5 | 400 | Test title size reduction |
| D: Motiva 11/500, body 11 | 11 | 500 | 11 | 400 | Test body size reduction |

All presets leave letter-spacing/tracking at **WPF natural spacing** and do not claim to replicate browser CSS tracking or glyph hints. Progress number remains 11 DIP/400 to avoid shifting medal/numeric positioning. Title and body positions, 283×70 geometry, text boxes, icon, C07 gold masks/shadows, wave compositor and entrance sound are unchanged. Changes occur live and persist in the existing backwards-compatible `notification-customization.json` `ModernTypography` property; old files default to Baseline. No Motiva font files are redistributed.

## Owner visual qualification protocol

1. Verify **Motiva Sans really exists as an installed font**, and record full face name/style; any fallback invalidates the comparison.
2. Set Popup profile=Modern, Popup size=100%, Windows display scale=100%, use identical known title/description fixtures and original Steam toast screenshot. Start on **Baseline + Motiva Sans** in Popup font, capture screenshot.
3. Select A, B, C, D **without changing any other settings**, capture each at identical DPI and timing (prefer fully settled 900–1500ms) and note differences in hooked 'a', glyph advance widths, visual density, text top alignment and ellipsis.
4. Repeat once in **sans-serif + Baseline** to record the Segoe UI appearance as a contrast, not as a presumed Steam font.
5. Treat the original title/body font size, line height, family selection, font file identity and letter spacing as independent unknowns until CSS computed-style or owner original source is linked. Do not fit arbitrary letter tracking without evidence.
6. Return to **Baseline** after testing. Only promote a final preset into the production default with owner approval and matching source/visual test evidence.

## QA and limits

- `dotnet test tests/Rasc.Core.Tests/Rasc.Core.Tests.csproj -c Release`: existing 178 plus new preset mapping / backwards JSON tests.
- `dotnet build src/Rasc.Desktop/Rasc.Desktop.csproj -c Release --no-incremental` on Windows: WPF app compile.
- WPF real-HWND `font-sound-settings` scenario now explicitly switches typography B (weight 500), C (title size 11), then Baseline on a visible Modern toast before Classic switch.
- Existing `modern-rare`, `modern-stack-two`, `modern-progress`, Classic and C07 Chromium reference checks must continue to pass.
- Owner actual Windows screenshot comparison, independent Work 1 verification, original achievement-specific computed CSS and physical Motiva font raster: **NOT VERIFIED** unless separately logged.

**Subsystem:** P004 OPEN. P003 accepted appearance preserved; no merge or original runtime equivalence claim.
