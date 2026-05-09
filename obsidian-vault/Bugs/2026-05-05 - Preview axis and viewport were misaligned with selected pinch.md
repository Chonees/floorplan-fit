---
type: bug
status: fixed
date: 2026-05-05
loop: Loop 1
layer: Desktop
---

# Preview axis and viewport were misaligned with selected pinch

## What

El preview pod?a mostrar un pinch `Height` seleccionado pero seguir usando el eje `Width` para handles, hint y drag. Adem?s, la geometr?a se renderizaba sobre el rect completo del control en vez de reservar el ?rea ?til entre handles.

## Why

Eso hac?a que:

- los handles visibles no coincidieran con la intenci?n del pinch seleccionado
- el hint textual dijera left/right cuando en realidad el usuario esperaba top/bottom
- el plano quedara mal contenido visualmente respecto del ?rea de agarre

## Root cause

- `OnSelectedPinchMarkerChanged(...)` no sincronizaba `SelectedPinchAxis` con `PinchMarkerDto.AxisTag`
- `FloorPlanPreviewControl` calculaba viewport sobre `Bounds` completos
- `PreviewViewport.Project(...)` asum?a un rect origen `(0,0)`, as? que no serv?a para un sub-rect interno del canvas

## Fix

- sincronizar el eje activo con el `AxisTag` del pinch seleccionado
- introducir `GetGeometryViewportBounds(...)` para reservar el ?rea interna real de render entre handles
- corregir `PreviewViewport.Project(...)` para respetar `Bounds.Left/Top/Bottom`
- usar ese rect interno tanto para render como para hit-test

## Files

- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`
