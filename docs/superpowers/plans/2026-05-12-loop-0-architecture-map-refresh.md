# Loop 0 Architecture Map Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refresh the canonical architecture map and its supporting Obsidian notes so the repository truth matches the post-native-dimension working tree before deeper code modularization begins.

**Architecture:** Treat Loop 0 as a documentation-first architecture slice. The main artifact is `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`; supporting Obsidian notes record the decision, the drift audit, and the refreshed current truth. No production behavior changes belong in this loop.

**Execution note (verified during RED checks):** the map drift is larger than first estimated. The refresh must restore exhaustiveness for the whole post-2026-05-09 wave, not just a handful of native-dimension files.

**Tech Stack:** Markdown, PowerShell, git, Obsidian notes, existing repository docs conventions.

---

## File Structure

- Modify: `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`
  - Purpose: canonical architecture map; must become truthful for the current working tree.
- Modify: `obsidian-vault/Current State.md`
  - Purpose: top-level current truth; should point at Loop 0 as the immediate architectural next step and record the looped modularization decision.
- Modify: `obsidian-vault/Implementation/2026-05-12 - Architecture map drift after native dimension milestone.md`
  - Purpose: keep the drift audit, but mark it superseded once the refreshed map note exists.
- Create: `obsidian-vault/Implementation/2026-05-12 - Architecture map refreshed after native dimension milestone.md`
  - Purpose: record what the Loop 0 refresh actually changed and why it matters.

---

## Tasks

### Task 1: Prove the canonical map is stale, then refresh its metadata and architectural summary

**Files:**
- Modify: `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`

- [ ] **Step 1: Run the RED verification search against the map**

Run:

```powershell
Select-String -Path ".\docs\explicacion de toda la app\2026-04-30 - mapa completo de arquitectura y archivos.md" `
  -Pattern "last_verified: 2026-05-09|file_count: 323|Las dimensiones CAD siguen pendientes"
```

Expected:

- one match for `last_verified: 2026-05-09`
- one match for `file_count: 323`
- one match for `Las dimensiones CAD siguen pendientes`

This proves the map is stale before we edit it.

- [ ] **Step 2: Recompute the working-tree coverage numbers**

Run:

```powershell
$trackedPresent = git ls-files |
  Where-Object { Test-Path $_ } |
  Where-Object { $_ -notmatch '^(bin|obj|\.artifacts-test|\.git|\.vs|workspace)/' } |
  Measure-Object | Select-Object -ExpandProperty Count

$untrackedRelevant = git ls-files --others --exclude-standard |
  Where-Object { $_ -notmatch '^(bin|obj|\.artifacts-test|\.git|\.vs|workspace)/' } |
  Measure-Object | Select-Object -ExpandProperty Count

"tracked_present=$trackedPresent"
"untracked_relevant=$untrackedRelevant"
"total_relevant=$($trackedPresent + $untrackedRelevant)"
```

Expected:

```text
tracked_present=452
untracked_relevant=0
total_relevant=452
```

- [ ] **Step 3: Update the map frontmatter and top summary with the fresh truth**

Replace the current top block with:

```markdown
---
type: architecture-map
date: 2026-04-30
last_verified: 2026-05-12
status: active
scope: current-working-tree-files
file_count: 452
---

# Mapa completo de arquitectura y archivos de Floorplan Fit

## Proposito del documento

Este documento es el mapa exhaustivo del repositorio. Explica que parte de la arquitectura toca cada archivo, que responsabilidad tiene, por que importa y como encaja en el producto.

Cobertura revalidada el **2026-05-12** contra `git ls-files` y `git ls-files --others --exclude-standard`: **452 archivos relevantes presentes** = **452 versionados presentes** + **0 nuevos no versionados**. Se excluyen `bin/`, `obj/`, `.artifacts-test/`, `.git/`, `.vs/`, `workspace/` y archivos locales de usuario.

## Actualizacion 2026-05-12

