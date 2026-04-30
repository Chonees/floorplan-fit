---
type: decision
date: 2026-04-25
status: accepted
---

# Slice 1 Import Foundation Architecture

## Decision

El Slice 1 se implementa como una base de importación canónica para Loop 1, con un flujo mínimo pero completo:

`DXF real -> lectura -> contexto de medición -> documento importado -> floorplan template -> floorplan version -> Library mínima`

## Architecture

- `Contracts`: `ImportFloorPlanRequest`, `ImportFloorPlanResponse`, `FloorPlanLibraryItemDto`
- `Domain`: `MeasurementContext`, `ImportedDocument`, `FloorPlanTemplate`, `FloorPlanVersion`
- `Application`: caso de uso `ImportFloorPlan`
- `Infrastructure`: `IDxfGateway` + adaptador concreto DXF + repositorios SQLite + filesystem helpers
- `Desktop`: Library mínima con import y listado de floor plans

## Why

- El primer problema real no es todavía extraer walls sino institucionalizar el floor plan como activo reusable del sistema
- Esto fija una frontera de verdad estable antes de agregar extracción, review, curado y publicación
- Permite que el futuro Loop 1 crezca sin mezclar onboarding documental con interpretación geométrica demasiado temprano

## Consequences

- La UI inicial queda deliberadamente mínima
- La persistencia arranca desde el día 1
- La extracción de walls queda como siguiente slice encima de esta base
