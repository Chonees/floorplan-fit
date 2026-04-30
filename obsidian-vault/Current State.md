---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-04-30
---

# Current State

## Project

Floorplan Fit es una herramienta desktop local-first para importar floor plans y site plans en DXF, curar walls, detectar el envelope construible y generar opciones determin?sticas de encaje.

## Canonical Sources

- `MVP-UX.md`
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md`
- `AGENTS.md`

## Product Truth

- Runtime principal: **C# + .NET 10 LTS**
- UI: **Avalonia UI + MVVM**
- Persistencia de producto: **SQLite**
- Geometr?a: **NetTopologySuite**
- DXF: **IxMilia.Dxf** detr?s de interfaces
- Estilo arquitect?nico: **monolito modular local-first**

## Repository Truth

- El repo ya no est? en estado "solo docs": existe el slice ejecutable de import + Library sobre `SANTA-BARBARA.dxf`.
- Loop 1 todav?a no est? cerrado, pero ya tiene implementado su n?cleo sem?ntico y la mayor parte del lifecycle de Application.
- Loop 2 todav?a no tiene implementaci?n real de envelope, fit engine ni proposals.
- Los DXF de `PLANS/originalFloorPlans/` son la fuente primaria de verdad para Loop 1.
- `PLANS/catalog/` queda como referencia legacy/comparativa, no como fuente can?nica del dominio.

## Loop 1 Truth

- Orden aprobado para cerrar Loop 1: **extracci?n -> review/curation persistida -> publish de versi?n activa -> UI de review/curado**.
- Balance aprobado: **core sem?ntico + canvas m?nimo real**; no editor CAD rico todav?a.
- Ajuste de alcance aprobado: Loop 1 debe cerrar con **walls exactas + curated spaces m?nimos + constraints b?sicas por ambiente**.
- La **creaci?n de paredes nuevas** pertenece primero a **Loop 2** como propuesta de adaptaci?n, con promoci?n opcional de vuelta a Loop 1 si el usuario la quiere canonizar.

## Implementation Truth

- `FloorPlanTemplate` ya separa `CurrentVersionId` de `ActivePublishedCurationId`.
- Domain ya tiene `FloorPlanCuration`, `ExtractedWallCandidate`, `CuratedWall`, `CuratedSpace`, groups/joins y constraints base.
- Application ya tiene handlers para:
  - `ExtractWallCandidates`
  - `StartOrResumeCuration`
  - `AcceptWallCandidate`
  - `RejectWallCandidate`
  - `UpdateCuratedWallMetadata`
  - `PublishFloorPlanCuration`
- Infrastructure ya persiste en SQLite:
  - `active_published_curation_id` en `floorplan_templates`
  - `wall_extraction_runs`
  - `extracted_wall_candidates`
  - `floorplan_curations`
  - `curated_walls`
  - `geometry_paths` + `geometry_segments` para las geometr?as de walls detectadas
- Decisi?n vigente: en draft, **rechazar** un candidate elimina desde repositorio la `CuratedWall` derivada de ese source candidate. Soft-delete/deactivation queda diferido.
- Verificaci?n actual:
  - `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter FullyQualifiedName~FloorPlans.Curation` -> **8/8**
  - `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj` -> **13/13**
  - `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj` -> **10/10**

## Knowledge / Workflow Truth

- `floorplan-fit-teaching-mode` debe cargarse junto con `superpowers:using-superpowers` como gu?a base de trabajo en este repo.
- Preferencia activa del usuario: explicar qu? se hace, por qu? se hace y ense?ar el razonamiento mientras implementamos.
- Preferencia activa del usuario: documentar pasos importantes en `docs/` y `obsidian-vault/`.
- Regla vigente del repo: **never build after changes**.
- Durante implementaci?n, la verificaci?n activa queda limitada a `dotnet test`.
- `scripts/dev-desktop.bat` usa `dotnet watch run`, as? que hoy NO cuenta como path v?lido de verificaci?n durante coding.
- Gotcha operativo: en worktrees hay que editar apuntando expl?citamente al path del worktree o el cambio cae en el workspace principal.
- Gotcha operativo: no correr `dotnet test` en paralelo para distintos proyectos dentro del mismo worktree; aparecen locks `CS2012` en `obj/bin`.

## Immediate Next Steps

1. Implementar el extractor real de wall candidates sobre DXF en Infrastructure.
2. Empezar a derivar estados reales de Library (`Imported`, `Extracted`, `Curated Draft`, `Published`) apoy?ndonos en las nuevas tablas.
3. Reci?n despu?s atacar la review screen m?nima en Desktop.
4. Mantener la verificaci?n secuencial con `dotnet test` mientras siga vigente la regla `never build after changes`.

## Relevant Notes

- [[Decisions/2026-04-30 - Loop 1 Completion Order]]
- [[Decisions/2026-04-30 - Loop 1 Uses Semantic Core Plus Minimal Review Canvas]]
- [[Decisions/2026-04-30 - Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2]]
- [[Implementation/2026-04-30 - Loop 1 Curated Walls and Spaces Design]]
- [[Implementation/2026-04-30 - Loop 1 Implementation Plan]]
