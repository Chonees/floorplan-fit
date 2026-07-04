---
type: Implementation
date: 2026-06-04
project: floorplan-fit
status: active
tags:
  - floorplan-fit
  - loop1
  - fit-curation
  - pinch-groups
---

# Fit pinch groups can be removed with their pinches

## Change

The Floorplan Review Fit panel now lets the operator delete a selected pinch group, removing the group and all pinches that belong to it.

## Why

Deleting only individual pinches forced noisy manual cleanup when an entire adjustment group was wrong. The robust behavior mirrors franjas: select the structure, delete the structure, and clear dependent children explicitly.

## Design

- Application owns the use case in `RemovePinchGroupHandler`.
- The handler validates:
  - curation exists,
  - curation is `Draft`,
  - group exists,
  - group belongs to the active curation.
- Persistence exposes explicit operations:
  - `IPinchMarkerRepository.RemoveByGroupAsync(curationId, pinchGroupId)` deletes dependent pinches.
  - `IPinchGroupRepository.RemoveAsync(pinchGroupId)` deletes the group.
- Delete order is intentional: markers first, group second.
- Desktop routes the mutation through `FloorPlanReviewMutationCoordinator.RemovePinchGroupAsync`.
- `FloorPlanReviewViewModel.RemoveSelectedPinchGroupAsync` stops pinch-placement mode, refreshes the session, clears selected group/marker, and shows Spanish status messages.
- `ReviewFloorPlanWindow.axaml` adds `Eliminar grupo de pinches` under `Grupos de pinches`, enabled via `CanRemoveSelectedPinchGroup`.

## Verification

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter RemovePinchGroupHandlerTests --no-restore` => 1/1.
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Pinch_repositories_remove_a_group_and_only_its_markers" --no-restore` => 1/1.
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~RemoveSelectedPinchGroupAsync_removes_selected_group_and_all_its_markers" --artifacts-path ".artifacts-test/pinch-group-delete-run"` => 1/1.
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~Fit_tool_uses_a_canvas_first_icon_palette_instead_of_a_workbench_column|FullyQualifiedName~AddDesktopSlice1_registers_review_session_services" --artifacts-path ".artifacts-test/pinch-group-delete-layout"` => 2/2.

## Gotcha

The local desktop app was running and locked normal Desktop output DLLs. Tests were re-run with isolated `--artifacts-path` output instead of killing the user app.

## Related

- [[../Bugs/2026-06-04 - Pinch groups could not be deleted from Fit panel]]
- [[2026-06-01 - Fit franja nodes can be removed individually]]
- [[2026-05-22 - Fit pinch groups panel and handle gating]]
