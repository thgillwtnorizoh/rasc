# P003-C03-C05 — Owner-supplied rare achievement alpha masks

**Owner report:** November 18, 2026. C03-C04 single-HWND continuous Modern stack is **OWNER-QUALIFIED as gapless**. Rare gold glow is **INVISIBLE** in the owner's Windows tests; a yellow star is visible but not a halo. The owner explicitly supplied two complete PNG Data URLs and authorized implementation.

## Original/source evidence

- First user-supplied asset is labeled **achievement_square_mask.png**, a 64×64 RGBA PNG whose Base64 starts with \`iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAW60lEQVR4nNWbSZMd\` (7,900 Base64 characters, 5,924 bytes in the matching public CodePen mirror).
- Second asset is labeled **achievement_star_mask.png**, a 64×64 RGBA PNG whose Base64 starts with \`iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAcZklEQVR4nJ2bd7hd\` (9,772 Base64 characters, 7,327 bytes).
- The same prefixes and CSS mask placement are independently visible in https://codepen.io/Dataminer/pen/pooWzpo and in https://github.com/Zenrac/Zenrac.github.io/blob/main/css/style.css (mirror). The actual binary mask payloads for this development branch were sourced from that matching public CodePen mirror and encoded as native WPF Resources, without transcoding or resampling.
- **Git blob identity:** square e81be0ce97e8efbbe663567783f5c7d858da064f; star 211aca32f4dd2d54515286627d161785f86de3a1.
- The public Steam Community CSS references files with these names, but equivalence to a particular release of Valve's *desktop toast* assets is **NOT independently established**. The user identifies them as official Valve masks; that is an owner-supplied attribution, not a proven version match.
- **Rights:** Valve/Steam and the CodePen author's asset redistribution permissions were not independently established. Images are temporarily included in an isolated draft Work 2 branch/build for the owner's authorized fidelity testing. **Mandatory OWNER licensing decision before merge/public redistribution.** See \`src/Rasc.Desktop/Assets/SteamRareMasks/NOTICE.md\`.

## What went wrong previously

C03-C04 built an original WPF subtractive outer rectangle (68×68 mask with hard 48×48 hole) and a procedural 14-spoke star. These are **NOT** equivalent to the real 64×64 alpha masks in the reference. In particular, applying two serial masks plus the low-opacity radial gradient can nearly erase the visible light, even if the nested rotation transforms and static shadows are running. This is a **reconstructed likely mechanism**, not a verified Windows crash or compositing trace.

## Development correction

- Use \`BitmapImage\` and \`ImageBrush\` with \`Stretch=Fill\`, applying the original 64×64 PNG **alpha channel** as a full-size mask to the 68×68 root and the rotating 88×88 inner layer. No drawn polygon or rectangular hole.
- Keep the CodePen reference gold radial gradient, original stop colors and opacity, 18s reverse inner mask, 6s forward inner light, 48px fixed image and dual near/far box shadow.
- Preserve shared single-HWND, gapless 1/2/3 Modern stack, continuous backdrop/watermark, desktop/fullscreen-only anchor, Classic and profile transitions.
- Add an actual WPF smoke that checks decoded mask dimensions/non-uniform alpha, visually rendered **gold pixels OUTSIDE** the 48×48 icon, and a second capture requiring **moving halo pixels**; this closes the previous "invisible but tests passed" loophole.
- No runtime network requests or Steam client interaction. No GC/SQLite/Steam history writes from preview.

## Evidence classifications

- **OWNER-QUALIFIED:** gapless C03-C04 Modern stack.
- **OWNER-REPORTED DISCREPANCY:** no gold glow despite yellow rare icon.
- **CONFIRMED CODEPEN REFERENCE:** two Base64 masks and CSS layer mapping.
- **ENGINEERING:** WPF alpha masks from PNG resources, test-pixel thresholds.
- **VERIFIED COMPONENT:** pending exact-SHA Windows CI.
- **INDEPENDENTLY VERIFIED / OWNER GUI:** not yet performed on C05.
- **DISCREPANCY / LIMIT:** rights/authentic client-build status unresolved; dynamic WPF opacity/blur fidelity still needs owner screenshots.

**P003 subsystem OPEN.**
