---
type: Decision
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - preview
  - render
  - modularization
  - loop-2
---

# Loop 2C targets preview render composition via scene and composer

## Decision

El siguiente slice del preview no va a tocar `FloorPlanReviewViewModel` todavia.

Va a atacar primero la **composicion de render** de `FloorPlanPreviewControl` mediante:

- una snapshot `PreviewRenderScene`
- un `PreviewRenderComposer`

## Why

Despues de Loop 2A y Loop 2B, el hotspot verificado que queda en el preview shell es `Render(...)`:

- orden de layers
- branch curated-vs-detected artifacts
- dibujo de geometria base
- labels, dimensions, pinch markers, handles

Esa secuencia es parte del comportamiento actual, pero no deberia seguir viviendo inline en el control.

## Consequences

- `FloorPlanPreviewControl` sigue consolidandose como shell Avalonia
- los helpers de preparacion de scene pueden quedarse en el control por ahora
- la composicion de layers pasa a un archivo dedicado
- Loop 3 queda explicitamente postergado hasta terminar este cleanup del preview
