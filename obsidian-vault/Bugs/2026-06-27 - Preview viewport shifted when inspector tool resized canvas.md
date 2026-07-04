# Preserved preview viewport on inspector tool size changes

Date: 2026-06-27
Type: Bugfix
Scope: Loop 1 Desktop preview / inspector tools

## Symptom
When the user was already zoomed into a floor plan and switched right-side inspector tools such as Overview, Position, Actions, or Fit constraints, the preview view could visibly shift.

## Root Cause
`ReviewFloorPlanWindow.axaml` shows the `FitToolPalette` above the preview only when `IsFitToolSelected` is true. That changes the `FloorPlanPreviewControl.Bounds` height. `FloorPlanPreviewControl` already preserved zoom/pan when geometry changes caused a new base viewport, but it did not mark the viewport for preservation when the control render size changed.

## Fix
`FloorPlanPreviewControl` now listens to `BoundsProperty` changes. If width or height changed, it sets `preserveViewportOnNextRender`, reusing the existing `PreserveZoomStateForBaseViewportChange(...)` path to keep the same world point anchored on screen.

## Evidence
- RED: focused test failed because `ShouldPreserveViewportOnBoundsChange(...)` did not exist.
- GREEN: `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~ShouldPreserveViewportOnBoundsChange_only_when_render_size_changes"` passed 1/1.
- Regression slice: `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --no-build --filter "FullyQualifiedName~FloorPlanPreviewControlTests"` passed 65/65.

## Files
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
