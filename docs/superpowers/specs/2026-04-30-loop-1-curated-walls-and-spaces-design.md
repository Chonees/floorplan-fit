# Loop 1 Curated Walls and Spaces Design

**Goal:** Close Loop 1 with a reusable canonical curation model that turns an imported floor plan DXF into exact curated walls, minimal curated spaces, user-defined constraints, and a publishable active version ready for future site-plan adaptation.

## Relationship to previous designs

This design builds on:

- `docs/superpowers/specs/2026-04-25-slice-1-import-foundation-design.md`
- `docs/superpowers/specs/2026-04-29-slice-1-executable-design.md`

The earlier documents established the import, managed storage, hashing, Desktop shell, and SQLite persistence foundation. This document defines the next major Loop 1 milestone: turning an imported floor plan into a canonical, editable, publishable curation that future adaptation flows can trust.

## Product scope

- **Product loop:** Loop 1 — floor plan curation
- **Architecture layers involved:** Desktop, Application, Domain, Infrastructure, Contracts

The output of this slice is no longer just “an imported floor plan”. The output is a reusable semantic base for the product:

- curated walls with exact geometry and movement intent
- minimal curated spaces with exact measurements
- basic structured constraints and free-form notes
- a published active curation version for future Loop 2 runs

## Why this design changed

The repository docs originally leaned toward a `walls-only` v1. That was a good simplification for the fit engine, but it is not enough for the real MVP once the user clarified these needs:

- exact measurements per room
- explicit room identity
- open-plan cases such as living + kitchen sharing one space
- rules such as “do not touch this area”
- minimum bathroom or room dimensions
- wall-level exactness such as thickness or assembly hints like `2x4` / `2x6`

Because of that, the correct Loop 1 close is:

- **not** a CAD-rich editor
- **not** a blind list-based curation
- **not** pure `walls-only`

It is a semantic core with:

- exact curated walls
- minimal curated spaces
- basic room and wall constraints
- a minimal real review canvas

## In scope

This Loop 1 close includes:

1. Extracting technical wall candidates from an imported floor plan version
2. Persisting those candidates as temporary extraction results
3. Starting or resuming a `FloorPlanCuration` draft
4. Accepting or rejecting wall candidates
5. Curating accepted walls with exact geometry-backed metadata
6. Curating minimal spaces / rooms with exact measurements
7. Capturing user intent as:
   - free-form notes
   - structured constraints
8. Publishing a canonical curation version
9. Tracking an active published curation per floor plan template
10. Showing a minimal review UI with:
    - candidate list
    - canvas highlight
    - metadata inspector
    - draft save
    - publish

## Out of scope

This slice does not include:

- site plan import
- envelope extraction
- fit proposal generation
- advanced CAD-like editing
- merge/split editing tools with rich manual geometry manipulation
- advanced room auto-detection
- door/window active semantics
- legal rule packs
- AI or opaque heuristics
- creation of brand-new walls as part of the base curation

New wall creation belongs first to Loop 2 adaptation proposals. If the user later wants to promote a successful adaptation back into the reusable floor plan base, that promotion becomes a new Loop 1 published curation version.

## End-to-end Loop 1 flow

The full Loop 1 flow becomes:

`Import DXF -> Extract wall candidates -> Start curation draft -> Review visually -> Curate walls -> Curate spaces -> Save draft -> Publish active curation`

More explicitly:

1. The user imports a floor plan DXF.
2. The system institutionalizes the file through managed storage and persistence.
3. The user triggers wall extraction for that imported floor plan version.
4. The system persists technical `ExtractedWallCandidate` records.
5. The user opens a review session.
6. The review UI shows the detected candidates and highlights them on a minimal canvas.
7. The user accepts or rejects candidates.
8. The user assigns wall semantics to accepted candidates.
9. The user defines minimal spaces / rooms and their exact measurements.
10. The user adds notes and structured constraints.
11. The user saves a draft.
12. The user publishes a canonical curation version.
13. The floor plan template points to that curation as its active reusable base.

