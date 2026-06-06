---
type: Bug
date: 2026-06-01
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - measurement-bindings
  - reactive-preview
  - cross-axis
---

# Cross-axis bound dimensions stay behind during pinch preview

## Verified root cause

Dragging a Width compression handle correctly updates the preview geometry on the X axis, and dragging a Height handle correctly updates it on the Y axis.

The gap was in node-bound dimension projection. `DimensionIntervalReactiveProjector` skipped every manual-verified dimension whose measurement corridor axis did not match the active selected pinch band's axis. That meant:

- active Width band -> Width-bound dimensions recalculated, Height-bound dimensions stayed authored/static;
- active Height band -> Height-bound dimensions recalculated, Width-bound dimensions stayed authored/static.

This made opposite-axis cotas look detached from the floor plan even though their underlying geometry paths were moving in the preview.

## Fixed

`DimensionIntervalReactiveProjector` now resolves live nodes for cross-axis bound dimensions too.

Same-axis dimensions keep the existing behavior: they recompute their measured span/text along their own authored visual axis.

Opposite-axis dimensions now translate by the average live-node delta on the active compression axis only. This keeps the cota attached to the moved floor plan without changing its measured number or deforming its authored CAD shape.

## Why this is the senior model

If a Width adjustment moves a wall left/right, a vertical Height cota attached to that wall should ride left/right with the wall, but its height value should not change. Conversely, a horizontal Width cota should ride up/down during a Height adjustment without recalculating width.

The fix intentionally separates:

- **measurement change**: only same-axis bound dimensions recalculate value;
- **visual accompaniment**: opposite-axis bound dimensions translate with the affected geometry.

## Verification

- RED: `Project_translates_height_dimensions_with_width_preview_delta_without_recalculating_the_height` failed because the Height dimension stayed at X=100 instead of moving to X=80.
- GREEN: the same test passed after cross-axis dimensions translated with active-axis node deltas.
- Focused projector suite passed 13/13:
  `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --nologo --filter "FullyQualifiedName~DimensionIntervalReactiveProjectorTests" --artifacts-path .testartifacts\dotnet-test-artifacts-cross-axis-application`
- Application tests passed 83/83:
  `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --nologo --artifacts-path .testartifacts\dotnet-test-artifacts-cross-axis-app-all`
- Focused Desktop preview slice passed 69/69:
  `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~NativeDimensionPreviewControlTests|FullyQualifiedName~MeasurementBindingPreviewLayerRendererTests" --artifacts-path .testartifacts\dotnet-test-artifacts-cross-axis-desktop-focused`
- `git diff --check` exited 0 with only line-ending warnings.
