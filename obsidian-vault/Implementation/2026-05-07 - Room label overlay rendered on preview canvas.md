---
type: Implementation
project: floorplan-fit
date: 2026-05-07
status: superseded
replaces: [[Inbox/2026-05-07 - Room labels are persisted but not drawn on preview canvas]]
replaced_by: [[Implementation/2026-05-07 - DXF-like room label rendering]]
---

# Room label overlay rendered on preview canvas

## What

The Review preview canvas now renders extracted room labels directly over the floor plan image.

## Why

The user could see labels in the right-side `Rooms` panel but not on the actual plan. The data existed with DXF `X`/`Y` coordinates; the missing piece was binding and rendering those labels in `FloorPlanPreviewControl`.

## Where

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

## Behavior

- `FloorPlanPreviewControl` now exposes a `RoomLabels` styled property.
- `ReviewFloorPlanWindow.axaml` binds `RoomLabels` from `FloorPlanReviewViewModel`.
- Each `RoomLabelDto.X/Y` insertion point is projected through the same preview viewport as walls.
- The control draws a small anchor dot at the insertion point and a readable yellow badge with the room name.
- Pinch markers still render after labels, so interactive pinch points remain visually dominant.

## Current limitation

The label is placed at the DXF text insertion point, not inferred room center. This is correct for MVP; later we can improve placement if DXF insertion points feel visually off.

## Verification

- RED: focused Desktop tests initially failed because `FloorPlanPreviewControl` had no `RoomLabels` property and no `ProjectRoomLabel` helper.
- GREEN: focused Desktop tests passed after adding the property, XAML binding, and render projection.
- Full sequential verification after the change:
  - `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --no-restore` -> 20/20
  - `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --no-restore` -> 25/25
  - `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --no-restore` -> 23/23
  - `git diff --check` -> exit 0

## Links

- [[Current State]]
- [[Implementation/2026-05-07 - Room label candidates extracted into review]]

## Superseded

The initial badge overlay was replaced by DXF-like text rendering that preserves height, rotation, alignment, style name and color metadata from the source DXF.
