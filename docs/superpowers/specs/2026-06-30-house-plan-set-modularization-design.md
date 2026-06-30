# House Plan Set Modularization Design

## Goal

Modularize Floorplan Fit as a **House Plan Set** tool, not as a floor-plan-only tool.

The current app has strong support for importing, extracting, reviewing, curating, adjusting, and exporting a floor plan. The next architecture step is to structure that existing capability so it can become the canonical adjustment engine for a full house package:

- floor plan
- electrical plan
- roof plan
- facade/elevation sheets

The product rule is simple:

> The published, curated floor plan produces the canonical adjustment. Dependent sheets register against that canonical model and receive sheet-specific projections of the approved adjustment.

This deliberately avoids four competing fit engines.

## Current-State Evidence

Verified in the working tree:

- The solution is already a local-first modular monolith with `Desktop`, `Application`, `Domain`, `Infrastructure`, and `Contracts` projects.
- `TECH-STACK-ARCHITECTURE-DATAFLOW.md` currently describes a floor-plan-centered flow: raw DXF, extraction by CAD families, curation, site-plan envelope, deterministic fit, export, and audit.
- Code search found no first-class `HousePlanSet`, `PlanSetVersion`, `PlanSheet`, `SheetRegistration`, electrical-plan module, roof-plan module, or facade/elevation module.
- Existing electrical mentions are extraction exclusions such as `ELECTRICAL WIRING`, not a managed electrical sheet model.
- Existing facade concepts are domain/test vocabulary such as `WallRole.Facade` and `ConstraintKind.PreserveFacade`, not facade/elevation sheet support.
- Existing `roof` mentions in preview/dimension code are internal geometric naming for dimension shape lines, not roof-plan support.

So this design is not a rename exercise. It is an incremental product architecture shift: keep the floor-plan capabilities, then wrap them in a plan-set model.

## Chosen Approach

Use a **thin HousePlanSet backbone** first, then migrate behavior into it in phases.

Do not split into new `.csproj` projects yet. The current layered monolith is enough. The first win is domain vocabulary, ownership, and use-case boundaries, not project-file ceremony.

Target structure inside the current projects:

```text
Domain/
  PlanSets/
  FloorPlans/
  SitePlans/
  Adjustments/

Application/
  PlanSets/
    Import/
    Classification/
    Registration/
    AdjustmentProjection/
    ExportAudit/
    DataCollection/
  FloorPlans/
  SitePlans/

Contracts/
  PlanSets/
  FloorPlans/
  SitePlans/

Infrastructure/
  Persistence/
  Dxf/
  Storage/

Desktop/
  ViewModels/
  Controls/
```

`FloorPlans` does not disappear. It becomes the canonical sheet engine inside a broader `PlanSets` product model.

## Core Concepts

### HousePlanSet

Represents one house package across one or more drawing sheets.

Owns:

- identity of the house/package
- active plan-set version
- relation between sheets
- canonical floor-plan sheet reference

Does not own:

- DXF parsing
- rendering
- site-plan fit math
- SQLite details

### PlanSetVersion

Represents a versioned set of related sheets for a house.

Owns:

- collection of sheet versions
- canonical floor-plan sheet id
- publication/version status
- compatibility metadata for adjustment/export

Does not own:

- per-sheet extraction internals
- raw file storage details

### PlanSheet

Represents one sheet inside a plan set.

Sheet types:

```text
FloorPlan
ElectricalPlan
RoofPlan
FacadeElevation
Unknown
```

Owns:

- sheet type
- imported document id
- measurement context
- status
- sheet-specific metadata

Does not own:

- adjustment decisions
- curation internals of the canonical floor plan

### SheetRegistration

Describes how a dependent sheet relates to the canonical floor-plan coordinate system.

Owns:

- source sheet id
- canonical floor-plan sheet id
- registration method
- transform parameters
- confidence
- anchor matches
- warnings
- manual confirmation status

Does not own:

- fit proposal generation
- export file writing

### CanonicalFloorPlanAdjustment

The existing Loop 2 adjustment concept, reframed as one canonical adjustment for the house.

Input:

- published floor-plan curation
- site/buildable envelope
- certified footprint
- allowed pinch groups/markers
- project overrides

Output:

- approved adjustment transform
- changed dimensions/metrics
- fit proposal/audit data

Does not know:

