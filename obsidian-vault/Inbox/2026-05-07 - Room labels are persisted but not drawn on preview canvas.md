---
type: Inbox
project: floorplan-fit
date: 2026-05-07
status: superseded
replaces:
replaced_by: [[Implementation/2026-05-07 - Room label overlay rendered on preview canvas]]
---

# Room labels are persisted but not drawn on preview canvas

## What

Room label candidates are currently visible only in the right-side `Rooms` panel of the Review UI.

## Evidence

- `RoomLabelDto` includes `X` and `Y`, so the DXF insertion point is persisted and available.
- `FloorPlanReviewViewModel` loads `session.RoomLabels` into the `RoomLabels` collection.
- `ReviewFloorPlanWindow.axaml` binds `RoomLabels` only to the right-side `Rooms` list.
- `FloorPlanPreviewControl` currently binds and renders `GeometryPaths` and `PinchMarkers`; it has no `RoomLabels` bindable property and no room-label render pass.

## Product implication

The data exists and is organized, but the floor plan image does not yet overlay the room text labels at their DXF coordinates.

## Recommended next implementation

Add `RoomLabels` to `FloorPlanPreviewControl`, bind it from `ReviewFloorPlanWindow.axaml`, and render each label by projecting `RoomLabelDto.X/Y` through the same preview viewport used for geometry.

## Gotcha

DXF text coordinates are insertion points, not guaranteed room centers. MVP should render them as small badges at the insertion point first; later we can improve placement/centering.

## Superseded

This gap was closed by rendering `RoomLabels` directly in `FloorPlanPreviewControl` and binding them from `ReviewFloorPlanWindow.axaml`.
