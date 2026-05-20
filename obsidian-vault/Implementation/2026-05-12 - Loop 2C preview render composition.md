---
type: Implementation
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - preview
  - render
  - modularization
  - loop-2
---

# Loop 2C preview render composition

## What changed

Se extrajo la composicion de layers del preview a `PreviewRenderComposer`, usando una snapshot `PreviewRenderScene` para que `FloorPlanPreviewControl` deje de mezclar shell Avalonia con layer ordering y branch render logic.

## Why

Despues de Loop 2A y Loop 2B, el hotspot real que quedaba en el preview shell era `Render(...)`. Este slice limpia el orden de layers sin reescribir todavia los helpers de scene preparation.

## Where

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `docs/superpowers/specs/2026-05-12-loop-2c-preview-render-composition-design.md`
- `docs/superpowers/plans/2026-05-12-loop-2c-preview-render-composition-plan.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewRenderComposerTests" --artifacts-path .\.artifacts-test\desktop-loop2c-task1-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewRenderComposerTests" --artifacts-path .\.artifacts-test\desktop-loop2c-task2-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-loop2c-task3-red`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewRenderComposerTests" --artifacts-path .\.artifacts-test\desktop-loop2c-task3-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewRenderComposerTests|FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewInteractionCoordinatorTests|FullyQualifiedName~PreviewCollectionObserverHubTests" --artifacts-path .\.artifacts-test\desktop-loop2c-final-focused`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop2c-full`

## Learned

- La frontera correcta para este slice es composer + scene snapshot, no una reescritura completa de render helpers.
- Los helpers de scene preparation pueden quedarse en el control por ahora sin contaminar el ownership del layer order.
- `FloorPlanPreviewControl` bajo de **1363** a **1307** lineas al sacar `Render(...)` como hotspot de orquestacion.