- electrical symbols
- roof overhang rules
- facade elevation height rules

### SheetAdjustmentProjection

Projects the canonical adjustment to each dependent sheet.

Owns:

- sheet-specific projection rules
- registration confidence handling
- projection warnings
- before/after geometry snapshots

Does not own:

- canonical fit search
- site-plan envelope extraction

### MultiSheetExportAudit

Exports the coherent adjusted package and records why it is trustworthy.

Owns:

- exported files per sheet
- transform/projection audit
- confidence summary
- manual confirmations
- warnings

Does not own:

- calculating the canonical adjustment
- classifying sheet type

## Module Definitions

## 1. HousePlanSet / PlanSetVersion

### Responsibility

Model a house as a versioned package of related sheets. It is the product-level aggregate above individual floor plans.

### Data Owned

- `house_plan_sets`
- `plan_set_versions`
- canonical floor-plan sheet reference
- version/status metadata

### Use Cases

- create house plan set
- create plan-set version
- attach sheet to plan-set version
- select canonical floor-plan sheet
- get plan-set summary

### Ports

- `IHousePlanSetRepository`
- `IPlanSetVersionRepository`
- `IPlanSetReader`

### DTOs

- `HousePlanSetDto`
- `PlanSetVersionDto`
- `PlanSetSummaryDto`

### Persistence

```text
house_plan_sets
- id
- code
- name
- active_version_id
- is_active
- created_at_utc

plan_set_versions
- id
- house_plan_set_id
- version_label
- canonical_floor_plan_sheet_id
- status
- created_at_utc
```

### Tests

- creating a plan set without sheets is allowed
- publishing/adjusting requires a canonical floor-plan sheet
- only one canonical floor-plan sheet is active per plan-set version

### Dependencies Allowed

- Domain identifiers/statuses
- document ids
- sheet ids

### Dependencies Prohibited

- Avalonia
- SQLite APIs
- IxMilia APIs
- site-plan envelope internals

### Must Not Know

It must not know how walls, dimensions, electrical symbols, roof edges, or facade openings are extracted.

## 2. SheetImport

### Responsibility

Import managed DXF files as sheets in a plan set, preserving source file identity and measurement context.

### Data Owned

- `plan_sheets`
- link from sheet to imported document
- sheet import status

### Use Cases

- import floor-plan sheet
- import electrical sheet
- import roof sheet
- import facade/elevation sheet
- replace sheet version
- list sheets for plan-set version

### Ports

- existing `IManagedFileStorage`
- existing `IFileHashService`
- existing `IImportedDocumentRepository`
- new `IPlanSheetRepository`
- existing `IDxfGateway` where DXF metadata is needed

### DTOs

- `ImportPlanSheetRequest`
- `ImportPlanSheetResponse`
- `PlanSheetDto`

### Persistence

```text
plan_sheets
- id
- plan_set_version_id
- sheet_type
- imported_document_id
- measurement_context_id
- name
- status
- created_at_utc
```

### Tests

- imported sheet stores managed file path, not transient external path
- floor-plan import can still support the current library flow
- unknown sheet type can be stored without breaking existing floor-plan behavior

### Dependencies Allowed

- Documents
- Measurement
- Storage ports
- DXF metadata gateway

### Dependencies Prohibited

- curation rules
- fit rules
- projection rules

### Must Not Know

It must not know whether the sheet can be adjusted. Import only makes it a managed sheet.

## 3. SheetClassification

### Responsibility

Classify a sheet as floor/electrical/roof/facade/unknown using explicit user input first and DXF hints second.

### Data Owned

- classification result
- confidence
- evidence used
- manual override status

### Use Cases

- classify imported sheet
- override sheet type
- explain classification

### Ports

- `ISheetClassifier`
- `ISheetClassificationRepository`

### DTOs

- `SheetClassificationDto`
- `ClassifySheetResponse`

### Persistence

```text
sheet_classifications
- id
- plan_sheet_id
- classified_type
- confidence
- evidence_json
- is_user_confirmed
- created_at_utc
```

### Tests

- user-selected type beats heuristic type
- electrical layer evidence can classify as electrical with non-final confidence
- unknown classification is valid and does not block plan-set creation

### Dependencies Allowed

- DXF metadata/read-only layer names
- sheet import data

### Dependencies Prohibited

- adjustment engine
- UI state
- export writer

