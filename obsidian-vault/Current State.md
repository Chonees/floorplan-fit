---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-05-02
---

# Current State

## Project

Floorplan Fit es una herramienta desktop local-first para importar floor plans y site plans en DXF, curar walls, detectar el envelope construible y generar opciones determinísticas de encaje.

## Canonical Sources

- `MVP-UX.md`
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md`
- `AGENTS.md`

## Product Truth

- Runtime principal: **C# + .NET 10 LTS**
- UI: **Avalonia UI + MVVM**
- Persistencia de producto: **SQLite**
- Geometría: **NetTopologySuite**
- DXF: **IxMilia.Dxf** detrás de interfaces
- Estilo arquitectónico: **monolito modular local-first**

## Repository Truth

- El repo ya no está en estado "solo docs": existe el slice ejecutable de import + Library sobre `SANTA-BARBARA.dxf`.
- Loop 1 todavía no está cerrado, pero ya tiene implementado su núcleo semántico, lifecycle principal, read-model base de review y review UI mínima.
- Loop 2 todavía no tiene implementación real de envelope, fit engine ni proposals.
- Los DXF de `PLANS/originalFloorPlans/` son la fuente primaria de verdad para Loop 1.
- `PLANS/catalog/` queda como referencia legacy/comparativa, no como fuente canónica del dominio.

## Loop 1 Truth

- Orden aprobado para cerrar Loop 1: **extracción -> review/curation persistida -> publish de versión activa -> UI de review/curado**.
- Balance aprobado: **core semántico + canvas mínimo real**; no editor CAD rico todavía.
- Ajuste de alcance aprobado: Loop 1 debe cerrar con **walls exactas + curated spaces mínimos + constraints básicas por ambiente**.
- La **creación de paredes nuevas** pertenece primero a **Loop 2** como propuesta de adaptación, con promoción opcional de vuelta a Loop 1 si el usuario la quiere canonizar.

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
  - `GetFloorPlanReviewSession`
  - `OpenFloorPlanReviewSession`
- Infrastructure ya persiste en SQLite:
  - `active_published_curation_id` en `floorplan_templates`
  - `wall_extraction_runs`
  - `extracted_wall_candidates`
  - `floorplan_curations`
  - `curated_walls`
  - `geometry_paths` + `geometry_segments` para las geometrías de walls detectadas
- Infrastructure ya tiene:
  - `IxMiliaWallExtractor` real filtrando capas wall-like
  - `SqliteFloorPlanLibraryReader` derivando `Imported`, `Extracted`, `Curated Draft` y `Published`
  - `SqliteFloorPlanReviewSessionReader` para cargar template summary, candidates, curated walls y geometría
  - `SqliteFloorPlanExtractionSourceReader` para resolver la versión actual y el DXF gestionado que usa la extracción
- Desktop ya tiene review UI mínima:
  - `LibraryViewModel` con `SelectedItem`, `Extract Walls` y `Open Review`
  - import que auto-extrae walls después de guardar el DXF
  - `ReviewFloorPlanWindow` con lista de candidates, preview geométrico y panel inspector
  - `FloorPlanReviewViewModel` con acciones de `accept`, `reject`, `save metadata` y `publish`
- Decisión vigente: en draft, **rechazar** un candidate elimina desde repositorio la `CuratedWall` derivada de ese source candidate. Soft-delete/deactivation queda diferido.
- Verificación actual:
  - `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter FullyQualifiedName~FloorPlans.Curation` -> **8/8**
  - `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj` -> **16/16**
  - `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj` -> **17/17**
  - `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj` -> **4/4**

## Knowledge / Workflow Truth

- `floorplan-fit-teaching-mode` debe cargarse junto con `superpowers:using-superpowers` como guía base de trabajo en este repo.
- Preferencia activa del usuario: explicar qué se hace, por qué se hace y enseñar el razonamiento mientras implementamos.
- Preferencia activa del usuario: documentar pasos importantes en `docs/` y `obsidian-vault/`.
- Preferencia activa del usuario: trabajar inline en la branch actual; NO usar worktrees para este repo.
- Regla vigente del repo: **never build after changes**.
- Durante implementación, la verificación activa queda limitada a `dotnet test`.
- Revalidacion secuencial hecha el **2026-05-02**: Application **16/16**, Infrastructure **17/17**, Desktop **4/4**.
- `scripts/dev-desktop.bat` usa `dotnet watch run`, así que hoy NO cuenta como path válido de verificación durante coding.
- Gotcha operativo: no correr `dotnet test` en paralelo para distintos proyectos dentro del mismo repo; aparecen locks `CS2012` en `obj/bin`.
- Gotcha operativo: si quedó abierta una instancia de `FloorplanFit.Desktop`, puede bloquear `bin/Debug` y romper tests que compilan el proyecto Desktop.
- Drift confirmado: `docs/explicacion del proyecto/2026-04-29 - checklist manual del slice 1 ejecutable.md` ya quedo historico en el punto donde dice que la Library no hidrata desde SQLite al iniciar; el codigo actual si lo hace via `MainWindow_OnOpened -> LibraryViewModel.LoadAsync -> GetFloorPlanLibraryHandler -> SqliteFloorPlanLibraryReader`.

## Immediate Next Steps

1. Hacer una pasada manual de runtime de la review UI cuando el usuario quiera validar interacción visual real.
2. Bajar `CuratedSpaces` mínimos + constraints por ambiente para completar la parte espacial que todavía falta en Loop 1.
3. Mejorar la review UI con operaciones más ricas si hiciera falta (merge/split, filtros, zoom), pero eso ya queda después del cierre canónico.
4. Mantener la verificación secuencial con `dotnet test` mientras siga vigente la regla `never build after changes`.

## Relevant Notes

- [[Decisions/2026-04-30 - Loop 1 Completion Order]]
- [[Decisions/2026-04-30 - Loop 1 Uses Semantic Core Plus Minimal Review Canvas]]
- [[Decisions/2026-04-30 - Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2]]
- [[Implementation/2026-04-30 - Loop 1 Curated Walls and Spaces Design]]
- [[Implementation/2026-04-30 - Loop 1 Implementation Plan]]
- [[Bugs/2026-05-02 - Open Review crashes right after extraction because committed SQLite transaction is reused]]
- [[Implementation/2026-05-02 - Fixed first-open review transaction crash]]
