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

# Loop 4A inspector presentation cleanup

## What changed

Se extrajo la presentacion derivada del inspector desde `FloorPlanReviewViewModel` a `FloorPlanReviewInspectorCoordinator`.

## Why

Despues de cerrar Loop 3 completo, el siguiente cluster residual del Desktop shell ya no era queue ni selection state sino:

- `SelectedArtifactTypeLabel`
- `SelectedArtifactTitle`
- `SelectedArtifactSubtitle`
- `SelectedArtifactDetails`
- summaries de curated/dimension/text height
- `InteractionHint`
- `NormalizeSelectedInspectorTool()`

Eso seguia mezclando reglas de presentacion con el shell observable del ViewModel.

## Where

- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewInspectorCoordinator.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelArchitectureTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/DimensionEditingFloorPlanReviewViewModelTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewSemanticPaletteTests.cs`

## Scope that now belongs to the coordinator

- normalized inspector tool fallback
- selected artifact title/subtitle/details
- curated detected/resolved/decision summaries
- curated selected color fallback
- selected artifact position summary
- selected label text height summary
- interaction hint text
- dimension association text/debug formatting

## What stayed in the ViewModel

- observable properties
- collections and selected entities
- `NotifyUxStateChanged()`
- final `OnPropertyChanged(...)` fan-out
- access to current dimension association dictionary

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop4a-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelArchitectureTests|FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~DimensionEditingFloorPlanReviewViewModelTests" --artifacts-path .\.artifacts-test\desktop-loop4a-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop4a-full`

## Learned

- El primer slice sano de Loop 4 no era tocar refresh/session apply todavía; primero convenia sacar la presentation policy del inspector porque estaba totalmente acoplada al shell.
- El guardrail de `PreviewSemanticPalette.TransparentArgb` tuvo que moverse del ViewModel al nuevo inspector coordinator; el test falló correctamente y mostró que el ownership había cambiado.
- Despues de Loop 4A, el siguiente corte sano ya no es texto/presentation sino el bloque de **session apply / refresh shell**.