- El milestone de **native dimensions** ya es parte activa de la arquitectura real; no sigue pendiente.
- Loop 1 ahora ya incluye extraccion, persistencia, preview fiel, override/editing groundwork y export `adjusted DXF` para dimensiones nativas.
- `FloorPlanPreviewControl` y `FloorPlanReviewViewModel` siguen siendo hotspots grandes, por eso el repo entra en un programa de modularizacion por loops.
- El orden activo del saneamiento es: **Loop 0 verdad canonica -> Loop 1 sistema visual -> Loop 2 preview composition/interactions -> Loop 3 review orchestration -> Loop 4 cleanup final**.
```

- [ ] **Step 4: Replace the stale dimensions-pending sentence in the 2026-05-08 update**

Replace this old sentence:

```markdown
- Las dimensiones CAD siguen pendientes, pero deben entrar como familia propia de artifacts: geometry + text + anchors + valor original/recalculable durante pinches/adaptacion.
```

With:

```markdown
- Las dimensiones CAD ya entraron como familia propia: extraccion DXF nativa, `DimensionDto`, `extracted_dimensions`, line segments/primitives persistidos, measurement context expuesto, preview fiel, editor nativo, asociatividad reactiva parcial y export `adjusted DXF`.
```

- [ ] **Step 5: Run the GREEN verification search against the map**

Run:

```powershell
Select-String -Path ".\docs\explicacion de toda la app\2026-04-30 - mapa completo de arquitectura y archivos.md" `
  -Pattern "last_verified: 2026-05-12|file_count: 452|Las dimensiones CAD ya entraron como familia propia"
```

Expected:

- one match for `last_verified: 2026-05-12`
- one match for `file_count: 452`
- one match for `Las dimensiones CAD ya entraron como familia propia`

---

### Task 2: Add the missing native-dimension, measurement, and adjusted-DXF file entries to the canonical map

**Files:**
- Modify: `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`

- [ ] **Step 1: Run the RED search proving the map is missing key native-dimension files**

Run:

```powershell
Select-String -Path ".\docs\explicacion de toda la app\2026-04-30 - mapa completo de arquitectura y archivos.md" `
  -Pattern "IxMiliaAdjustedDxfExporter.cs|SaveFloorPlanDimensionOverrideHandler.cs|SqliteFloorPlanDimensionOverrideRepository.cs|NativeDimensionEditor.cs|ReactiveDimensionProjector.cs|MeasurementContextDto.cs"