## Library states

The Library must show these states:

1. **Imported**  
   The floor plan exists in the repository but has no extraction results yet.

2. **Extracted**  
   The floor plan has technical extracted wall candidates, but no curation draft yet.

3. **Curated Draft**  
   The floor plan has an in-progress draft curation.

4. **Published**  
   The floor plan has an active published curation reusable by future site-plan work.

## Layer responsibilities

### Desktop

Responsible for:

- Library actions for extraction, curation, resume, and publish
- minimal review screen
- candidate selection
- canvas highlighting
- metadata editing surface
- draft save and publish triggers

Desktop does not own business rules for curation validity, version promotion, or persistence semantics.

### Application

Responsible for:

- orchestrating extraction
- orchestrating curation lifecycle
- draft creation / resume / publish rules
- wall candidate accept / reject
- curated wall metadata updates
- curated space metadata updates
- structured constraint persistence orchestration
- mapping review state into UI-facing DTOs

### Domain

Responsible for:

- extraction result identity and status
- curation version semantics
- curated wall invariants
- curated space invariants
- publish state transitions
- active published curation rules

### Infrastructure

Responsible for:

- actual wall extraction implementation
- geometry persistence
- SQLite schema and repositories
- loading review sessions from SQLite

### Contracts

Responsible for:

- review session DTOs
- candidate DTOs
- curated wall DTOs
- curated space DTOs
- commands / responses exposed to Desktop

## Data model

### Existing geometry base reused

The current technical document already defines:

#### `geometry_paths`

- `id`
- `is_closed`
- `bounding_box_json`
- `serialized_format`

#### `geometry_segments`

- `id`
- `geometry_path_id`
- `segment_order`
- `segment_type`
- coordinates / curve data

That geometry foundation must now serve:

- extracted wall candidates
- curated walls
- curated spaces
- future proposal geometry in Loop 2

## Technical extraction layer

### `wall_extraction_runs`

This design continues the technical flow already suggested in the architecture doc.

Minimum columns:

- `id`
- `floorplan_version_id`
- `status`
- `started_at_utc`
- `finished_at_utc`
- `extractor_version`
- `notes`

### `extracted_wall_candidates`

These remain **technical temporary output**, not business truth.

Minimum columns:

- `id`
- `wall_extraction_run_id`
- `source_entity_ref`
- `source_layer`
- `geometry_path_id`
- `thickness_mm`
- `confidence`
- `detection_notes`
- `candidate_status` (`Pending`, `Accepted`, `Rejected`)
- `sort_order`

### Important rule

`ExtractedWallCandidate` is not the canonical domain artifact.  
It is the technical proposal from which humans build canonical curation.

## Canonical curation layer

### `floorplan_curations`

Minimum columns:

- `id`
- `floorplan_version_id`
- `curation_version`
- `status` (`Draft`, `Published`, `Superseded`)
- `based_on_curation_id` nullable
- `notes`
- `created_at_utc`
- `published_at_utc` nullable

### Publish rule

- a `Published` curation is immutable
- later edits do not mutate the published row
- later edits create a new `Draft`
- that draft may be based on the latest published curation
- when published, it becomes the new active published curation

This is what preserves auditability and lets the user improve the floor plan over time without destroying history.

### `curated_walls`

Canonical, reusable wall records.

Minimum columns:

- `id`
- `floorplan_curation_id`
- `stable_wall_id`
- `source_candidate_id`
- `source_entity_ref`
- `geometry_path_id`
- `wall_role`
- `mobility_level`
- `protection_level`
- `thickness_mm`
- `assembly_code`
- `height_mm` nullable
- `is_exterior`
- `is_structural_hint`
- `wall_group_id` nullable
- `sort_order`
- `notes`

### Curated wall precision policy

The first publishable curation must know enough to support future fit decisions:

- exact wall geometry in plan
- exact wall length by derivation from geometry
- thickness
- wall role
- movement / protection intent
- exterior / structural hints
- grouping when relevant

