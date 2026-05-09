# Tech Stack, Arquitectura y Flujo de Datos ? MVP v1

## Objetivo t?cnico

Construir una herramienta desktop `.exe`, local-first, sin AI obligatoria en v1, orientada a:

1. importar floor plans DXF
2. extraer artifacts CAD del floor plan en familias separadas
3. curar visualmente esos artifacts con una superficie CAD-faithful
4. persistir una versi?n publicada reutilizable del floor plan
5. importar site plans DXF
6. extraer autom?ticamente el envelope construible
7. superponer floor plan publicado + site plan en escala 1:1
8. diagnosticar si entra o no entra
9. generar opciones determin?sticas usando solo zonas de pinch autorizadas
10. exportar propuesta, m?tricas y auditor?a

La regla t?cnica principal es simple:

> El sistema no adapta un dibujo plano; adapta una versi?n curada, auditada y persistida del floor plan.

---

# 1. Stack congelado para v1

## Runtime y lenguaje

- **C#**
- **.NET 10 LTS**

### Motivo

- runtime maduro
- excelente tooling en Windows
- buen fit para dominio complejo + desktop + geometr?a
- soporte LTS

---

## UI Desktop

- **Avalonia UI**
- **CommunityToolkit.Mvvm**
- **Microsoft.Extensions.Hosting**
- **Microsoft.Extensions.DependencyInjection**
- **Microsoft.Extensions.Logging**

### Motivo

- app desktop real
- patr?n MVVM claro
- separaci?n entre UI y casos de uso
- buen soporte para Windows
- permite canvas custom para preview CAD-like sin acoplarlo al dominio

---

## Persistencia

- **SQLite**
- **Microsoft.Data.Sqlite**

### Motivo

- local-first
- sin servidor
- archivo ?nico
- ACID
- ideal para herramienta t?cnica standalone
- suficiente para versionar imports, extraction runs, curations, artifacts, pinches, proposals y auditor?a

---

## DXF

- **IxMilia.Dxf** detr?s de puertos de Application

Puertos principales actuales:

- `IDxfGateway`
- `IWallExtractor`
- `IRoomLabelExtractor`
- `IOpeningExtractor`
- `IFixedPlanComponentExtractor`
- `IProtectedDetailAssemblyExtractor`

Puertos previstos:

- `IDimensionExtractor`
- `ISitePlanEnvelopeExtractor`
- `IAdjustedDxfExporter`

### Motivo

- lectura DXF real en .NET
- desacopla Application/Domain de detalles de IxMilia
- permite tests con fakes en Application
- mantiene extractores especializados por familia CAD

---

## Geometr?a 2D

- **NetTopologySuite**

### Motivo

- overlay
- contains
- intersects
- difference
- buffer
- validaci?n geom?trica
- envelope y fit futuro

### Nota vigente

El schema actual de `geometry_segments` persiste principalmente segmentos lineales. Los arcos de puertas se pueden aplanar a polil?neas para preview. Para fidelidad CAD completa de arcos, el schema debe extenderse o activarse con `segment_type`, centro, radio y sentido.

---

## Motor de encaje

- **DeterministicFitEngine** propio

### Motivo

- explicable
- testeable
- auditable
- controlable por reglas de producto
- puede explicar qu? pinch groups us? y cu?nto recort?
- evita heur?sticas opacas antes de cerrar el modelo de dominio

### No entra como dependencia obligatoria en v1

- AI como motor de decisi?n
- OR-Tools como dependencia obligatoria
- solver opaco sin trazabilidad

---

# 2. Estilo arquitect?nico

## Decisi?n

**Monolito modular local-first**

### Por qu?

El problema complejo no es despliegue distribuido. El problema complejo es:

- lectura DXF
- extracci?n por familias CAD
- curado persistido
- correcciones humanas auditables
- visual fidelity suficiente
- envelope detection
- overlay 1:1
- fit con zonas autorizadas
- export y auditor?a

Por eso:

- **1 app desktop**
- **1 base SQLite**
- **m?dulos bien aislados**
- **puertos claros entre capas**

---

# 3. Estructura de soluci?n

```text
/src
  FloorplanFit.Desktop
  FloorplanFit.Application
  FloorplanFit.Domain
  FloorplanFit.Infrastructure
  FloorplanFit.Contracts

/tests
  FloorplanFit.Application.Tests
  FloorplanFit.Desktop.Tests
  FloorplanFit.Infrastructure.Tests
```

