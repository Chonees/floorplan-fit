---
type: Decision
date: 2026-05-12
project: floorplan-fit
status: current
tags:
  - architecture
  - desktop
  - review
  - modularization
  - loop-3
---

# Loop 3B starts with selection routing before selection presentation

## Decision

**Loop 3B** arranca por **selection routing + snapshot truth** y recien despues extrae **selection presentation side effects**.

## Why

Despues de cerrar Loop 3A, el hotspot real del `FloorPlanReviewViewModel` ya no es la mutacion sino la verdad de seleccion repartida entre `SelectPreviewPath(...)`, snapshot capture/replay y muchos `OnSelectedXChanged(...)`. Si atacamos primero la verdad de routing, el segundo corte de side effects queda mucho mas seguro y auditable.

## Tradeoffs

- **A favor**: menor blast radius, mejor progresion arquitectonica, separa verdad de seleccion de efectos de UI.
- **En contra**: requiere dos sub-slices dentro de Loop 3B y el ViewModel sigue parcialmente cargado hasta cerrar ambos.

## Next

Despues de Loop 3B, el siguiente paso recomendado es evaluar si hace falta un mini **Loop 3C** para queue/filter orchestration o si ya conviene pasar directo a **Loop 4**.