`height_mm` is useful metadata for future growth, but it is not a hard dependency of the first 2D fit engine slice.

### `curated_wall_groups`

Minimum columns:

- `id`
- `floorplan_curation_id`
- `group_code`
- `name`
- `group_type`
- `priority`

### `curated_wall_joins`

Minimum columns:

- `id`
- `floorplan_curation_id`
- `wall_a_id`
- `wall_b_id`
- `junction_type`
- `junction_x`
- `junction_y`

Join editing may remain minimal in the first slice, but preserving basic wall relationship structure is valuable.

## Minimal curated spaces

This is the key extension beyond pure `walls-only`.

### `curated_spaces`

Minimum columns:

- `id`
- `floorplan_curation_id`
- `stable_space_id`
- `name`
- `space_type`
- `geometry_path_id`
- `area_square_meters`
- `min_width_mm`
- `min_depth_mm`
- `is_protected`
- `functional_tags_json`
- `sort_order`
- `notes`

### Space semantics rule

The model must support both:

- clearly bounded rooms
- open-plan spaces such as living + kitchen sharing one geometric space

For open-plan cases, the preferred first-slice design is:

- one real geometry
- one `space_type` such as `OpenPlan`
- multiple functional tags in `functional_tags_json`

That is better than inventing fake dividing walls that do not exist in the source house.

## Notes and constraints

### `constraint_intent_notes`

Free-form user intent records.

Minimum columns:

- `id`
- `scope_type`
- `scope_id`
- `raw_text`
- `status`
- `created_at_utc`

These notes are:

- saved
- shown
- auditable

They should not directly mutate geometry in the first slice.

### `structured_constraints`

Structured rules intended for future deterministic reasoning.

Minimum columns:

- `id`
- `scope_type`
- `scope_id`
- `constraint_kind`
- `constraint_strength`
- `target_type`
- `target_id`
- `weight`
- `payload_json`
- `source`
- `created_at_utc`

### First-slice constraint kinds

For walls:

- `lock_wall`
- `protect_wall`
- `max_wall_move`
- `preserve_group`
- `preserve_facade`

For spaces:

- `preserve_space`
- `min_space_area`
- `min_space_width`
- `min_space_depth`

This gives the MVP a clean path to express rules like:

- “do not touch this wall”
- “protect this facade”
- “this bathroom must remain at least this large”
- “this space cannot shrink below this width”

## Template active truth

The current repository already has `FloorPlanTemplate.current_version_id`, but that field points to the active imported floor plan version, not necessarily the active reusable curation.

### Required extension

`floorplan_templates` should gain:

- `active_published_curation_id` nullable

### Rule

- `current_version_id` = latest active imported floor plan version
- `active_published_curation_id` = active canonical reusable curation

These concepts must remain separate.

## Application use cases

The Application layer should introduce small focused handlers rather than one giant curation service.

### `ExtractWallCandidatesHandler`

Responsibilities:

- load the floor plan version
- run the extractor
- persist a `wall_extraction_run`
- persist `extracted_wall_candidates`
- transition Library state to `Extracted`

### `GetFloorPlanReviewSessionHandler`

Responsibilities:

- load floor plan, extraction state, draft curation, curated walls, curated spaces, notes, and structured constraints
- return the model required by the review UI

### `StartOrResumeCurationHandler`

Responsibilities:

- create a draft if none exists
- resume the latest draft if present
- if the latest truth is published, allow creating a new draft based on it

### `AcceptWallCandidateHandler`

Responsibilities:

- mark candidate as accepted
- create or reactivate the corresponding curated wall

### `RejectWallCandidateHandler`

Responsibilities:

- mark candidate as rejected
- remove or deactivate any curated wall derived from it in the current draft

### `UpdateCuratedWallMetadataHandler`

Responsibilities:

- update wall role
- update mobility / protection
- update wall assembly metadata
- update structural / exterior hints
- update grouping
- update notes

### `CreateOrUpdateCuratedSpaceHandler`

