---
type: bug
status: fixed
date: 2026-05-05
loop: Loop 1
layer: Desktop
---

# Preview canvas overflowed its parent container vertically

## What

El canvas que dibuja el floor plan dentro de `Preview` pod?a verse m?s alto que el contenedor principal del panel `Preview`, dando la sensaci?n de que el rect?ngulo del floor plan se sal?a de la app.

## Why

`FloorPlanPreviewControl` es un control custom que dibuja su propio rect?ngulo de canvas. El panel `Preview` no ten?a clipping expl?cito, por lo que el render pod?a aparecer fuera del ?rea visual esperada.

## Root cause

- `ReviewFloorPlanWindow.axaml` no aplicaba `ClipToBounds` al `Border` del panel Preview
- el `Grid` interno tampoco clippeaba su contenido
- `FloorPlanPreviewControl` no declaraba clipping ni stretch expl?cito en la celda `*`

## Fix

- agregar `ClipToBounds="True"` al `Border` del panel Preview
- agregar `ClipToBounds="True"` al `Grid` interno del Preview
- agregar `ClipToBounds="True"`, `HorizontalAlignment="Stretch"` y `VerticalAlignment="Stretch"` al `FloorPlanPreviewControl`
- cubrirlo con test de layout

## Files

- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