```

Expected: **no matches**.

- [ ] **Step 2: Add the Application/Contracts/Domain entries for the dimension family**

Under the relevant Application, Contracts, and Domain sections, add these exact lines:

```markdown
- `src/FloorplanFit.Application/Abstractions/DetectedDimension.cs` - Mision: contrato detectado de una dimension nativa extraida desde DXF. Importancia: separa extraccion CAD de persistencia/domain truth. Use case: transportar measurement, texto y anchors base desde Infrastructure hacia Application.
- `src/FloorplanFit.Application/Abstractions/DetectedDimensionLineSegment.cs` - Mision: contrato detectado para segmentos de linea de la geometria visible de una cota. Importancia: permite preview fiel y round-trip sin degradar a texto solo. Use case: persistir extension lines y dimension lines del bloque `*D...`.
- `src/FloorplanFit.Application/Abstractions/DetectedDimensionPrimitives.cs` - Mision: contrato detectado para primitives canonicos de una dimension (`LINE`, `TEXT/MTEXT`, `INSERT`, `CIRCLE`, `ARC`, `SOLID`). Importancia: sostiene la fidelidad CAD y la futura edicion exacta. Use case: conservar endpoint dots, texto y primitives del bloque renderizado.
- `src/FloorplanFit.Application/Abstractions/IAdjustedDxfExporter.cs` - Mision: puerto Application para exportar un DXF ajustado gestionado. Importancia: evita acoplar casos de uso a IxMilia directo. Use case: sacar un `ExportedAdjustedDxf` desde overrides curados.
- `src/FloorplanFit.Application/Abstractions/IDimensionExtractor.cs` - Mision: puerto Application para leer dimensiones nativas desde floor plans. Importancia: vuelve testeable la familia de dimensiones. Use case: inyectar extractor real o fake en la corrida de extraccion.
- `src/FloorplanFit.Application/Abstractions/IExtractedDimensionRepository.cs` - Mision: persistir dimensiones extraidas dentro de una extraction run. Importancia: da lifecycle propio a la familia de cotas. Use case: guardar `extracted_dimensions` sin mezclarlo con room labels u openings.
- `src/FloorplanFit.Application/Abstractions/IFloorPlanDimensionOverrideRepository.cs` - Mision: persistir snapshots curados de edicion de dimensiones nativas. Importancia: separa DXF importado de verdad curada editable. Use case: guardar, restaurar y cargar overrides de cotas en Review.
- `src/FloorplanFit.Application/FloorPlans/Curation/ExportAdjustedDxfHandler.cs` - Mision: caso de uso para exportar un nuevo DXF ajustado desde la curation. Importancia: conecta edicion CAD-faithful con un artefacto reutilizable real. Use case: emitir `ExportedAdjustedDxf` sin mutar el import original.
- `src/FloorplanFit.Application/FloorPlans/Curation/ExportAdjustedDxfResponse.cs` - Mision: respuesta plana del caso de uso de export ajustado. Importancia: desacopla Desktop del detalle de storage/export concreto. Use case: devolver ruta/metadata del DXF exportado al flujo de Review.
- `src/FloorplanFit.Application/FloorPlans/Curation/RestoreFloorPlanDimensionOverrideHandler.cs` - Mision: restaurar una cota nativa a su snapshot extraido. Importancia: da recovery seguro para edicion manual. Use case: descartar una edicion destructiva o volver al CAD authored.
- `src/FloorplanFit.Application/FloorPlans/Curation/SaveFloorPlanDimensionOverrideHandler.cs` - Mision: guardar overrides de edicion de cotas nativas. Importancia: convierte la manipulacion visual en verdad curada persistida. Use case: persistir cambios del editor nativo antes de exportar o reabrir Review.
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionAssociationProjector.cs` - Mision: proyectar asociaciones inferidas entre cotas y measurable edges. Importancia: habilita reactividad futura sin contaminar el import original. Use case: recalcular cotas no editadas cuando cambia la geometria preview.
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionGeometryProjector.cs` - Mision: reconstruir geometria visible de cotas desde anchors y primitives. Importancia: unifica la logica entre reactividad y edicion manual. Use case: refrescar texto/linework de una cota en preview.
- `src/FloorplanFit.Application/FloorPlans/Review/MeasurableEdgeProjector.cs` - Mision: construir measurable edges compartidos desde walls/openings. Importancia: da base comun para asociatividad de dimensiones. Use case: indexar spans medibles para recalculo reactivo.
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs` - Mision: recalcular cotas asociadas a geometria viva de preview. Importancia: acerca la review al comportamiento CAD reactivo sin mutar snapshots base. Use case: actualizar measurement/texto cuando cambian edges medibles.
- `src/FloorplanFit.Contracts/FloorPlans/DimensionAnchorReferenceDto.cs` - Mision: DTO de referencia de anchor para una cota nativa. Importancia: expone asociaciones sin filtrar Infrastructure a Desktop directo. Use case: mostrar/usar anchors proyectados en preview y editor.
- `src/FloorplanFit.Contracts/FloorPlans/DimensionAssociationDto.cs` - Mision: DTO de asociacion inferida entre una cota y un edge medible. Importancia: hace visible la semantica reactiva en Review. Use case: decidir si una cota puede recalcularse o no.
- `src/FloorplanFit.Contracts/FloorPlans/DimensionDto.cs` - Mision: DTO principal de una dimension nativa en Review. Importancia: entrega texto, measurement, primitives y metadata a Desktop. Use case: renderizar, inspeccionar o editar una cota.
- `src/FloorplanFit.Contracts/FloorPlans/DimensionLineSegmentDto.cs` - Mision: DTO de segmento lineal visible de una cota. Importancia: sostiene preview fiel sin depender del midpoint heuristico. Use case: dibujar extension/dimension lines desde el bloque real.
- `src/FloorplanFit.Contracts/FloorPlans/DimensionPrimitiveDtos.cs` - Mision: DTOs de primitives canonicos de una cota nativa. Importancia: habilita fidelidad CAD y round-trip. Use case: representar lineas, texto, inserts, circulos, arcos y solids.
- `src/FloorplanFit.Contracts/FloorPlans/MeasurableEdgeDto.cs` - Mision: DTO de edge medible compartido por walls/openings. Importancia: da una identidad de medicion para preview reactivo. Use case: exponer spans candidatos para asociaciones de cotas.
- `src/FloorplanFit.Contracts/FloorPlans/MeasurementContextDto.cs` - Mision: DTO del contexto de unidades/tolerancias del documento importado. Importancia: hace confiable el grid world-space y la lectura de medidas. Use case: informar source units y tolerancias al preview/review.
- `src/FloorplanFit.Domain/FloorPlans/ExtractedDimension.cs` - Mision: entidad Domain persistida de una dimension nativa extraida. Importancia: le da lifecycle propio a la familia de cotas. Use case: guardar texto, measurement, layer y metadata base dentro de una extraction run.
- `src/FloorplanFit.Domain/FloorPlans/ExtractedDimensionLineSegment.cs` - Mision: value object Domain para segmentos visibles de una cota. Importancia: preserva linework authored separado del resto de artifacts. Use case: persistir extension/dimension lines por dimension.
- `src/FloorplanFit.Domain/FloorPlans/ExtractedDimensionPrimitives.cs` - Mision: value object Domain para primitives visibles de una cota. Importancia: sostiene CAD fidelity y export ajustado. Use case: conservar inserts, texto y simbolos terminales del bloque DXF.
- `src/FloorplanFit.Domain/FloorPlans/FloorPlanDimensionOverride.cs` - Mision: entidad Domain para snapshot curado de una cota editada. Importancia: separa import original de verdad curada editable. Use case: reabrir, restaurar y exportar una dimension ajustada.
```

