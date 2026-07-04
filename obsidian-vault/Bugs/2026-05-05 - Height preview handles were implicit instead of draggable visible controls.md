---
type: bug
status: fixed
date: 2026-05-05
loop: Loop 1
layer: Desktop
---

# Height preview handles were implicit instead of draggable visible controls

## What

El preview de compresi?n usaba hotzones impl?citas en los bordes del canvas para iniciar drag, mientras que los handles verdes visibles eran solo decorativos. Eso hac?a que el preview de `Height` se sintiera roto o inconsistente respecto de `Width`.

## Why

La UX promet?a "tirar desde el handle visible", pero el hit-test real no estaba atado a esos handles. En `Height` se notaba m?s porque top/bottom era menos descubrible que left/right.

## Root cause

- `FloorPlanPreviewControl.ResolveEdgeDrag(...)` resolv?a drag por proximidad al borde completo
- `RenderCompressionHandles(...)` dibujaba handles aparte, sin compartir geometr?a con el hit-test
- no hab?a tests que exigieran dos handles visibles para `Height` ni drag real desde el handle bottom

## Fix

- se movi? la geometr?a can?nica de handles a `FloorPlanPreviewGeometry`
- el render y el hit-test ahora usan los mismos rect?ngulos visibles
- se agregaron tests para:
  - dos handles visibles de `Height`
  - resoluci?n del bottom handle
  - compresi?n vertical top/bottom

## Files

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`