Algunos proyectos de tests previstos originalmente todav?a pueden no existir. La verdad operativa actual vive en los proyectos presentes del working tree.

---

# 4. Responsabilidad de cada capa

## `FloorplanFit.Desktop`

Responsable de:

- ventanas Avalonia
- MVVM
- canvas de review
- renderers de preview
- overlays CAD-like
- zoom/pan
- selecci?n visual
- paneles laterales
- comandos de usuario

No conoce:

- SQL
- detalles de IxMilia
- reglas internas de extracci?n
- invariantes de dominio profundas

---

## `FloorplanFit.Application`

Responsable de:

- casos de uso
- puertos
- orquestaci?n
- transacciones mediante `IUnitOfWork`
- coordinaci?n entre dominio e infraestructura

Casos actuales:

- import floor plan
- extract floor plan artifacts
- open/get review session
- start/resume curation
- reject wall candidate
- add pinch group
- add pinch marker
- remove pinch marker
- remove false-positive openings
- remove false-positive opening labels
- remove false-positive fixed components
- remove false-positive protected details
- publish floor plan curation

Casos previstos:

- import site plan
- extract buildable envelope
- create adaptation project
- generate fit options
- approve proposal
- export adjusted DXF/report

---

## `FloorplanFit.Domain`

Responsable de:

- entidades de negocio
- estados v?lidos
- invariantes
- vocabulario del dominio
- identidad de templates/versiones/curations
- pinches y grupos como intenci?n de adaptaci?n

No conoce:

- Avalonia
- SQLite
- IxMilia.Dxf
- filesystem

---

## `FloorplanFit.Infrastructure`

Responsable de:

- adaptadores IxMilia
- profile/conventions de extracci?n
- repositorios SQLite
- read-models SQLite
- schema/migraciones
- filesystem/storage gestionado
- hashing
- geometr?a concreta cuando aplique
- fit/export concretos cuando existan

---

## `FloorplanFit.Contracts`

Responsable de:

- DTOs
- requests/responses
- contratos entre Application, Infrastructure y Desktop

Regla:

> Contracts transporta datos planos; no contiene reglas de negocio ni detalles de UI.

---

# 5. M?dulos de negocio

```text
Documents
Measurement
Geometry
FloorPlans
FloorPlanExtraction
FloorPlanCuration
PinchCuration
CadArtifacts
SitePlans
EnvelopeDetection
Fitting
Export
Audit
```

---

# 6. Interfaces clave

## Floor plan / import

```text
IDxfGateway
IManagedFileStorage
IFileHashService
IFloorPlanTemplateRepository
IFloorPlanVersionRepository
IImportedDocumentRepository
IMeasurementContextRepository
IFloorPlanLibraryReader
IFloorPlanExtractionSourceReader
```

## Extraction Loop 1

```text
IWallExtractor
IRoomLabelExtractor
IOpeningExtractor
IFixedPlanComponentExtractor
IProtectedDetailAssemblyExtractor
```

Futuro:

```text
IDimensionExtractor
```

## Curation Loop 1

```text
IFloorPlanCurationRepository
IWallExtractionRunRepository
IExtractedWallCandidateRepository
IExtractedRoomLabelRepository
IExtractedOpeningCandidateRepository
IExtractedOpeningLabelRepository
IExtractedFixedPlanComponentRepository
IExtractedProtectedDetailAssemblyRepository
IPinchGroupRepository
IPinchMarkerRepository
IFloorPlanReviewSessionReader
IUnitOfWork
```

## Loop 2 previsto

```text
ISitePlanRepository
IBuildableEnvelopeRepository
IAdaptationProjectRepository
IFitEngine
IExportService
IAuditLog
```

---

# 7. Flujo de datos exacto

## A. Importaci?n de Floor Plan

```text
FloorPlan DXF externo
  -> ManagedFileStorage
  -> IDxfGateway
  -> ImportedDocument
  -> MeasurementContext
  -> FloorPlanTemplate
  -> FloorPlanVersion
  -> SQLite
  -> Library item
```

### Regla

La app lee y hashea la copia gestionada, no el archivo externo temporal. As? el registro persistido describe exactamente el archivo que la app controla.

---

## B. Extracci?n CAD-faithful de Floor Plan

```text
FloorPlanVersion actual
  -> FloorPlanExtractionSourceReader
  -> DxfExtractionProfile.PointeHomes
  -> Extractors por familia
  -> WallExtractionRun
  -> Extracted artifacts persistidos
  -> Review session
```

