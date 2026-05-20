# Loop 1 Native Dimension Extraction Design

## Goal

Make the **next Loop 1 extraction** bring in all useful native floor-plan dimensions so the Review screen can show the human-visible measurement text for each one.

For the first slice, this means:

- read native `DIMENSION` entities from floor plans
- exclude `ELECTRICAL WIRING` dimensions for now
- persist each dimension as its own extracted artifact family
- expose a read-only `Dimensions` section in Review so the user can audit what came through

## Problem

Right now the repo treats dimensions as an acknowledged future family, but they still do not exist in the real extraction pipeline.

That gap matters because:

1. a tiny crop of the floor plan already shows multiple chained dimensions, so the full drawing clearly contains a large amount of measurement intent
2. `SEMINOLE2000.dxf` contains **328** native `DIMENSION` entities, but **2** are on `ELECTRICAL WIRING`, leaving **326** immediately useful architectural dimensions
3. `SANTA-BARBARA.dxf` contains **114** native `DIMENSION` entities
4. the obvious shortcut is wrong: many dimensions have `DIMENSION.dxf.text = ''`, yet AutoCAD still shows real visible text like `5'-8"`, `10'-2"`, and `10'-4"`

The verification against `SEMINOLE2000.dxf` showed why:

- the **rendered visible text** often lives inside the dimension geometry block (`*D...`) as `MTEXT` / `TEXT`
- the raw `DIMENSION.dxf.text` field alone is not enough
- blindly regenerating display text from `get_measurement()` is not CAD-faithful either, because displayed rounding/formatting can differ from the raw numeric measurement

So the real problem is not just ?count the dimensions.?

The real problem is:

> how do we extract dimensions in a way that preserves the text the human actually sees in CAD, while keeping the first slice small enough to ship quickly?

## Chosen Approach

Add a new **read-only native dimension family** to the existing Loop 1 extraction pipeline.

This slice will do four things:

1. extract native floor-plan `DIMENSION` entities from modelspace
2. filter out any dimension whose source layer is `ELECTRICAL WIRING`
3. persist dimension metadata plus resolved visible display text
4. show those extracted dimensions in Review as a dedicated audit section

This slice will **not** yet do these things:

- render dimension lines / extension lines / arrows on the preview canvas
- support excluding dimensions from curation
- support dragging or editing dimensions
- scrape site-plan measurements from generic `TEXT` / `MTEXT`
- recalculate dimension values during pinch preview

That is intentional.

The user?s immediate request is to **see what dimensions come through in the next extraction**, not to finish the entire final dimension system in one shot.

## Core Design Principle

For this slice, the most important rule is:

> preserve the CAD-visible measurement text first; richer geometry editing can come later

This repo already learned the hard way that ?looks close enough? is not the same as CAD-faithful truth.

For dimensions, the first lie would be:

- reading only `DIMENSION.dxf.text`
- or generating every label from raw measurement
- and pretending that is the text the user saw in the drawing

That would be technically lazy and product-dishonest.

## Text Resolution Rule

Each extracted dimension needs one resolved `DisplayText` field.

Resolve it in this exact priority order:

### 1. Geometry block rendered text (preferred)

If the dimension references a geometry block like `*D169`, inspect that block and gather visible `MTEXT` / `TEXT` contents.

This is the preferred source because it matched verified real examples such as:

- `5'-8"`
- `10'-2"`
- `10'-4"`
- `7'-4" GB BRACE PANEL`
- `13'-11" TO CL. TO W.C.`

### 2. `DIMENSION.dxf.text` override

If the geometry block does not yield visible text, use `DIMENSION.dxf.text` when it is non-empty and not just the placeholder `<>`.

### 3. Generated fallback from measurement (last resort)

Only if both prior sources fail, generate fallback display text from the measured numeric value.

For the first slice:

- if the source unit is inch/foot, format using simple architectural feet-inch output
- otherwise use invariant decimal text plus unit context

This fallback exists for resilience, not as the normal happy path.

