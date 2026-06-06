# Fit franja axis can be changed after creation

## What changed
Existing measurement corridors/franjas can now be changed between `Width` and `Height` from the Fit right panel.

## Why
The operator can accidentally create many A/B nodes as width when the measured relation is actually height. Recreating every franja manually is too expensive and error-prone.

## Implementation
- Added `ChangeMeasurementCorridorAxisHandler` in Application.
- Added `UpdateAsync` to measurement corridor and measurement node repositories, including SQLite implementations.
- Changing a franja axis updates:
  - the corridor `AxisTag`
  - corridor band min/max coordinates from the selected guide geometry
  - each node `AxisCoordinate` for the new axis
  - each affected `DimensionIntervalBinding` interval start/end coordinate
- Desktop Fit panel now shows `Tipo de franja` with a Width/Height selector and `Cambiar tipo` button for the selected franja.

## Verification
- RED confirmed missing handler/ViewModel API before implementation.
- Application focused tests passed: `DimensionIntervalBindingHandlersTests` 3/3.
- Infrastructure focused tests passed: `MeasurementIntervalBindingPersistenceIntegrationTests` 3/3.
- Desktop focused tests passed: 70/70 across layout, service registration, ViewModel, preview binding, XAML init slices.
