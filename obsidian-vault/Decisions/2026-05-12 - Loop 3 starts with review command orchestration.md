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

# Loop 3 starts with review command orchestration

## Decision

El primer slice de **Loop 3** arranca por **review command orchestration**, no por selection state ni por queue filtering.

## Why

`FloorPlanReviewViewModel` tiene hoy el bloque procedural mas repetitivo del review shell: status messages, `CreateScope()`, `GetRequiredService<...Handler>()`, `HandleAsync(...)` y `RefreshSessionAsync(...)` repetidos en muchas mutaciones. Ese es el seam mas rentable para adelgazar primero.

## Tradeoffs

- **A favor**: mayor payoff arquitectonico, muy buen test surface existente, menos riesgo que tocar los partials de seleccion primero.
- **En contra**: deja selection state y queue state para slices posteriores.

## Next

Despues de Loop 3A, el siguiente corte recomendado pasa a ser **selection state**, con un ViewModel ya mas chico y menos procedural.
