---

project: floorplan-fit

repo: https://github.com/Chonees/floorplan-fit

status: active

updated: 2026-05-03

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

- Loop 1 todav?a no est? cerrado, pero ya tiene implementado su n?cleo sem?ntico, lifecycle principal, read-model base de review y review UI m?nima.

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

  - `GetFloorPlanReviewSession`

  - `OpenFloorPlanReviewSession`

- Infrastructure ya persiste en SQLite:

  - `active_published_curation_id` en `floorplan_templates`

  - `wall_extraction_runs`

  - `extracted_wall_candidates`

  - `floorplan_curations`

  - `curated_walls`

  - `geometry_paths` + `geometry_segments` para las geometr?as de walls detectadas

- Infrastructure ya tiene:

  - `IxMiliaWallExtractor` real filtrando capas wall-like

  - `SqliteFloorPlanLibraryReader` derivando `Imported`, `Extracted`, `Curated Draft` y `Published`

  - `SqliteFloorPlanReviewSessionReader` para cargar template summary, candidates, curated walls y geometr?a

  - `SqliteFloorPlanExtractionSourceReader` para resolver la versi?n actual y el DXF gestionado que usa la extracci?n

- Desktop ya tiene review UI m?nima:

  - `LibraryViewModel` con `SelectedItem`, `Extract Walls` y `Open Review`

  - import que auto-extrae walls despu?s de guardar el DXF

  - `ReviewFloorPlanWindow` con lista de candidates, preview geom?trico centrado sobre fondo blanco, label de selecci?n activa, click-to-select sobre el plano y panel inspector con scroll real

  - `FloorPlanReviewViewModel` con acciones de `accept`, `reject`, `save metadata` y `publish`, m?s feedback expl?cito de qu? candidate o curated wall se est? previewing

  - `FloorPlanPreviewControl` recentra la geometr?a con padding consistente, repinta en tiempo real cuando cambia la selecci?n o la colecci?n observada y permite seleccionar paths por click directo sobre el preview

- Decisi?n vigente: en draft, **rechazar** un candidate elimina desde repositorio la `CuratedWall` derivada de ese source candidate. Soft-delete/deactivation queda diferido.

- Verificaci?n actual:

  - `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter FullyQualifiedName~FloorPlans.Curation` -> **8/8**

  - `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj` -> **16/16**

  - `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj` -> **17/17**

  - `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj` -> **12/12**



## Knowledge / Workflow Truth



- `floorplan-fit-teaching-mode` debe cargarse junto con `superpowers:using-superpowers` como gu?a base de trabajo en este repo.

- Preferencia activa del usuario: explicar qu? se hace, por qu? se hace y ense?ar el razonamiento mientras implementamos.

- Preferencia activa del usuario: documentar pasos importantes en `docs/` y `obsidian-vault/`.

- Preferencia activa del usuario: trabajar inline en la branch actual; NO usar worktrees para este repo.

- Ya no se versiona el estado visual local de Obsidian (`app.json`, `appearance.json`, `graph.json`, `workspace.json`); solo queda la configuraci?n compartida que realmente aporta al proyecto.
- Regla vigente del repo: **never build after changes**.

- Durante implementaci?n, la verificaci?n activa queda limitada a `dotnet test`.

- El mapa exhaustivo `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` fue revalidado el **2026-05-04** despu?s del cleanup documental; hoy cubre **209 archivos relevantes del working tree** (**202 versionados presentes + 7 nuevos del working tree**) y mantiene en cada entrada **misi?n + importancia + use case**.

- Revalidacion secuencial hecha el **2026-05-02**: Application **16/16**, Infrastructure **17/17**, Desktop **4/4**.

- Revalidaci?n puntual hecha el **2026-05-03** sobre la review UI Desktop: preview geometry + layout + selecci?n interactiva -> **12/12** tests Desktop.

- `scripts/dev-desktop.bat` usa `dotnet watch run`, as? que hoy NO cuenta como path v?lido de verificaci?n durante coding.

- Gotcha operativo: no correr `dotnet test` en paralelo para distintos proyectos dentro del mismo repo; aparecen locks `CS2012` en `obj/bin`.

- Gotcha operativo: si qued? abierta una instancia de `FloorplanFit.Desktop`, puede bloquear `bin/Debug` y romper tests que compilan el proyecto Desktop.

- Drift confirmado: `docs/explicacion del proyecto/2026-04-29 - checklist manual del slice 1 ejecutable.md` ya quedo historico en el punto donde dice que la Library no hidrata desde SQLite al iniciar; el codigo actual si lo hace via `MainWindow_OnOpened -> LibraryViewModel.LoadAsync -> GetFloorPlanLibraryHandler -> SqliteFloorPlanLibraryReader`.



## Immediate Next Steps



1. Hacer una pasada manual de runtime de la review UI cuando el usuario quiera validar interacci?n visual real.

2. Bajar `CuratedSpaces` m?nimos + constraints por ambiente para completar la parte espacial que todav?a falta en Loop 1.

3. Mejorar la review UI con operaciones m?s ricas si hiciera falta (merge/split, filtros, zoom), pero eso ya queda despu?s del cierre can?nico.

4. Mantener la verificaci?n secuencial con `dotnet test` mientras siga vigente la regla `never build after changes`.



## Relevant Notes



- [[Decisions/2026-04-30 - Loop 1 Completion Order]]

- [[Decisions/2026-04-30 - Loop 1 Uses Semantic Core Plus Minimal Review Canvas]]

- [[Decisions/2026-04-30 - Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2]]

- [[Implementation/2026-04-30 - Loop 1 Curated Walls and Spaces Design]]

- [[Implementation/2026-04-30 - Loop 1 Implementation Plan]]

- [[Implementation/2026-04-30 - Mapa completo de arquitectura y archivos del repo]]

- [[Bugs/2026-05-02 - Open Review crashes right after extraction because committed SQLite transaction is reused]]

- [[Implementation/2026-05-02 - Fixed first-open review transaction crash]]

- [[Implementation/2026-05-03 - Review preview layout and highlight fix]]
