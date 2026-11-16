# CS Virtual Achievements (RASC)

An independent Windows companion for displaying and browsing virtual Counter-Strike: Global Offensive achievements without representing them as Steam-published achievements.

**Project status:** Research / P000 OPEN. No production implementation authorized by this repository bootstrap.

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
- **P001:** proposed Windows application foundation. **NOT AUTHORIZED**.
- **P002:** proposed classic/modern notification renderers. **NOT AUTHORIZED**.

Research and checkpoint records belong under `research/P000/` when verified. Maintain recovery notes in-repository. Do not run old Steam client binaries or interact with the installed Steam client as part of static reference analysis.

## Related references

- [Original May 2023 SteamTracking manifest](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientManifest/steam_client_win32), version `1685488080`.
- [Original classic notification layout](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/friends/AchievementNotification.res).
- [Original classic styles](https://github.com/SteamTracking/SteamTracking/blob/90999d0307ac7f2b92f805e8f3a5d159421eaf8a/ClientExtracted/resource/styles/steam.styles).
- [GT-610 community Game Coordinator](https://github.com/GT-610/csgo-gc).

All original-behavior claims must retain exact authority and confidence; do not substitute a visual preview for runtime verification.
