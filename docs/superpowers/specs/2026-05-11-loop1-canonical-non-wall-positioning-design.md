# Loop 1 Canonical Non-Wall Positioning Design

## Goal

Let the user move **any non-wall artifact** directly in the Loop 1 preview and persist that new location as **canonical curated truth**.

This includes:

- `RoomLabel`
- `OpeningLabel`
- `OpeningCandidate`
- `FixedPlanComponent`
- `ProtectedDetailAssembly`

This explicitly excludes:

- `WallCandidate`
- `PinchMarker`

## Problem

The current Loop 1 review surface is still extractor-position-first for every movable artifact:

- labels render at their detected `X/Y`
- openings/fixed/protected geometry render at their detected geometry-path positions
- only walls and pinch markers have explicit authored semantics today

That creates a real product gap:

1. the user may know that a label or object is semantically correct but spatially misplaced
2. the preview currently cannot capture that correction as curated truth
3. downstream consumers would still inherit extractor placement instead of user-authored placement

If this feature is implemented as a visual-only drag layer in Desktop, the system becomes dishonest:

> the user would think they corrected the floor plan, but only the current screen would know it

That is not acceptable for Floorplan Fit. Loop 1 curation must produce reusable, auditable, canonical truth.

## Chosen Approach

Use a **persisted position overlay** for non-wall artifacts, parallel to the new curated-classification overlay.

The system keeps:

- original extracted truth unchanged
- curated position truth as an overlay keyed by curation + source artifact identity

The reader then resolves:

- detected position/geometry
- plus curated position override when present

The rest of the app must consume the **resolved curated position**, not the raw extracted position.

## Core Design Principle

Not every artifact should be moved the same way.

There are two fundamentally different spatial models in this repo:

### 1. Point-anchored artifacts

These already live as an anchor point:

- `RoomLabel`
- `OpeningLabel`

For these, the correct canonical override is:

- absolute curated `X/Y`

### 2. Geometry-path artifacts

These live as one or more geometry paths:

- `OpeningCandidate`
- `FixedPlanComponent`
- `ProtectedDetailAssembly`

For these, the correct canonical override is:

- translation `dx/dy`

This keeps the original extracted geometry auditable while still letting curated truth become canonical for preview, reuse, and later fit/export.

## UX Decisions

### 1. Dragging is direct-manipulation authoring

The user should be able to:

- select a movable non-wall artifact
- drag it directly in the preview
- see it move live
- release it
- have the new location persisted automatically

There should not be a second “apply move” workflow for this slice.

### 2. Walls stay spatially authoritative

Walls are the base structural truth and remain non-movable in review.

If the user thinks a wall is wrong, the current action remains exclusion/rejection rather than repositioning.

### 3. Pinch markers remain fixed after placement

Pinches are authored intent about shrink permission, not loose draggable annotations.

Once placed:

- they may be removable
- they may be re-added elsewhere manually
- but they must not become draggable artifacts

### 4. First slice supports move only

This slice supports:

- translate / move

This slice does **not** support:

- rotate
- scale
- free reshape
- vertex editing

That keeps the interaction robust and the persistence model small.

### 5. Restore must exist

Every movable artifact needs a “restore detected position” path.

Without restore, one accidental drag would permanently pollute the curated state with no safe recovery path.

## Domain Model

Introduce a new canonical position-overlay model, conceptually:

- `FloorPlanArtifactPosition`

Fields:

- `FloorPlanCurationId`
- `SourceArtifactKind`
- `SourceArtifactId`
- `PositionMode`
- `ResolvedX`
- `ResolvedY`
- `TranslationDx`
- `TranslationDy`
- `UpdatedAtUtc`

### Position modes

#### `AbsolutePoint`

Used for:

- `RoomLabel`
- `OpeningLabel`

Valid fields:

- `ResolvedX`
- `ResolvedY`

Invalid/unused fields:

- `TranslationDx`
- `TranslationDy`

#### `Translation`

Used for:

- `OpeningCandidate`
- `FixedPlanComponent`
- `ProtectedDetailAssembly`

Valid fields:

- `TranslationDx`
- `TranslationDy`

Invalid/unused fields:

- `ResolvedX`
- `ResolvedY`

## Source Artifact Kinds

The new position overlay should support these artifact kinds:

- `RoomLabel`
- `OpeningLabel`
- `OpeningCandidate`
- `FixedPlanComponent`
- `ProtectedDetailAssembly`

It must explicitly reject:

- `WallCandidate`
- `PinchMarker`

This rule belongs in Domain/Application validation, not just in Desktop.

## Persistence

Add a new SQLite table:

- `floorplan_artifact_positions`

Columns:

- `floorplan_curation_id`
- `source_artifact_kind`
- `source_artifact_id`
- `position_mode`
- `resolved_x`
- `resolved_y`
- `translation_dx`
- `translation_dy`
- `updated_at_utc`

Uniqueness:

- one active row per `(floorplan_curation_id, source_artifact_kind, source_artifact_id)`

Same philosophy as curated classification:

- extracted records remain untouched
- curation writes an overlay
- the read model resolves truth

## Contracts

The review/session DTO layer needs resolved position data, not just raw extractor coordinates.

### Labels

`RoomLabelDto` and `OpeningLabelDto` should expose resolved coordinates as the coordinates the UI consumes.

That means:

- if no position overlay exists, DTO `X/Y` come from extraction
- if an overlay exists, DTO `X/Y` come from the curated point override

The UI should not need to know whether those coordinates were detected or curated in order to render correctly.

Optional metadata may be added for inspector display:

- `HasManualPosition`
- `DetectedX`
- `DetectedY`

### Curated geometry artifacts

`CuratedPlanArtifactDto` should expose enough information for:

- live preview drag
- selection
- inspector display
- restore action

Recommended additions:

- `HasManualPosition`
- `TranslationDx`
- `TranslationDy`

The geometry itself should still resolve through `GeometryPathIds`, but the preview must render the translated version.

## Read Model Resolution

`SqliteFloorPlanReviewSessionReader` becomes the source of resolved position truth.

### Labels

For each `RoomLabel` and `OpeningLabel`:

- read extractor coordinates
- read position overlay if present
- return resolved DTO coordinates

### Geometry artifacts

For movable non-wall geometry:

- read base geometry paths
- detect owning artifact overlays
- apply `dx/dy` translation to the resolved preview geometry returned for those paths

### Critical rule

The reader should return **resolved geometry/position** that the review UI uses directly.

That ensures future consumers can reuse the same logic:

- fit
- export
- audit replay

This avoids re-implementing translation math in multiple layers.

## Application Layer

Add explicit use cases:

- `SaveArtifactPositionHandler`
- `RestoreArtifactPositionHandler`

### Save behavior

For labels:

- persist absolute `X/Y`

For geometry artifacts:

- persist `dx/dy`

### Restore behavior

Remove or neutralize the active position override so that the detected position becomes the resolved position again.

Unlike curated classification, the restore behavior here does **not** need a `DetectedDefault` row if the lineage model for positions is designed to allow deletion safely.  
If lineage inheritance is later added for position overlays, this assumption must be re-evaluated.

## Desktop Interaction Model

### Preview

The preview control must gain drag-authoring behavior for movable non-wall artifacts:

1. pointer down on movable artifact
2. capture drag state
3. render live translated preview during drag
4. pointer release persists canonical new position

### Selection precedence

If a geometry path belongs to a movable curated artifact, that movable artifact gets drag/selection ownership before a wall candidate.

### Labels

Room labels and opening labels need hit-target support in preview, not just text rendering.

That means Desktop must be able to:

- estimate/select their rendered text bounds
- begin a drag from those bounds

### Inspector

Selected movable artifacts should display:

- current resolved position
- whether it was manually moved
- restore action

## Architecture Impact

### Product Loop

- **Loop 1**: floor plan curation

### Layers

- **Desktop**: drag interaction, live preview, hit-testing, inspector actions
- **Application**: save/restore position use cases
- **Domain**: position overlay model and invariants
- **Infrastructure**: SQLite persistence and resolved projection
- **Contracts**: resolved-position DTO shape

## Files Likely Impacted

### Domain

- `src/FloorplanFit.Domain/FloorPlans/`  
  add position overlay entity + enums/source-kind support

### Contracts

- `RoomLabelDto`
- `OpeningLabelDto`
- `CuratedPlanArtifactDto`
- possibly `FloorPlanReviewSessionDto`

### Application

- new abstraction for artifact-position repository
- new save/restore handlers

### Infrastructure

- SQLite schema initializer
- position repository
- review session reader projection logic

### Desktop

- `FloorPlanPreviewControl`
- preview hit-testing/rendering helpers
- `FloorPlanReviewViewModel`
- `ReviewFloorPlanWindow.axaml`
- label preview renderer / geometry preview renderer as needed

## Acceptance Criteria

1. Any `RoomLabel` can be dragged in preview and reopens at the curated canonical position.
2. Any `OpeningLabel` can be dragged in preview and reopens at the curated canonical position.
3. Any `OpeningCandidate`, `FixedPlanComponent`, or `ProtectedDetailAssembly` can be dragged in preview and reopens at the curated canonical position.
4. `WallCandidate` cannot be dragged.
5. `PinchMarker` cannot be dragged after placement.
6. A moved artifact can be restored to detected position.
7. The review session always renders resolved curated position, not raw extractor position.
8. The design keeps extracted geometry/records auditable and unchanged.

## Testing Strategy

### Domain / Application

- validation tests for supported vs unsupported movable artifact kinds
- save-position tests for absolute point mode
- save-position tests for translation mode
- restore-position tests

### Infrastructure

- schema test for `floorplan_artifact_positions`
- review reader test for resolved room-label coordinates
- review reader test for resolved opening-label coordinates
- review reader test for translated curated geometry paths
- regression test that extracted source data remains unchanged

### Desktop

- hit-test/drag tests for room labels
- hit-test/drag tests for opening labels
- hit-test/drag tests for curated geometry artifacts
- regression test that walls are not draggable
- regression test that pinch markers are not draggable
- restore-position UI flow tests

## Tradeoffs

### Pros

- canonical curated position becomes real persisted truth
- extracted source remains auditable
- same resolved-position model can feed preview, fit, and export later
- movement is limited to robust translation semantics, avoiding CAD-like complexity

### Cons

- label dragging requires new text hit-testing logic
- geometry movement requires resolved-path translation logic in the read model
- there is a new distinction between extracted geometry and resolved curated geometry that future developers must respect

## Alternatives Considered

### 1. Visual-only drag layer in Desktop

Rejected.

Why:

- not canonical
- not reusable
- dishonest product behavior

### 2. Rewrite extracted source coordinates/geometry directly

Rejected.

Why:

- destroys extractor audit truth
- makes root-cause analysis harder
- mixes extraction with curation responsibilities

### 3. Full geometry rewriting for every moved artifact

Rejected for this slice.

Why:

- too heavy
- unnecessary for move-only semantics
- translation overlays are enough for the problem the user actually wants solved

## Recommendation

Implement canonical non-wall positioning as a **position overlay system**:

- absolute curated point overrides for labels
- canonical translation overlays for non-wall geometry artifacts
- resolved position consumed everywhere in review
- walls and pinch markers explicitly excluded

This is the most serious, modular, and robust path for the repository as it exists today.