Responsibilities:

- create a new minimal space
- update its geometry reference and measurement metadata
- set protection and notes
- represent open-plan semantics through functional tags

### `SaveCurationDraftHandler`

Responsibilities:

- persist the current draft state without publishing

### `PublishFloorPlanCurationHandler`

Responsibilities:

- validate the draft is publishable
- create a published curation version
- update `active_published_curation_id`
- mark older published versions superseded if necessary

## Publishability rules

The first publish rule should stay strict enough to protect future work, but not so strict that the user cannot make progress.

A draft should be publishable only if:

1. it has at least one curated wall
2. all curated walls have a `stable_wall_id`
3. all curated walls have a `mobility_level`
4. all curated walls have a geometry reference
5. if spaces exist, each one has geometry and area

This is enough to make the output reusable without over-policing early iterations.

## Desktop review UI

The first UI should be intentionally minimal but spatially real.

### Layout

- **left panel:** candidate list and state summary
- **center:** review canvas
- **right panel:** inspector

### Required interactions

- select candidate
- highlight selected geometry on canvas
- accept / reject candidate
- edit curated wall metadata
- create or edit minimal spaces
- save draft
- publish

### Explicitly deferred

- rich CAD editing
- arbitrary wall drawing
- split / merge authoring tools
- precision handle dragging

The goal is trustworthy review and curation, not a general drafting workstation.

## Error handling

### Extraction failures

If extraction fails, the floor plan stays import-valid but extraction-invalid.

The system must:

- preserve the import truth
- surface an extraction failure message
- avoid partial candidate persistence

### Draft integrity failures

If a draft has invalid required metadata during publish:

- publish must fail cleanly
- the draft remains editable
- the UI must tell the user exactly what is missing

### Geometry consistency failures

If a wall or space references a missing `geometry_path_id`, the system must reject that operation instead of silently publishing broken data.

## Testing strategy

### Domain tests

Add focused tests for:

- curation publish transition rules
- immutable published behavior
- draft-from-published behavior
- curated wall invariants
- curated space invariants

### Application tests

Add tests for:

- extraction orchestration
- accept / reject flow
- draft save
- publish flow
- update of `active_published_curation_id`

### Infrastructure tests

Add integration tests for:

- geometry persistence
- extraction result persistence
- curation draft persistence
- publish persistence
- loading a full review session

### Manual Desktop verification

The first review UI must be manually verified for:

- extraction from a real fixture
- visible candidate list
- visible highlight
- draft save
- publish
- Library state transition

## Acceptance criteria

This Loop 1 close is complete only when all of these are true:

1. A floor plan can move from `Imported` to `Extracted`.
2. The user can open a real review session.
3. Extracted wall candidates can be accepted or rejected.
4. Accepted walls can be curated with exact geometry-backed metadata.
5. Minimal spaces can be created or curated with exact measurements.
6. Structured constraints and free-form notes can be persisted.
7. A draft can be saved and resumed.
8. A curation can be published.
9. A `floorplan_template` can point to an active published curation.
10. The published result is reusable as the canonical base for future Loop 2 adaptation.

## Loop boundary with future adaptation work

Loop 1 owns:

- canonical reusable house truth
- curated walls
- minimal curated spaces
- room / wall constraints
- publishable active base

Loop 2 owns:

- site-specific overrides
- envelope fit
- proposal generation
- proposal wall changes
- future creation of new walls as proposal-level adaptation

If a Loop 2 proposal becomes generally desirable, it may later be promoted into a new Loop 1 curation version. That promotion is an explicit workflow, not an automatic mutation of the base truth.

## Summary

The correct Loop 1 close is not “import DXF and stop”, and it is not “build a mini-AutoCAD first”.

The correct close is:

`imported floor plan -> extracted wall candidates -> curated walls + curated spaces + constraints -> draft -> published active curation`

That gives the product a real reusable semantic base strong enough for precise future fit work, while keeping the first serious curation slice bounded and teachable.
