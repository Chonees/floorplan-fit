# Tech Stack, Arquitectura y Flujo de Datos — MVP v1

## Objetivo técnico

Construir una herramienta desktop `.exe`, local-first, sin AI, orientada a:

1. importar floor plans DXF
2. extraer walls candidatas
3. curarlas y persistirlas en DB
4. importar site plans DXF
5. extraer automáticamente el envelope construible
6. superponer floor plan curado + site plan en escala 1:1
7. generar opciones determinísticas de encaje de **walls only**
8. exportar propuesta y auditoría

---

# 1. Stack congelado para v1

## Runtime y lenguaje

- **C#**
- **.NET 10 LTS**

### Motivo

- runtime maduro
- excelente tooling en Windows
- muy buen fit para dominio complejo + desktop + librerías geométricas
- LTS actual confirmada

Referencia:
- https://dotnet.microsoft.com/platform/support-policy

---

## UI Desktop

- **Avalonia UI**
- **CommunityToolkit.Mvvm**
- **Microsoft.Extensions.Hosting**
- **Microsoft.Extensions.DependencyInjection**
- **Microsoft.Extensions.Logging**

### Motivo

- app desktop real
- patrón MVVM claro
- separa UI de dominio
- buen soporte para Windows

Referencias:
- https://avaloniaui.net/avalonia/windows/
- https://docs.avaloniaui.net/docs/how-to/mvvm-how-to
- https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/

---

## Persistencia

- **SQLite**
- **Microsoft.Data.Sqlite**

### Motivo

- local-first
- sin servidor
- archivo único
- ACID
- ideal para una herramienta técnica standalone

Referencias:
- https://www.sqlite.org/about.html
- https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/

---

## DXF

- **IxMilia.Dxf** detrás de `IDxfGateway`

### Motivo

- lectura/escritura DXF en .NET
- suficiente para v1
- desacoplar librería del dominio desde el inicio

Referencia:
- https://github.com/ixmilia/dxf

---

## Geometría 2D

- **NetTopologySuite**

### Motivo

- overlay
- contains
- intersects
- difference
- buffer
- operaciones topológicas confiables

Referencia:
- https://github.com/NetTopologySuite/NetTopologySuite

---

## Motor de encaje

- **DeterministicFitEngine** propio

### Motivo

- más simple
- más trazable
- más controlable
- mejor para walls-only
- evita modelar demasiado pronto constraints que todavía no están cerradas

### No entra en v1

- AI
- OR-Tools como dependencia obligatoria
- heurísticas opacas

---

# 2. Estilo arquitectónico

## Decisión

**Monolito modular local-first**

### Por qué

El problema complejo no es de despliegue distribuido.  
El problema complejo es:

- lectura DXF
- extracción
- curado persistido
- envelope detection
- overlay 1:1
- fit options
- auditoría

Por eso:

- **1 app**
- **1 base SQLite**
- **módulos bien aislados**

---

# 3. Estructura de solución

```text
/src
  FloorplanFit.Desktop
  FloorplanFit.Application
  FloorplanFit.Domain
  FloorplanFit.Infrastructure
  FloorplanFit.Contracts

/tests
  FloorplanFit.Domain.Tests
  FloorplanFit.Application.Tests
  FloorplanFit.Infrastructure.Tests
  FloorplanFit.GoldenFiles.Tests
  FloorplanFit.Architecture.Tests
```

---

# 4. Responsabilidad de cada capa

## `FloorplanFit.Desktop`

Responsable de:

- ventanas
- canvas
- overlays
- grids/listas
- interacción del usuario
- navegación

No conoce:

- SQL
- parser DXF
- detalles topológicos

---

## `FloorplanFit.Application`

Responsable de:

- casos de uso
- orquestación
- coordinación entre dominio e infraestructura

Ejemplos:

- ImportFloorPlan
- RunWallExtraction
- SaveWallCuration
- ImportSitePlan
- ExtractBuildableEnvelope
- CreateAdaptationProject
- GenerateFitOptions
- ExportAdjustedDxf

---

## `FloorplanFit.Domain`

Responsable de:

- modelo del negocio
- invariantes
- entidades principales
- reglas de encaje
- constraints estructuradas
- propuestas y métricas

No conoce:

- Avalonia
- SQLite
- IxMilia.Dxf
- NetTopologySuite

---

## `FloorplanFit.Infrastructure`

Responsable de:

- adapters DXF
- persistencia SQLite
- operaciones geométricas NTS
- filesystem
- export
- fit engine concreto

---

## `FloorplanFit.Contracts`

Responsable de:

- DTOs
- requests/responses
- contratos entre capas

---

# 5. Módulos de negocio

```text
Documents
Measurement
Geometry
FloorPlans
WallExtraction
WallCuration
SitePlans
Constraints
Fitting
Export
Audit
```

---

# 6. Interfaces clave

```text
IDxfGateway
IWallExtractor
IGeometryEngine
IFloorPlanRepository
IWallCurationRepository
ISitePlanRepository
IAdaptationProjectRepository
IFitEngine
IExportService
IAuditLog
```

### Regla

El dominio trabaja contra interfaces.  
Las librerías concretas viven solo en infraestructura.

---

# 7. Flujo de datos exacto

