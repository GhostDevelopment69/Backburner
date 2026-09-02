# Backburner — Developer Documentation
 
This document is for anyone contributing code to Backburner. It covers the
project's purpose, current architecture, safety principles, and how to get
a working dev environment running.
 
---
 
## What Backburner is
 
Backburner is a Windows 11 tray utility that suspends background resource
drain (services, and eventually startup apps/processes) on manual user
command, and restores everything when deactivated. The original motivation
was replicating the performance gains of Xbox Full Screen Experience mode
without forcing controller-only, handheld-style UI on the user.
 
The gaming use case is the default target, not a hard boundary. The engine
is designed to be profile-driven so it can be forked or extended toward
other foreground-heavy workloads (creative apps, unsupported-hardware
installs, etc.) without touching core logic.
 
## Non-negotiable design principles
 
These aren't style preferences — they're the actual trust and safety
contract of the project. Any PR that violates these gets rejected
regardless of how good the feature is:
 
1. **No silent activation.** The app never suspends anything without an
   explicit user action (tray click, hotkey, or CLI command). No
   auto-start on boot, no background daemon, no scheduled activation
   unless a future feature makes that strictly opt-in and visibly
   configured.
2. **No network calls, no telemetry.** Zero phone-home behavior, period.
3. **Always restorable.** Every suspend action must have a corresponding,
   tested restore path. If you add a new type of system state that gets
   modified (services, startup entries, process priority, whatever comes
   next), it is not mergeable until snapshot + restore both exist and are
   verified.
4. **Crash-safe.** The app must recover system state even if it's killed
   uncleanly mid-session — not just on a graceful exit.
5. **Least privilege, and documented.** If a change requires touching a
   new Windows API or elevation scope, explain why in the PR description.
## Current architecture
 
```
/src        engine + tray application (WinForms, .NET)
/profiles   suppression target lists (JSON, not yet implemented)
/docs       explanations of what each toggle does and why
```
 
### Key files (as of now)
 
- **`Program.cs`** — entry point. On startup, checks for an orphaned
  snapshot file from a previous crashed session and restores it before
  the tray app even launches.
- **`TrayContext.cs`** — the entire UI. A `NotifyIcon` with an
  Activate/Deactivate toggle and Exit. No window, no form.
- **`ServiceSnapshot.cs`** — the core engine. Handles:
  - `Capture()` — snapshots current status + start mode of a given
    service list to `snapshot.json`
  - `Suspend()` — stops services that are safely stoppable
  - `Restore()` — restores service *status* from a snapshot
  - `RestoreStartMode()` — restores service *start mode* from a snapshot
    (separate from status restore due to a WMI API quirk — see inline
    comments)
  - `ClearSnapshot()` — deletes the snapshot file after a clean restore
### Why status and start mode are handled separately
 
`ServiceController` (the simpler .NET API) can read and set a service's
*running status* but has no concept of *start mode* (Automatic / Manual /
Disabled). Start mode requires WMI (`System.Management`,
`Win32_Service`). The two APIs also disagree on string formats — WMI
*reads* start mode as `"Auto"` but *writes* it via `ChangeStartMode`
expecting `"Automatic"`. This mismatch is handled in
`RestoreStartMode()` — don't "simplify" this without checking both
directions still work.
 
## Safety net status
 
| Layer | Status |
|---|---|
| Manual activate/deactivate only | Done |
| Service status snapshot + restore | Done |
| Service start mode snapshot + restore | Done |
| Crash recovery on next app launch | Done |
| Hardcoded engine-level denylist (independent of profile) | In progress |
| Task Scheduler fallback restore (recovers even if app never relaunches) | In progress |
| Heartbeat/watchdog process | Planned (v2) |
| Persistent action logging | Planned |
 
The two in-progress items are the current priority. Until both are done,
**do not expand the test service list beyond safe, disposable services**
(e.g. Print Spooler). We are intentionally not touching anything with
real dependents until the recovery guarantees are solid, since this is
being developed and tested on a primary daily-driver machine, not a
disposable VM.
 
## Getting a dev environment running
 
1. Install the .NET SDK (LTS) — [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
2. Install VS Code + the **C# Dev Kit** extension
3. Clone the repo, check out the `development` branch — this is where
   active work happens, not `main` or `testing`
4. From `/src`, run `dotnet build` to confirm the toolchain works
5. Run the app **as Administrator** — service start/stop requires
   elevation, you'll get access-denied exceptions otherwise
6. To manually test: Activate should stop the test service (Print
   Spooler by default), Deactivate should restore it. Confirm via
   `sc query spooler` or the Services app.
### Testing crash recovery
 
Don't skip this if you touch anything in `ServiceSnapshot.cs`:
 
1. Run as Administrator, click Activate
2. Confirm `snapshot.json` exists and the test service is stopped
3. Force-kill the app via Task Manager (not the tray Exit option — that's
   a clean shutdown and doesn't test this path)
4. Relaunch the app
5. Confirm the service auto-restored and `snapshot.json` is gone
If this doesn't hold, do not merge — this is the core guarantee the
whole project is built around.
 
## Branch structure
 
- **`main`** — stable releases only
- **`testing`** — new fixes/features land here before release
- **`development`** — active work happens here
PRs should target `development` unless otherwise agreed.
 
## Where to start
 
Good first areas, in rough priority order:
 
1. Engine-level hardcoded denylist (see safety net table above)
2. Task Scheduler fallback restore
3. Moving the hardcoded test service list into a real `/profiles/default.json`
4. Persistent logging of suspend/restore actions
Open an issue or comment on an existing one before starting substantial
work, so effort doesn't collide.
 
## Questions
 
If something in this doc is unclear or out of date, that's a bug — open
an issue or just ask. This file should always reflect what the code
actually does, not what it's supposed to do eventually.
