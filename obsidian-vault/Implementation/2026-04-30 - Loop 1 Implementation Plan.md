---
type: implementation
date: 2026-04-30
status: active
---

# Loop 1 Implementation Plan

## Scope

- extraction de wall candidates
- curated walls exactas
- curated spaces m?nimos
- constraints b?sicas por wall/space
- draft / publish / active curation
- review UI m?nima apoyada en el modelo correcto

## Why

Este es el primer cierre serio de Loop 1 y deja la base reusable que despu?s necesita Loop 2 para overlay, fit y propuestas.

## Execution Notes

- El plan ejecutable vive en `docs/superpowers/plans/2026-04-30-loop-1-curated-walls-and-spaces-implementation.md`
- La ejecuci?n debe respetar TDD estricto: test rojo -> c?digo m?nimo -> test verde
- Bajo la regla actual del repo `never build after changes`, la verificaci?n durante implementaci?n queda limitada a `dotnet test`
- `scripts/dev-desktop.bat` usa `dotnet watch run`, as? que queda diferido como path de runtime hasta tener autorizaci?n expl?cita para una pasada separada
- No correr `dotnet test` de distintos proyectos en paralelo dentro del mismo worktree porque se pisan `obj/bin` y aparece `CS2012`

## Ordered Milestones

1. dominio sem?ntico base y estados de Library **(completado)**
2. puertos Application + lifecycle handlers **(completado)**
3. schema SQLite + repositorios de curaci?n **(completado)**
4. extracci?n real de wall candidates **(pr?ximo milestone)**
5. review/metadata/publish de curated walls
6. curated spaces m?nimos + constraints por ambiente
7. Library states + review screen m?nima

## Progress Notes

- Milestone 1 consolidado: `FloorPlanCuration`, `ExtractedWallCandidate`, `CuratedWall`, `CuratedSpace`, constraints base y separaci?n entre `CurrentVersionId` y `ActivePublishedCurationId`.
- Milestone 2 consolidado: handlers de Application para `ExtractWallCandidates`, `StartOrResumeCuration`, `AcceptWallCandidate`, `RejectWallCandidate`, `UpdateCuratedWallMetadata` y `PublishFloorPlanCuration`.
- Milestone 3 consolidado: SQLite ya persiste `active_published_curation_id`, `wall_extraction_runs`, `extracted_wall_candidates`, `floorplan_curations`, `curated_walls` y las geometr?as detectadas en `geometry_paths` / `geometry_segments`.
- Verificaci?n actual:
  - sub-suite `FloorPlans.Curation`: **8/8**
  - proyecto `FloorplanFit.Application.Tests`: **13/13**
  - proyecto `FloorplanFit.Infrastructure.Tests`: **10/10**
- Tradeoff activo: `RejectWallCandidate` hoy remueve la `CuratedWall` derivada desde repositorio; si m?s adelante hace falta auditor?a fina dentro del draft, eso puede evolucionar a desactivaci?n blanda.

## Next Focus

- Implementar el extractor real de wall candidates sobre DXF.
- Empezar a derivar estados reales de Library usando las nuevas tablas de extracci?n y curado.
- Despu?s reci?n subir la review screen m?nima en Desktop.
