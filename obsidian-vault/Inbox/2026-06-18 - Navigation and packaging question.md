---
type: inbox
date: 2026-06-18
status: active
---

# Navigation and packaging question

## User concern

The user questioned why the app starts at the Library and then opens Edit / Adjust to Site Plan as separate modal windows instead of feeling like one single-page app with internal navigation.

The user also questioned why a CMD/console appears when launching the app, because the desired product should feel like a real downloadable desktop app, not a developer command-line tool.

## Verified current code truth

- `FloorplanFit.Desktop` is an Avalonia desktop app with `<OutputType>WinExe</OutputType>`, so the project itself is configured as a windowed Windows executable.
- Startup creates `MainWindow` with `LibraryViewModel`, so Library is currently the shell/root screen.
- `Edit` opens `ReviewFloorPlanWindow` via `ShowDialog(...)`.
- `Adjust to Site Plan` opens `SitePlanAdjustmentWindow` via `ShowDialog(...)`.
- The visible CMD is explained by the development launcher (`scripts/dev-desktop.bat`) using `dotnet watch ... run`, not by the product architecture.
- There is no checked-in installer/publish profile yet.
- Runtime workspace currently lives under `AppContext.BaseDirectory/workspace`, meaning a zip-style portable app can work from a writable folder, but a real installed app should move workspace data to a user-writable location such as LocalAppData.

## Product direction to decide

Recommended minimal direction: keep Library as the home shell, but replace modal Edit/Adjust windows with an internal navigation/workbench region:

```text
Library -> Edit Workbench -> Adjust Workbench -> Export
```

This makes the app feel like one product while preserving the existing Loop 1 / Loop 2 mental model.

