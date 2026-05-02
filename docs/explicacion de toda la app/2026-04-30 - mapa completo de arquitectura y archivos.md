---
type: architecture-map
date: 2026-04-30
status: active
scope: tracked-files-plus-new-docs
file_count: 203
---

# Mapa completo de arquitectura y archivos de Floorplan Fit

## Propósito del documento

Este documento es el **mapa exhaustivo del repositorio**. Su misión es explicar **qué parte de la arquitectura toca cada archivo**, **qué responsabilidad tiene** y **cómo encaja en el objetivo del producto**.

La cobertura fue verificada contra `git ls-files` el **2026-04-30** y luego se amplió con **2 documentos nuevos creados en esta misma pasada**. El total cubierto es **203 archivos**: **201 trackeados por git + 2 docs nuevos**. Esto excluye a propósito `bin/`, `obj/`, caches locales, procesos temporales y directorios no versionados, porque **no son fuente canónica de la app**.

## Big Picture

Floorplan Fit es un monolito modular local-first en .NET 10 con Avalonia, SQLite e IxMilia DXF. Hoy la parte más desarrollada del producto es **Loop 1**: importar un floor plan, extraer walls, revisarlas, curarlas y publicar una versión reusable. Loop 2 (adaptación contra site plan, fit engine y propuestas) sigue mayormente como diseño.

## Mapa de arquitectura

