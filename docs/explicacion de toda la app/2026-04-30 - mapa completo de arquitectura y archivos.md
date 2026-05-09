---
type: architecture-map
date: 2026-04-30
last_verified: 2026-05-09
status: active
scope: current-working-tree-files
file_count: 323
---

# Mapa completo de arquitectura y archivos de Floorplan Fit

## Proposito del documento

Este documento es el mapa exhaustivo del repositorio. Explica que parte de la arquitectura toca cada archivo, que responsabilidad tiene, por que importa y como encaja en el producto.

Cobertura revalidada el **2026-05-09** contra `git ls-files` y `git ls-files --others --exclude-standard`: **323 archivos relevantes presentes** = **302 versionados presentes** + **21 nuevos no versionados todavia**. Se excluyen `bin/`, `obj/`, `.artifacts-test/`, `.git/`, `.vs/`, `workspace/` y archivos locales de usuario.

## Actualizacion 2026-05-08

- La arquitectura activa de Loop 1 es **CAD-faithful curation + pinch-native shrink zones**.
- El centro del curado ya no es `CuratedWall`; ahora se publican artefactos separados: wall candidates, pinch groups/markers, room labels, opening candidates/labels, fixed plan components y protected detail assemblies.
- `DxfExtractionProfile.PointeHomes` concentra convenciones debiles de Pointe Homes para no esconder reglas de SEMINOLE2000 en extractores genericos.
- `FloorPlanPreviewControl` queda como shell interactivo; el dibujo vive en renderers chicos bajo `Controls/Preview/`.
- Las dimensiones CAD siguen pendientes, pero deben entrar como familia propia de artifacts: geometry + text + anchors + valor original/recalculable durante pinches/adaptacion.

## Big Picture

Floorplan Fit es un monolito modular local-first en .NET 10 con Avalonia, SQLite e IxMilia DXF. La promesa de `MVP-UX.md` es curar un floor plan una vez, reutilizarlo muchas veces y transformar cada site plan en una decision tecnica corta y auditable.

Loop 1 hoy busca una superficie de curado visualmente fiel al CAD: cada familia entra separada, se selecciona, se corrige/remueve y se persiste antes de publicar. Loop 2 —site plan envelope, deterministic fit, proposals y output adaptado— todavia consume esta base publicada en el futuro.

## Pipeline activo por familia CAD

```text
DXF real -> profile/conventions -> extractor IxMilia -> detected model Application -> entidad Domain -> SQLite/read-model -> DTO Contracts -> preview layer/seleccion/curado -> published library truth
```

| Familia | Proposito | Estado |
| --- | --- | --- |
| Wall candidates | Estructura base detectada | Activa, con hints 2x4/2x6 por geometria |
| Pinch groups/markers | Zonas recortables autorizadas | Activa, grupos nombrados por Width/Height |
| Room labels | Nombres de ambientes | Activa, render DXF-like |
| Opening candidates/labels | Puertas/ventanas y modelo/tamano | Activa, seleccion/removal |
| Fixed plan components | Fixtures/cabinets/toilets/tubs | Activa, color DXF preservado |
| Protected detail assemblies | Detalles humedos/protegidos | Activa, MISC/HATCH como WetAreaDetail |
| Dimensions | Cotas CAD recalculables | Pendiente; debe ser familia separada |

## Convencion de lectura

Cada entrada usa: **Mision**, **Importancia**, **Use case**. Si algo dice pendiente/futuro, existe como soporte o decision pero no recorre todo el loop aun.


## Raiz del repositorio y gobierno tecnico

Gobierno tecnico, solucion, fuentes canonicas y configuracion global.

- `.gitignore` — Mision: Archivo de gobierno/proyecto .gitignore. Importancia: mantiene la solucion operable. Use case: configurar, abrir o entender el repo.


- `AGENTS.md` — Mision: Reglas humanas/operativas del repo. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.

- `Directory.Build.props` — Mision: Archivo de gobierno/proyecto Directory.Build. Importancia: mantiene la solucion operable. Use case: configurar, abrir o entender el repo.

- `FloorplanFit.sln` — Mision: Archivo de gobierno/proyecto Floorplan Fit. Importancia: mantiene la solucion operable. Use case: configurar, abrir o entender el repo.

- `LICENSE` — Mision: Archivo de gobierno/proyecto LICENSE. Importancia: mantiene la solucion operable. Use case: configurar, abrir o entender el repo.

- `MVP-UX.md` — Mision: Fuente canonica de la promesa UX: curar una vez y reutilizar muchas. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.

- `TECH-STACK-ARCHITECTURE-DATAFLOW.md` — Mision: Fuente canonica de stack, capas y flujo de datos. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.

- `global.json` — Mision: Archivo de gobierno/proyecto global. Importancia: mantiene la solucion operable. Use case: configurar, abrir o entender el repo.


## Fixtures catalogados y referencias legacy

JSONs historicos: referencia comparativa, no fuente primaria.

- `PLANS/catalog/santa-barbara.json` — Mision: Fixture santa-barbara.json. Importancia: material real/legacy para validar el pipeline. Use case: correr extracciones o comparar resultados.

- `PLANS/catalog/seminole-2000.json` — Mision: Fixture seminole-2000.json. Importancia: material real/legacy para validar el pipeline. Use case: correr extracciones o comparar resultados.


## Fixtures canonicos de floor plans

DXF reales desde donde Loop 1 extrae y cura artefactos CAD.

- `PLANS/originalFloorPlans/SANTA-BARBARA.dxf` — Mision: Fixture SANTA-BARBARA.dxf. Importancia: material real/legacy para validar el pipeline. Use case: correr extracciones o comparar resultados.

- `PLANS/originalFloorPlans/SEMINOLE2000.dxf` — Mision: Fixture SEMINOLE2000.dxf. Importancia: material real/legacy para validar el pipeline. Use case: correr extracciones o comparar resultados.


## Fixtures canonicos de site plans

DXF reales para Loop 2: envelope, fit y auditoria futura.

- `PLANS/originalsSitePlans/158 DAWSON STREET.dxf` — Mision: Fixture 158 DAWSON STREET.dxf. Importancia: material real/legacy para validar el pipeline. Use case: correr extracciones o comparar resultados.


## Documentacion narrativa historica

Explicaciones humanas de slices, fixes y validaciones.

