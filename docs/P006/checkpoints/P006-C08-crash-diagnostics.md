# RASC P006-C08 — bounded crash and Classic toast timeline diagnostics

Project / Campaign / Actor / Assignment: CS:GO Achievements Resurrection / P006-C08 / Work 2 / first-party local diagnostic logging.
Starting SHA: `9e735e931d0e878f5761fc744efa506b32de41c4` — latest P006-C07 active development branch (not stale default main).
Status: VERIFIED CHECKPOINT (Windows CI); real CS:GO fullscreen runtime and root cause NOT VERIFIED. Not owner-accepted.
Exact tested production SHA: `900fc6ce7236bc754fd8ff290165c8de8f04d895`.
Published merge SHA: `81fb3c57a052bcfd5b48718f2aea674ac23a12e2`.
Identical tested/merged source tree SHA: `fc7bdb876becf4bedcfa7904f19130370067dc26`.

## Owner evidence
November 22 owner clarified original runtime symptoms: Classic toast freezes halfway through slide, achievement sound still plays, then RASC process exits/crashes. A previously proposed render-starvation watchdog passed simulated GUI tests but **did not establish the true root cause**; the actual WPF/Win32 exception or native termination is unknown. No stack trace, temporal log or dump from the failing legacy game was provided. Classify as OWNER-OBSERVED runtime report / DISCREPANCY, not CONFIRMED native mechanism.

## Scope and integration
- Add `DiagnosticLog`, a dependency-free, best-effort, local-only rolling session text logger in Rasc.Core. New log each process run under `%LOCALAPPDATA%/RASC/logs/rasc-{UTC}-{PID}-{attempt}.log`, capped at 2 MiB and most recent 8 sessions; lines have ISO 8601 UTC timestamp, severity, category, message; multiline exceptions normalized. Writes are thread-safe and flushed, fatal explicitly fsyncs when possible. If IO unavailable, log failure must not break normal app.
- Application hooks: startup, WPF DispatcherUnhandledException, AppDomain.UnhandledException, TaskScheduler.UnobservedTaskException, normal exit; preserve original `last-unhandled-error.txt` and `last-shutdown.txt` instead of silently replacing them or swallowing exceptions.
- Presenters: Classically enqueued events, stage entering/visible/exiting, geometry/clip at stage transitions (not every render frame), animation segments, callback starvation/resumption, window open/retire and anchor HWND dimensions/fullscreen mode. No card names or event IDs in diagnostic messages. Modern/profile gate: batch size and presentation queue/dispatch + selected mode; cue type and success/fail; monitor source mode and exception stack.
- README gives path and post-crash reproduction. No automatic uploads, invasive hooks, remote telemetry, native crash dumps, Windows registry or original GC data changes.
- Test fixtures: cross-platform logger appends, single-line exception, file size/cap, pruning and inaccessible destination; real WPF `classic-only` scenario checks that actual presentation generates Classic queue/stage/window events.
- Side effects: logging can slightly increase disk IO on stage transitions/queue dispatch; logs are best effort. Exception strings may reveal local paths and require owner review before sharing. No per-frame logging.

## Verification plan
[Windows Actions 45031676859](https://github.com/thgillwtnorizoh/rasc/actions/runs/45031676859) **SUCCESS** on exact production SHA: **264/264 core xUnit tests, 0 failed, 0 skipped**; native WPF app compile; real-WPF Classic-only and suppressed-render Classic smoke; real UI smoke assertions for emitted diagnostic log entries; all existing Modern, rare glow, P006 achievement-library, burst and wave tests passed; self-contained Windows x64 package/artifacts uploaded. Three preceding feature runs (45031308318, 45031421419, 45031547339) failed while resolving Windows file-sharing test semantics and a new AppLog namespace import; failures preserved as engineering evidence. Original fullscreen CS:GO crash and Windows Event Viewer correlation were **NOT RUN** and remain **UNVERIFIED RUNTIME**.
After owner reproduces: preserve newest rasc log, `last-unhandled-error.txt`, `last-shutdown.txt`, `last-classic-animation.txt`, approximate wall time + fullscreen mode. Distinguish managed exception logs from abruptly truncated logs (native kill/crash, access violation, power or process termination).
Acceptance: owner qualifies actual CS:GO Classic crash and approves contents/privacy/retention. Do not assert an unproven renderer fix.

Coverage gained: persistent pre-crash timing/state and centralized managed exception information for next investigation.
Remaining: application shutdown without managed events, actual native crash reason, independent QA, production screenshot comparison.
Next bounded task / actor: Owner reproduce Classic halfway-popup → sound → crash with build containing P006-C08; preserve newest session log and optional last-unhandled error; Work 1 independently analyze actual exception/foreground HWND sequence and propose bounded fix.
Subsystem: OPEN.
