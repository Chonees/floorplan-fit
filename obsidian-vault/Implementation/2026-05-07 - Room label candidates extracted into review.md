---
type: Implementation
project: floorplan-fit
date: 2026-05-07
status: current
replaces:
replaced_by:
---

# Room label candidates extracted into review

## What

Loop 1 now extracts room names from DXF `ROOM LBLS` text entities as separate **room label candidates** and shows them in the Review UI.

## Why

The curation flow needs lightweight semantic context such as `KITCHEN`, `LIVING ROOM`, `BEDROOM`, `PATIO`, etc. for future fit/adaptation decisions, but those labels are not walls and should not be attached to individual wall lines.

## Where

- `src/FloorplanFit.Application/Abstractions/DetectedRoomLabel.cs`
- `src/FloorplanFit.Application/Abstractions/IRoomLabelExtractor.cs`
- `src/FloorplanFit.Application/Abstractions/IExtractedRoomLabelRepository.cs`
- `src/FloorplanFit.Domain/FloorPlans/ExtractedRoomLabel.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaRoomLabelExtractor.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedRoomLabelRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Contracts/FloorPlans/RoomLabelDto.cs`
- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`

## Behavior

- Reads `TEXT` and `MTEXT` entities from exact layer `ROOM LBLS`.
- Normalizes multiline/MTEXT formatting into plain text.
- Filters out generic labels like `FLOOR PLAN` and `WALL LEGEND`.
- Keeps labels with room-like vocabulary such as kitchen, living, bedroom, bath, patio, garage, closet, pantry, utility, porch, etc.
- Persists labels per `wall_extraction_run` in `extracted_room_labels`.
- Exposes labels through `FloorPlanReviewSessionDto.RoomLabels`.
- Shows labels in a right-side `Rooms` card in the review screen.

## Current limitation

These are **labels only**, not room boundary polygons. Existing extraction runs are not backfilled; re-run `Extract Walls` and reopen Review to see room labels for an already imported template.

## Verification

Sequential verification was used because parallel test projects can lock shared build outputs in this repo.

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --no-restore` -> 20/20
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --no-restore` -> 25/25
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --no-restore` -> 21/21

## Links

- [[Current State]]
- [[Inbox/2026-05-07 - Room names extraction for review curation]]
- [[Implementation/2026-05-06 - Geometry-based wall thickness inference]]