### Must Not Know

It must not know how a sheet will be projected. Classification only names the sheet type.

## 4. SheetRegistration

### Responsibility

Relate a dependent sheet to the canonical floor-plan coordinate frame.

### Data Owned

- registration method
- transform
- anchors/matches
- confidence
- warnings
- user confirmation

### Use Cases

- register electrical sheet against floor plan
- register roof sheet against floor plan
- record manual calibration anchors
- confirm/reject registration
- get registration confidence summary

### Ports

- `ISheetRegistrationRepository`
- `ISheetRegistrationService`
- `ISheetAnchorExtractor`

### DTOs

- `SheetRegistrationDto`
- `SheetAnchorDto`
- `SheetAnchorMatchDto`
- `RegistrationConfidenceDto`

### Persistence

```text
sheet_registrations
- id
- dependent_sheet_id
- canonical_floor_plan_sheet_id
- method
- transform_json
- confidence
- status
- warnings_json
- created_at_utc
- confirmed_at_utc

sheet_registration_anchors
- id
- sheet_registration_id
- sheet_id
- anchor_type
- source_entity_ref
- x
- y
- confidence
- payload_json

sheet_registration_anchor_matches
- id
- sheet_registration_id
- canonical_anchor_id
- dependent_anchor_id
- relationship_type
- confidence
```

### Tests

- dependent sheet cannot be projected without registration or explicit manual override
- low-confidence registration requires confirmation before automatic export
- electrical registration can start with whole-sheet similarity transform
- roof/facade registration can be stored without pretending it is the same transform as electrical

### Dependencies Allowed

- canonical floor-plan read models
- sheet metadata
- geometry primitives
- dimension/anchor DTOs

### Dependencies Prohibited

- fit proposal search
- DXF export writing
- Avalonia controls

### Must Not Know

It must not decide the site fit. It only answers: how does this sheet relate to the canonical floor-plan frame?

## 5. CanonicalFloorPlanAdjustment

### Responsibility

Own the single fit/adjustment decision for the house package, based on the published curated floor plan.

### Data Owned

- canonical adjustment run
- selected proposal
- transform/trim plan
- affected pinch groups
- changed dimensions/metrics

### Use Cases

- create adjustment project for plan set
- generate canonical fit proposals
- approve canonical proposal
- expose canonical adjustment transform for projections

### Ports

- existing/future `IFitEngine`
- existing/future `IAutoFitPlanSuggester` as optional suggestion input, not authority
- `ICanonicalAdjustmentRepository`
- `ICanonicalAdjustmentReader`

### DTOs

- `CanonicalAdjustmentRunDto`
- `CanonicalAdjustmentProposalDto`
- `ApprovedCanonicalAdjustmentDto`
- `AdjustmentTransformDto`

### Persistence

Can extend existing Loop 2 concepts:

```text
adaptation_projects
- add plan_set_version_id
- keep floorplan_curation_id
- keep site_plan_id / buildable_envelope_id

fit_runs
fit_proposals
proposal_pinch_group_changes
proposal_metrics
```

### Tests

- only published floor-plan curation can produce canonical adjustment
- dependent sheets are not allowed to create independent fit decisions
- approved proposal exposes a stable transform/projection input

### Dependencies Allowed

- FloorPlanCuration
- SitePlans/Envelope
- Measurement/Dimensions
- Pinch groups/markers

### Dependencies Prohibited

- electrical-plan symbol rules
- roof overhang projection rules
- facade/elevation projection rules
- Desktop UI details

### Must Not Know

It must not know how each dependent sheet renders or exports. It produces the one approved house adjustment.

## 6. SheetAdjustmentProjection

### Responsibility

Apply the approved canonical adjustment to dependent sheets using sheet-specific rules.

### Data Owned

- projection run per sheet
- projection method
- projected transform
- warnings
- confidence after projection
- changed artifacts summary

### Use Cases

- project adjustment to electrical sheet
- project adjustment to roof sheet
- project adjustment to facade/elevation sheet
- list sheets requiring manual confirmation
- approve projected sheet result

### Ports

- `ISheetAdjustmentProjector`
- `IElectricalSheetProjector`
- `IRoofSheetProjector`
- `IFacadeElevationProjector`
- `ISheetProjectionRepository`

### DTOs

