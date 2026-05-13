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

# Loop 3A review command orchestration

## What changed

Se completo la extraccion de la ceremonia mutante de `FloorPlanReviewViewModel` a `FloorPlanReviewMutationCoordinator`, dejando al ViewModel como shell de estado/seleccion/refresh y sacandole el inline `CreateScope + GetRequiredService<Handler> + HandleAsync`.

## Why

Despues de limpiar preview en Loop 2, el hotspot principal del Desktop layer paso a ser `FloorPlanReviewViewModel`. El mejor primer corte no era selection state sino la orquestacion procedural repetida de mutaciones, porque tenia alto payoff arquitectonico con menor blast radius.

## Where

- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/CuratedArtifactFloorPlanReviewViewModelTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/DimensionEditingFloorPlanReviewViewModelTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- `docs/superpowers/specs/2026-05-12-loop-3a-review-command-orchestration-design.md`
- `docs/superpowers/plans/2026-05-12-loop-3a-review-command-orchestration-plan.md`

## Scope that now belongs to the coordinator

- reject de wall candidates
- publish de curation
- add pinch group
- add pinch marker
- save/restore artifact position
- save/restore label text height
- save/restore dimension overrides
- save/restore curated classification
- exclude curated artifacts
- remove room labels
- remove opening labels
- remove pinch markers
- export adjusted DXF

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests|FullyQualifiedName~MovableArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop3a-first-slice-final`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~FloorPlanReviewViewModelTests.RemoveSelectedOpeningLabelAsync|FullyQualifiedName~FloorPlanReviewViewModelTests.RemoveSelectedPinchAsync" --artifacts-path .\.artifacts-test\desktop-loop3a-curated-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests.RestoreSelectedArtifactPositionAsync_deletes_dimension_override_and_keeps_dimension_selected|FullyQualifiedName~FloorPlanReviewViewModelTests.RejectSelectedCandidateAsync_rejects_selected_accepted_candidate" --artifacts-path .\.artifacts-test\desktop-loop3a-final-mutations-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~CuratedArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~LabelTextHeightFloorPlanReviewViewModelTests|FullyQualifiedName~MovableArtifactFloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests|FullyQualifiedName~FloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop3a-focused-verification`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop3a-full-after-final-slice`

## Learned

- El boundary correcto para este loop era **mutation orchestration**, no selection state.
- El source-reading architecture test funciona bien como guardrail para verificar que `FloorPlanReviewViewModel` deja de resolver handlers inline.
- Cerrado Loop 3A, en el ViewModel solo quedan queries de `OpenFloorPlanReviewSessionHandler` y `GetFloorPlanReviewSessionHandler`; el siguiente corte sano pasa a **Loop 3B selection state**.