Familias actuales:

```text
Wall candidates
Room labels
Opening candidates
Opening labels
Fixed plan components
Protected detail assemblies
Pinch groups/markers creados por humano
```

Familia prevista:

```text
Dimensions
```

### Regla

Cada familia CAD debe tener lifecycle propio:

```text
extractor
  -> detected model
  -> domain entity
  -> repository/schema
  -> DTO
  -> preview layer
  -> selection/correction/removal
  -> publishable truth
```

No se debe convertir todo en paredes. Esa es la trampa arquitect?nica que despu?s rompe el fit.

---

## C. Review y Curation Loop 1

```text
Open Review
  -> StartOrResumeCuration
  -> FloorPlanReviewSessionReader
  -> Review DTO
  -> Desktop ViewModel
  -> Preview CAD-like
  -> Human corrections
  -> Pinch groups/markers
  -> Publish Curation
```

Acciones humanas actuales:

- rechazar wall candidates inv?lidos
- crear pinch groups nombrados
- agregar pinch markers estrat?gicos
- remover pinch markers equivocados
- remover opening geometry falsa positiva
- remover opening label falsa positiva
- remover fixed component falso positivo
- remover protected detail falso positivo
- publicar curation activa

### Fuente can?nica para Loop 2

Loop 2 no debe trabajar contra:

- DXF crudo
- entidades temporales de IxMilia
- extraction runs no curadas
- preview state runtime-only

Loop 2 debe trabajar contra:

- `FloorPlanTemplate`
- `FloorPlanVersion`
- `FloorPlanCuration` publicada
- wall candidates v?lidos/no rechazados
- artifacts CAD persistidos y no removidos
- pinch groups
- pinch markers
- future dimensions curadas
- project-specific overrides

---

## D. Adaptaci?n por Site Plan

```text
SitePlan DXF
  -> ManagedFileStorage
  -> SitePlan import
  -> Envelope extraction
  -> BuildableEnvelope
  -> Select published FloorPlanCuration
  -> Overlay 1:1
  -> Diagnose overflow
  -> Choose allowed pinch groups
  -> DeterministicFitEngine
  -> Fit proposals
  -> Proposal review
  -> Export + audit
```

### Regla UX/t?cnica

La extracci?n del envelope debe ser mayoritariamente autom?tica.

Esperado:

- usuario normalmente confirma
- usuario solo corrige cuando realmente fall?

---

# 8. Modelo de datos

Esta secci?n describe el modelo conceptual y las tablas activas o previstas. El schema real puede migrar de forma incremental en `SqliteSchemaInitializer`.

## 8.1 Measurement

```text
measurement_contexts
- id
- source_unit
- to_millimeters_factor
- linear_tolerance_mm
- angular_tolerance_deg
- created_at_utc
```

---

## 8.2 Documentos

```text
imported_documents
- id
- document_type
- original_file_name
- storage_path
- sha256
- dxf_version
- measurement_context_id
- imported_at_utc
```

`document_type`:

- floor_plan_dxf
- site_plan_dxf
- exported_adjusted_dxf
- exported_report

---

## 8.3 Geometr?a

```text
geometry_paths
- id
- is_closed
- bounding_box_json
- serialized_format
```

```text
geometry_segments
- id
- geometry_path_id
- segment_order
- segment_type
- start_x
- start_y
- end_x
- end_y
- center_x
- center_y
- radius
- clockwise
```

### Nota

Para el estado actual, las geometr?as del preview se apoyan principalmente en segmentos lineales. Arcos nativos y dimensiones requieren endurecer este modelo.

---

## 8.4 Librer?a de Floor Plans

```text
floorplan_templates
- id
- code
- name
- current_version_id
- active_published_curation_id
- is_active
```

```text
floorplan_versions
- id
- floorplan_template_id
- imported_document_id
- geometry_fingerprint
- created_at_utc
```

---

## 8.5 Extraction run

```text
wall_extraction_runs
- id
- floorplan_version_id
- extractor_version
- status
- created_at_utc
```

El nombre hist?rico de la tabla conserva `wall`, pero la corrida hoy agrupa m?s que walls: room labels, openings, fixed components y protected details tambi?n se atan a esa run.

---

## 8.6 Wall candidates

```text
extracted_wall_candidates
- id
- wall_extraction_run_id
- source_entity_ref
- source_layer
- geometry_path_id
- thickness_mm
- confidence
- detection_notes
- status
- sort_order
```