- `docs/explicacion del proyecto/2026-04-25 - explicacion de archivos tocados en slice 1.md` — Mision: Documento de trabajo 2026 04 25   explicacion de archivos tocados en slice 1. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/explicacion del proyecto/2026-04-29 - bugfix de versionado al reimportar el mismo dxf.md` — Mision: Documento de trabajo 2026 04 29   bugfix de versionado al reimportar el mismo dxf. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/explicacion del proyecto/2026-04-29 - checklist manual del slice 1 ejecutable.md` — Mision: Documento de trabajo 2026 04 29   checklist manual del slice 1 ejecutable. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/explicacion del proyecto/2026-04-29 - explicacion exhaustiva de archivos del slice 1 ejecutable.md` — Mision: Documento de trabajo 2026 04 29   explicacion exhaustiva de archivos del slice 1 ejecutable. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/explicacion del proyecto/2026-04-29 - hidratacion de library desde sqlite y fix de avln3001.md` — Mision: Documento de trabajo 2026 04 29   hidratacion de library desde sqlite y fix de avln3001. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/explicacion del proyecto/2026-04-29 - validacion real del slice 1 ejecutable.md` — Mision: Documento de trabajo 2026 04 29   validacion real del slice 1 ejecutable. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.


## Mapa exhaustivo y navegacion total

Este documento vivo de arquitectura.

- `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` — Mision: Documento de trabajo 2026 04 30   mapa completo de arquitectura y archivos. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.


## Planes de implementacion ejecutables

Planes operativos antes/durante cambios grandes.

- `docs/superpowers/plans/2026-04-25-slice-1-import-foundation.md` — Mision: Documento de trabajo 2026 04 25 slice 1 import foundation. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-04-29-slice-1-executable-implementation.md` — Mision: Documento de trabajo 2026 04 29 slice 1 executable implementation. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-04-30-loop-1-curated-walls-and-spaces-implementation.md` — Mision: Documento de trabajo 2026 04 30 loop 1 curated walls and spaces implementation. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-05-03-review-preview-layout-and-selection-fix.md` — Mision: Documento de trabajo 2026 05 03 review preview layout and selection fix. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-05-04-loop1-pinch-curation-preview.md` — Mision: Documento de trabajo 2026 05 04 loop1 pinch curation preview. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-05-04-pinch-native-cleanup.md` — Mision: Documento de trabajo 2026 05 04 pinch native cleanup. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-05-04-pinch-review-ux-redesign.md` — Mision: Documento de trabajo 2026 05 04 pinch review ux redesign. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-05-07-preview-layer-refactor.md` — Mision: Documento de trabajo 2026 05 07 preview layer refactor. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-05-07-protected-detail-assemblies.md` — Mision: Documento de trabajo 2026 05 07 protected detail assemblies. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/plans/2026-05-07-room-label-candidates.md` — Mision: Documento de trabajo 2026 05 07 room label candidates. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.


## Disenos y especificaciones tecnicas

Specs de comportamiento, alcance y tradeoffs.

- `docs/superpowers/specs/2026-04-25-slice-1-import-foundation-design.md` — Mision: Documento de trabajo 2026 04 25 slice 1 import foundation design. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/specs/2026-04-29-slice-1-executable-design.md` — Mision: Documento de trabajo 2026 04 29 slice 1 executable design. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/specs/2026-04-30-loop-1-curated-walls-and-spaces-design.md` — Mision: Documento de trabajo 2026 04 30 loop 1 curated walls and spaces design. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/specs/2026-05-03-review-preview-layout-and-selection-design.md` — Mision: Documento de trabajo 2026 05 03 review preview layout and selection design. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/specs/2026-05-04-loop1-pinch-curation-preview-design.md` — Mision: Documento de trabajo 2026 05 04 loop1 pinch curation preview design. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/specs/2026-05-04-pinch-native-cleanup-design.md` — Mision: Documento de trabajo 2026 05 04 pinch native cleanup design. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.

- `docs/superpowers/specs/2026-05-04-pinch-review-ux-redesign-design.md` — Mision: Documento de trabajo 2026 05 04 pinch review ux redesign design. Importancia: conserva razonamiento y planificacion. Use case: entender por que se toco una zona sin leer todo el historial.


## Configuracion interna del vault

Soporte de Obsidian para el repositorio de conocimiento.

- `obsidian-vault/.obsidian/core-plugins.json` — Mision: Nota/configuracion durable core plugins. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.


## Registro de bugs

Causas raiz y fixes persistidos.

- `obsidian-vault/Bugs/2026-04-29 - Reimport creates new template from managed filename suffix.md` — Mision: Nota/configuracion durable 2026 04 29   Reimport creates new template from managed filename suffix. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-02 - Open Review crashes right after extraction because committed SQLite transaction is reused.md` — Mision: Nota/configuracion durable 2026 05 02   Open Review crashes right after extraction because committed SQLite transaction is reused. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-04 - Legacy pinch markers schema crashes open review.md` — Mision: Nota/configuracion durable 2026 05 04   Legacy pinch markers schema crashes open review. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-04 - Wrong Avalonia XML namespace breaks compiled app XAML.md` — Mision: Nota/configuracion durable 2026 05 04   Wrong Avalonia XML namespace breaks compiled app XAML. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-05 - Height preview handles were implicit instead of draggable visible controls.md` — Mision: Nota/configuracion durable 2026 05 05   Height preview handles were implicit instead of draggable visible controls. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-05 - Preview axis and viewport were misaligned with selected pinch.md` — Mision: Nota/configuracion durable 2026 05 05   Preview axis and viewport were misaligned with selected pinch. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-05 - Preview canvas overflowed its parent container vertically.md` — Mision: Nota/configuracion durable 2026 05 05   Preview canvas overflowed its parent container vertically. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-05 - Preview control used parent bounds for local rendering.md` — Mision: Nota/configuracion durable 2026 05 05   Preview control used parent bounds for local rendering. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-05 - Review window used a rigid size that overflowed smaller screens.md` — Mision: Nota/configuracion durable 2026 05 05   Review window used a rigid size that overflowed smaller screens. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-06 - Axis-tagged pinch migration duplicated markers when groups shared an axis.md` — Mision: Nota/configuracion durable 2026 05 06   Axis tagged pinch migration duplicated markers when groups shared an axis. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/2026-05-07 - Opening labels rendered white and included non-opening notes.md` — Mision: Nota/configuracion durable 2026 05 07   Opening labels rendered white and included non opening notes. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Bugs/README.md` — Mision: Nota/configuracion durable README. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.


## Decisiones arquitectonicas persistentes

Decisiones que condicionan producto y arquitectura.

