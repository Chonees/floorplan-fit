---
type: Implementation
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - preview
  - interaction
  - modularization
  - loop-2
---

# Loop 2A preview interaction extraction

## What changed

Se extrajo la coordinacion de interaccion del preview a `PreviewInteractionCoordinator` para que `FloorPlanPreviewControl` deje de mezclar pointer-state transitions con el shell Avalonia y el render.

## Why

El hotspot de Desktop ya no estaba en los renderers sino en la state machine de interaccion. Para un refactor quirurgico y seguro, primero habia que sacar `pointer press / move / release / wheel`, dejando wiring de observers y render decomposition para slices posteriores.

## Where

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `docs/superpowers/specs/2026-05-12-loop-2a-preview-interaction-extraction-design.md`
- `docs/superpowers/plans/2026-05-12-loop-2a-preview-interaction-extraction-plan.md`

## Verification

- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests" --artifacts-path .\.artifacts-test\desktop-loop2a-task3-green`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PreviewInteractionCoordinatorTests|FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-loop2a-final-focused`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\.artifacts-test\desktop-loop2a-full`

## Learned

- `FloorPlanPreviewControl` ya puede actuar mas como shell: captura/aplica `InteractionState`, libera pointer capture y publica eventos, mientras que el coordinator decide transiciones y outcomes.
- El comportamiento viejo del edge drag importaba: move/release debian seguir invalidando y limpiando estado sin marcar el evento como handled.
- Loop 2A no cierra la modularizacion del preview; todavia quedan observer wiring/invalidation repetition y render composition cleanup para un Loop 2B.
