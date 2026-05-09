---
type: implementation
status: completed
date: 2026-05-06
project: floorplan-fit
area: Loop 1 curation
---

# Pinch groups persisted for named shrink zones

## What

Loop 1 pinch curation now persists named pinch groups so strategic pinches can be organized by shrink zone, such as `Patio`, `Main Width`, or `Kitchen Height`.

## Why

The future site-plan fit engine must not treat every `Width` or every `Height` pinch as one global pool. It needs semantic shrink zones so it can trim only the minimum necessary group, for example shrinking only the patio before touching the main body of the plan.

## Where

- Domain:
  - `src/FloorplanFit.Domain/FloorPlans/PinchGroup.cs`
  - `src/FloorplanFit.Domain/FloorPlans/PinchMarker.cs`
- Contracts:
  - `src/FloorplanFit.Contracts/FloorPlans/PinchGroupDto.cs`
  - `src/FloorplanFit.Contracts/FloorPlans/PinchMarkerDto.cs`
  - `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs`
- Application:
  - `src/FloorplanFit.Application/Abstractions/IPinchGroupRepository.cs`
  - `src/FloorplanFit.Application/FloorPlans/Curation/AddPinchGroupHandler.cs`
  - `src/FloorplanFit.Application/FloorPlans/Curation/AddPinchMarkerHandler.cs`
- Infrastructure:
  - `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchGroupRepository.cs`
  - `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchMarkerRepository.cs`
  - `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs`
  - `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- Desktop:
  - `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
  - `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
  - `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`

## Behavior

- A pinch group stores:
  - name
  - axis (`Width` / `Height`)
  - curation id
  - sort order
- A pinch marker now stores `PinchGroupId`.
- Review sessions return both groups and markers.
- The Review UI can create/select groups and place pinches into the selected group.
- Compression preview uses the selected group, so dragging a width/height handle does not automatically affect every marker on that axis.

## Migration

`SqliteSchemaInitializer` supports:

- legacy grouped markers using `pinch_group_id + curated_wall_id`
- previous axis-tagged markers using `source_candidate_id + axis_tag`

Axis-tagged markers are migrated into default `Width` / `Height` groups per curation.

## Verification

- `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --no-restore` -> `17/17`
- `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --no-restore` -> `20/20`
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --no-restore` -> `21/21`
