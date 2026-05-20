# 2026-05-18 - Preview zoom max increased from 40x to 80x

## What
- Se volvió a subir el tope de zoom máximo del preview, ahora de `40x` a `80x`.
- Se actualizaron tests explícitos del contrato de zoom máximo.

## Why
- El usuario seguía necesitando más precisión visual al trabajar muy cerca sobre el preview.
- El límite anterior seguía quedando corto para marcar con comodidad en casos muy finos.

## Where
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`

## Verified
- Zoom-focused desktop tests: `7/7 PASS`
- Wider preview/geometry/interaction suite: `69/69 PASS`

## Learned
- El cálculo de wheel zoom estaba bien; solo hubo que subir otra vez el techo.
- Algunos tests de clamp necesitaban inputs más agresivos para llegar realmente al máximo nuevo.
