---
type: decision
date: 2026-04-25
status: accepted
---

# Initial Implementation Order

## Decision

La implementación debe arrancar por la **fundación de solución + Loop 1 de curado canónico de floor plans**.

## Why

- El MVP tiene dos loops, pero el segundo depende del primero
- El flujo técnico define que el motor consume `FloorPlanCuration`, `CuratedWalls` y `StructuredConstraints`
- El repositorio todavía no tiene estructura de solución real, así que primero hace falta bajar la arquitectura a proyectos concretos

## First Slice

1. Scaffold de solución `.NET`
2. Contratos e interfaces clave
3. Pipeline mínimo de importación DXF y normalización de unidades
4. Extracción de wall candidates
5. Curado/publicación persistida en SQLite

## Consequences

- Se posterga el fit engine hasta tener verdad canónica persistida
- Loop 2 se implementa sobre una base reutilizable y testeable
- La primera demo valiosa no es “ajustar un lote”, sino “curar una tipología y reutilizarla correctamente”