## A. Curado de Floor Plan

```text
FloorPlan DXF
  -> DxfGateway
  -> WallExtractor
  -> ExtractedWallCandidates
  -> UI Review
  -> CuratedWalls
  -> Publish Curation
  -> Persist canonical curation in DB
```

### Fuente canónica para el motor

El motor NO trabaja contra:

- DXF crudo
- entities temporales
- extracción no validada

El motor SÍ trabaja contra:

- `FloorPlanCuration`
- `CuratedWalls`
- `StructuredConstraints`

---

## B. Adaptación por Site Plan

```text
SitePlan DXF
  -> DxfGateway
  -> Envelope Extraction
  -> BuildableEnvelope
  -> Overlay 1:1 with CuratedWalls
  -> Fit Engine
  -> Fit Proposals
  -> Proposal Review
  -> Export
```

### Regla UX/técnica

La extracción del envelope debe ser **mayoritariamente automática**.

Esperado:

- usuario normalmente confirma
- usuario solo corrige cuando realmente falló

---

# 8. Modelo de datos exacto

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

## 8.3 Geometría

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

Esto preserva:

- líneas
- arcos
- curvaturas

Es obligatorio para soportar site plans no rectangulares.

---

## 8.4 Librería de Floor Plans

```text
floorplan_templates
- id
- code
- name
- current_version_id
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

## 8.5 Extracción automática de walls

```text
wall_extraction_runs
- id
- floorplan_version_id
- extractor_version
- status
- created_at_utc
```

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
```

### Importante

Esto es resultado técnico temporal.  
Todavía NO es la capa canónica de negocio.

---

## 8.6 Curado canónico persistido

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

```text
curated_walls
- id
- floorplan_curation_id
- stable_wall_id
- source_candidate_id
- source_entity_ref
- geometry_path_id
- wall_role
- mobility_level
- protection_level
- thickness_mm
- is_exterior
- is_structural_hint
- wall_group_id
- sort_order
```

```text
curated_wall_groups
- id
- floorplan_curation_id
- group_code
- name
- group_type
- priority
```

```text
curated_wall_joins
- id
- floorplan_curation_id
- wall_a_id
- wall_b_id
- junction_type
- junction_x
- junction_y
```

### Regla de negocio

La **curación publicada** es:

- persistida
- reutilizable
- editable después
- versionable

O sea:

- siempre hay una versión activa
- el usuario puede volver, editar y republicar otra

---

## 8.7 Site Plans y Envelope

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

## 8.8 Notas libres

```text
constraint_intent_notes
- id
- scope_type
- scope_id
- raw_text
- status
- created_at_utc
```

En v1:

- se guarda
- se muestra
- se audita

No altera geometría automáticamente.

---

## 8.9 Constraints estructuradas

```text
structured_constraints
- id
- scope_type
- scope_id
- constraint_kind
- constraint_strength
- target_type
- target_id
- weight
- payload_json
- source
- created_at_utc
```

Ejemplos de `constraint_kind`:

- inside_envelope
- lock_wall
- preserve_group
- max_wall_move
- protect_wall
- preserve_facade

---

## 8.10 Proyecto de adaptación

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
- esta versión curada activa

---

## 8.11 Corridas del motor

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
proposal_wall_changes
- id
- fit_proposal_id
- curated_wall_id
- change_type
- delta_x
- delta_y
- delta_length
- new_geometry_path_id
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

## 8.12 Export y auditoría

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
- curations
- constraints
- site envelopes
- runs
- proposals
- audit

### En filesystem

- DXFs originales
- DXFs exportados
- reportes
- previews

---

# 10. Fit engine v1

## Estrategia

Motor determinístico, explicable y testeable.

### Input

- `BuildableEnvelope`
- `FloorPlanCuration`
- `CuratedWalls`
- `StructuredConstraints`

### Output

- `FitProposal[]`

### Estrategias iniciales

- `FrontAnchorFitStrategy`
- `LeftEdgeFitStrategy`
- `RightEdgeFitStrategy`
- `MinWallMovementStrategy`
- `MinAreaLossStrategy`

### Pipeline

```text
Load curated walls
  -> Load envelope
  -> Overlay 1:1
  -> Detect conflicts
  -> Apply deterministic movement strategies
  -> Validate hard constraints
  -> Score soft constraints
  -> Rank proposals
```

---

# 11. Decisiones explícitas de scope

## Entran en v1

- walls only
- extracción de walls
- curado persistido
- envelope irregular
- overlay 1:1
- propuestas determinísticas
- export + auditoría

## No entran en v1

- AI
- puertas/ventanas activas
- solver avanzado tipo OR-Tools
- reglas legales completas multi-jurisdicción
- automatización estética

## Sí quedan preparadas en arquitectura

- Door
- Window
- Block
- rule packs versionados
- constraints más ricas

---

# 12. Resumen ejecutivo

La arquitectura correcta para este proyecto es:

> **desktop local-first + monolito modular + curación persistida de walls + envelope extraction automática + fit engine determinístico**

Y la clave del flujo de datos es:

> **DXF crudo -> extracción -> curado persistido en DB -> reutilización en site plans -> proposals auditables**

Ese es el núcleo técnico que hay que construir.
