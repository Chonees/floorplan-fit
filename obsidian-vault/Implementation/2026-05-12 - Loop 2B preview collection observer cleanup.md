---
type: Implementation
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - preview
  - observer
  - modularization
  - loop-2
---

# Loop 2B preview collection observer cleanup

## What changed

Se extrajo la plomeria de colecciones observables del preview a `PreviewCollectionObserverHub`, dejando a `FloorPlanPreviewControl` bastante mas cerca de un shell Avalonia real.

## Why

Despues de Loop 2A, el siguiente hotspot verificado ya no era la interaccion sino la repeticion de attach / detach / invalidate de colecciones observables. Este slice limpia esa responsabilidad sin mezclarla con render decomposition ni con `FloorPlanReviewViewModel`.

## Where

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `docs/superpowers/specs/2026-05-12-loop-2b-preview-collection-observer-cleanup-design.md`
- `docs/superpowers/plans/2026-05-12-loop-2b-preview-collection-observer-cleanup-plan.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewCollectionObserverHubTests" --artifacts-path .\.artifacts-test\desktop-loop2b-task2-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-loop2b-task3-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewCollectionObserverHubTests" --artifacts-path .\.artifacts-test\desktop-loop2b-task3-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewCollectionObserverHubTests|FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2b-final-focused`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop2b-full`

## Learned

- La frontera correcta no era un `partial` cosmetico sino un hub stateful chico que sabe de attach / replace / detach y nada mas.
- `FloorPlanPreviewControl` puede conservar los property-change handlers, pero ya no tiene por que ser duenio del bookkeeping de suscripciones.
- El control sigue siendo hotspot, pero bajo de **1747** a **1363** lineas al sacar la repeticion mecanica de observers.
