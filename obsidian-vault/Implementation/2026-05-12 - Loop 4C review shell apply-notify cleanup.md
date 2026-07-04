---
type: Implementation
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - review
  - modularization
  - loop-4
---

# Loop 4C review shell apply-notify cleanup

## What changed

Se extrajo el shell residual de apply/notify desde `FloorPlanReviewViewModel` a dos coordinators chicos:

- `FloorPlanReviewApplyCoordinator`
- `FloorPlanReviewNotificationCoordinator`

## Why

Despues de Loop 4B, el ViewModel ya no mezclaba queries ni projections, pero todavia quedaba barro en:

- `ApplySessionProjection(...)`
- `ApplySelectionPresentationOutcome(...)`
- `NotifyReviewQueueStateChanged()`
- `NotifyUxStateChanged()`

Ese bloque seguia mezclando apply truth, replay de seleccion, sync/clear de editors, normalizacion de inspector tool y fan-out manual de `OnPropertyChanged(...)`.

## Where

- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewApplyCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewNotificationCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewInspectorCoordinator.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`

## Scope that now belongs to the coordinators

### `FloorPlanReviewApplyCoordinator`

- session apply planning desde `ReviewSessionProjection`
- selection replay planning post-refresh
- selection-presentation apply planning
- linked candidate / pinch group resolution
- editor sync/clear planning

### `FloorPlanReviewNotificationCoordinator`

- queue property notification sets
- UX/inspector property notification sets
- normalizacion de `SelectedInspectorTool`

## What stayed in the ViewModel

- observable properties
- `ObservableCollection` ownership
- final property assignment
- `OnPropertyChanged(...)`
- command entrypoints

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop4c1-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop4c1-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop4c2-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop4c2-green-rerun`

## Learned

- El corte sano no era un coordinator gigante de shell; separar **apply truth** y **notification truth** dio mejor modularidad y menor blast radius.
- Mantener el ViewModel como emisor final de `OnPropertyChanged(...)` preserva mejor el comportamiento MVVM que empujar notificaciones a otro objeto.
- Despues de Loop 4C, la deuda residual deja de ser un hotspot de codigo y pasa a ser cierre documental / truth maintenance.