- [ ] **Step 3: Add the Infrastructure/Desktop entries for the dimension family**

Under the relevant Infrastructure and Desktop sections, add these exact lines:

```markdown
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedDxfExporter.cs` - Mision: adaptador real IxMilia para escribir un DXF ajustado con cotas nativas y primitives authored. Importancia: cierra el round-trip CAD-faithful. Use case: exportar un documento nuevo que reabra con las dimensiones editadas.
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDimensionExtractor.cs` - Mision: extractor real de entidades `DIMENSION` y su bloque `*D...`. Importancia: convierte cotas nativas en una familia CAD propia y no en texto decorativo. Use case: leer measurement, texto visible, line segments y primitives del floor plan.
- `src/FloorplanFit.Infrastructure/Persistence/ResolvedFloorPlanDimensionProjector.cs` - Mision: resolver la verdad efectiva de una cota entre extraccion base y override curado. Importancia: evita que Desktop haga merge semantico por su cuenta. Use case: cargar Review con la version vigente de cada cota.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteExtractedDimensionRepository.cs` - Mision: persistir `extracted_dimensions` y payloads asociados en SQLite. Importancia: completa el lifecycle de la familia de cotas. Use case: guardar line segments, primitives y metadata de la extraction run.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanDimensionOverrideRepository.cs` - Mision: persistir snapshots de edicion de dimensiones nativas. Importancia: vuelve durable la edicion exacta y el export posterior. Use case: guardar/restaurar overrides por curation y dimension.
- `src/FloorplanFit.Infrastructure/Persistence/SqliteMeasurementContextRepository.cs` - Mision: persistir el contexto de unidades/tolerancias del documento. Importancia: evita inferencias ambiguas de measurement en Review. Use case: leer source unit, factor a mm y tolerancias desde SQLite.
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewLayerRenderer.cs` - Mision: renderer de linework y primitives visibles de cotas nativas. Importancia: mueve la fidelidad CAD fuera del mega-control. Use case: dibujar cotas con texto y lineas reales en el canvas.
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewProjector.cs` - Mision: proyectar cotas del DTO al espacio visible del preview. Importancia: separa transformaciones de render de la interaccion del control. Use case: preparar render plans de dimensiones y anchors.
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionEditor.cs` - Mision: coordinar la edicion manual de una cota nativa en preview. Importancia: encapsula handles, drag y rebuild sin inflar mas el control principal. Use case: mover endpoints/texto de una cota editada.
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionHitTester.cs` - Mision: hit-test especializado para seleccionar cotas nativas y sus handles. Importancia: evita mezclar reglas de dimension editing con walls/openings. Use case: detectar clicks y drags sobre linework/texto de una cota.
```