- `SheetProjectionRequest`
- `SheetProjectionResultDto`
- `ProjectedSheetWarningDto`
- `ProjectedSheetChangeDto`

### Persistence

```text
sheet_projection_runs
- id
- canonical_adjustment_run_id
- plan_sheet_id
- sheet_registration_id
- projection_method
- confidence
- status
- warnings_json
- created_at_utc

sheet_projection_changes
- id
- sheet_projection_run_id
- artifact_type
- artifact_ref
- change_type
- before_geometry_snapshot_id
- after_geometry_snapshot_id
- notes
```

### Tests

- electrical projection can use registration transform plus canonical trim transform
- roof projection preserves explicit overhang rule metadata when present
- facade projection affects horizontal references without inventing vertical changes
- failed/low-confidence projection blocks automatic package export unless user confirms

### Dependencies Allowed

- canonical adjustment output
- sheet registration
- sheet type
- sheet-specific extracted anchors/artifacts

### Dependencies Prohibited

- site-plan envelope extraction
- canonical fit scoring
- UI rendering

### Must Not Know

It must not choose a better fit. Projection applies an already-approved canonical adjustment.

## 7. MultiSheetExportAudit

### Responsibility

Export a coherent adjusted house package and make the result explainable.

### Data Owned

- package export run
- exported artifact paths
- per-sheet export status
- audit summary
- confidence summary

### Use Cases

- export canonical adjusted floor plan only
- export adjusted multi-sheet package
- generate projection audit report
- list warnings/manual confirmations included in export

### Ports

- existing/future `IAdjustedDxfExporter`
- existing/future `IAdjustedSitePlanExporter`
- new `IMultiSheetExportService`
- new `IPlanSetExportRepository`
- existing `IManagedFileStorage`

### DTOs

- `MultiSheetExportRequest`
- `MultiSheetExportResultDto`
- `ExportedPlanSheetDto`
- `ProjectionAuditSummaryDto`

### Persistence

Extend existing export/audit concepts:

```text
plan_set_exports
- id
- plan_set_version_id
- canonical_adjustment_run_id
- status
- confidence_summary_json
- created_at_utc

plan_set_exported_sheets
- id
- plan_set_export_id
- plan_sheet_id
- sheet_projection_run_id
- storage_path
- status
- warnings_json
```

### Tests

- package export includes floor plan and only eligible confirmed dependent sheets
- low-confidence unconfirmed sheet is reported, not silently exported as trusted
- audit identifies projection method and confidence per sheet

### Dependencies Allowed

- canonical adjustment
- projection runs
- storage/export ports

### Dependencies Prohibited

- extraction heuristics
- registration matching internals
- UI state

### Must Not Know

It must not recalculate registration or fit. Export consumes approved/proven artifacts.

## 8. DataCollection

### Responsibility

Collect evidence about how well registration/projection works so the system improves from real usage.

### Data Owned

- classification corrections
- registration confirmations/rejections
- anchor match quality
- projection warnings
- manual overrides
- export outcomes

### Use Cases

- record user confirmed registration
- record user corrected sheet type
- record projection warning acknowledged
- summarize quality by sheet type
- identify repeated extraction/registration failures

### Ports

- `IPlanSetTelemetryRepository`
- `IPlanSetQualityReader`

### DTOs

- `RegistrationQualityEventDto`
- `ProjectionQualityEventDto`
- `SheetClassificationCorrectionDto`
- `PlanSetQualitySummaryDto`

### Persistence

Can use `audit_events` first before new telemetry tables:

```text
audit_events
- aggregate_type: PlanSheet | SheetRegistration | SheetProjection | PlanSetExport
- aggregate_id
- event_type
- payload_json
- occurred_at_utc
```

If audit becomes overloaded, split later into:

```text
plan_set_quality_events
```

### Tests

- user confirmation creates audit/quality event
- projection failure creates queryable quality event
- data collection does not block core workflow if telemetry write fails non-destructively

### Dependencies Allowed

- module event DTOs
- audit/event persistence

### Dependencies Prohibited

- core domain decisions
- fit scoring
- UI rendering

### Must Not Know

It must not make product decisions. It observes quality and stores evidence.

## Sheet-Specific Rules

## FloorPlan

Role:

- canonical source of truth
- richly extracted and curated
- owns pinch groups/markers and certified footprint

Adjustment behavior:

- produces canonical adjustment
- can be deeply curated before publication

## ElectricalPlan

Role:

- dependent overlay-like sheet
- usually sparse dimensions
- often follows floor-plan footprint and rooms

Initial adjustment behavior:

- whole-sheet similarity registration
- then project canonical trim/scale/move
- preserve electrical symbols relative to walls/rooms where anchors exist

Manual fallback:

- user confirms two or more anchors if confidence is low

## RoofPlan

Role:

- dependent but not a simple floor-plan clone
- may include overhangs, eaves, ridges, valleys, roof outlines

Initial adjustment behavior:

- register exterior footprint relationship
- preserve overhang metadata when known
- project footprint changes carefully

Manual fallback:

- require confirmation where roof boundary cannot be related to floor exterior confidently

## Facade/Elevation

Role:

- dependent projected view
- shares horizontal references/opening alignment with floor plan
- vertical dimensions are elevation-specific

Initial adjustment behavior:

- do not apply raw 2D floor transform blindly
- project horizontal adjustments through facade references
- preserve vertical heights unless explicit elevation rules say otherwise

Manual fallback:

- require facade-specific calibration before trusted export

## Incremental Phases

## Phase 1 - Conceptual PlanSet Backbone

Goal:

Introduce `HousePlanSet`, `PlanSetVersion`, and `PlanSheet` as conceptual/domain/application boundaries without moving all existing floor-plan behavior.

Scope:

- add vocabulary and docs
- map current floor-plan template/version to canonical floor-plan sheet conceptually
- keep current UI behavior intact

### Phase 1 implementation bridge

The first implementation slice exposes existing `FloorPlanLibraryItemDto` rows as `PlanSetLibraryItemDto` rows. This is intentionally a read-model bridge:

- `HousePlanSetId` maps to the existing `FloorPlanTemplate.TemplateId`.
- `ActivePlanSetVersionId` maps to the current `FloorPlanVersion` id.
- The only sheet is a canonical `FloorPlan` sheet.
- Dependent electrical, roof, and facade/elevation sheets are not persisted in Phase 1.

This keeps the current app working while giving future PlanSet features a stable product vocabulary.

Exit criteria:

- architecture docs explain that current floor-plan library item is the first canonical sheet of a future plan set
- no existing floor-plan flow needs a big rewrite

## Phase 2 - Multiple Sheets per House

Goal:

Allow a plan-set version to contain more than one managed sheet.

Scope:

- import/register metadata for non-floor sheets
- classify sheet types
- store imported docs as sheets

Exit criteria:

- a house can have floor + electrical sheets stored as related data
- existing floor-plan review still works against the canonical floor sheet

### Phase 2 implementation bridge

The Phase 2 implementation stores dependent sheets in `plan_sheets` using the current `FloorPlanVersion.Id` as the temporary `PlanSetVersionId`. User-selected sheet type is the first classification source.

This enables a current house plan set to hold a canonical floor-plan sheet plus dependent electrical, roof, or facade/elevation sheet records without introducing registration/projection yet.

## Phase 3 - Electrical Registration

Goal:

Relate electrical sheet to canonical floor plan.

Scope:

- whole-sheet similarity registration first
- optional anchors for walls/rooms/openings/symbol clusters
- confidence and confirmation UI/API path

Exit criteria:

- electrical sheet has stored registration method, transform, confidence, and confirmation status

### Phase 3 implementation bridge

The first Phase 3 implementation stores a whole-sheet similarity registration for electrical sheets in `sheet_registrations`. The current `PlanSetVersionId` still maps to the canonical `FloorPlanVersion.Id`, so `canonical_floor_plan_version_id` uses that same bridge until explicit `PlanSetVersion` records exist.

This phase records method, transform, confidence, warning, and confirmation status only. It does not project adjustments, export dependent sheets, or create an electrical fit engine.

## Phase 4 - Project Canonical Adjustment to Electrical

Goal:

Apply approved floor-plan adjustment to registered electrical sheet.

Scope:

- consume approved canonical adjustment
- generate projected electrical result
- report warnings/confidence

Exit criteria:

- electrical projection can be exported only when confidence/confirmation rules pass

### Phase 4 implementation bridge

The first Phase 4 implementation stores electrical projection records in `sheet_adjustment_projections`. It composes the stored electrical registration transform with the approved canonical `AdjustedSitePlanPlacementDto` affine placement instead of running a second fit engine.

