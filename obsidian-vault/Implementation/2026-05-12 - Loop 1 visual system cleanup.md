---
type: Implementation
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - visual-system
  - modularization
  - loop-1
---

# Loop 1 visual system cleanup

## What changed

Se cerro el primer slice tecnico de limpieza visual: el shell Desktop y el preview ahora tienen ownership mas claro sobre sus tokens visuales.

## Why

Antes de partir `FloorPlanPreviewControl` y `FloorPlanReviewViewModel`, hacia falta sacar decisiones visuales escondidas en hex inline y brushes locales para que la modularizacion siguiente no arrastre ruido cosmetico mezclado con comportamiento.

## Where

- `src/FloorplanFit.Desktop/App.axaml`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewSemanticPalette.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewWorkspaceRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/CompressionHandlePreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/AppXamlInitializationTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewSemanticPaletteTests.cs`
- `docs/superpowers/plans/2026-05-12-loop-1-visual-system-cleanup.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AppXamlInitializationTests" --artifacts-path .\.artifacts-test\desktop-loop1-xaml-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewSemanticPaletteTests" --artifacts-path .\.artifacts-test\desktop-loop1-palette-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AppXamlInitializationTests|FullyQualifiedName~PreviewSemanticPaletteTests" --artifacts-path .\.artifacts-test\desktop-loop1-final-focused`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop1-full`

## Learned

- `App.axaml` debe ser due?o del chrome compartido (buttons, chips, inputs, borders), mientras que `PreviewSemanticPalette` debe ser due?o exclusivo del workspace/grid/handle/default preview surface.
- Para refactors arquitectonicos de estilo, los tests que leen source sirven para fijar ownership boundaries, no solo runtime behavior.
- El warning `CS8625` de `OpenFloorPlanReviewSessionHandler.cs` sigue apareciendo en los test runs, pero no bloquea este slice; no pertenece al alcance de Loop 1.