- `obsidian-vault/Decisions/2026-04-25 - DXF as Primary Truth and Catalog as Legacy Reference.md` — Mision: Nota/configuracion durable 2026 04 25   DXF as Primary Truth and Catalog as Legacy Reference. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-25 - Floorplan Teaching Skill Always On.md` — Mision: Nota/configuracion durable 2026 04 25   Floorplan Teaching Skill Always On. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-25 - Initial Implementation Order.md` — Mision: Nota/configuracion durable 2026 04 25   Initial Implementation Order. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-25 - Project Identity and Knowledge Stack.md` — Mision: Nota/configuracion durable 2026 04 25   Project Identity and Knowledge Stack. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-25 - Santa Barbara as First Canonical Fixture.md` — Mision: Nota/configuracion durable 2026 04 25   Santa Barbara as First Canonical Fixture. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-25 - Slice 1 Import Foundation Architecture.md` — Mision: Nota/configuracion durable 2026 04 25   Slice 1 Import Foundation Architecture. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-29 - Copy Imported DXFs Into Managed Workspace.md` — Mision: Nota/configuracion durable 2026 04 29   Copy Imported DXFs Into Managed Workspace. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-29 - Keep Legacy Catalog JSONs as Oracle During Slice 1.md` — Mision: Nota/configuracion durable 2026 04 29   Keep Legacy Catalog JSONs as Oracle During Slice 1. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-29 - Parse And Hash Managed DXF Copy.md` — Mision: Nota/configuracion durable 2026 04 29   Parse And Hash Managed DXF Copy. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-29 - Thin Desktop Included In Executable Slice 1.md` — Mision: Nota/configuracion durable 2026 04 29   Thin Desktop Included In Executable Slice 1. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2.md` — Mision: Nota/configuracion durable 2026 04 30   Loop 1 Adds Minimal Curated Spaces and Defers New Walls To Loop 2. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Completion Order.md` — Mision: Nota/configuracion durable 2026 04 30   Loop 1 Completion Order. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-04-30 - Loop 1 Uses Semantic Core Plus Minimal Review Canvas.md` — Mision: Nota/configuracion durable 2026 04 30   Loop 1 Uses Semantic Core Plus Minimal Review Canvas. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-05-07 - CAD-faithful curation is the library publishing contract.md` — Mision: Nota/configuracion durable 2026 05 07   CAD faithful curation is the library publishing contract. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Decisions/2026-05-07 - Extraction pipeline targets Pointe floorplans with profile-backed conventions.md` — Mision: Nota/configuracion durable 2026 05 07   Extraction pipeline targets Pointe floorplans with profile backed conventions. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.


## Experimentos del vault

Evidencia y exploraciones no siempre canonicas.

- `obsidian-vault/Experiments/2026-05-06 - DXF wall thickness signals from geometry not color.md` — Mision: Nota/configuracion durable 2026 05 06   DXF wall thickness signals from geometry not color. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Experiments/README.md` — Mision: Nota/configuracion durable README. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.


## Bitacora de implementacion

Que se implemento, por que y donde.

- `obsidian-vault/Implementation/2026-04-29 - .NET 10 SDK Installed.md` — Mision: Nota/configuracion durable 2026 04 29   .NET 10 SDK Installed. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Desktop Dev Watch Launcher.md` — Mision: Nota/configuracion durable 2026 04 29   Desktop Dev Watch Launcher. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Desktop Shortcut Created.md` — Mision: Nota/configuracion durable 2026 04 29   Desktop Shortcut Created. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Documentation Sync Final Pass.md` — Mision: Nota/configuracion durable 2026 04 29   Documentation Sync Final Pass. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Library Startup Hydration and MainWindow Loader Fix.md` — Mision: Nota/configuracion durable 2026 04 29   Library Startup Hydration and Main Window Loader Fix. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Manual Validation Checklist for Slice 1 Executable.md` — Mision: Nota/configuracion durable 2026 04 29   Manual Validation Checklist for Slice 1 Executable. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Reimport Versioning Bug Fixed.md` — Mision: Nota/configuracion durable 2026 04 29   Reimport Versioning Bug Fixed. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Repository Audit Status.md` — Mision: Nota/configuracion durable 2026 04 29   Repository Audit Status. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Design.md` — Mision: Nota/configuracion durable 2026 04 29   Slice 1 Executable Design. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Implementation Plan.md` — Mision: Nota/configuracion durable 2026 04 29   Slice 1 Executable Implementation Plan. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Executable Kickoff.md` — Mision: Nota/configuracion durable 2026 04 29   Slice 1 Executable Kickoff. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Slice 1 Infrastructure Import Pipeline.md` — Mision: Nota/configuracion durable 2026 04 29   Slice 1 Infrastructure Import Pipeline. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-29 - Validation Results and App Control Blockers.md` — Mision: Nota/configuracion durable 2026 04 29   Validation Results and App Control Blockers. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-30 - Loop 1 Curated Walls and Spaces Design.md` — Mision: Nota/configuracion durable 2026 04 30   Loop 1 Curated Walls and Spaces Design. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-30 - Loop 1 Implementation Plan.md` — Mision: Nota/configuracion durable 2026 04 30   Loop 1 Implementation Plan. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-30 - Mapa completo de arquitectura y archivos del repo.md` — Mision: Nota/configuracion durable 2026 04 30   Mapa completo de arquitectura y archivos del repo. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-30 - Requirement Tension Between Walls-Only and Room Constraints.md` — Mision: Nota/configuracion durable 2026 04 30   Requirement Tension Between Walls Only and Room Constraints. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-04-30 - Status Audit and Documentation Drift.md` — Mision: Nota/configuracion durable 2026 04 30   Status Audit and Documentation Drift. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-02 - Fixed first-open review transaction crash.md` — Mision: Nota/configuracion durable 2026 05 02   Fixed first open review transaction crash. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-02 - Revalidacion del estado actual y drift del checklist manual.md` — Mision: Nota/configuracion durable 2026 05 02   Revalidacion del estado actual y drift del checklist manual. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-03 - Review preview layout and highlight fix.md` — Mision: Nota/configuracion durable 2026 05 03   Review preview layout and highlight fix. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-04 - Pinch curation preview prototype.md` — Mision: Nota/configuracion durable 2026 05 04   Pinch curation preview prototype. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-04 - Pinch native cleanup and minimal review UI.md` — Mision: Nota/configuracion durable 2026 05 04   Pinch native cleanup and minimal review UI. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-06 - Geometry-based wall thickness inference.md` — Mision: Nota/configuracion durable 2026 05 06   Geometry based wall thickness inference. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-06 - Pinch groups persisted for named shrink zones.md` — Mision: Nota/configuracion durable 2026 05 06   Pinch groups persisted for named shrink zones. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - DXF-like room label rendering.md` — Mision: Nota/configuracion durable 2026 05 07   DXF like room label rendering. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Door and window opening candidates extracted into review.md` — Mision: Nota/configuracion durable 2026 05 07   Door and window opening candidates extracted into review. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Fixed plan components extracted into review.md` — Mision: Nota/configuracion durable 2026 05 07   Fixed plan components extracted into review. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Pointe Homes CAD extraction profile.md` — Mision: Nota/configuracion durable 2026 05 07   Pointe Homes CAD extraction profile. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Preview layer refactor for CAD-faithful curation.md` — Mision: Nota/configuracion durable 2026 05 07   Preview layer refactor for CAD faithful curation. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Preview wheel zoom and dotted workspace.md` — Mision: Nota/configuracion durable 2026 05 07   Preview wheel zoom and dotted workspace. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Preview-selectable openings for false positive removal.md` — Mision: Nota/configuracion durable 2026 05 07   Preview selectable openings for false positive removal. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Protected detail assemblies for wet-area curation.md` — Mision: Nota/configuracion durable 2026 05 07   Protected detail assemblies for wet area curation. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Removable opening false positives in review.md` — Mision: Nota/configuracion durable 2026 05 07   Removable opening false positives in review. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Room label baseline alignment fix.md` — Mision: Nota/configuracion durable 2026 05 07   Room label baseline alignment fix. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Room label candidates extracted into review.md` — Mision: Nota/configuracion durable 2026 05 07   Room label candidates extracted into review. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/2026-05-07 - Room label overlay rendered on preview canvas.md` — Mision: Nota/configuracion durable 2026 05 07   Room label overlay rendered on preview canvas. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Implementation/Vault Bootstrap.md` — Mision: Nota/configuracion durable Vault Bootstrap. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.