- [ ] **Step 4: Run the GREEN verification search for the newly mapped files**

Run:

```powershell
Select-String -Path ".\docs\explicacion de toda la app\2026-04-30 - mapa completo de arquitectura y archivos.md" `
  -Pattern "IxMiliaAdjustedDxfExporter.cs|SaveFloorPlanDimensionOverrideHandler.cs|SqliteFloorPlanDimensionOverrideRepository.cs|NativeDimensionEditor.cs|ReactiveDimensionProjector.cs|MeasurementContextDto.cs"
```

Expected: **matches for all six filenames**.

---

### Task 3: Refresh the Desktop hotspot descriptions and supporting Obsidian notes

**Files:**
- Modify: `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`
- Modify: `obsidian-vault/Current State.md`
- Modify: `obsidian-vault/Implementation/2026-05-12 - Architecture map drift after native dimension milestone.md`
- Create: `obsidian-vault/Implementation/2026-05-12 - Architecture map refreshed after native dimension milestone.md`

- [ ] **Step 1: Tighten the existing hotspot entries in the architecture map**

Update the existing map lines for these files to make the ownership clearer:

```markdown
- `src/FloorplanFit.Desktop/App.axaml` - Mision: concentrar el sistema visual base de la app Desktop (brushes, estilos y convenciones compartidas). Importancia: hoy es el punto mas visible de tokenizacion parcial y por eso prepara el Loop 1 visual. Use case: unificar background, paneles, estados visuales y reglas de estilo comunes.
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` - Mision: shell interactivo del preview para zoom, pan, seleccion y coordinacion de herramientas/renderers. Importancia: es hotspot estructural y principal candidato del Loop 2 de modularizacion. Use case: orquestar la superficie CAD-faithful sin volver a absorber toda la logica de dibujo.
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` - Mision: orquestar el flujo de Review, seleccion, acciones y pinch tools desde Desktop. Importancia: es hotspot estructural y principal candidato del Loop 3 de modularizacion. Use case: coordinar la pantalla de curado sin concentrar para siempre toda la logica de estado.
```

- [ ] **Step 2: Ensure `Current State.md` reflects Loop 0 as the first architectural next step**

Under `## Immediate Next Steps`, keep this line as item `0`:

```markdown
0. Ejecutar **Loop 0** del programa de modularizacion: refrescar el mapa canonico de arquitectura y alinear la documentacion con la realidad post-`native dimensions` antes de seguir partiendo codigo.
```

And keep the architectural decision bullet in Repository Truth:

```markdown
- Decision de arquitectura tomada el **2026-05-12**: la limpieza senior del repo avanza como **modularizacion por loops** con **doc-first order**. Secuencia aprobada: **Loop 0 verdad canonica -> Loop 1 sistema visual -> Loop 2 preview composition/interactions -> Loop 3 review orchestration -> Loop 4 cleanup final**.
```

