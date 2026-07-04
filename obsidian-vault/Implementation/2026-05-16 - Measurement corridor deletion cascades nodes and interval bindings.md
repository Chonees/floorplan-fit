# 2026-05-16 - Measurement corridor deletion cascades nodes and interval bindings

## What
- Se implementó `Eliminar franja` en Review.
- Borrar una franja ahora elimina también:
  - sus puntos de medida (`measurement_nodes`)
  - sus relaciones manuales de cota (`dimension_interval_bindings`)

## Why
- La UX permitía crear franjas, pero no limpiarlas si quedaban mal.
- Como SQLite no tenía foreign keys ni `ON DELETE CASCADE`, dejar el delete parcial iba a romper el modelo con estado huérfano.

## Where
- `src/FloorplanFit.Application/FloorPlans/Curation/RemoveMeasurementCorridorHandler.cs`
- `src/FloorplanFit.Application/Abstractions/IMeasurementCorridorRepository.cs`
- `src/FloorplanFit.Application/Abstractions/IMeasurementNodeRepository.cs`
- `src/FloorplanFit.Application/Abstractions/IDimensionIntervalBindingRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementCorridorRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementNodeRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteDimensionIntervalBindingRepository.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewMutationCoordinator.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`

## Verified
- Application handlers: `2/2 PASS`
- Infrastructure persistence/review: `3/3 PASS`
- Desktop measurement binding suite: `59/59 PASS`

## Learned
- En este repo el delete correcto de franjas tenía que orquestarse en Application porque la persistencia no garantiza cascade.
- UX mínima usable: botón `Eliminar franja` deshabilitado si no hay franja seleccionada, y al borrar hay que limpiar la selección local de corridor/nodes.