- `obsidian-vault/Implementation/2026-05-08 - Architecture map refreshed for CAD-faithful curation.md` ? Mision: Nota durable del refresh del mapa completo de arquitectura. Importancia: deja trazable por que el mapa paso a reflejar CAD-faithful curation y pinch-native shrink zones. Use case: recuperar el contexto del inventario arquitectonico actualizado.

- `obsidian-vault/Implementation/2026-05-09 - MVP UX refreshed for CAD-faithful pinch curation.md` ? Mision: Nota durable del refresh de la fuente UX canonica. Importancia: registra el reemplazo del flujo accept-first por CAD-faithful curation con pinch groups/markers. Use case: entender por que `MVP-UX.md` ya no describe curated-wall UX.

- `obsidian-vault/Implementation/2026-05-09 - Tech stack architecture dataflow refreshed for CAD artifacts and pinch fit.md` ? Mision: Nota durable del refresh de la fuente tecnica canonica. Importancia: registra el reemplazo del modelo wall-only por artifact families, published curation y fit con zonas autorizadas. Use case: entender por que `TECH-STACK-ARCHITECTURE-DATAFLOW.md` ya no habla de CuratedWalls ni walls-only.

## Inbox del vault

Captura rapida de necesidades y tensiones.

- `obsidian-vault/Inbox/2026-05-04 - Curation requirement for shrinkable spans and protected openings.md` — Mision: Nota/configuracion durable 2026 05 04   Curation requirement for shrinkable spans and protected openings. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-04 - MVP pinch tool workflow from line review.md` — Mision: Nota/configuracion durable 2026 05 04   MVP pinch tool workflow from line review. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-04 - Pinch placement UX gotchas in prototype.md` — Mision: Nota/configuracion durable 2026 05 04   Pinch placement UX gotchas in prototype. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-04 - Pinch zones for non-scaling floor plan compression.md` — Mision: Nota/configuracion durable 2026 05 04   Pinch zones for non scaling floor plan compression. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-04 - Proposed MVP simplification axis-tagged pinch zones.md` — Mision: Nota/configuracion durable 2026 05 04   Proposed MVP simplification axis tagged pinch zones. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-04 - Recommended MVP grouped line pinches with optional twin pairing.md` — Mision: Nota/configuracion durable 2026 05 04   Recommended MVP grouped line pinches with optional twin pairing. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-06 - Existing extraction runs do not backfill new wall thickness hints.md` — Mision: Nota/configuracion durable 2026 05 06   Existing extraction runs do not backfill new wall thickness hints. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-06 - Requirement to bring wall assemblies and room names into curation.md` — Mision: Nota/configuracion durable 2026 05 06   Requirement to bring wall assemblies and room names into curation. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-07 - Guided SDD questions for protected detail assemblies.md` — Mision: Nota/configuracion durable 2026 05 07   Guided SDD questions for protected detail assemblies. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-07 - Room labels are persisted but not drawn on preview canvas.md` — Mision: Nota/configuracion durable 2026 05 07   Room labels are persisted but not drawn on preview canvas. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-07 - Room names extraction for review curation.md` — Mision: Nota/configuracion durable 2026 05 07   Room names extraction for review curation. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/2026-05-07 - Wet area enclosure curation gap in Seminole2000.md` — Mision: Nota/configuracion durable 2026 05 07   Wet area enclosure curation gap in Seminole2000. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Inbox/README.md` — Mision: Nota/configuracion durable README. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.


## Raiz de Obsidian

Estado actual, home e indices del vault.

- `obsidian-vault/Current State.md` — Mision: Nota/configuracion durable Current State. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.

- `obsidian-vault/Home.md` — Mision: Nota/configuracion durable Home. Importancia: conserva contexto entre sesiones. Use case: recuperar decisiones, bugs, experimentos o estado actual.


## Automatizacion local operativa

Scripts de conveniencia para operar la app.

- `scripts/dev-desktop.bat` — Mision: Script local dev desktop. Importancia: reduce pasos manuales fragiles. Use case: lanzar o preparar la app local.


## Skill local del repositorio

Reglas de trabajo/ensenanza para este repo.

- `skills/floorplan-fit-teaching-mode/SKILL.md` — Mision: Recurso del skill local SKILL. Importancia: sostiene la metodologia de trabajo del repo. Use case: explicar cambios por loop/capa/tradeoff.

- `skills/floorplan-fit-teaching-mode/assets/teaching-response-template.md` — Mision: Recurso del skill local teaching response template. Importancia: sostiene la metodologia de trabajo del repo. Use case: explicar cambios por loop/capa/tradeoff.

- `skills/floorplan-fit-teaching-mode/references/pressure-scenarios.md` — Mision: Recurso del skill local pressure scenarios. Importancia: sostiene la metodologia de trabajo del repo. Use case: explicar cambios por loop/capa/tradeoff.


## Puertos y contratos internos de Application

Boundary de casos de uso: modelos detectados y puertos, sin SQLite/Avalonia/IxMilia.

