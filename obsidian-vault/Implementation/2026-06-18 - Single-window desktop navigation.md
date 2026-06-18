---
type: implementation
date: 2026-06-18
status: active
---

# Single-window desktop navigation

## What changed

The Desktop app now keeps the main product screens inside one `MainWindow` shell:

```text
Library -> Edit -> Adjust to Site Plan -> Library
```

## Why

The previous behavior opened Loop 1 Edit and Loop 2 Adjust as modal windows via `ShowDialog(...)`. That was useful for fast iteration, but product-wise it felt like jumping between separate tools instead of using one coherent app.

## Verified code truth

- `ReviewFloorPlanWindow.axaml` is now a `UserControl`.
- `SitePlanAdjustmentWindow.axaml` is now a `UserControl`.
- `MainWindow.axaml` embeds both controls and switches visibility through `LibraryViewModel`.
- `MainWindow.axaml.cs` no longer creates `ReviewFloorPlanWindow` / `SitePlanAdjustmentWindow` instances or calls `ShowDialog(this)` for the main product screens.
- Small dialogs remain dialogs: file picker, export picker, and pinch-group naming.
- Follow-up correction: `RequestedThemeVariant="Dark"` was removed from the embedded `UserControl` roots after Avalonia reported `AVLN2000`; the theme remains on the top-level `MainWindow`.

## Product scope

- Product loop: shared foundation between Loop 1 and Loop 2.
- Architecture layer: Desktop only.

## Verification

No build was run, following repo rules.

Source-level checks verified:

- `MainWindow.axaml` embeds both main workbenches.
- `MainWindow.axaml.cs` no longer opens the main screens with modal dialogs.
- Review and Adjust roots are `UserControl`.
- `git diff --check` passed for the touched files, with CRLF warnings only.
