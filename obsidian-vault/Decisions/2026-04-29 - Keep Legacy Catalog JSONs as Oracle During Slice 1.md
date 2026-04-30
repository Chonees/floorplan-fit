---
type: decision
date: 2026-04-29
status: accepted
---

# Keep Legacy Catalog JSONs as Oracle During Slice 1

## Decision

Durante el cierre del **Slice 1 ejecutable**, los archivos de `PLANS/catalog/*.json` se conservan como **or?culo legacy/comparativo** y **no** se eliminan todav?a.

La fuente primaria de verdad sigue siendo el DXF original bajo `PLANS/originalFloorPlans/`.

## Why

- `TECH-STACK-ARCHITECTURE-DATAFLOW.md` define que la verdad can?nica del producto termina en entidades persistidas y curaciones en DB, no en JSON intermedio.
- El primer slice ejecutable necesita validar importaci?n real desde DXF, hash, unidades y persistencia; meter una regeneraci?n completa de JSON como prerequisito mezcla scope y retrasa el objetivo principal.
- Los JSON actuales todav?a sirven como baseline de comparaci?n para datos estables como unidad, bounding box y metadatos de origen.
- Eliminarlos ahora nos quitar?a un punto de contraste ?til justo cuando el pipeline real todav?a no est? estabilizado.

## Operational Rule

- Para el Slice 1 ejecutable, leer siempre desde DXF original.
- Mantener `PLANS/catalog/*.json` solo como referencia de comparaci?n o fixture legacy.
- Si generamos artefactos propios desde DXF, deben ser expl?citamente secundarios (por ejemplo snapshots de test o golden files), no la verdad can?nica del dominio.
- Reci?n cuando el pipeline propio est? estable y cubierto por tests, evaluar mover esos JSON a una carpeta `legacy` o eliminarlos.

## Consequences

- El pr?ximo foco sigue siendo: `DXF real -> metadata/unidades -> hash -> persistencia real -> library entry`.
- La regeneraci?n propia de artefactos derivados puede venir despu?s como capa de validaci?n, no como bloqueo del Slice 1.