- `src/FloorplanFit.Application/Abstractions/DetectedFixedPlanComponent.cs` — Mision: Modelo/puerto de Application para Detected Fixed Plan Component. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/DetectedFloorPlanDocument.cs` — Mision: Modelo/puerto de Application para Detected Floor Plan Document. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/DetectedOpeningCandidate.cs` — Mision: Modelo/puerto de Application para Detected Opening Candidate. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/DetectedOpeningExtraction.cs` — Mision: Modelo/puerto de Application para Detected Opening Extraction. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/DetectedOpeningLabel.cs` — Mision: Modelo/puerto de Application para Detected Opening Label. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/DetectedProtectedDetailAssembly.cs` — Mision: Modelo/puerto de Application para Detected Protected Detail Assembly. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/DetectedRoomLabel.cs` — Mision: Modelo/puerto de Application para Detected Room Label. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/DetectedWallCandidate.cs` — Mision: Modelo/puerto de Application para Detected Wall Candidate. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/FloorPlanExtractionSource.cs` — Mision: Modelo/puerto de Application para Floor Plan Extraction Source. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/GeometryPoint.cs` — Mision: Modelo/puerto de Application para Geometry Point. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IClock.cs` — Mision: Modelo/puerto de Application para IClock. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IDxfGateway.cs` — Mision: Modelo/puerto de Application para IDxf Gateway. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IExtractedFixedPlanComponentRepository.cs` — Mision: Modelo/puerto de Application para IExtracted Fixed Plan Component Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IExtractedOpeningCandidateRepository.cs` — Mision: Modelo/puerto de Application para IExtracted Opening Candidate Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IExtractedOpeningLabelRepository.cs` — Mision: Modelo/puerto de Application para IExtracted Opening Label Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IExtractedProtectedDetailAssemblyRepository.cs` — Mision: Modelo/puerto de Application para IExtracted Protected Detail Assembly Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IExtractedRoomLabelRepository.cs` — Mision: Modelo/puerto de Application para IExtracted Room Label Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IExtractedWallCandidateRepository.cs` — Mision: Modelo/puerto de Application para IExtracted Wall Candidate Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IFileHashService.cs` — Mision: Modelo/puerto de Application para IFile Hash Service. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IFixedPlanComponentExtractor.cs` — Mision: Modelo/puerto de Application para IFixed Plan Component Extractor. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanCurationRepository.cs` — Mision: Modelo/puerto de Application para IFloor Plan Curation Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanExtractionSourceReader.cs` — Mision: Modelo/puerto de Application para IFloor Plan Extraction Source Reader. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanLibraryReader.cs` — Mision: Modelo/puerto de Application para IFloor Plan Library Reader. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanReviewSessionReader.cs` — Mision: Modelo/puerto de Application para IFloor Plan Review Session Reader. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanTemplateRepository.cs` — Mision: Modelo/puerto de Application para IFloor Plan Template Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IFloorPlanVersionRepository.cs` — Mision: Modelo/puerto de Application para IFloor Plan Version Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IImportedDocumentRepository.cs` — Mision: Modelo/puerto de Application para IImported Document Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IManagedFileStorage.cs` — Mision: Modelo/puerto de Application para IManaged File Storage. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IMeasurementContextRepository.cs` — Mision: Modelo/puerto de Application para IMeasurement Context Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IOpeningExtractor.cs` — Mision: Modelo/puerto de Application para IOpening Extractor. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IPinchGroupRepository.cs` — Mision: Modelo/puerto de Application para IPinch Group Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IPinchMarkerRepository.cs` — Mision: Modelo/puerto de Application para IPinch Marker Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IProtectedDetailAssemblyExtractor.cs` — Mision: Modelo/puerto de Application para IProtected Detail Assembly Extractor. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IRoomLabelExtractor.cs` — Mision: Modelo/puerto de Application para IRoom Label Extractor. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IUnitOfWork.cs` — Mision: Modelo/puerto de Application para IUnit Of Work. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IWallExtractionRunRepository.cs` — Mision: Modelo/puerto de Application para IWall Extraction Run Repository. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.

- `src/FloorplanFit.Application/Abstractions/IWallExtractor.cs` — Mision: Modelo/puerto de Application para IWall Extractor. Importancia: mantiene casos de uso desacoplados de Infrastructure/Desktop. Use case: transportar o persistir artefactos detectados sin filtrar detalles concretos.


## Casos de uso de curado Loop 1

Acciones humanas persistidas: pinches, rechazo, remocion de falsos positivos y publish.

- `src/FloorplanFit.Application/FloorPlans/Curation/AddPinchGroupHandler.cs` — Mision: Caso de uso o helper de Application para Add Pinch Group Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/AddPinchMarkerHandler.cs` — Mision: Caso de uso o helper de Application para Add Pinch Marker Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/PublishFloorPlanCurationHandler.cs` — Mision: Caso de uso o helper de Application para Publish Floor Plan Curation Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/RejectWallCandidateHandler.cs` — Mision: Caso de uso o helper de Application para Reject Wall Candidate Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/RemoveFixedPlanComponentHandler.cs` — Mision: Caso de uso o helper de Application para Remove Fixed Plan Component Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/RemoveOpeningCandidateHandler.cs` — Mision: Caso de uso o helper de Application para Remove Opening Candidate Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/RemoveOpeningLabelHandler.cs` — Mision: Caso de uso o helper de Application para Remove Opening Label Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/RemovePinchMarkerHandler.cs` — Mision: Caso de uso o helper de Application para Remove Pinch Marker Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/RemoveProtectedDetailAssemblyHandler.cs` — Mision: Caso de uso o helper de Application para Remove Protected Detail Assembly Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Curation/StartOrResumeCurationHandler.cs` — Mision: Caso de uso o helper de Application para Start Or Resume Curation Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.


## Casos de uso de extraccion automatica

Orquestacion de DXF -> extraction run -> artefactos persistidos.

- `src/FloorplanFit.Application/FloorPlans/Extraction/ExtractWallCandidatesHandler.cs` — Mision: Orquesta la extraction run completa: walls, rooms, openings, fixed components y protected details. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.


## Casos de uso de importacion

Entrada de DXF a Library local.

- `src/FloorplanFit.Application/FloorPlans/Import/FloorPlanCodeNormalizer.cs` — Mision: Caso de uso o helper de Application para Floor Plan Code Normalizer. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanHandler.cs` — Mision: Caso de uso o helper de Application para Import Floor Plan Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Import/ImportFloorPlanResultFactory.cs` — Mision: Caso de uso o helper de Application para Import Floor Plan Result Factory. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.


## Casos de uso de Library

Lecturas para la pantalla principal.

- `src/FloorplanFit.Application/FloorPlans/Library/GetFloorPlanLibraryHandler.cs` — Mision: Caso de uso o helper de Application para Get Floor Plan Library Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.


## Casos de uso de review

Apertura e hidratacion de la sesion de curado.

- `src/FloorplanFit.Application/FloorPlans/Review/GetFloorPlanReviewSessionHandler.cs` — Mision: Caso de uso o helper de Application para Get Floor Plan Review Session Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.

- `src/FloorplanFit.Application/FloorPlans/Review/OpenFloorPlanReviewSessionHandler.cs` — Mision: Caso de uso o helper de Application para Open Floor Plan Review Session Handler. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.


## Proyecto Application

Proyecto .NET de puertos y casos de uso.

- `src/FloorplanFit.Application/FloorplanFit.Application.csproj` — Mision: Caso de uso o helper de Application para Floorplan Fit.Application. Importancia: concentra orquestacion de producto fuera de UI y SQLite. Use case: ejecutar acciones de Loop 1 desde Desktop/tests.


## DTOs de Contracts

Datos planos entre Application/Infrastructure/Desktop.

