# Review Preview Layout and Selection Design


> [!NOTE] HISTORICAL / UI-SUPERSEDED ? 2026-05-09
> This design documents a real layout/selection fix, but references to curated-wall side lists belong to the old review model.
>
> Current preview direction is CAD-faithful: `FloorPlanPreviewControl` acts as an interaction shell and rendering is split into focused preview layers for room labels, openings, fixed components, protected details, pinch markers, workspace background, and compression handles.


## Goal

Hacer que la review UI de Loop 1 permita identificar visualmente, sin ambigüedad, qué línea se está curando y que las listas laterales escalen con scroll real en vez de alturas rígidas.

## Problem

La review actual tiene tres problemas visibles:

1. el preview no aprovecha bien el espacio central y el canvas blanco queda “flotando” sobre fondo negro
2. el highlight de la línea seleccionada no se percibe como feedback inmediato y confiable
3. las listas laterales usan composición rígida (`StackPanel` + alturas fijas), lo que limita la cantidad visible y produce scroll poco prolijo

## Root Cause

### Preview

`FloorPlanPreviewControl` dibuja correctamente, pero la ventana lo aloja dentro de una composición que no estira el canvas al espacio disponible de forma limpia.

### Highlight

El `FloorPlanReviewViewModel` sí actualiza `HighlightGeometryPathId`, pero el control visual no declara explícitamente que cambios en sus propiedades visuales deban invalidar el render. Además el estilo del highlight es demasiado débil para servir como guía UX.

### Lists / Scroll

`ReviewFloorPlanWindow.axaml` combina `ScrollViewer`, `StackPanel` y `ListBox` con alturas rígidas, especialmente en el inspector. Eso hace que la UI quede guiada por pixels hardcodeados en vez de por contenido + espacio disponible.

## Chosen Approach

### 1. Preview layout

Recomponer la ventana de review usando `Grid` en las tres columnas principales y dentro del inspector. El preview central debe ocupar todo el espacio restante, con canvas blanco completo y centrado estable.

### 2. Preview rendering

Extraer la lógica de proyección a un helper puro testeable y hacer que el control invalide visual al cambiar:

- `GeometryPaths`
- `HighlightGeometryPathId`

### 3. Selection feedback

El path seleccionado debe renderizarse con:

- color rojo fuerte
- grosor claramente superior al resto

El resto del plano queda en gris suave.

### 4. Preview interaction

El preview debe soportar click-to-select mediante hit-testing sobre segmentos proyectados. Al tocar un path:

- si existe una curated wall asociada, se prioriza esa selecci?n
- si no existe, se selecciona el candidate correspondiente
- la label visible debe dejar claro qu? entidad qued? activa

### 5. Scroll behavior

Las listas deben crecer hasta el espacio asignado por layout y luego scrollear dentro de su región. Se eliminan alturas rígidas donde traban el contenido.

## Acceptance Criteria

1. El preview queda centrado dentro de su canvas blanco al abrir la review.
2. Al cambiar de candidate o curated wall, el highlight cambia visualmente en tiempo real.
3. La línea seleccionada es obvia para el usuario sin necesidad de adivinar.
4. La lista de candidates y la de curated walls muestran más elementos cuando hay espacio y hacen scroll correctamente cuando no lo hay.

## Files Expected

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- `tests/FloorplanFit.Desktop.Tests/...`
