# 2026-05-16 - Saving dimension interval binding crashed after refresh cleared measurement selection

## What
- Se corrigió un `NullReferenceException` al apretar **Guardar qué mide**.
- El fix captura antes del refresh:
  - `corridorId`
  - `startNodeId`
  - `endNodeId`
  - `dimensionDisplayText`
- Después del refresh ya no vuelve a depender de `SelectedMeasurementCorridor` / `SelectedMeasurementStartNode` / `SelectedMeasurementEndNode` para reconstruir selección o mensaje.

## Why
- Después de `RefreshSessionAsync`, la UI puede limpiar selección de franja/puntos.
- Leer esos `Selected*` después del refresh era frágil y causaba el crash real visto en runtime.

## Where
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`

## Verified
- Focused desktop save-binding suite: `12/12 PASS`
- Wider desktop measurement binding suite: `60/60 PASS`

## Learned
- El patrón correcto para este ViewModel es: capturar ids antes del refresh y replayear con esos ids capturados.
- Los tests unitarios no siempre reproducen exactamente el timing de la UI, pero el stack trace + el código mostraban una raíz inequívoca.
