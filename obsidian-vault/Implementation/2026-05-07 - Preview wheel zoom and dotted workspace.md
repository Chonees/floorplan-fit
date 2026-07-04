---
project: floorplan-fit
type: Implementation
date: 2026-05-07
status: active
---

# Preview wheel zoom, pan, and dotted workspace

## What changed

The Loop 1 review preview now supports mouse-wheel zoom, click-and-drag panning with the wheel / middle mouse button, and a subtle dotted workspace background instead of a flat white canvas.

## Why

The user needs to inspect CAD geometry, pinch markers, and DXF-like room labels comfortably inside the preview without changing the curated data. Zoom belongs to the Desktop preview interaction layer, not the persisted curation model.

## Implementation

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - Added runtime-only preview zoom state.
  - Mouse wheel updates zoom using a clamp from `0.35x` to `6x`.
  - Zoom is anchored at the current mouse position, so the world point under the cursor stays visually stable.
  - Middle mouse press captures the pointer and dragging updates the runtime pan offset, so the user can slide around the floor plan after zooming.
  - The preview background now renders a subtle dot grid workspace.
  - Rendering, hit-testing, room labels, pinch markers, and compression preview all use the transformed viewport.

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs`
  - `PreviewViewport` can now apply a user transform, project double world coordinates, and unproject screen coordinates back to world coordinates.
  - Hit-testing can reuse an explicit transformed viewport so clicking/pinch placement remains accurate while zoomed.

- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
  - Covers zoom factor bounds, cursor-anchored zoom, middle-button pan state, and workspace dot placement.

- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`
  - Covers viewport user transform and zoom-aware hit-testing.

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj` -> 33/33 passing
- `git diff --check` -> exit 0

## Notes

This intentionally does not persist zoom/pan. The next UX improvement, if needed, is a reset-view affordance.
