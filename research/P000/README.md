# P000-C01 — Steam notification reference consolidation

**Project:** CS Virtual Achievements (RASC). **Assignment:** [Issue #1](https://github.com/thgillwtnorizoh/rasc/issues/1). **Status:** RESEARCH CHECKPOINT; P000 OPEN.

**One-time operator exception (owner-directed, 2026-11-16):** Headquarters handles Work 1 research and same-operator checking for **P000-C01 only**, because separate session quota is unavailable. The tests in this campaign are **SELF-VERIFIED**, not independent Work 1/Work 2 verification. No independent-runtime, production, or subsystem-acceptance label is inferred. Restore separate roles in subsequent campaigns unless separately authorized.

## Evidence map

- [Source provenance](sources/provenance.md): archive hashes, exact manifest, custom client boundary.
- [Classic renderer](classic/reference.md): original 2023 VGUI layout, styles, text, settings and binary markers.
- [Modern renderer](modern/reference.md): local modern JS/CSS, popup-window state machine, settings and sounds.
- [Renderer contract](comparison/renderer-contract.md): separable implementation targets and fixture matrix.
- [Discrepancy/coverage register](validation/discrepancies.md).
- [Verification report](validation/verification-report.md): tests, commands, limits.
- [Offline archive checks](tools/verify_archives.py): standard-library-only, strictly read-only.

## Research boundary

This is **source-derived reference research**, not a finished application or proof of visual equivalence. Source claims are labeled CONFIRMED ORIGINAL (direct original source), RECONSTRUCTED, or ENGINEERING; runtime unknowns are UNVERIFIED RUNTIME. The modern client archive is user-supplied and potentially customized; any claim of Valve-stock equivalence needs a version-matched clean source or runtime test. Uploaded proprietary artifacts must not be copied to the repository.

**Explicit exclusions:** WPF/production implementation, CS:GO game integration, DLL execution, code injection, old Steam authentication, reproduction/redistribution of Steam assets, original runtime comparison, and owner acceptance.
