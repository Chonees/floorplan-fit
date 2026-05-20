---
type: Decision
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - preview
  - modularization
  - loop-2
---

# Loop 2B targets preview observer wiring via collection hub

## Decision

El siguiente slice de modularizacion del preview no va a tocar render ni `FloorPlanReviewViewModel`.

Va a atacar primero el **observer wiring / invalidation repetition** de `FloorPlanPreviewControl` mediante un helper dedicado: `PreviewCollectionObserverHub`.

## Why

Despues de Loop 2A, el hotspot verificado ya no es la state machine de interaccion sino la plomeria repetitiva de colecciones observables:

- handlers `OnXChanged(...)`
- `Attach...CollectionObserver(...)`
- `Detach...CollectionObserver(...)`
- `...CollectionChanged(...) => InvalidateVisual()`

Esa repeticion es mecanica, testeable y tiene una frontera arquitectonica limpia.

## Consequences

- `FloorPlanPreviewControl` sigue consolidandose como shell Avalonia
- el nuevo hub sera duenio de attach / replace / detach / invalidate bridge
- render decomposition queda para un slice posterior
- `FloorPlanReviewViewModel` queda explicitamente fuera de alcance por ahora