## Data Model

Introduce a new extracted family shaped for **dimension audit first**, not full preview rendering yet.

### Application detected model

Add `DetectedDimension` with fields conceptually like:

- `SourceEntityRef`
- `SourceLayer`
- `SourceEntityKind` = `DIMENSION`
- `GeometryBlockName`
- `DisplayText`
- `DisplayTextSource`
- `RawTextOverride`
- `MeasurementSourceUnits`
- `MeasurementMillimeters`
- `SourceUnit`
- `DimType`
- `Angle`
- `ObliqueAngle`
- `DefPoint`
- `DefPoint2`
- `DefPoint3`
- `Confidence`
- `DetectionNotes`

### Domain model

Add `ExtractedDimension` persisted under the same extraction run used by the other Loop 1 artifacts.

Important design choice:

- keep both **source-units measurement** and **millimeter measurement**

Why:

- source units preserve original CAD meaning
- millimeters align with the repo?s normalization and future fit math

### Text source enum

Add an explicit source indicator so the system knows where the visible label came from:

- `GeometryBlock`
- `DimensionTextOverride`
- `GeneratedFallback`

That makes debugging and future UI honesty much easier.

## Extraction Pipeline Integration

The existing extraction orchestration already writes one `WallExtractionRun` and then persists multiple artifact families.

The dimension slice should extend that same orchestration rather than inventing a second run type.

### Application layer changes

Add:

- `IDimensionExtractor`
- `IExtractedDimensionRepository`
- `DetectedDimension`

Then extend `ExtractWallCandidatesHandler` to:

- call the dimension extractor after the existing artifact extractors
- map detected dimensions to domain `ExtractedDimension`
- persist them under the current extraction run

This keeps the current product mental model intact:

> one extraction run gives you the current detected artifact families for that floor-plan version

## Infrastructure Design

### Extractor

Add `IxMiliaDimensionExtractor` in the DXF infrastructure layer.

Responsibilities:

- open the DXF
- iterate modelspace `DIMENSION` entities
- ignore any entity on `ELECTRICAL WIRING`
- resolve measurement via `get_measurement()`
- resolve visible text using the priority rule above
- capture anchor metadata (`defpoint`, `defpoint2`, `defpoint3`)
- capture type/layer/angles/geometry block name
- convert the measured value to millimeters using the DXF source unit

### Schema

Add a new SQLite table for extracted dimensions.

Conceptual columns:

- `id`
- `wall_extraction_run_id`
- `source_entity_ref`
- `source_layer`
- `source_entity_kind`
- `geometry_block_name`
- `display_text`
- `display_text_source`
- `raw_text_override`
- `measurement_source_units`
- `measurement_millimeters`
- `source_unit`
- `dim_type`
- `angle`
- `oblique_angle`
- `defpoint_x`
- `defpoint_y`
- `defpoint_z`
- `defpoint2_x`
- `defpoint2_y`
- `defpoint2_z`
- `defpoint3_x`
- `defpoint3_y`
- `defpoint3_z`
- `confidence`
- `detection_notes`
- `sort_order`

This is intentionally metadata-first.

It does **not** try to persist full dimension-line geometry in the first slice.

## Contracts and Read Model

Add `DimensionDto` to the contracts layer and extend `FloorPlanReviewSessionDto` with a `Dimensions` collection.

`SqliteFloorPlanReviewSessionReader` should load dimensions alongside the other extracted families.

For the first slice, dimensions are **read-only audit artifacts**.

That means the read model does not need:

- classification overlays
- position overrides
- exclusion state

It only needs to faithfully project the persisted extraction result.

## Review UX

### Product Loop + Architecture Layer

- **Loop 1**: floor plan curation / audit
- **Infrastructure**: new extraction family
- **Application**: orchestration and repository contract
- **Domain**: extracted dimension model
- **Contracts**: review-session DTO
- **Desktop**: list + inspector visibility

### UI behavior

Add a dedicated `Dimensions` section to the Review queue.

