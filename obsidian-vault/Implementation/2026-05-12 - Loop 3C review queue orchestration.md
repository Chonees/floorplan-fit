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

# Loop 3C review queue orchestration

## What changed

Se completo la extraccion del bloque de review queue desde `FloorPlanReviewViewModel` a `FloorPlanReviewQueueCoordinator`, sacando de adentro del ViewModel:

- filtering por bucket
- search matching
- projection de visibles
- grouping de curated artifacts
- normalizacion del expansion state
- collapse de secciones exclusivas

## Why

Despues de cerrar Loop 3A y 3B, el siguiente hotspot real del Desktop shell seguia siendo el bloque de queue:

- `RefreshReviewQueue(...)`
- `NormalizeQueueExpansion(...)`
- `CollapseQueueSectionsExcept(...)`
- `MatchesReviewQueueFilter(...)`
- `MatchesReviewQueueSearch(...)`

Ese bloque mezclaba filtro, proyeccion, grouping y reglas de expansion en el mismo ViewModel, lo cual seguia empastando la lectura de `FloorPlanReviewViewModel`.

## Where

- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewQueueCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

## Scope that now belongs to the coordinator

- `BuildProjection(...)`
- `NormalizeExpansion(...)`
- `CollapseSectionsExcept(...)`
- filter matching
- search matching
- curated artifact grouping

## What stayed in the ViewModel

- observable collections reales
- apply de projection
- apply de expansion state
- `OnPropertyChanged(...)` de summaries/titles/flags
- load/refresh flow del review shell

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop3c-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop3c-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop3c-full`

## Learned

- La review queue tenia una frontera Desktop clarisima: projection + expansion policy. Sacarla a un coordinator fue mas limpio que seguir escondiendo helpers dentro del mismo ViewModel.
- El test de rehoming del expander cuando cambia el filtro pincha una regla UX sutil que no convenia dejar implicita.
- Cerrado Loop 3C, **Loop 3 completo** deja de ser el hotspot principal; el siguiente frente ya pasa a **Loop 4 cleanup final**.
