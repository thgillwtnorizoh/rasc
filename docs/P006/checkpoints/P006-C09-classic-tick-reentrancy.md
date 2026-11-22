# RASC P006-C09 — Classic rendering reentrancy regression and crash correction

Project / Campaign / Actor / Assignment: CS:GO Achievements Resurrection / P006-C09 / Work 2 / Classic six-burst KeyNotFoundException lifetime.
Status: VERIFIED CHECKPOINT (Windows core/real-WPF development), original CS:GO post-fix runtime NOT VERIFIED; owner acceptance pending.

Starting SHA: `62479a95d3a37a78b5554fab9dd9c3a57ed98cdb` (active P006 development, not stale default main).
Exact tested SHA: `db06fbd53a43e8798f33bc3bf17741270cebfae2`. Published merge SHA: `b10f91bc5e55d3f125180cfcb24ef917b000d905`. Both have the **same** source-tree SHA `1118d89203042b42f586fd2a5062ed43bb13a6e5`.

## Owner evidence (CONFIRMED APP RUNTIME)
Owner uploaded `rasc-20261122T154755609-26292-00.log` and `last-unhandled-error.txt` from local CS:GO fullscreen test. The session started 2026-11-22 15:47:55 UTC. At 15:48:58.591 the monitor accepted six new unlocks; the Classic host created a card, repeatedly advanced/retired cards and played audio normally. At 15:48:59.874 `System.Collections.Generic.KeyNotFoundException` terminated process during `ClassicToastHost.Tick()`, source line 298: `_text[frame.Event.EventId]`. The stack passes through `ClassicToastHost.OnRendering`, WPF `MediaContext.RenderMessageHandlerCore` and `HwndTarget.OnResize`. This is CONFIRMED APP exception and validates the owner-reported half-entrant-card, sound, crash progression; no original Steam behavior is inferred.

## Historical regression provenance
Path history at `src/Rasc.Desktop/ClassicToastHost.cs`:
- 2026-11-17 `2ac0a565a8918822d56eff050da5a7e16ab21d6c` introduced shared Classic card host, synchronized `_cards` rendering cache and `_text.Remove(id)` retirement.
- 2026-11-17 `984db970f0c6ac9880333606da8195a7d2b57543` introduced `CompositionTarget.Rendering` per-frame slide advancement (and display metric resize path), still without reentrant Tick protection.
- P003-C03-C02 issue #15 previously addressed Modern→Classic profile switching crash (Classic-only was negative control, original exception trace not provided). It did NOT establish this six-burst host cache lookup invariant. Do not claim identical root cause.
- P006-C07 `5f2a32d2` added a supplemental animation timer for fullscreen callback starvation, retaining the older direct dictionary indexing. P006-C08 `cd378d5286dc` added logs but did not change scheduler cache lifetime. Later instantaneous multi-achievement delivery made the old untested path easy to reach. Thus code history supports a latent renderer reentrancy defect **surfacing**, not proven source reversion.
- Previous scheduler xUnit exercised multi-award FIFO in isolation, while Classic real-WPF smoke exercised single preview/partial-slide and Modern waves; none injected a nested `Tick()` that moved scheduler after an outer frame snapshot.

## Bounded functional repair
- Serialize `ClassicToastHost.Tick()` across synchronous WPF callbacks; nested calls set a deferred-redraw flag instead of advancing the scheduler and deleting `_text` while the outer frame snapshot is still being applied.
- Drain at most 16 immediate deferred ticks and queue remainder at DispatcherPriority.Background to avoid UI starvation; preserve monotonic scheduler and existing Classic layout/audio/Modern behaviour.
- Replace unsafe `_text[id]` access with `TryGetValue`: unexpected future invariant failures are locally logged and the invalid frame skipped rather than terminating the entire app. No fallback/placeholder achievement is invented.
- Keep prior C07 fallback timer and C08 logs, add limited `classic.reentry` diagnostic signals.
- Add real-WPF `classic-reentrant-six-burst` smoke. Six previews are enqueued together, a test-only monotonic clock and hook cause a nested renderer Tick to advance beyond card retirement before the original frame is materialized. **Old implementation crashes with KeyNotFoundException** under this sequence (deduced from exact old source); fixed implementation defers reentry and retires the stack safely. Assert at least one deferral, no cache-miss diagnostic and no residual window. No gameplay or permanent test-only user control.

## Tests and confidence
[Windows CI 45041913080](https://github.com/thgillwtnorizoh/rasc/actions/runs/45041913080) **SUCCESS on exact tested head**: 264/264 xUnit core checks (0 failed, 0 skipped); native WPF compile; all Classic/Modern real-window regression scenarios, including PASS `classic-only`, PASS `classic-render-stall`, **PASS `classic-reentrant-six-burst`**, and P006 sealed/immutable wave suites. Windows x64 self-contained artifact built and uploaded. Initial development Actions 45041780285 failed solely because the synthetic smoke clock was incorrectly typed as TimeSpan? instead of Func<TimeSpan>; subsequent edit fixed that harness error, and final run passed. Preserve that historical failure.
Source analysis and deterministic synthetic test design: RECONSTRUCTED/ENGINEERING; original nested callback trigger only partially established by WPF stack. Acceptance of code mechanism requires actual regression test success. Real user rerun of original CS:GO after fix: NOT RUN.
Independent Work 1 and original game fullscreen/alt-tab crash repetition: NOT VERIFIED.
Known limitations: new defensive missing-content branch skips a broken frame, records invariant, no crash. RASC logs have bounded retention. Unrelated instant award source/GC and Classic visual calibration excluded.
Updated evidence: this checkpoint, PR comments, CI; original failing user log is not copied to repository because it contains local stack/path data.
Next bounded task / Actor: Owner/Work 1 verify live six-burst fullscreen with merged build; inspect newest `%LOCALAPPDATA%\\RASC\\logs\\rasc-*.log` for `classic.reentry`, `classic.invariant`, and any `crash.dispatcher`. Work 2 addresses any reproducible remaining discrepancy.
Subsystem: OPEN; owner acceptance pending.