- `src/FloorplanFit.Contracts/FloorPlans/FixedPlanComponentDto.cs` — Mision: DTO/contrato de Fixed Plan Component Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanLibraryItemDto.cs` — Mision: DTO/contrato de Floor Plan Library Item Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/FloorPlanReviewSessionDto.cs` — Mision: DTO/contrato de Floor Plan Review Session Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/GeometryPathDto.cs` — Mision: DTO/contrato de Geometry Path Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/GeometrySegmentDto.cs` — Mision: DTO/contrato de Geometry Segment Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanRequest.cs` — Mision: DTO/contrato de Import Floor Plan Request. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/ImportFloorPlanResponse.cs` — Mision: DTO/contrato de Import Floor Plan Response. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/OpenFloorPlanReviewSessionResponse.cs` — Mision: DTO/contrato de Open Floor Plan Review Session Response. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/OpeningCandidateDto.cs` — Mision: DTO/contrato de Opening Candidate Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/OpeningLabelDto.cs` — Mision: DTO/contrato de Opening Label Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/PinchGroupDto.cs` — Mision: DTO/contrato de Pinch Group Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/PinchMarkerDto.cs` — Mision: DTO/contrato de Pinch Marker Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/ProtectedDetailAssemblyDto.cs` — Mision: DTO/contrato de Protected Detail Assembly Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/RoomLabelDto.cs` — Mision: DTO/contrato de Room Label Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.

- `src/FloorplanFit.Contracts/FloorPlans/WallCandidateDto.cs` — Mision: DTO/contrato de Wall Candidate Dto. Importancia: cruza boundaries con datos planos. Use case: renderizar, seleccionar o actualizar estado en Review/Library.


## Proyecto Contracts

Proyecto .NET de contratos compartidos.

- `src/FloorplanFit.Contracts/FloorplanFit.Contracts.csproj` — Mision: Archivo de gobierno/proyecto Floorplan Fit.Contracts. Importancia: mantiene la solucion operable. Use case: configurar, abrir o entender el repo.


## Composicion DI del desktop

Wiring de handlers, repos, extractores y viewmodels.

- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs` — Mision: Pieza Desktop/Avalonia para Desktop Service Registration. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.


## Controles visuales custom

Base del canvas/geometry de Review.

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` — Mision: Shell interactivo del preview: zoom, pan, seleccion, hit-test y delegacion a renderers. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs` — Mision: Pieza Desktop/Avalonia para Floor Plan Preview Geometry. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.


## Renderers modulares del preview CAD-faithful

Capas chicas de dibujo/hit-test; evitan un mega-renderer.

- `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs` — Mision: Pieza Desktop/Avalonia para Cad Text Preview Layer Renderer. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/Controls/Preview/CompressionHandlePreviewLayerRenderer.cs` — Mision: Pieza Desktop/Avalonia para Compression Handle Preview Layer Renderer. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/Controls/Preview/FixedPlanComponentPreviewLayerRenderer.cs` — Mision: Pieza Desktop/Avalonia para Fixed Plan Component Preview Layer Renderer. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/Controls/Preview/OpeningPreviewLayerRenderer.cs` — Mision: Pieza Desktop/Avalonia para Opening Preview Layer Renderer. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/Controls/Preview/PinchMarkerPreviewLayerRenderer.cs` — Mision: Pieza Desktop/Avalonia para Pinch Marker Preview Layer Renderer. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewArtifactGeometryIndex.cs` — Mision: Indice de hit-test con prioridad entre protected details, fixed components, openings y walls. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewWorkspaceRenderer.cs` — Mision: Pieza Desktop/Avalonia para Preview Workspace Renderer. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/Controls/Preview/ProtectedDetailPreviewLayerRenderer.cs` — Mision: Pieza Desktop/Avalonia para Protected Detail Preview Layer Renderer. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.


## ViewModels MVVM

Estado y comandos de UI.

- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` — Mision: Pieza Desktop/Avalonia para Floor Plan Review View Model. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs` — Mision: Pieza Desktop/Avalonia para Library View Model. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.


## Proyecto Avalonia desktop

Vistas, entry point, code-behind minimo y metadata.

- `src/FloorplanFit.Desktop/App.axaml` — Mision: Pieza Desktop/Avalonia para App. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/App.axaml.cs` — Mision: Pieza Desktop/Avalonia para App.axaml. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/FloorplanFit.Desktop.csproj` — Mision: Pieza Desktop/Avalonia para Floorplan Fit.Desktop. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/MainWindow.axaml` — Mision: Pieza Desktop/Avalonia para Main Window. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/MainWindow.axaml.cs` — Mision: Pieza Desktop/Avalonia para Main Window.axaml. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/Program.cs` — Mision: Pieza Desktop/Avalonia para Program. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/Properties/AssemblyInfo.cs` — Mision: Pieza Desktop/Avalonia para Assembly Info. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml` — Mision: Pieza Desktop/Avalonia para Review Floor Plan Window. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.

- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs` — Mision: Pieza Desktop/Avalonia para Review Floor Plan Window.axaml. Importancia: sostiene la experiencia de curado visual. Use case: operar Review, Library, preview, zoom/pan o paneles de artefactos.


## Entidades de documentos

Dominio de documentos importados.

- `src/FloorplanFit.Domain/Documents/ImportedDocument.cs` — Mision: Entidad/valor de dominio para Imported Document. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/Documents/ImportedDocumentType.cs` — Mision: Entidad/valor de dominio para Imported Document Type. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.


## Entidades y enums de floor plans

Verdad de negocio de templates, curations, extraction runs y artefactos curables.

