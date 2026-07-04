---
type: Implementation
date: 2026-06-01
project: floorplan-fit
status: active
tags:
  - floorplan-fit
  - loop1
  - fit-curation
  - dimensions
  - franjas
---

# Selecting a bound dimension opens its Fit franja

## Change

Touching/selecting a dimension that already has a saved `DimensionIntervalBinding` now selects the bound franja in the right Fit panel.

## Why

The previous behavior only selected the dimension itself. The binding summary could say that the dimension had a saved relation, but `SelectedMeasurementCorridor` stayed on the previous franja, so the right panel did not show the franja the dimension belonged to.

## Design

- `FloorPlanReviewViewModel.OnSelectedDimensionChanged` now resolves the saved binding for the selected dimension.
- When a binding exists, the ViewModel restores the measurement selection from that binding:
  - selected franja/corridor;
  - selected node, using the binding start node;
  - selected A/start node;
  - selected B/end node.
- Unbound dimensions keep the existing behavior: they do not force a franja and can still use the currently selected franja for manual assignment.

## Verification

- RED: `SelectDimension_selects_the_bound_franja_and_nodes_in_the_fit_panel` failed because selecting the bound dimension left the unrelated franja selected.
- GREEN: the same test passed after selecting the saved measurement binding from `OnSelectedDimensionChanged`.
- Focused slices passed:
  - `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~MeasurementBindingFloorPlanReviewViewModelTests" --artifacts-path .testartifacts\dotnet-test-artifacts-select-dim-franja-vm-slice` => 19/19.
  - `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests|FullyQualifiedName~FloorPlanReviewViewModelTests" --artifacts-path .testartifacts\dotnet-test-artifacts-select-dim-franja-selection-slice` => 54/54.
- `git diff --check` exited 0.
