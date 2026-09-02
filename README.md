# Backburner
 
**Put your background apps on the backburner.**
 
Backburner is a free, open-source Windows utility that suspends background
resource drain — services, startup apps, idle bloat — while you're actively
gaming, and restores everything the instant you're done. No controller-only
mode. No console shell. No forced app switch. Just your desktop, minus the
noise fighting your foreground app for CPU and RAM.
 
---
 
## Why
 
Windows 11's Xbox Full Screen Experience gets a real performance boost when
active — but it locks you into a controller-driven, handheld-style interface
to get it. There's no reason the performance gain and the interface should
be bundled together. Backburner unbundles them: same idea, none of the
lock-in.
 
## What it does
 
- Snapshots the current state of running services, startup apps, and
  background processes
- Suspends or defers a configurable list of background bloat on your command
- Restores everything to its original state when you're done — automatically,
  even on a crash or unclean shutdown
- Runs nothing in the background unless you tell it to. No service, no
  scheduled task, no auto-start, no telemetry, no network calls, ever
## What it doesn't do
 
- It does not touch your desktop, taskbar, mouse, or keyboard
- It does not require a controller
- It does not run silently or activate itself
- It does not phone home
## How it works
 
Backburner is a small system-tray application. You activate it manually —
tray icon, hotkey, or CLI right before you launch whatever you're playing.
It reads a local config profile describing which services and processes are
safe to suspend, snapshots their current state, and suspends them. When you
deactivate, it restores everything from that snapshot.
 
The suppression target list lives in a plain JSON config file, not in code.
That's deliberate — different machines, different OEM bloat, different
priorities. Fork the profile, not the engine.
 
```
/src        engine + tray application
/profiles   community-contributed suppression profiles
/docs       what each toggle does, and why
```
 
## Status
 
Early development. Core suspend/restore cycle is the current focus before
anything else gets built on top of it.
 
## Philosophy
 
Backburner is not for sale and never will be. It exists to be forked,
picked apart, and improved by anyone who wants a leaner version for their
own hardware, or wants to take the same engine somewhere entirely different.
The gaming profile is the default, not the ceiling.
 
Security and trust matter more than features here. No hidden behaviour, no
silent activation, no data leaving your machine. If something in this repo
doesn't match that, open an issue.
 
## Requirements
 
- Windows 11
- .NET runtime (see releases for the current target version)
- Administrator privileges (required to manage services — Backburner only
  ever touches what's listed in your active profile)
## Contributing
 
Pull requests, new profiles, and issue reports are all welcome. If you're
adding a profile for a different use case — creative apps, unsupported
hardware, anything else — it belongs in `/profiles`, not baked into core.
 
## License
 
MIT. Take it, fork it, ship your own version.

