# 2026-05-17 - Preview zoom max increased from 6x to 40x

## What
- Se subió el tope de zoom máximo del preview de `6x` a `40x`.
- Se agregaron tests explícitos para fijar ese contrato.

## Why
- El usuario necesitaba más precisión visual para ubicar puntos y trabajar mejor en el preview.
- El límite anterior estaba hardcodeado y frenaba el flujo de curado fino.

## Where
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`

## Verified
- Zoom-focused desktop tests: `7/7 PASS`
- Wider preview/geometry/interaction suite: `69/69 PASS`

## Learned
- El cuello real era un literal simple (`MaximumUserZoomFactor = 6d`), no un problema de viewport math.
- Conviene fijar el máximo con tests explícitos para que no vuelva a bajar sin querer.

