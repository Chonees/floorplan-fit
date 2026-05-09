---
type: bug
status: fixed
date: 2026-05-05
project: floorplan-fit
area: Loop 1 review UI
---

# Preview control used parent bounds for local rendering

## What

The `FloorPlanPreviewControl` was clipped visually even after the preview container stopped overflowing, because the rendered canvas was shifted downward inside its own control.

## Why

Avalonia renders a custom control in local drawing coordinates, but the preview code was using `Bounds` directly. When the control sits below title/help rows, `Bounds.Top` carries the parent layout offset; drawing with that rect creates blank space at the top and cuts the lower floor plan content.

## Fix

`FloorPlanPreviewControl` now converts Avalonia layout bounds into local render bounds with origin `(0, 0)` before drawing, resolving drag handles, and calculating the geometry viewport.

## Where

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`

## Verification

- Red: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter FullyQualifiedName~Preview_control_localizes_parent_bounds_before_rendering --no-restore` failed because the control did not expose local render bound normalization.
- Green: same targeted test passed after the fix.
- Regression: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --no-restore` passed `21/21`.
