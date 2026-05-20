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
  - loop-3
---

# Loop 3B selection state extraction

## What changed

Se completo la extraccion del bloque de selection state de `FloorPlanReviewViewModel` a `FloorPlanReviewSelectionCoordinator`, primero sacando routing/snapshot truth y despues los presentation side effects de seleccion.

## Why

Despues de cerrar Loop 3A, el hotspot principal ya no era la ceremonia mutante sino la mezcla de:

- preview-hit routing
- selection snapshot capture/replay
- cross-clears de seleccion
- `HighlightGeometryPathId`
- `PreviewSelectionLabel`
- sync/clear de curated editors y label text height editor

Todo eso en el mismo ViewModel era una mala frontera de responsabilidad para la capa Desktop.

## Where

- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSelectionCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/LabelTextHeightFloorPlanReviewViewModelTests.cs`
- `docs/superpowers/specs/2026-05-12-loop-3b-selection-state-design.md`
- `docs/superpowers/plans/2026-05-12-loop-3b-selection-state-plan.md`

## Scope that now belongs to the coordinator

- preview-hit priority resolution
- selection snapshot capture
- moved-artifact snapshot derivation
- replay resolution after refresh
- selection presentation outcomes for:
  - candidate
  - curated artifact
  - room label
  - pinch marker
  - opening candidate
  - opening label
  - fixed plan component
  - protected detail assembly
  - dimension

## What stayed in the ViewModel

- `[ObservableProperty]` ownership
- observable collections
- final property assignment
- `NotifyUxStateChanged()`
- load/refresh query flow
- editor value application helpers

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop3b2-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop3b2-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~MovableArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop3b-final-focused`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop3b-full`

## Learned

- Para esta codebase, separar primero la **verdad de seleccion** y despues los **presentation side effects** fue mucho mas seguro que intentar reescribir todo selection state de una.
- Un `SelectionPresentationOutcome` chico alcanza para sacar strings, highlight rules y editor sync del ViewModel sin moverle el ownership de propiedades observables.
- Cerrado Loop 3B, el siguiente dilema sano ya no es selection state: ahora toca decidir entre **Loop 3C queue/filter orchestration** o pasar a **Loop 4 cleanup final**.
