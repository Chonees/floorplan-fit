---
type: bug
status: fixed
date: 2026-05-05
loop: Loop 1
layer: Desktop
---

# Review window used a rigid size that overflowed smaller screens

## What

La review window estaba hardcodeada en `Width="1450"` y `Height="920"`. En pantallas m?s chicas o con scaling, la propia ventana se sal?a del viewport visible y parec?a que el contenedor del preview “desbordaba”.

## Why

Aunque el preview interno ya hab?a sido mejorado, la ventana madre segu?a imponiendo un tama?o fijo demasiado grande. Entonces el problema no era solo del canvas: tambi?n era del shell de la pantalla.

## Root cause

- `ReviewFloorPlanWindow.axaml` fijaba tama?o r?gido
- no abr?a maximizada
- el layout interior estaba obligado a vivir dentro de una ventana que pod?a no entrar en la pantalla

## Fix

- cambiar el tama?o por defecto a algo m?s razonable (`1280x800`)
- abrir `ReviewFloorPlanWindow` con `WindowState="Maximized"`
- dejar test de layout para evitar volver a `1450x920`

## Files

- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
