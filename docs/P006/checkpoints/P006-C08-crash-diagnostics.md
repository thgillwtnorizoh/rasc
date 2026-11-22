# RASC P006-C08 — bounded crash and Classic toast timeline diagnostics

Project / Campaign / Actor / Assignment: CS:GO Achievements Resurrection / P006-C08 / Work 2 / first-party local diagnostic logging.
Starting SHA: `9e735e931d0e878f5761fc744efa506b32de41c4` — latest P006-C07 active development branch (not stale default main).
Status: ENGINEERING IMPLEMENTATION; exact-head build/test results pending publication. This checkpoint is NOT owner acceptance.

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
Use existing Windows Actions workflow to build, run all core xUnit tests, all WPF GUI smoke scenarios and self-contained x64 packaging; assert exact SHA and preserve historical suites. No native fullscreen CS:GO or Windows Event Viewer crash reproduction yet, so error cause remains **UNVERIFIED RUNTIME**.
After owner reproduces: preserve newest rasc log, `last-unhandled-error.txt`, `last-shutdown.txt`, `last-classic-animation.txt`, approximate wall time + fullscreen mode. Distinguish managed exception logs from abruptly truncated logs (native kill/crash, access violation, power or process termination).
Acceptance: owner qualifies actual CS:GO Classic crash and approves contents/privacy/retention. Do not assert an unproven renderer fix.

Coverage gained: persistent pre-crash timing/state and centralized managed exception information for next investigation.
Remaining: application shutdown without managed events, actual native crash reason, independent QA, production screenshot comparison.
Next bounded task / actor: Work 2 verify latest CI; owner reproduce and supply sanitized diagnostics; Work 1 independent explanation.
Subsystem: OPEN.