A projection is `ReadyForExport` only when registration is confirmed, confidence is high enough, and the canonical adjustment has no compression steps. Canonical compression is recorded as a review blocker in this first slice so piecewise deformation is not silently exported as a simple affine transform.

## Phase 5 - Roof Registration and Projection

Goal:

Support roof plan without pretending it is just another floor overlay.

Scope:

- footprint/eave/ridge anchors
- overhang preservation rules
- warnings when roof relation is ambiguous

Exit criteria:

- roof projection stores rules/warnings and can be reviewed before export

### Phase 5 implementation bridge

The first Phase 5 implementation reuses the shared registration/projection stores but adds `rule_summary` so roof-specific overhang preservation is not lost. Roof registration uses `RoofFootprintWithOverhang`; roof projection uses `RoofOverhangPreserving` and carries `PreserveOverhangInches=<value>` into the persisted projection.

This phase still does not rewrite/export roof DXF geometry. Missing overhang rules, low confidence, unconfirmed registration, or canonical compression steps force manual confirmation before export.

## Phase 6 - Facade/Elevation Analysis

Goal:

Treat facades/elevations as projected views with horizontal correspondence, not as floor-plan-like sheets.

Scope:

- horizontal reference anchors
- opening center relationships
- vertical preservation rules

Exit criteria:

- facade/elevation projection design is proven with at least one real example before automation is trusted

### Phase 6 implementation bridge

The first Phase 6 implementation reuses the shared registration/projection stores and adds facade-specific method names instead of creating a facade fit engine. Facade/elevation registration uses `FacadeHorizontalReference` and stores `PreserveVertical=true;HorizontalReference=<name>` in `rule_summary`.

Facade/elevation projection uses `FacadeHorizontalPreservingVerticals`. It composes the approved canonical floor-plan horizontal scale/offset into the registered horizontal reference, but keeps rotation and vertical translation at `0` so the elevation's height/vertical scale is not silently distorted. Missing vertical-preservation metadata, low confidence, unconfirmed registration, or canonical compression steps force manual confirmation before export.

## Phase 7 - Multi-Sheet Export with Audit

Goal:

Export one coherent adjusted house package.

Scope:

- floor plan adjusted DXF
- eligible dependent sheet exports
- audit report listing projection confidence and manual confirmations

Exit criteria:

- user can adjust once and export a package that says which sheets were automatically projected, which required confirmation, and what confidence each had

### Phase 7 implementation bridge

The first Phase 7 implementation creates the package audit boundary before adding dependent-sheet DXF rewriting. `CreateMultiSheetExportAuditHandler` consumes the canonical floor-plan export path plus explicit dependent projection ids, persists `plan_set_exports` and `plan_set_exported_sheets`, and reports automatic vs manual sheet status, confidence, warnings, projection method, and rule summary.

Data collection starts with `audit_events`. A `PlanSetExportAuditCreated` event is best-effort: telemetry failure does not block saving the export audit. This phase still does not recalculate fit/registration, discover missing projections, rewrite dependent DXFs, or add Desktop UI.

### Phase 7 sheet-discovery bridge

`CreateMultiSheetExportAuditHandler` now supports two modes: explicit projection ids, or automatic PlanSet discovery when `DependentProjections` is empty. Discovery reads dependent sheets from `IPlanSheetReader`, reads existing projections for the plan-set/canonical adjustment, and records sheets without projections as `MissingProjection`.

`MissingProjection` counts as a manual-confirmation blocker. This makes incomplete packages visible instead of silently omitting unprojected sheets. The phase still does not generate missing projections or rewrite dependent DXFs.

### Phase 7 package-manifest bridge

Each multi-sheet export audit now writes a physical JSON manifest through `IPlanSetExportManifestWriter`. The returned `PackageManifestPath` is persisted on `plan_set_exports` and returned in `MultiSheetExportAuditDto`.

The manifest is the first concrete package artifact: it records canonical/dependent sheet export status, confidence, warnings, projection methods, rule summaries, and missing projections. This still does not create a zip archive, copy dependent DXFs, or rewrite dependent sheet geometry.

## Data Flow

