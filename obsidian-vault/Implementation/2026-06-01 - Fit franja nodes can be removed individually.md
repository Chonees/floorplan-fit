---
type: Implementation
date: 2026-06-01
project: floorplan-fit
status: active
tags:
  - floorplan-fit
  - loop1
  - fit-curation
  - measurement-nodes
  - franjas
---

# Fit franja nodes can be removed individually

## Change

The Floorplan Review Fit panel now lets the operator delete a selected node inside `Nodos de esta franja` without deleting the whole franja.

## Why

Deleting an entire franja was too destructive when the operator only mis-clicked one node. The robust behavior is granular: remove the selected `MeasurementNode`, remove only interval bindings that reference that node, and keep the corridor/franja plus unrelated nodes intact.

## Design

- Application owns the use case in `RemoveMeasurementNodeHandler`.
- The handler validates that the curation exists, is still `Draft`, and that the selected node belongs to the active curation.
- Persistence exposes two explicit operations:
  - `IMeasurementNodeRepository.DeleteAsync(nodeId)` deletes the single node.
  - `IDimensionIntervalBindingRepository.DeleteByNodeAsync(curationId, nodeId)` deletes only bindings where the node is `start_node_id` or `end_node_id`.
- Desktop uses `FloorPlanReviewMutationCoordinator.RemoveMeasurementNodeAsync` so the ViewModel stays orchestration-focused.
- After deletion, `FloorPlanReviewViewModel` refreshes the session, keeps the selected franja when it still exists, clears the selected node, and clears A/B endpoints only if they referenced the removed node.
- The right Fit panel adds a scoped `Eliminar nodo` button under `Nodos de esta franja`, enabled only when a node is selected.

## Verification

- RED/GREEN Application: `RemoveMeasurementNodeHandler_deletes_selected_node_and_bindings_that_reference_it`.
- RED/GREEN Desktop ViewModel: `RemoveSelectedMeasurementNodeAsync_deletes_selected_node_keeps_franja_and_clears_invalid_binding_selection`.
- RED/GREEN Layout: `Fit_tool_uses_a_canvas_first_icon_palette_instead_of_a_workbench_column` failed until `Eliminar nodo` was added to XAML.
- Infrastructure regression: `Repositories_can_delete_a_single_node_and_only_interval_bindings_that_reference_it` verifies SQL deletes bindings where the removed node is start or end and preserves unrelated bindings.
- Focused slices passed:
  - `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --nologo --filter "FullyQualifiedName~DimensionIntervalBindingHandlersTests" --artifacts-path .testartifacts\dotnet-test-artifacts-remove-node-app-slice` => 4/4.
  - `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~MeasurementBindingFloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests" --artifacts-path .testartifacts\dotnet-test-artifacts-remove-node-desktop-slice` => 23/23.
  - `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --nologo --filter "FullyQualifiedName~MeasurementIntervalBindingPersistenceIntegrationTests" --artifacts-path .testartifacts\dotnet-test-artifacts-remove-node-infra-slice` => 4/4.
- `git diff --check` exited 0.
