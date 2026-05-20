---
project: floorplan-fit
type: bug
date: 2026-05-04
status: fixed
tags:
  - desktop
  - sqlite
  - migration
  - runtime
related:
  - "[[Current State]]"
  - "[[Implementation/2026-05-04 - Pinch native cleanup and minimal review UI]]"
---

# Legacy pinch markers schema crashes Open Review

## Symptom

Al abrir review desde Desktop, la app explotaba con:

`SQLite Error 1: 'no such column: source_candidate_id'`

El stack trace ca?a en:

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `GetPinchMarkers(...)`
- `OpenFloorPlanReviewSessionHandler`

## Root cause

La branch ya hab?a migrado el modelo activo de pinch desde:

- `pinch_group_id`
- `curated_wall_id`

hacia el shape pinch-native:

- `source_candidate_id`
- `axis_tag`

Pero `SqliteSchemaInitializer` solo hac?a `CREATE TABLE IF NOT EXISTS pinch_markers (...)`.

Eso significa que un `workspace/app.db` viejo conservaba la tabla legacy intacta. La lectura nueva consultaba `source_candidate_id`, pero la base local segu?a teniendo `curated_wall_id`, y por eso reventaba runtime.

## Fix

Se agreg? auto-migraci?n en `SqliteSchemaInitializer`:

1. detecta si `pinch_markers` sigue en schema legacy
2. renombra la tabla vieja a `pinch_markers_legacy`
3. crea la tabla pinch-native nueva
4. migra los rows existentes usando:
   - `curated_walls.source_candidate_id`
   - `pinch_groups.axis_tag`
5. elimina la tabla temporal legacy

Tambi?n se agreg? un test de regresi?n:

- `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs`

## Verification

- se valid? la hip?tesis inspeccionando el `workspace/app.db` real y confirmando que `pinch_markers` segu?a con `pinch_group_id` + `curated_wall_id`
- se prob? el SQL de migraci?n sobre una copia de la base local y el `SELECT` nuevo pas? a funcionar con `source_candidate_id` + `axis_tag`
- despu?s de la correcci?n, el `workspace/app.db` qued? con el schema pinch-native esperado

## Learned

Cuando una branch cambia shape de tablas locales en SQLite, `CREATE TABLE IF NOT EXISTS` NO es migraci?n. Si el modelo activo cambia, hace falta reconciliar schema legacy o resetearlo expl?citamente.
