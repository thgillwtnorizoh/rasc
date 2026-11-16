# P000-C01 — verification report

**Actor:** HQ as single research/test operator for this campaign only. **Classification:** SELF-VERIFIED STATIC CHECKS; NOT INDEPENDENTLY VERIFIED.

## Environment and commands

- Python standard library (`hashlib`, `zipfile`), Linux container, no DLL execution or network calls from Python.
- Re-read public reference files via GitHub connector at exact pinned SteamTracking commit `90999d0307ac7f2b92f805e8f3a5d159421eaf8a`.
- Ran `python research/P000/tools/verify_archives.py --classic /mnt/data/bins_win32.zip.552301e7ebae1bbd0c5a141020798635551c0433 --modern /mnt/data/dogbones.zip`.

**Observed result:** **40/40 PASS; 0 FAIL**. Script checks SHA-256, ZIP CRCs, exact item counts/size, classic binary marker presence, modern bundle/build stamps, JS motion/queue/audio marker references, CSS gradient and dimensions, and absence of a direct desktop-toast selector override in the suspected custom skin. These are **static assertions**, not functional runtime QA.

## Source review checks

Six May 2023 text sources fetched at pinned `90999d0307ac7f2b92f805e8f3a5d159421eaf8a` and blob hashes recorded in [provenance](../sources/provenance.md). Relevant geometry/config/text values independently read directly this execution (same operator).

Modern JS examined at:
- notification registry module: `achievement_notification_toast`, `ToastAchievement`, default `G=5*1e3`;
- desktop module `15148`: `N=0,K=300,V=300,X=0,he=283`, `MoveTo`, `ShowWindow`, `HideWindow`, state machine;
- capacity selection `St=ct?1:3`, active queue and `PopNextToastNotification`;
- desktop CSS hashed classes and original sounds.

Modern JS content is minified and symbols are build-specific; code offsets and module IDs are for this exact supplied bundle only, not a stable API.

## NOT RUN

Actual Steam client, achievement trigger, real notification window, audio playback, 60/120fps video qualification, original overlay/render-hook debugging, Windows runtime scaling, independent actor QA, .NET build/tests, CI, or integration with GT-610. No basis to report functional or complete-system pass/fail counts.

## Internal review / limitations

Reviewed all requirements against the distinction between supported static source and unknown runtime. Prior claims of generic identical popup height, universal 5.6s lifetime, complete stock-mod parity and exact classic easing were narrowed. Further work should focus only on the explicit discrepancies, not broad rescanning. Owner acceptance is pending; restore independent verification at the appropriate future runtime/acceptance gate.