### Importante

Un wall candidate detectado no equivale a ?pared curada final?. Es evidencia t?cnica revisable.

---

## 8.7 Room labels

```text
extracted_room_labels
- id
- wall_extraction_run_id
- source_entity_ref
- source_layer
- text
- x
- y
- text_height
- rotation_degrees
- text_style_name
- horizontal_alignment
- vertical_alignment
- attachment_point
- color_argb
- confidence
- sort_order
```

### Regla

Los room labels se renderizan como texto CAD-like. No son badges inventados por UI.

---

## 8.8 Openings

```text
extracted_opening_candidates
- id
- wall_extraction_run_id
- source_entity_ref
- source_layer
- kind
- geometry_kind
- geometry_path_id
- confidence
- color_argb
- sort_order
```

```text
extracted_opening_labels
- id
- wall_extraction_run_id
- source_entity_ref
- source_layer
- text
- x
- y
- text_height
- rotation_degrees
- text_style_name
- horizontal_alignment
- vertical_alignment
- attachment_point
- color_argb
- confidence
- sort_order
```

### Regla

Puertas y ventanas deben preservarse como artifacts propios porque el fit no debe deformarlas ni borrarlas sin explicaci?n.

---

## 8.9 Fixed plan components

```text
extracted_fixed_plan_components
- id
- wall_extraction_run_id
- source_entity_ref
- source_layer
- kind
- geometry_kind
- geometry_path_id
- confidence
- color_argb
- sort_order
```

```text
extracted_fixed_plan_component_paths
- fixed_plan_component_id
- geometry_path_id
- sort_order
```

Ejemplos:

- toilets
- tubs
- sinks
- appliances
- cabinets
- fixtures

---

## 8.10 Protected detail assemblies

```text
extracted_protected_detail_assemblies
- id
- wall_extraction_run_id
- source_entity_ref
- source_layer
- kind
- geometry_kind
- geometry_path_id
- confidence
- color_argb
- sort_order
```

```text
extracted_protected_detail_assembly_paths
- protected_detail_assembly_id
- geometry_path_id
- sort_order
```

Ejemplos:

- wet-area details
- shower/tub enclosures
- hatches relevantes
- safety/detail geometry que no debe convertirse en wall

---

## 8.11 Curation

```text
floorplan_curations
- id
- floorplan_version_id
- curation_version
- status
- notes
- created_at_utc
- published_at_utc
```

La curation publicada es:

- persistida
- reutilizable
- editable despu?s
- versionable
- fuente de verdad para Loop 2

---

## 8.12 Pinch groups y pinch markers

```text
pinch_groups
- id
- floorplan_curation_id
- name
- axis_tag
- sort_order
```

```text
pinch_markers
- id
- floorplan_curation_id
- pinch_group_id
- source_candidate_id
- geometry_path_id
- position_ratio
- max_trim_mm
- sort_order
```

### Regla de negocio

El fit futuro no pregunta ?qu? l?nea puedo escalar?. Pregunta:

> ?qu? grupos de pinches autoriz? el usuario, en qu? eje, con cu?nto m?ximo recortable?

---

## 8.13 Dimensiones CAD futuras

Las dimensiones no deben modelarse como labels decorativos.

Deben tener una familia propia:

```text
extracted_dimensions
- id
- wall_extraction_run_id
- source_entity_ref
- source_layer
- displayed_text
- measured_value_original
- measured_value_computed
- unit
- orientation
- dimension_line_path_id
- text_x
- text_y
- text_height
- rotation_degrees
- color_argb
- style_name
- anchor_payload_json
- sort_order
```

### Regla

Durante preview/adaptaci?n por pinches, una dimensi?n debe poder recalcularse desde geometr?a transformada para auditar el cambio.

---

## 8.14 Site Plans y Envelope previstos

```text
site_plans
- id
- imported_document_id
- name
- boundary_path_id
- created_at_utc
```

```text
buildable_envelopes
- id
- site_plan_id
- boundary_path_id
- area_square_meters
- extraction_method
- is_validated
- created_at_utc
```

---

## 8.15 Proyecto de adaptaci?n previsto

```text
adaptation_projects
- id
- name
- site_plan_id
- buildable_envelope_id
- floorplan_template_id
- floorplan_version_id
- floorplan_curation_id
- status
- created_at_utc
```

Esto representa:

- este lote
- esta casa
- esta versi?n publicada
- estos overrides del caso

---

## 8.16 Corridas del motor previstas

```text
fit_runs
- id
- adaptation_project_id
- engine_version
- started_at_utc
- finished_at_utc
- result_status
```

```text
fit_proposals
- id
- fit_run_id
- rank
- summary
- hard_penalty
- soft_penalty
- total_score
- result_geometry_snapshot_id
- created_at_utc
```

```text
proposal_pinch_group_changes
- id
- fit_proposal_id
- pinch_group_id
- axis_tag
- trim_amount_mm
- affected_geometry_snapshot_id
- notes
```

```text
proposal_artifact_impacts
- id
- fit_proposal_id
- artifact_type
- artifact_id
- impact_type
- before_geometry_path_id
- after_geometry_path_id
- notes
```

```text
proposal_metrics
- id
- fit_proposal_id
- metric_code
- metric_value
```

---

## 8.17 Export y auditor?a

```text
exported_artifacts
- id
- adaptation_project_id
- fit_proposal_id
- artifact_type
- storage_path
- created_at_utc
```

```text
audit_events
- id
- aggregate_type
- aggregate_id
- event_type
- payload_json
- occurred_at_utc
```

---

# 9. Estructura de archivos runtime

```text
/workspace
  app.db

  /library
    /raw-dxf
    /previews

  /siteplans
    /raw-dxf

  /projects
    /{project-id}
      /exports
      /reports
```

## Regla

### En SQLite

- metadata
- imports
- extraction runs
- curation state
- artifacts detectados
- artifact corrections/removals
- pinch groups
- pinch markers
- site envelopes
- fit runs/proposals futuros
- audit events

### En filesystem

- DXFs originales gestionados
- DXFs exportados
- reportes
- previews/snapshots

---

# 10. Fit engine v1 previsto

## Estrategia

Motor determin?stico, explicable y testeable.

### Input

- `BuildableEnvelope`
- published `FloorPlanCuration`
- valid wall candidates
- active CAD artifacts
- pinch groups
- pinch markers
- project overrides
- future dimensions

### Output

- `FitProposal[]`
- m?tricas
- auditor?a de cambios
- snapshots/exportables

### Estrategias iniciales

- `FrontAnchorFitStrategy`
- `UseSelectedPinchGroupsStrategy`
- `MinimumTrimStrategy`
- `PreserveOpeningsStrategy`
- `PreserveFixedArtifactsStrategy`
- `MinAreaLossStrategy`

### Pipeline

```text
Load published curation
  -> Load active artifacts
  -> Load pinch groups/markers
  -> Load envelope
  -> Overlay 1:1
  -> Detect overflow/conflicts
  -> Allocate required trim across allowed pinch groups
  -> Transform affected geometry
  -> Recompute dimensions/metrics when available
  -> Validate protected artifacts
  -> Score proposals
  -> Rank proposals
```

---

# 11. Decisiones expl?citas de scope

## Entran en v1

- desktop local-first
- import de floor plans DXF
- extracci?n de wall candidates
- extracci?n de room labels
- extracci?n de doors/windows como openings
- extracci?n de labels de openings
- extracci?n de fixed plan components
- extracci?n de protected detail assemblies
- curation persistida
- pinch groups y pinch markers
- review CAD-like con zoom/pan
- envelope irregular de site plan
- overlay 1:1
- propuestas determin?sticas auditables
- export + auditor?a

## Entran despu?s o siguen pendientes

- dimensiones CAD como artifact propio
- arcos nativos sin flattening
- corrections backbone m?s rico: mover, ocultar, renombrar, reclasificar
- room boundaries/polygons
- site plan extraction completo
- fit engine real
- proposal review real
- export DXF ajustado real

## No entran en v1

- CAD gen?rico completo
- AI como decisor obligatorio
- solver opaco
- reglas legales completas multi-jurisdicci?n
- automatizaci?n est?tica

---

# 12. Resumen ejecutivo

La arquitectura correcta para este proyecto es:

> **desktop local-first + monolito modular + curation CAD-faithful + artifacts separados + pinch groups/markers + envelope extraction autom?tica + fit engine determin?stico y auditable**

Y la clave del flujo de datos es:

> **DXF crudo -> extracci?n por familias CAD -> review/curation persistida -> publicaci?n reusable -> site plan envelope -> fit usando pinches autorizados -> proposals auditables -> export**

Ese es el n?cleo t?cnico que hay que construir.