- **Raíz del repo**: gobierno técnico, solución, fuentes canónicas y configuración global.
- **PLANS/**: fixtures DXF y referencias legacy usadas como verdad de entrada o comparación.
- **docs/**: documentación narrativa, specs y planes ejecutables.
- **obsidian-vault/**: conocimiento duradero del proyecto para estado, decisiones, bugs e implementación.
- **skills/**: skill local que obliga a explicar el repo con foco en loops y capas.
- **src/FloorplanFit.Domain/**: reglas puras del negocio.
- **src/FloorplanFit.Application/**: puertos y casos de uso.
- **src/FloorplanFit.Contracts/**: DTOs de intercambio para UI/read-models.
- **src/FloorplanFit.Infrastructure/**: SQLite, DXF, storage, hashing y runtime local.
- **src/FloorplanFit.Desktop/**: app Avalonia MVVM que orquesta la experiencia del usuario.
- **tests/**: evidencia automatizada por capa.

## Convención de lectura

- **Archivo**: path exacto trackeado por git.
- **Misión**: por qué existe y qué responsabilidad sostiene.
- **Capa/Área**: se deduce por la sección donde aparece el archivo.

## Raíz del repositorio y gobierno técnico

Estos archivos definen la identidad del repositorio y la verdad global que todas las capas deben respetar.

- `.gitignore` — Define qué archivos locales, temporales o generados no deben entrar al control de versiones.
- `AGENTS.md` — Fija las reglas operativas del agente en este repo: tono, verificación, skills obligatorias, TDD estricto y protocolos de documentación/memoria.
- `Directory.Build.props` — Centraliza propiedades compartidas de compilación/target para todos los proyectos .NET de la solución.
- `FloorplanFit.sln` — Agrupa todos los proyectos de la app, infraestructura, dominio, contratos y tests en una sola solución.
- `LICENSE` — Declara la licencia legal del repositorio.
- `MVP-UX.md` — Fuente canónica del flujo de producto y de las pantallas esperadas para Loop 1 y Loop 2.
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md` — Fuente canónica del stack, las capas, el modelo de datos y el flujo extremo a extremo de la app.
- `global.json` — Ancla la versión del SDK .NET que debe usar el repo.

## Fixtures catalogados y referencias legacy

Estos archivos son fixtures o insumos del producto usados para importar, comparar o más adelante adaptar.

- `PLANS/catalog/santa-barbara.json` — Referencia catalogada legacy para santa barbara; sirve como oracle comparativo mientras la verdad canónica sigue siendo el DXF original.
- `PLANS/catalog/seminole-2000.json` — Referencia catalogada legacy para seminole 2000; sirve como oracle comparativo mientras la verdad canónica sigue siendo el DXF original.

## Fixtures canónicos de floor plans

Estos archivos son fixtures o insumos del producto usados para importar, comparar o más adelante adaptar.

- `PLANS/originalFloorPlans/SANTA-BARBARA.dxf` — Fixture DXF canónico de floor plan usado para importar, extraer walls y validar el Loop 1 sobre SANTA-BARBARA.dxf.
- `PLANS/originalFloorPlans/SEMINOLE2000.dxf` — Fixture DXF canónico de floor plan usado para importar, extraer walls y validar el Loop 1 sobre SEMINOLE2000.dxf.

## Fixtures canónicos de site plans

Estos archivos son fixtures o insumos del producto usados para importar, comparar o más adelante adaptar.

- `PLANS/originalsSitePlans/158 DAWSON STREET.dxf` — Fixture DXF canónico de site plan reservado para el futuro Loop 2 sobre 158 DAWSON STREET.dxf.

## Documentación narrativa histórica del proyecto

Estos archivos documentan el diseño, el plan o la historia de implementación del repositorio.

- `docs/explicacion del proyecto/2026-04-25 - explicacion de archivos tocados en slice 1.md` — Documento explicativo histórico sobre explicacion de archivos tocados en slice 1; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo.
- `docs/explicacion del proyecto/2026-04-29 - acceso directo local para abrir floorplan fit.md` — Documento explicativo histórico sobre acceso directo local para abrir floorplan fit; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo.
- `docs/explicacion del proyecto/2026-04-29 - bugfix de versionado al reimportar el mismo dxf.md` — Documento explicativo histórico sobre bugfix de versionado al reimportar el mismo dxf; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo.
- `docs/explicacion del proyecto/2026-04-29 - checklist manual del slice 1 ejecutable.md` — Documento explicativo histórico sobre checklist manual del slice 1 ejecutable; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo.
- `docs/explicacion del proyecto/2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md` — Documento explicativo histórico sobre explicacion exhaustiva de archivos del slice 1 ejecutable; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo.
- `docs/explicacion del proyecto/2026-04-29 - hidratacion de library desde sqlite y fix de avln3001.md` — Documento explicativo histórico sobre hidratacion de library desde sqlite y fix de avln3001; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo.
- `docs/explicacion del proyecto/2026-04-29 - launcher de desarrollo continuo para desktop.md` — Documento explicativo histórico sobre launcher de desarrollo continuo para desktop; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo.
- `docs/explicacion del proyecto/2026-04-29 - validacion real del slice 1 ejecutable.md` — Documento explicativo histórico sobre validacion real del slice 1 ejecutable; captura decisiones, validaciones o walkthroughs hechos durante la evolución del repo.

## Mapa exhaustivo y navegación total del repositorio

Estos archivos documentan el diseño, el plan o la historia de implementación del repositorio.

- `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` — Mapa exhaustivo del repositorio creado para explicar la arquitectura y la misión de cada archivo existente de la app.

## Planes de implementación ejecutables

Estos archivos documentan el diseño, el plan o la historia de implementación del repositorio.

- `docs/superpowers/plans/2026-04-25-slice-1-import-foundation.md` — Plan ejecutable de implementación para 2026 04 25 slice 1 import foundation; descompone milestones y orden de trabajo.
- `docs/superpowers/plans/2026-04-29-slice-1-executable-implementation.md` — Plan ejecutable de implementación para 2026 04 29 slice 1 executable implementation; descompone milestones y orden de trabajo.
- `docs/superpowers/plans/2026-04-30-loop-1-curated-walls-and-spaces-implementation.md` — Plan ejecutable de implementación para 2026 04 30 loop 1 curated walls and spaces implementation; descompone milestones y orden de trabajo.

## Diseños y especificaciones técnicas

Estos archivos documentan el diseño, el plan o la historia de implementación del repositorio.

- `docs/superpowers/specs/2026-04-25-slice-1-import-foundation-design.md` — Especificación o diseño técnico de 2026 04 25 slice 1 import foundation design; define modelo, alcance y decisiones de arquitectura.
- `docs/superpowers/specs/2026-04-29-slice-1-executable-design.md` — Especificación o diseño técnico de 2026 04 29 slice 1 executable design; define modelo, alcance y decisiones de arquitectura.
- `docs/superpowers/specs/2026-04-30-loop-1-curated-walls-and-spaces-design.md` — Especificación o diseño técnico de 2026 04 30 loop 1 curated walls and spaces design; define modelo, alcance y decisiones de arquitectura.

## Configuración interna del vault

Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.

- `obsidian-vault/.obsidian/app.json` — Configuración mínima del vault de Obsidian para que el conocimiento del proyecto sea navegable localmente.

## Registro de bugs

Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.

- `obsidian-vault/Bugs/2026-04-29 - Reimport creates new template from managed filename suffix.md` — Nota de bug sobre Reimport creates new template from managed filename suffix; documenta síntoma, causa raíz o fix relacionado.
- `obsidian-vault/Bugs/README.md` — Nota de bug sobre README; documenta síntoma, causa raíz o fix relacionado.

## Decisiones arquitectónicas persistentes

Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.

- `obsidian-vault/Decisions/2026-04-25 - DXF as Primary Truth and Catalog as Legacy Reference.md` — Decisión persistente del proyecto sobre DXF as Primary Truth and Catalog as Legacy Reference; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-25 - Floorplan Teaching Skill Always On.md` — Decisión persistente del proyecto sobre Floorplan Teaching Skill Always On; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-25 - Initial Implementation Order.md` — Decisión persistente del proyecto sobre Initial Implementation Order; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-25 - Project Identity and Knowledge Stack.md` — Decisión persistente del proyecto sobre Project Identity and Knowledge Stack; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-25 - Santa Barbara as First Canonical Fixture.md` — Decisión persistente del proyecto sobre Santa Barbara as First Canonical Fixture; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-25 - Slice 1 Import Foundation Architecture.md` — Decisión persistente del proyecto sobre Slice 1 Import Foundation Architecture; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-29 - Copy Imported DXFs Into Managed Workspace.md` — Decisión persistente del proyecto sobre Copy Imported DXFs Into Managed Workspace; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-29 - Keep Legacy Catalog JSONs as Oracle During Slice 1.md` — Decisión persistente del proyecto sobre Keep Legacy Catalog JSONs as Oracle During Slice 1; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-29 - Parse And Hash Managed DXF Copy.md` — Decisión persistente del proyecto sobre Parse And Hash Managed DXF Copy; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-29 - Thin Desktop Included In Executable Slice 1.md` — Decisión persistente del proyecto sobre Thin Desktop Included In Executable Slice 1; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2.md` — Decisión persistente del proyecto sobre Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Completion Order.md` — Decisión persistente del proyecto sobre Loop 1 Completion Order; explica el porqué arquitectónico o de workflow.
- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Uses Semantic Core Plus Minimal Review Canvas.md` — Decisión persistente del proyecto sobre Loop 1 Uses Semantic Core Plus Minimal Review Canvas; explica el porqué arquitectónico o de workflow.

## Área de experimentos del vault

Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.

- `obsidian-vault/Experiments/README.md` — README contenedor del área de experimentos del vault.

## Bitácora de implementación

Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.

- `obsidian-vault/Implementation/2026-04-25 - Slice 1 Files Explanation Map.md` — Bitácora de implementación o validación sobre Slice 1 Files Explanation Map; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - .NET 10 SDK Installed.md` — Bitácora de implementación o validación sobre .NET 10 SDK Installed; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Desktop Dev Watch Launcher.md` — Bitácora de implementación o validación sobre Desktop Dev Watch Launcher; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Desktop Shortcut Created.md` — Bitácora de implementación o validación sobre Desktop Shortcut Created; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Documentation Sync Final Pass.md` — Bitácora de implementación o validación sobre Documentation Sync Final Pass; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Exhaustive File Explanation Synced.md` — Bitácora de implementación o validación sobre Exhaustive File Explanation Synced; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Exhaustive File Explanation for Executable Slice 1.md` — Bitácora de implementación o validación sobre Exhaustive File Explanation for Executable Slice 1; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Library Startup Hydration and MainWindow Loader Fix.md` — Bitácora de implementación o validación sobre Library Startup Hydration and MainWindow Loader Fix; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Manual Validation Checklist for Slice 1 Executable.md` — Bitácora de implementación o validación sobre Manual Validation Checklist for Slice 1 Executable; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Reimport Versioning Bug Fixed.md` — Bitácora de implementación o validación sobre Reimport Versioning Bug Fixed; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Repository Audit Status.md` — Bitácora de implementación o validación sobre Repository Audit Status; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Design.md` — Bitácora de implementación o validación sobre Slice 1 Executable Design; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Implementation Plan.md` — Bitácora de implementación o validación sobre Slice 1 Executable Implementation Plan; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Kickoff.md` — Bitácora de implementación o validación sobre Slice 1 Executable Kickoff; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Infrastructure Import Pipeline.md` — Bitácora de implementación o validación sobre Slice 1 Infrastructure Import Pipeline; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-29 - Validation Results and App Control Blockers.md` — Bitácora de implementación o validación sobre Validation Results and App Control Blockers; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-30 - Loop 1 Curated Walls and Spaces Design.md` — Bitácora de implementación o validación sobre Loop 1 Curated Walls and Spaces Design; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-30 - Loop 1 Implementation Plan.md` — Bitácora de implementación o validación sobre Loop 1 Implementation Plan; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-30 - Requirement Tension Between Walls-Only and Room Constraints.md` — Bitácora de implementación o validación sobre Requirement Tension Between Walls Only and Room Constraints; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/2026-04-30 - Status Audit and Documentation Drift.md` — Bitácora de implementación o validación sobre Status Audit and Documentation Drift; registra progreso, checks y hallazgos concretos del repo.
- `obsidian-vault/Implementation/Vault Bootstrap.md` — Nota base que describe cómo quedó inicializado el vault y cómo se organiza el conocimiento persistente.
- `obsidian-vault/Implementation/2026-04-30 - Mapa completo de arquitectura y archivos del repo.md` — Nota de implementación que registra la creación del mapa exhaustivo de arquitectura y archivos del repositorio.

## Inbox del vault

Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.

- `obsidian-vault/Inbox/README.md` — README contenedor del inbox del vault para capturas rápidas.

## Raíz del conocimiento en Obsidian

Estos archivos forman parte del conocimiento persistente del proyecto dentro del vault de Obsidian.

- `obsidian-vault/Current State.md` — Resumen vivo del estado real del repo, verificación, próximos pasos y verdad canónica del proyecto.
- `obsidian-vault/Home.md` — Home del vault: punto de entrada humano para navegar decisiones, implementación, bugs y estado actual.

## Automatización local operativa

- `scripts/dev-desktop.bat` — Atajo local para desarrollo continuo de la app desktop con dotnet watch; hoy no se usa como verificación de coding por la regla never build after changes.

## Activos del skill local

Estos archivos enseñan al agente cómo explicar y trabajar correctamente dentro de este repositorio.

- `skills/floorplan-fit-teaching-mode/assets/teaching-response-template.md` — Template base de la respuesta pedagógica que se usa al explicar cambios en este repo.

## Referencias del skill local

Estos archivos enseñan al agente cómo explicar y trabajar correctamente dentro de este repositorio.

- `skills/floorplan-fit-teaching-mode/references/pressure-scenarios.md` — Escenarios de presión para verificar que la enseñanza siga siendo rigurosa incluso bajo pedidos ambiguos o apurados.

## Skill local del repositorio

Estos archivos enseñan al agente cómo explicar y trabajar correctamente dentro de este repositorio.

- `skills/floorplan-fit-teaching-mode/SKILL.md` — Skill local del repo que obliga a explicar cambios anclados a loops de producto y capas de arquitectura.

## Puertos y contratos internos de Application

Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.

- `src/FloorplanFit.Application/Abstractions/DetectedFloorPlanDocument.cs` — DTO técnico que representa el resultado de leer un DXF de floor plan: versión DXF, unidad, factor a milímetros y fingerprint geométrico.
- `src/FloorplanFit.Application/Abstractions/DetectedWallCandidate.cs` — DTO técnico de salida del extractor de walls antes de convertirlo en entidad de dominio persistible.
- `src/FloorplanFit.Application/Abstractions/FloorPlanExtractionSource.cs` — Describe desde qué versión y qué archivo gestionado hay que correr la extracción de walls.
- `src/FloorplanFit.Application/Abstractions/GeometryPoint.cs` — Valor simple 2D usado por extractores para describir geometría sin acoplarse a Infrastructure.
- `src/FloorplanFit.Application/Abstractions/IClock.cs` — Puerto para abstraer el tiempo actual y volver testeables las operaciones temporales.
- `src/FloorplanFit.Application/Abstractions/ICuratedWallRepository.cs` — Puerto de escritura/lectura para curated walls.
- `src/FloorplanFit.Application/Abstractions/IDxfGateway.cs` — Puerto para leer DXF de floor plans sin acoplar Application a IxMilia.
- `src/FloorplanFit.Application/Abstractions/IExtractedWallCandidateRepository.cs` — Puerto para persistir y consultar wall candidates extraídos.
- `src/FloorplanFit.Application/Abstractions/IFileHashService.cs` — Puerto para calcular hashes de archivos importados.
- `src/FloorplanFit.Application/Abstractions/IFloorPlanCurationRepository.cs` — Puerto para manejar draft/published de curaciones de floor plans.
- `src/FloorplanFit.Application/Abstractions/IFloorPlanExtractionSourceReader.cs` — Puerto de lectura optimizado para resolver la versión actual y el path DXF gestionado usado por la extracción.
- `src/FloorplanFit.Application/Abstractions/IFloorPlanLibraryReader.cs` — Puerto de lectura optimizado para hidratar la Library desde persistencia.
- `src/FloorplanFit.Application/Abstractions/IFloorPlanReviewSessionReader.cs` — Puerto de lectura optimizado para construir una sesión de review completa desde SQLite.
- `src/FloorplanFit.Application/Abstractions/IFloorPlanTemplateRepository.cs` — Puerto para crear, actualizar y consultar floor plan templates.
- `src/FloorplanFit.Application/Abstractions/IFloorPlanVersionRepository.cs` — Puerto para persistir versiones de floor plans y calcular el próximo número de versión.
- `src/FloorplanFit.Application/Abstractions/IImportedDocumentRepository.cs` — Puerto para persistir los documentos DXF importados.
- `src/FloorplanFit.Application/Abstractions/IManagedFileStorage.cs` — Puerto para copiar archivos importados al workspace gestionado de la app.
- `src/FloorplanFit.Application/Abstractions/IMeasurementContextRepository.cs` — Puerto para persistir el contexto de unidades/tolerancias de cada import.
- `src/FloorplanFit.Application/Abstractions/IUnitOfWork.cs` — Puerto transaccional para confirmar cambios coordinados entre varios repositorios.
- `src/FloorplanFit.Application/Abstractions/IWallExtractionRunRepository.cs` — Puerto para persistir cada corrida automática de extracción.
- `src/FloorplanFit.Application/Abstractions/IWallExtractor.cs` — Puerto del extractor real de walls a partir de un DXF gestionado.

## Casos de uso del curado Loop 1

Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.

- `src/FloorplanFit.Application/FloorPlans/Curation/AcceptWallCandidateHandler.cs` — Caso de uso que acepta un candidate pendiente y lo convierte en CuratedWall inicial dentro de un draft.
- `src/FloorplanFit.Application/FloorPlans/Curation/PublishFloorPlanCurationHandler.cs` — Caso de uso que valida y publica una curación draft, activándola en el template.
- `src/FloorplanFit.Application/FloorPlans/Curation/RejectWallCandidateHandler.cs` — Caso de uso que rechaza un candidate y elimina la CuratedWall derivada en el draft actual.
- `src/FloorplanFit.Application/FloorPlans/Curation/StartOrResumeCurationHandler.cs` — Caso de uso que crea o retoma el draft de curación de una versión de floor plan.
- `src/FloorplanFit.Application/FloorPlans/Curation/UpdateCuratedWallMetadataHandler.cs` — Caso de uso que actualiza la metadata semántica de una wall ya curada.

## Casos de uso de extracción automática

Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.

- `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs` — Caso de uso que corre el extractor, crea la corrida de extracción y persiste wall candidates + geometría.

## Casos de uso de importación

Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.

- `src/FloorplanFit.Application/FloorPlans/Import/FloorPlanCodeNormalizer.cs` — Normaliza nombres de archivos en códigos estables de templates para evitar duplicados semánticos.
- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs` — Caso de uso de importación: copia DXF, lo lee, crea template/version/document/measurement context y persiste todo.
- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanResultFactory.cs` — Fábrica que traduce el resultado de importación a DTOs de Contracts para la UI.

## Casos de uso de lectura de Library

Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.

- `src/FloorplanFit.Application/FloorPlans/Library/GetFloorPlanLibraryHandler.cs` — Caso de uso de lectura que devuelve la Library de floor plans desde el read-model.

## Casos de uso de review

Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.

- `src/FloorplanFit.Application/FloorPlans/Review/GetFloorPlanReviewSessionHandler.cs` — Caso de uso de lectura que devuelve la review session ya hidratada para un template.
- `src/FloorplanFit.Application/FloorPlans/Review/OpenFloorPlanReviewSessionHandler.cs` — Caso de uso que asegura draft activo y luego abre la sesión de review completa.

## Proyecto Application

Acá vive la **orquestación de casos de uso**. No debería haber detalles concretos de SQLite, Avalonia o IxMilia.

- `src/FloorplanFit.Application/FloorplanFit.Application.csproj` — Proyecto de Application: define el ensamblado que contiene puertos y casos de uso.

## DTOs de Contracts

- `src/FloorplanFit.Contracts/FloorPlans/CuratedWallDto.cs` — DTO plano que la UI usa para mostrar y editar walls curadas sin tocar entidades de dominio.
- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs` — DTO de cada fila de la Library con estado derivado, versión activa y unidad fuente.
- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs` — DTO agregado que empaqueta resumen del template, geometría, candidates y curated walls para la review UI.
- `src/FloorplanFit.Contracts/FloorPlans/GeometryPathDto.cs` — DTO de una trayectoria geométrica compuesta por segmentos ordenados.
- `src/FloorplanFit.Contracts/FloorPlans/GeometrySegmentDto.cs` — DTO de un segmento lineal individual dentro de un geometry path.
- `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanRequest.cs` — Contrato de entrada para solicitar importación de un DXF.
- `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanResponse.cs` — Contrato de salida de la importación para refrescar la UI.
- `src/FloorplanFit.Contracts/FloorPlans/OpenFloorPlanReviewSessionResponse.cs` — Contrato de salida que devuelve draft activo + review session al abrir review.
- `src/FloorplanFit.Contracts/FloorPlans/WallCandidateDto.cs` — DTO de cada wall candidate extraída con estado, confianza y referencia geométrica.

## Proyecto Contracts

- `src/FloorplanFit.Contracts/FloorplanFit.Contracts.csproj` — Proyecto de Contracts: define los DTOs que desacoplan UI/read-models de las entidades de dominio.

## Composición DI del desktop

Acá vive la **experiencia de usuario desktop** sobre Avalonia + MVVM.

- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs` — Punto de composición DI del slice desktop: registra infrastructure, handlers, readers y viewmodels necesarios.

## Controles visuales custom

Acá vive la **experiencia de usuario desktop** sobre Avalonia + MVVM.

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` — Canvas mínimo custom que dibuja geometry paths y resalta la selección actual en review.

## ViewModels MVVM

Acá vive la **experiencia de usuario desktop** sobre Avalonia + MVVM.

- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` — ViewModel del flujo de review: abre sesión, maneja selección, accept/reject, edición de metadata y publish.
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs` — ViewModel de la Library: carga items, importa DXF, dispara extracción y abre la review session.

## Proyecto Avalonia desktop

Acá vive la **experiencia de usuario desktop** sobre Avalonia + MVVM.

- `src/FloorplanFit.Desktop/App.axaml` — Define recursos y arranque visual global de la app Avalonia.
- `src/FloorplanFit.Desktop/App.axaml.cs` — Bootstrap code-behind del Application de Avalonia.
- `src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj` — Proyecto de la app Avalonia desktop: composición, vistas, viewmodels y arranque.
- `src/FloorplanFit.Desktop/MainWindow.axaml` — Pantalla principal Library: listado de floor plans y acciones de import/extract/review.
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs` — Code-behind mínimo que conecta clicks de la Library con el LibraryViewModel y abre la review window.
- `src/FloorplanFit.Desktop/Program.cs` — Entry point real de la app desktop; construye y lanza Avalonia.
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml` — Pantalla de review mínima con lista de candidates, preview geométrico e inspector de curated walls.
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs` — Code-behind mínimo que delega acciones de review al FloorPlanReviewViewModel.

## Entidades de documentos

Acá vive la **verdad del negocio**. Son entidades, enums y reglas que deberían sobrevivir aunque cambie la infraestructura.

- `src/FloorplanFit.Domain/Documents/ImportedDocument.cs` — Entidad de dominio del documento DXF importado y almacenado por la app.
- `src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs` — Enum del tipo de documento importado.

## Entidades y enums de floor plans

Acá vive la **verdad del negocio**. Son entidades, enums y reglas que deberían sobrevivir aunque cambie la infraestructura.

- `src/FloorplanFit.Domain/FloorPlans/ConstraintIntentNote.cs` — Entidad prevista para notas humanas de constraints todavía no estructuradas.
- `src/FloorplanFit.Domain/FloorPlans/ConstraintKind.cs` — Enum de tipos de constraints estructuradas posibles.
- `src/FloorplanFit.Domain/FloorPlans/ConstraintStrength.cs` — Enum de fuerza o prioridad de una constraint.
- `src/FloorplanFit.Domain/FloorPlans/CuratedSpace.cs` — Entidad prevista para representar ambientes/espacios curados; hoy existe en dominio pero todavía no recorre el flujo completo.
- `src/FloorplanFit.Domain/FloorPlans/CuratedWall.cs` — Entidad canónica de una wall ya validada por humano y enriquecida con metadata reusable.
- `src/FloorplanFit.Domain/FloorPlans/CuratedWallGroup.cs` — Entidad prevista para agrupar walls curadas con significado conjunto.
- `src/FloorplanFit.Domain/FloorPlans/CuratedWallJoin.cs` — Entidad prevista para registrar joins o uniones entre walls curadas.
- `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidate.cs` — Entidad de dominio de una wall candidata detectada automáticamente y pendiente/aceptada/rechazada.
- `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidateStatus.cs` — Enum de estados de un wall candidate.
- `src/FloorplanFit.Domain/FloorPlans/FloorPlanCuration.cs` — Entidad que modela una versión draft/published del curado humano de un floor plan.
- `src/FloorplanFit.Domain/FloorPlans/FloorPlanCurationStatus.cs` — Enum del ciclo de vida de la curación.
- `src/FloorplanFit.Domain/FloorPlans/FloorPlanTemplate.cs` — Raíz de agregado de la tipología reusable: separa current version importada de active published curation.
- `src/FloorplanFit.Domain/FloorPlans/FloorPlanVersion.cs` — Entidad que representa una versión importada concreta del template.
- `src/FloorplanFit.Domain/FloorPlans/SpaceType.cs` — Enum de tipos de ambientes previstos para curated spaces.
- `src/FloorplanFit.Domain/FloorPlans/StructuredConstraint.cs` — Entidad prevista para constraints tipadas aplicables a walls/spaces.
- `src/FloorplanFit.Domain/FloorPlans/WallExtractionRun.cs` — Entidad que registra una corrida automática del extractor sobre una versión.
- `src/FloorplanFit.Domain/FloorPlans/WallMobilityLevel.cs` — Enum que indica cuánto se puede mover/estirar una wall.
- `src/FloorplanFit.Domain/FloorPlans/WallProtectionLevel.cs` — Enum que indica nivel de protección o intocabilidad de una wall.
- `src/FloorplanFit.Domain/FloorPlans/WallRole.cs` — Enum semántico del rol de una wall (partition, exterior, etc.).

## Valores de medición y unidades

Acá vive la **verdad del negocio**. Son entidades, enums y reglas que deberían sobrevivir aunque cambie la infraestructura.

- `src/FloorplanFit.Domain/Measurement/LengthUnit.cs` — Enum de unidades lineales soportadas.
- `src/FloorplanFit.Domain/Measurement/MeasurementContext.cs` — Entidad que encapsula unidad fuente, factor a milímetros y tolerancias geométricas.

## Proyecto Domain

Acá vive la **verdad del negocio**. Son entidades, enums y reglas que deberían sobrevivir aunque cambie la infraestructura.

- `src/FloorplanFit.Domain/FloorplanFit.Domain.csproj` — Proyecto de Domain: núcleo puro del negocio sin dependencias de infraestructura.

## Adaptadores DXF

Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDxfGateway.cs` — Adaptador real del puerto IDxfGateway para leer metadata de DXF con IxMilia.
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs` — Adaptador real del extractor de walls; filtra capas wall-like y descompone line/polyline en candidates.

## Persistencia SQLite y read-models

Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteCuratedWallRepository.cs` — Repositorio SQLite de curated walls.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs` — Repositorio SQLite de wall candidates; también persiste su geometría en geometry_paths/segments.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanCurationRepository.cs` — Repositorio SQLite de curations draft/published y su versionado.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanExtractionSourceReader.cs` — Reader SQLite que resuelve qué archivo/version actual usar para extracción desde la Library.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs` — Read-model SQLite de la Library; deriva Imported/Extracted/Curated Draft/Published.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs` — Read-model SQLite de review; junta template summary, candidates, curated walls y geometry paths.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs` — Repositorio SQLite de floor plan templates y active published curation.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionRepository.cs` — Repositorio SQLite de versiones de floor plans.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteImportedDocumentRepository.cs` — Repositorio SQLite de documentos importados.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementContextRepository.cs` — Repositorio SQLite de measurement contexts.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs` — Inicializador del schema SQLite; crea tablas y columnas necesarias del producto.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs` — Encapsula conexión y transacción activas de SQLite para que los repos compartan contexto.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteUnitOfWork.cs` — Implementación SQLite del Unit of Work.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs` — Repositorio SQLite de corridas de extracción.

## Workspace y runtime local

Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.

- `src/FloorplanFit.Infrastructure/Runtime/AppWorkspace.cs` — Resuelve la carpeta workspace local de la app y sus subdirectorios gestionados.

## Servicios de seguridad técnica

Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.

- `src/FloorplanFit.Infrastructure/Security/Sha256FileHashService.cs` — Implementación real del cálculo SHA-256 sobre archivos.

## Storage gestionado

Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.

- `src/FloorplanFit.Infrastructure/Storage/ManagedFileStorage.cs` — Implementación real del storage gestionado que copia DXF al library/raw-dxf del workspace.

## Proyecto Infrastructure

Acá viven los **adaptadores reales**: DXF, SQLite, filesystem y servicios técnicos.

- `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj` — Proyecto de Infrastructure: implementaciones reales de DXF, SQLite, storage, hashing y runtime.
- `src/FloorplanFit.Infrastructure/InfrastructureAssemblyMarker.cs` — Marcador simple del ensamblado de Infrastructure útil para composición o referencias.

## Tests de Application sobre curado

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AcceptWallCandidateHandlerTests.cs` — Verifica que aceptar candidates actualice estado y cree curated walls correctamente.
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanCurationTests.cs` — Verifica invariantes de la entidad FloorPlanCuration.
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/PublishFloorPlanCurationHandlerTests.cs` — Verifica reglas de publicación y activación de curaciones.
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RejectWallCandidateHandlerTests.cs` — Verifica que rechazar candidates actualice estado y remueva curated walls derivadas.
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/StartOrResumeCurationHandlerTests.cs` — Verifica creación/retoma de drafts de curación.
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/UpdateCuratedWallMetadataHandlerTests.cs` — Verifica persistencia de metadata semántica de curated walls.

## Tests de Application sobre extracción

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs` — Verifica orquestación del caso de uso de extracción sin depender del extractor real.

## Tests de Application sobre importación

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs` — Verifica el flujo Application de importación de floor plans.

## Tests de Application sobre Library

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Library/GetFloorPlanLibraryHandlerTests.cs` — Verifica que el handler de Library devuelva el read-model esperado.

## Tests de Application sobre review

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/GetFloorPlanReviewSessionHandlerTests.cs` — Verifica la lectura de review sessions desde Application.
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/OpenFloorPlanReviewSessionHandlerTests.cs` — Verifica que abrir review asegure draft activo y devuelva la sesión completa.

## Proyecto de tests de Application

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj` — Proyecto de tests unitarios de Application.
- `tests/FloorplanFit.Application.Tests/GlobalUsings.cs` — Importaciones globales para simplificar los tests de Application.

## Tests de wiring Desktop

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Desktop.Tests/Composition/DesktopServiceRegistrationTests.cs` — Verifica que el contenedor DI desktop resuelva el slice principal sin faltantes.

## Tests de ViewModels Desktop

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs` — Verifica el comportamiento principal del ViewModel de review.
- `tests/FloorplanFit.Desktop.Tests/ViewModels/LibraryViewModelTests.cs` — Verifica el comportamiento principal del ViewModel de Library.

## Proyecto de tests de Desktop

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj` — Proyecto de tests del wiring y viewmodels de Desktop.

## Tests de persistencia de curación

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs` — Verifica round-trips reales de persistencia SQLite para curations y curated walls.

## Tests de adaptadores DXF

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaDxfGatewayTests.cs` — Verifica lectura real de metadata DXF a través del gateway IxMilia.

## Tests del extractor real

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaWallExtractorTests.cs` — Verifica extracción real de wall candidates desde fixtures DXF.

## Tests del pipeline de importación

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Infrastructure.Tests/Imports/FloorPlanLibraryReaderIntegrationTests.cs` — Verifica que el read-model de Library derive los estados correctos desde SQLite.
- `tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs` — Verifica el pipeline real de importación con SQLite, storage y DXF.
- `tests/FloorplanFit.Infrastructure.Tests/Imports/ManagedFileStorageTests.cs` — Verifica la copia gestionada de DXF al workspace.

## Tests del source reader

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Infrastructure.Tests/Library/FloorPlanExtractionSourceReaderIntegrationTests.cs` — Verifica la resolución del source de extracción para la versión actual.

## Tests del read-model de review

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs` — Verifica que el read-model de review hidrate candidates, curated walls y geometría.

## Soporte común de tests

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Infrastructure.Tests/TestSupport/RepositoryPaths.cs` — Centraliza paths del repo/fixtures para los tests de Infrastructure.

## Proyecto de tests de Infrastructure

Estos archivos son evidencia automatizada de que la capa correspondiente hace lo que promete.

- `tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj` — Proyecto de tests de integración/unidad de Infrastructure.
- `tests/FloorplanFit.Infrastructure.Tests/GlobalUsings.cs` — Importaciones globales compartidas por los tests de Infrastructure.

## Verificación de cobertura

- Conteo total cubierto: **203**
- Base verificada por git: **201 archivos trackeados**
- Documentos nuevos agregados en esta pasada: **2**
- Método de verificación: `git ls-files` + inclusión explícita de los documentos nuevos de esta actualización.
- Criterio de completitud: cada path versionado relevante y cada documento nuevo creado en esta pasada aparece exactamente una vez en este documento.

## Qué NO cubre este mapa

- `bin/`, `obj/`, `.vs/`, caches de herramientas y otros artefactos generados.
- Directorios no versionados como residuos operativos locales.
- Estado runtime efímero del workspace `src/FloorplanFit.Desktop/bin/.../workspace/` porque cambia ejecución a ejecución.

## Cómo usar este documento

1. Si querés entender **producto**, arrancá por `MVP-UX.md` y `TECH-STACK-ARCHITECTURE-DATAFLOW.md`.
2. Si querés entender **flujo Loop 1**, seguí: Desktop Library -> Import handler -> Extract handler -> Review session reader -> Review UI.
3. Si querés entender **persistencia**, recorré `SqliteSchemaInitializer` y luego cada repo/read-model de `src/FloorplanFit.Infrastructure/Persistence/`.
4. Si querés entender **evidencia**, terminá en `tests/` por capa.
