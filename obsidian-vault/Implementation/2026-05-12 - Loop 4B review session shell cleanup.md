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

# Loop 4B review session shell cleanup

## What changed

Se extrajo el bloque de queries + projection derivada de la review session desde `FloorPlanReviewViewModel` a `FloorPlanReviewSessionCoordinator`.

## Why

Despues de cerrar Loop 4A, el siguiente cluster residual del Desktop shell ya no era presentation del inspector sino:

- apertura de session via `OpenFloorPlanReviewSessionHandler`
- refresh via `GetFloorPlanReviewSessionHandler`
- resolucion de `DraftCurationId`
- proyeccion de `CuratedPlanArtifacts`
- filtrado de `VisibleCuratedPlanArtifacts`
- armado de `DimensionAssociationsById`

Todo eso seguia mezclando query orchestration con el shell observable del ViewModel.

## Where

- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewSessionCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/CuratedArtifactFloorPlanReviewViewModelTests.cs`

## Scope that now belongs to the coordinator

- open/refresh review session queries
- published-vs-draft `DraftCurationId` resolution
- curated artifact projection fallback from detected artifacts
- visible curated artifact filtering
- dimension association dictionary projection
- session DTO -> ViewModel projection packaging through `ReviewSessionProjection`

## What stayed in the ViewModel

- observable properties
- collections and selected entities
- `ApplySessionProjection(...)`
- `RefreshReviewQueue()`
- selection replay and presentation apply
- `NotifyReviewQueueStateChanged()` / `NotifyUxStateChanged()`

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop4b-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop4b-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop4b-full`

## Learned

- El primer corte sano de este bloque no era mover `ApplySessionProjection(...)` entero; primero convenia sacar la query/projection truth para dejar el ViewModel como shell y bajar riesgo.
- `ReviewSessionProjection` permite desacoplar la session DTO cruda del apply shell sin forzar todavia un refactor mas grande de notifiers y side effects.
- Despues de Loop 4B, el siguiente hotspot real ya no es query/projection sino el shell residual de **apply/notify orchestration**.