- `src/FloorplanFit.Domain/FloorPlans/ConstraintIntentNote.cs` — Mision: Entidad/valor de dominio para Constraint Intent Note. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ConstraintKind.cs` — Mision: Entidad/valor de dominio para Constraint Kind. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ConstraintStrength.cs` — Mision: Entidad/valor de dominio para Constraint Strength. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/CuratedSpace.cs` — Mision: Entidad/valor de dominio para Curated Space. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/CuratedWallGroup.cs` — Mision: Entidad/valor de dominio para Curated Wall Group. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/CuratedWallJoin.cs` — Mision: Entidad/valor de dominio para Curated Wall Join. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedFixedPlanComponent.cs` — Mision: Entidad/valor de dominio para Extracted Fixed Plan Component. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedOpeningCandidate.cs` — Mision: Entidad/valor de dominio para Extracted Opening Candidate. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedOpeningLabel.cs` — Mision: Entidad/valor de dominio para Extracted Opening Label. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedProtectedDetailAssembly.cs` — Mision: Entidad/valor de dominio para Extracted Protected Detail Assembly. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedRoomLabel.cs` — Mision: Entidad/valor de dominio para Extracted Room Label. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidate.cs` — Mision: Entidad/valor de dominio para Extracted Wall Candidate. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/ExtractedWallCandidateStatus.cs` — Mision: Entidad/valor de dominio para Extracted Wall Candidate Status. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/FloorPlanCuration.cs` — Mision: Entidad/valor de dominio para Floor Plan Curation. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/FloorPlanCurationStatus.cs` — Mision: Entidad/valor de dominio para Floor Plan Curation Status. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/FloorPlanTemplate.cs` — Mision: Entidad/valor de dominio para Floor Plan Template. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/FloorPlanVersion.cs` — Mision: Entidad/valor de dominio para Floor Plan Version. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/PinchAxisTag.cs` — Mision: Entidad/valor de dominio para Pinch Axis Tag. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/PinchGroup.cs` — Mision: Entidad/valor de dominio para Pinch Group. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/PinchMarker.cs` — Mision: Entidad/valor de dominio para Pinch Marker. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/SpaceType.cs` — Mision: Entidad/valor de dominio para Space Type. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/StructuredConstraint.cs` — Mision: Entidad/valor de dominio para Structured Constraint. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/WallExtractionRun.cs` — Mision: Entidad/valor de dominio para Wall Extraction Run. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/WallMobilityLevel.cs` — Mision: Entidad/valor de dominio para Wall Mobility Level. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/WallProtectionLevel.cs` — Mision: Entidad/valor de dominio para Wall Protection Level. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/FloorPlans/WallRole.cs` — Mision: Entidad/valor de dominio para Wall Role. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.


## Valores de medicion y unidades

Unidades y escala real 1:1.

- `src/FloorplanFit.Domain/Measurement/LengthUnit.cs` — Mision: Entidad/valor de dominio para Length Unit. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.

- `src/FloorplanFit.Domain/Measurement/MeasurementContext.cs` — Mision: Entidad/valor de dominio para Measurement Context. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.


## Proyecto Domain

Nucleo puro de negocio.

- `src/FloorplanFit.Domain/FloorplanFit.Domain.csproj` — Mision: Entidad/valor de dominio para Floorplan Fit.Domain. Importancia: expresa verdad de negocio independiente de UI/SQLite. Use case: persistir y razonar sobre floor plans, curations, pinches o medicion.


## Adaptadores DXF

IxMilia + profile/conventions que traducen CAD real a modelos detectados.

- `src/FloorplanFit.Infrastructure/Dxf/DxfExtractionProfile.cs` — Mision: Centraliza convenciones CAD debiles de Pointe Homes para no hardcodearlas en extractores. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDxfGateway.cs` — Mision: Adaptador DXF para Ix Milia Dxf Gateway. Importancia: traduce CAD real a modelos de Application. Use case: extraer layers, bloques, labels, colores y geometry paths desde IxMilia.

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaFixedPlanComponentExtractor.cs` — Mision: Adaptador DXF para Ix Milia Fixed Plan Component Extractor. Importancia: traduce CAD real a modelos de Application. Use case: extraer layers, bloques, labels, colores y geometry paths desde IxMilia.

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaOpeningExtractor.cs` — Mision: Adaptador DXF para Ix Milia Opening Extractor. Importancia: traduce CAD real a modelos de Application. Use case: extraer layers, bloques, labels, colores y geometry paths desde IxMilia.

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaProtectedDetailAssemblyExtractor.cs` — Mision: Adaptador DXF para Ix Milia Protected Detail Assembly Extractor. Importancia: traduce CAD real a modelos de Application. Use case: extraer layers, bloques, labels, colores y geometry paths desde IxMilia.

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaRoomLabelExtractor.cs` — Mision: Adaptador DXF para Ix Milia Room Label Extractor. Importancia: traduce CAD real a modelos de Application. Use case: extraer layers, bloques, labels, colores y geometry paths desde IxMilia.

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaWallExtractor.cs` — Mision: Adaptador DXF para Ix Milia Wall Extractor. Importancia: traduce CAD real a modelos de Application. Use case: extraer layers, bloques, labels, colores y geometry paths desde IxMilia.


## Persistencia SQLite y read-models

Repositorios, schema y readers locales.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedFixedPlanComponentRepository.cs` — Mision: Pieza SQLite para Sqlite Extracted Fixed Plan Component Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedOpeningCandidateRepository.cs` — Mision: Pieza SQLite para Sqlite Extracted Opening Candidate Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedOpeningLabelRepository.cs` — Mision: Pieza SQLite para Sqlite Extracted Opening Label Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedProtectedDetailAssemblyRepository.cs` — Mision: Pieza SQLite para Sqlite Extracted Protected Detail Assembly Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedRoomLabelRepository.cs` — Mision: Pieza SQLite para Sqlite Extracted Room Label Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedWallCandidateRepository.cs` — Mision: Pieza SQLite para Sqlite Extracted Wall Candidate Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanCurationRepository.cs` — Mision: Pieza SQLite para Sqlite Floor Plan Curation Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanExtractionSourceReader.cs` — Mision: Pieza SQLite para Sqlite Floor Plan Extraction Source Reader. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanLibraryReader.cs` — Mision: Pieza SQLite para Sqlite Floor Plan Library Reader. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs` — Mision: Hidrata la review completa desde SQLite. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanTemplateRepository.cs` — Mision: Pieza SQLite para Sqlite Floor Plan Template Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanVersionRepository.cs` — Mision: Pieza SQLite para Sqlite Floor Plan Version Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteImportedDocumentRepository.cs` — Mision: Pieza SQLite para Sqlite Imported Document Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementContextRepository.cs` — Mision: Pieza SQLite para Sqlite Measurement Context Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchGroupRepository.cs` — Mision: Pieza SQLite para Sqlite Pinch Group Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqlitePinchMarkerRepository.cs` — Mision: Pieza SQLite para Sqlite Pinch Marker Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteSchemaInitializer.cs` — Mision: Crea y migra el schema SQLite de Library, curado y artefactos CAD. Importancia: es pieza estructural de la arquitectura actual. Use case: mantener Loop 1 curable y auditable.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteSession.cs` — Mision: Pieza SQLite para Sqlite Session. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteUnitOfWork.cs` — Mision: Pieza SQLite para Sqlite Unit Of Work. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.

- `src/FloorplanFit.Infrastructure/Persistence/SqliteWallExtractionRunRepository.cs` — Mision: Pieza SQLite para Sqlite Wall Extraction Run Repository. Importancia: persiste/lee la verdad local-first. Use case: reabrir Library/Review con estado, geometry paths y removals consistentes.


## Workspace y runtime local

Carpetas locales gestionadas.

- `src/FloorplanFit.Infrastructure/Runtime/AppWorkspace.cs` — Mision: Adaptador tecnico para App Workspace. Importancia: implementa capacidades reales detras de puertos. Use case: operar filesystem, runtime, hashing o storage gestionado.


## Servicios de seguridad tecnica

Hashing e integridad tecnica.

- `src/FloorplanFit.Infrastructure/Security/Sha256FileHashService.cs` — Mision: Adaptador tecnico para Sha256 File Hash Service. Importancia: implementa capacidades reales detras de puertos. Use case: operar filesystem, runtime, hashing o storage gestionado.


