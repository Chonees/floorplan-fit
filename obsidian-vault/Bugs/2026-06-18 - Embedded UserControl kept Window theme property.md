---
type: bug
date: 2026-06-18
status: fixed
---

# Embedded UserControl kept Window theme property

## Symptom

`dotnet watch` failed building `FloorplanFit.Desktop` with:

```text
Avalonia error AVLN2000: Unable to resolve suitable regular or attached property RequestedThemeVariant
```

The failing files were:

- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/SitePlanAdjustmentWindow.axaml`

## Root cause

The single-window navigation change converted the two main product screens from `Window` roots to `UserControl` roots, but left `RequestedThemeVariant="Dark"` on those controls.

That property is valid on the main `Window` shell, but not on these embedded `UserControl` roots.

## Fix

Removed `RequestedThemeVariant="Dark"` from:

- `ReviewFloorPlanWindow.axaml`
- `SitePlanAdjustmentWindow.axaml`

The main shell still keeps `RequestedThemeVariant="Dark"` in `MainWindow.axaml`.

## Verification

No build was run by the agent, following repo rules.

Source checks verified:

- Both embedded roots are `UserControl`.
- Neither embedded root contains `RequestedThemeVariant`.
- `MainWindow.axaml` still contains `RequestedThemeVariant="Dark"`.
- `git diff --check` passed for the touched files, with CRLF warnings only.

