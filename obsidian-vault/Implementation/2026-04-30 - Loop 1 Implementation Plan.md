---
type: implementation
date: 2026-04-30
status: active
---

# Loop 1 Implementation Plan

## Scope

- extraction de wall candidates
- curated walls exactas
- curated spaces mínimos
- constraints básicas por wall/space
- draft / publish / active curation
- review UI mínima apoyada en el modelo correcto

## Why

Este es el primer cierre serio de Loop 1 y deja la base reusable que después necesita Loop 2 para overlay, fit y propuestas.

## Execution Notes

- El plan ejecutable vive en `docs/superpowers/plans/2026-04-30-loop-1-curated-walls-and-spaces-implementation.md`
- La ejecución debe respetar TDD estricto: test rojo -> código mínimo -> test verde
- Bajo la regla actual del repo `never build after changes`, la verificación durante implementación queda limitada a `dotnet test`
- `scripts/dev-desktop.bat` usa `dotnet watch run`, así que queda diferido como path de runtime hasta tener autorización explícita para una pasada separada
- No correr `dotnet test` de distintos proyectos en paralelo dentro del mismo repo porque se pisan `obj/bin` y aparece `CS2012`
- Si queda abierta una instancia de `FloorplanFit.Desktop`, puede bloquear `bin/Debug` y romper tests que compilan el proyecto Desktop

## Ordered Milestones

1. dominio semántico base y estados de Library **(completado)**
2. puertos Application + lifecycle handlers **(completado)**
3. schema SQLite + repositorios de curación **(completado)**
4. extracción real de wall candidates **(completado)**
5. estados reales de Library **(completado)**
6. read-model de review session + wiring base de Desktop **(completado)**
7. review screen mínima en Desktop **(completado)**
8. curated spaces mínimos + constraints por ambiente

## Progress Notes

- Milestone 1 consolidado: `FloorPlanCuration`, `ExtractedWallCandidate`, `CuratedWall`, `CuratedSpace`, constraints base y separación entre `CurrentVersionId` y `ActivePublishedCurationId`.
- Milestone 2 consolidado: handlers de Application para `ExtractWallCandidates`, `StartOrResumeCuration`, `AcceptWallCandidate`, `RejectWallCandidate`, `UpdateCuratedWallMetadata` y `PublishFloorPlanCuration`.
- Milestone 3 consolidado: SQLite ya persiste `active_published_curation_id`, `wall_extraction_runs`, `extracted_wall_candidates`, `floorplan_curations`, `curated_walls` y las geometrías detectadas en `geometry_paths` / `geometry_segments`.
- Milestone 4 consolidado: `IxMiliaWallExtractor` ya extrae wall candidates reales desde `SANTA-BARBARA.dxf` filtrando capas wall-like y descomponiendo `LWPOLYLINE`.
- Milestone 5 consolidado: `SqliteFloorPlanLibraryReader` ya deriva `Imported`, `Extracted`, `Curated Draft` y `Published`.
- Milestone 6 consolidado: ya existe el slice de review session con:
  - DTOs de geometry / wall candidates / curated walls
  - `IFloorPlanReviewSessionReader`
  - `SqliteFloorPlanReviewSessionReader`
  - `OpenFloorPlanReviewSessionHandler`
  - registro DI en Desktop para el flujo de review
- Milestone 7 consolidado: Desktop ya tiene:
  - botón `Extract Walls`
  - botón `Open Review`
  - auto-extracción después del import
  - `ReviewFloorPlanWindow` con preview geométrico mínimo
  - inspector para aceptar/rechazar candidates, editar metadata y publicar
- Verificación actual:
  - sub-suite `FloorPlans.Curation`: **8/8**
  - proyecto `FloorplanFit.Application.Tests`: **16/16**
  - proyecto `FloorplanFit.Infrastructure.Tests`: **16/16**
  - proyecto `FloorplanFit.Desktop.Tests`: **4/4**
- Tradeoff activo: la preview actual es un canvas mínimo de líneas; todavía no tiene zoom/pan ni edición geométrica compleja.

## Next Focus

- Validar el runtime manual de la review UI con imports reales cuando el usuario quiera esa pasada.
- Bajar `CuratedSpaces` mínimos + constraints por ambiente.
- Dejar para después cualquier operación más pesada de UX geométrica (merge/split, zoom, filtros, navegación avanzada).