## Storage gestionado

Copia controlada de DXF importados.

- `src/FloorplanFit.Infrastructure/Storage/ManagedFileStorage.cs` — Mision: Adaptador tecnico para Managed File Storage. Importancia: implementa capacidades reales detras de puertos. Use case: operar filesystem, runtime, hashing o storage gestionado.


## Proyecto Infrastructure

Proyecto .NET de adaptadores reales.

- `src/FloorplanFit.Infrastructure/FloorplanFit.Infrastructure.csproj` — Mision: Adaptador tecnico para Floorplan Fit.Infrastructure. Importancia: implementa capacidades reales detras de puertos. Use case: operar filesystem, runtime, hashing o storage gestionado.

- `src/FloorplanFit.Infrastructure/InfrastructureAssemblyMarker.cs` — Mision: Adaptador tecnico para Infrastructure Assembly Marker. Importancia: implementa capacidades reales detras de puertos. Use case: operar filesystem, runtime, hashing o storage gestionado.


## Tests de Application

Evidencia de casos de uso y reglas de curado.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AddPinchGroupHandlerTests.cs` — Mision: Test/soporte de Add Pinch Group Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/AddPinchMarkerHandlerTests.cs` — Mision: Test/soporte de Add Pinch Marker Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanCurationTests.cs` — Mision: Test/soporte de Floor Plan Curation Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/PublishFloorPlanCurationHandlerTests.cs` — Mision: Test/soporte de Publish Floor Plan Curation Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RejectWallCandidateHandlerTests.cs` — Mision: Test/soporte de Reject Wall Candidate Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/RemoveOpeningArtifactHandlerTests.cs` — Mision: Test/soporte de Remove Opening Artifact Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/StartOrResumeCurationHandlerTests.cs` — Mision: Test/soporte de Start Or Resume Curation Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Extraction/ExtractWallCandidatesHandlerTests.cs` — Mision: Test/soporte de Extract Wall Candidates Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Import/ImportFloorPlanHandlerTests.cs` — Mision: Test/soporte de Import Floor Plan Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Library/GetFloorPlanLibraryHandlerTests.cs` — Mision: Test/soporte de Get Floor Plan Library Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/GetFloorPlanReviewSessionHandlerTests.cs` — Mision: Test/soporte de Get Floor Plan Review Session Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/OpenFloorPlanReviewSessionHandlerTests.cs` — Mision: Test/soporte de Open Floor Plan Review Session Handler Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/WallCandidateDtoTests.cs` — Mision: Test/soporte de Wall Candidate Dto Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj` — Mision: Test/soporte de Floorplan Fit.Application.Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Application.Tests/GlobalUsings.cs` — Mision: Test/soporte de Global Usings. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.


## Tests de Desktop

Evidencia de DI, VM, preview, layout y XAML.

- `tests/FloorplanFit.Desktop.Tests/Composition/DesktopServiceRegistrationTests.cs` — Mision: Test/soporte de Desktop Service Registration Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs` — Mision: Test/soporte de Floor Plan Preview Control Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs` — Mision: Test/soporte de Floor Plan Preview Geometry Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj` — Mision: Test/soporte de Floorplan Fit.Desktop.Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Desktop.Tests/Layout/AppXamlInitializationTests.cs` — Mision: Test/soporte de App Xaml Initialization Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs` — Mision: Test/soporte de Review Floor Plan Window Layout Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs` — Mision: Test/soporte de Floor Plan Review View Model Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Desktop.Tests/ViewModels/LibraryViewModelTests.cs` — Mision: Test/soporte de Library View Model Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.


## Tests de Infrastructure

Evidencia de SQLite, DXF, extraction profile y read-models.

- `tests/FloorplanFit.Infrastructure.Tests/Curation/FloorPlanCurationPersistenceIntegrationTests.cs` — Mision: Test/soporte de Floor Plan Curation Persistence Integration Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaDxfGatewayTests.cs` — Mision: Test/soporte de Ix Milia Dxf Gateway Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Extraction/DxfExtractionProfileTests.cs` — Mision: Test/soporte de Dxf Extraction Profile Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaFixedPlanComponentExtractorTests.cs` — Mision: Test/soporte de Ix Milia Fixed Plan Component Extractor Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaOpeningExtractorTests.cs` — Mision: Test/soporte de Ix Milia Opening Extractor Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaProtectedDetailAssemblyExtractorTests.cs` — Mision: Test/soporte de Ix Milia Protected Detail Assembly Extractor Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaRoomLabelExtractorTests.cs` — Mision: Test/soporte de Ix Milia Room Label Extractor Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Extraction/IxMiliaWallExtractorTests.cs` — Mision: Test/soporte de Ix Milia Wall Extractor Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj` — Mision: Test/soporte de Floorplan Fit.Infrastructure.Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/GlobalUsings.cs` — Mision: Test/soporte de Global Usings. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Imports/FloorPlanLibraryReaderIntegrationTests.cs` — Mision: Test/soporte de Floor Plan Library Reader Integration Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Imports/ImportFloorPlanIntegrationTests.cs` — Mision: Test/soporte de Import Floor Plan Integration Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Imports/ManagedFileStorageTests.cs` — Mision: Test/soporte de Managed File Storage Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Library/FloorPlanExtractionSourceReaderIntegrationTests.cs` — Mision: Test/soporte de Floor Plan Extraction Source Reader Integration Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Review/FloorPlanReviewSessionReaderIntegrationTests.cs` — Mision: Test/soporte de Floor Plan Review Session Reader Integration Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/Review/OpenFloorPlanReviewSessionIntegrationTests.cs` — Mision: Test/soporte de Open Floor Plan Review Session Integration Tests. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.

- `tests/FloorplanFit.Infrastructure.Tests/TestSupport/RepositoryPaths.cs` — Mision: Test/soporte de Repository Paths. Importancia: convierte comportamiento esperado en evidencia automatizada. Use case: detectar regresiones antes de probar manualmente.


## Verificacion de cobertura

- Conteo total cubierto: **323**
- Base fisica actual: **302 archivos versionados presentes**
- Nuevos relevantes no versionados todavia: **21**
- No lista archivos borrados del flujo viejo de curated walls; esa ausencia es intencional en esta branch pinch-native.
- Regla de mantenimiento: toda familia CAD nueva debe tener extractor, detected model, domain/persistence, DTO, preview layer, accion de curado y tests.

## Que NO cubre este mapa

- No cubre caches, binarios ni salidas temporales.
- No reemplaza `MVP-UX.md` ni `TECH-STACK-ARCHITECTURE-DATAFLOW.md`; los complementa con inventario operacional actual.

## Como usar este documento

- Para saber donde tocar algo, busca su familia y segui el pipeline.
- Para agregar dimensiones, no las metas como labels sueltas: deben tener lifecycle completo.
- Para reglas especificas de Pointe Homes, primero revisa `DxfExtractionProfile` antes de hardcodear en un extractor.