For the first slice, each row should show at least:

- visible `DisplayText`
- source layer
- measurement summary

Selecting a dimension should show a read-only inspector summary such as:

- display text
- measurement in source units and mm
- dim type
- layer
- anchor points
- text source (`GeometryBlock`, `Override`, `Fallback`)

### Important UX constraint

Dimensions are visible in Review, but **not yet curable** in this slice.

So the UI must not fake capabilities the backend does not support yet.

That means:

- no `Exclude from Curation` action for dimensions yet
- no preview hit-test selection from canvas yet
- no drag
- no text edit

The honest message is:

> dimensions are now visible for audit; richer dimension curation comes later

## Acceptance Criteria

1. Re-running extraction on `SEMINOLE2000.dxf` yields **326** extracted dimensions after excluding `ELECTRICAL WIRING`.
2. Re-running extraction on `SANTA-BARBARA.dxf` yields **114** extracted dimensions.
3. No extracted dimension comes from source layer `ELECTRICAL WIRING`.
4. At least one verified dimension whose `DIMENSION.dxf.text` is empty still surfaces the correct visible label from its geometry block, such as `10'-4"`.
5. Review session contracts expose a `Dimensions` collection.
6. Review UI shows a dedicated `Dimensions` count/section and lets the user inspect dimension rows.
7. The first slice remains read-only for dimensions and does not pretend dimensions are already fully curable or preview-rendered.

## Testing Strategy

### Infrastructure tests

Add extractor tests for:

- counts for `SEMINOLE2000` and `SANTA-BARBARA`
- filtering out `ELECTRICAL WIRING`
- resolving visible text from geometry block `MTEXT` / `TEXT`
- falling back to `DIMENSION.dxf.text` when needed
- using generated fallback only when both text sources are absent
- converting measurement to millimeters correctly

### Application tests

Add coverage proving `ExtractWallCandidatesHandler` now persists dimensions in the same extraction run.

### Persistence / reader tests

Add integration coverage for:

- schema creation/migration
- writing extracted dimensions
- reading `DimensionDto` through `SqliteFloorPlanReviewSessionReader`

### Desktop tests

Add layout/view-model coverage for:

- visible dimensions section
- dimension count
- selecting a dimension row
- read-only inspector summaries

No preview-canvas tests are needed for this slice because dimensions are not rendered there yet.

## Tradeoffs

### Pros

- gives the user immediate visibility into hundreds of real plan dimensions
- preserves the text the human actually sees in CAD
- avoids overcommitting to full dimension rendering before extraction truth is stable
- fits the repo?s ?separate artifact families? principle cleanly

### Cons

- no canvas overlay yet
- no exclusion/editing yet
- no site-plan text measurement extraction yet
- dimension-line geometry still remains for a later slice

## Alternatives Considered

### 1. Full CAD-faithful dimension rendering now

Rejected for this slice.

Why:

- too much scope at once
- mixes extraction, persistence, read model, preview rendering, and interaction in one jump
- delays the user?s immediate need to simply see what comes through

### 2. Use only `DIMENSION.dxf.text` or regenerate everything from measurement

Rejected.

Why:

- verification proved that many visually correct labels are not stored there directly
- fallback generation can disagree with what the CAD author chose to display
- not CAD-faithful

### 3. Scrape all apparent measures from generic `TEXT` / `MTEXT`

Rejected for the first slice.

Why:

- too noisy for floor plans
- mixes native dimensions with arbitrary notes
- belongs to the later site-plan measurement problem, not to this first Loop 1 native-dimension slice

## Recommendation

Implement dimensions next as a **native, read-only Review family**:

- extract all floor-plan `DIMENSION` entities except `ELECTRICAL WIRING`
- resolve display text from rendered geometry blocks first
- persist metadata plus source/mm measurements
- show the result in Review as a dedicated `Dimensions` section

That gives the user the fastest honest answer to:

> ?show me all the dimensions that the extraction can actually bring in?

without pretending the full future dimension-editing system is already done.
