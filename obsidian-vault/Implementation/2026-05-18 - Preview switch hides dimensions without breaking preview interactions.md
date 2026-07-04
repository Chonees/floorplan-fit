# Preview dimensions visibility switch

## What
Agregamos un switch visual en el panel Preview para mostrar u ocultar todas las cotas del canvas.

## Why
El usuario necesitaba despejar visualmente el preview para trabajar con más precisión sobre geometría, nodos y pinches sin el ruido de las dimensions.

## Where
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/App.axaml`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewRenderComposerTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`

## Learned
Ocultar solo el render no alcanza: si las cotas quedan invisibles pero siguen respondiendo al hit-test, la UX queda rota. Por eso el switch también apaga la interacción de dimensiones en el preview.