- [ ] **Step 3: Create the refresh completion note and supersede the drift-only note**

Create `obsidian-vault/Implementation/2026-05-12 - Architecture map refreshed after native dimension milestone.md` with:

```markdown
---
type: Implementation
date: 2026-05-12
project: floorplan-fit
status: current
replaces: [[Implementation/2026-05-12 - Architecture map drift after native dimension milestone]]
tags:
  - architecture
  - documentation
  - dimensions
  - modularization
---

# Architecture map refreshed after native dimension milestone

## What changed

Se actualizo el mapa canonico de arquitectura para que vuelva a reflejar la codebase real despues del milestone de native dimensions y del arranque del programa de modularizacion por loops.

## Why

El mapa habia quedado mintiendo: seguia diciendo que las dimensiones estaban pendientes y no mostraba varios archivos ya activos de extraccion, preview, override y export ajustado.

## Where

- `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md`
- `obsidian-vault/Current State.md`

## Learned

- El mapa exhaustivo tiene que refrescarse antes de partir archivos grandes o pierde valor como fuente canonica.
- La familia `native dimensions` ya es una parte central de Loop 1 y debe aparecer como tal en la documentacion del repo.
```

Then change the old drift note frontmatter to:

```markdown
status: superseded
replaced_by: [[Implementation/2026-05-12 - Architecture map refreshed after native dimension milestone]]
```

- [ ] **Step 4: Run the GREEN verification over the supporting docs**

Run:

```powershell
Select-String -Path ".\obsidian-vault\Current State.md" `
  -Pattern "Loop 0\*\* del programa de modularizacion|modularizacion por loops"

Select-String -Path ".\obsidian-vault\Implementation\2026-05-12 - Architecture map drift after native dimension milestone.md" `
  -Pattern "status: superseded|replaced_by:"

Test-Path ".\obsidian-vault\Implementation\2026-05-12 - Architecture map refreshed after native dimension milestone.md"
```

Expected:

- `Current State.md` returns the Loop 0 and modularization matches
- the drift note returns `status: superseded` and `replaced_by:`
- `Test-Path` returns `True`

---

### Task 4: Final verification and commit the Loop 0 documentation slice

**Files:**
- Verify only

- [ ] **Step 1: Run the final stale-claim safety search**

Run:

```powershell
Select-String -Path ".\docs\explicacion de toda la app\2026-04-30 - mapa completo de arquitectura y archivos.md" `
  -Pattern "Las dimensiones CAD siguen pendientes|file_count: 323|last_verified: 2026-05-09"
```

Expected: **no matches**.

- [ ] **Step 2: Run diff hygiene verification**

Run:

```powershell
git diff --check
git diff --stat
git status --short
```

Expected:

- `git diff --check` prints nothing
- `git diff --stat` only shows the canonical map and supporting Obsidian notes
- `git status --short` only shows the intended Loop 0 docs files

- [ ] **Step 3: Commit the Loop 0 slice**

Run:

```powershell
git add "docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md" `
        "obsidian-vault/Current State.md" `
        "obsidian-vault/Implementation/2026-05-12 - Architecture map drift after native dimension milestone.md" `
        "obsidian-vault/Implementation/2026-05-12 - Architecture map refreshed after native dimension milestone.md"

git commit -m "docs: refresh architecture map for native dimensions"
```

Expected:

- one docs-only commit containing the Loop 0 truth refresh

- [ ] **Step 4: Do not build**

This repo forbids builds after changes. Loop 0 completion is proven by documentation verification and git evidence only.

---

## Self-review

- This plan intentionally does **not** split `FloorPlanPreviewControl.cs` yet; that belongs to Loop 2.
- This plan intentionally does **not** split `FloorPlanReviewViewModel.cs` yet; that belongs to Loop 3.
- This plan intentionally does **not** change product behavior; it restores canonical truth so later modularization loops are honest and reviewable.
