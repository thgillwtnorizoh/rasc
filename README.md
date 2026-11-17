# CS Virtual Achievements (RASC)

An independent Windows companion for displaying and browsing virtual Counter-Strike: Global Offensive achievements without representing them as Steam-published achievements.

**Project status:** P000 research OPEN; P001-C01 native Windows foundation development checkpoint published and build-verified, user-runtime-qualified; independent QA and owner acceptance pending. P002 Classic first development slice AUTHORIZED by owner; modern renderer deferred.

## Scope and provenance

- Initial integration target: standalone CS:GO (Steam AppID `4465480`) using the GT-610 `csgo-gc` local achievements provider.
- Initially read-only: observe `csgo_gc/achievements.txt` and derive only progress justified by documented game statistics and achievement criteria.
- Notification reference profiles: **Steam Classic (May 2023 VGUI)** and **modern Steam**. Their original rendering mechanisms and behavior must be documented separately.
- Preserve distinctions among CONFIRMED ORIGINAL, RECONSTRUCTED, VERIFIED COMPONENT, INDEPENDENTLY VERIFIED, ENGINEERING, UNVERIFIED RUNTIME and DISCREPANCY.
- Research assets and proprietary Steam binaries remain in their original permitted locations; reference them by manifest, original URL and hash rather than committing redistributable copies.
- External/customized client resources are not automatically evidence of unmodified Valve behavior.

## Organization

- **Headquarters:** scope, evidence registers, review and proposed acceptance.
- **Work 1:** original-behavior research, independent fixtures and QA.
- **Work 2:** production implementation and development tests *only when separately authorized*.
- **Owner:** decides acceptance. A research checkpoint is not subsystem acceptance.

## Campaigns

- **P000:** Steam notification visual and runtime reference investigation. **OPEN**.
  - **P000-C01:** consolidate, independently verify, and publish classic/modern notification reference contracts, their provenance and discrepancies.
- **P001:** native Windows foundation. **AUTHORIZED / DEVELOPMENT CHECKPOINT** — [Issue #2](https://github.com/thgillwtnorizoh/rasc/issues/2); development tests and Windows build passed, independent QA and owner acceptance pending.
- **P002:** first **Steam Classic** desktop toast slice authorized for development under P000 source-based limits. Modern profile remains planned.

Research and checkpoint records belong under `research/P000/` when verified. Maintain recovery notes in-repository. Do not run old Steam client binaries or interact with the installed Steam client as part of static reference analysis.

## Related references

- [Original May 2023 SteamTracking manifest](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientManifest/steam_client_win32), version `1685488080`.
- [Original classic notification layout](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/AchievementNotification.res).
- [Original classic styles](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/styles/steam.styles).
- [GT-610 community Game Coordinator](https://github.com/GT-610/csgo-gc).

All original-behavior claims must retain exact authority and confidence; do not substitute a visual preview for runtime verification.

## First Windows development artifact

- [P001-C01 guide](docs/P001/README.md) — architecture, build/run instructions and supported profile.
- [P001-C01 checkpoint](docs/P001/checkpoints/P001-C01.md) — exact tested SHA, Windows CI artifact, testing and limitations.
- [Windows Actions artifact (self-contained x64)](https://github.com/thgillwtnorizoh/rasc/actions/runs/43906938776/artifacts/14246535435) — development build; owner observed live unlocks and tested shutdown.

The app does not modify your CS:GO files or grant Steam achievements. Select the GT-610 `csgo_gc/achievements.txt` path manually. First observation of existing awards establishes a silent baseline; subsequent observed changes appear in the event history. Original classic/modern Steam popup renderers are not yet implemented.

**Latest P001 runtime note:** Owner confirmed six live unlocks, then a restart without duplicate event rows. Owner reported an earlier ~5-second exit; the revised build was user-qualified with a prompt visible close and 24.3ms internally while actively monitoring. The cause of the old delay remains unknown. P001-D01 is RESOLVED for owner-observed runtime (not independent QA). See [P001-C01-C02 owner evidence](docs/P001/checkpoints/P001-C01-C02.md).