```text
House DXFs
  -> SheetImport
  -> SheetClassification
  -> PlanSetVersion
  -> FloorPlan extraction/curation/publication
  -> SheetRegistration for dependent sheets
  -> SitePlan envelope
  -> CanonicalFloorPlanAdjustment
  -> SheetAdjustmentProjection
  -> MultiSheetExportAudit
  -> DataCollection quality events
```

## Dependency Rules

Allowed direction:

```text
Desktop -> Application -> Domain
Desktop -> Contracts
Infrastructure -> Application ports
Infrastructure -> Domain
Contracts -> no business rules
```

Plan-set product flow:

```text
PlanSet
  -> SheetImport
  -> SheetClassification
  -> FloorPlanCuration
  -> SheetRegistration
  -> CanonicalFloorPlanAdjustment
  -> SheetAdjustmentProjection
  -> MultiSheetExportAudit
  -> DataCollection
```

Forbidden:

- dependent sheet creating its own site-fit decision
- export recalculating fit or registration
- Desktop owning registration math
- Domain knowing SQLite/IxMilia/Avalonia
- `Shared` becoming a dumping ground

## Testing Strategy

Use smallest useful checks. No build required by repo rule.

Phase-level test focus:

1. PlanSet domain/application invariants.
2. Sheet import/classification persistence behavior.
3. Electrical registration confidence rules.
4. Electrical projection consumes canonical adjustment and does not create its own fit.
5. Roof projection preserves overhang metadata when present.
6. Facade projection preserves vertical values unless explicit rules say otherwise.
7. Export audit includes per-sheet confidence and manual-confirmation status.

Regression protection:

- existing floor-plan import still works
- existing floor-plan review still opens
- existing curation publication remains source of Loop 2 truth
- existing site-plan adjustment still consumes published floor-plan curation

## Acceptance Criteria

1. The architecture treats a house as a `HousePlanSet`, not only as a floor-plan template.
2. A `PlanSetVersion` can contain a canonical floor plan and dependent sheets.
3. The curated/published floor plan remains the only source of canonical adjustment.
4. Electrical, roof, and facade/elevation sheets register against the canonical model.
5. Dependent sheets receive sheet-specific projections; they do not run independent fit engines.
6. Registration/projection confidence, warnings, and manual confirmations are persisted.
7. Multi-sheet export reports exactly which sheets were projected automatically and which required user confirmation.
8. Data collection records classification, registration, projection, confirmation, warning, and export quality events.
9. Existing floor-plan-first behavior can keep working during the transition.
10. Implementation can proceed incrementally through the seven phases without a big-bang rewrite.

## Tradeoffs

### Recommended path: thin backbone first

Pros:

- preserves current working floor-plan investment
- gives future sheets a home
- avoids premature `.csproj` splitting
- lets electrical prove the pattern before roof/facade complexity

Cons:

- some existing names stay floor-plan-centered during transition
- boundaries rely on discipline before compile-time project separation

### Alternative: split new projects now

Rejected for now.

Why:

- too much ceremony before behavior exists
- easy to create fake modularity
- slows down learning from real electrical/roof/facade examples

### Alternative: make every sheet independently adjustable

Rejected.

Why:

- creates competing truths
- produces inconsistent exports
- increases engine complexity without product value

### Alternative: blindly scale every dependent sheet

Rejected as general rule.

Why:

- acceptable as an early electrical shortcut
- wrong for roof and facade/elevation
- hides low-confidence results from the user

## Prompt for Future Implementation Sessions

```text
Continue the HousePlanSet modularization. Keep the current Floorplan Fit app working, but structure it as a plan-set tool.

Invariant:
The published floor-plan curation is the canonical source of adjustment. Dependent sheets register to it and receive projections. Do not create independent fit engines per sheet.

Work phase:
Use Phase 1 by default unless the user explicitly selects another phase.

Rules:
- no big-bang rewrite
- no build after changes
- smallest useful test/check per non-trivial logic change
- keep Desktop out of domain math
- persist confidence/warnings/manual confirmations
- use existing floor-plan flow as canonical sheet behavior
- add only the minimum code needed for the chosen phase
```

## Recommendation

Start with **Phase 1: Conceptual PlanSet Backbone**.

Do less first: document and name the product model, then introduce the smallest data/use-case seam that lets the existing floor-plan library become the canonical sheet of a plan set. Electrical registration is the first real dependent-sheet proof. Roof and facade/elevation come later, after the system has real registration evidence.
