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

# Loop 2 starts with surgical interaction extraction

## Decision

El primer slice de **Loop 2** va a ser **quirurgico y seguro**: sacar coordinacion de interaccion del `FloorPlanPreviewControl` antes de intentar una particion mas amplia de wiring, observers o render orchestration.

## Why

`FloorPlanPreviewControl` sigue siendo el hotspot principal, pero los renderers ya estan bastante mejor separados. El mayor valor ahora esta en aislar la state machine de pointer / zoom / pan / drag / dimension edit con el menor riesgo posible.

## Consequences

- Primero se extrae cerebro de interaccion, no mas renderers.
- El control principal deberia quedar como shell de composicion + puente de eventos.
- Observer wiring e invalidacion repetitiva pueden quedar para un slice posterior del mismo loop o para un follow-up si no agregan riesgo inmediato.

## Replaces

- Ninguna decision previa; esto refina la estrategia del Loop 2 dentro del programa de modularizacion aprobado.
