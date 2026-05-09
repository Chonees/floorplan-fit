# Pinch-Native Curation Cleanup Design


> [!IMPORTANT] PARTIALLY SUPERSEDED ? 2026-05-09
> This design remains useful as historical context for removing the curated-wall review flow.
>
> However, its axis-tag-only simplification and removal of `PinchGroup` were superseded. The active model reintroduced named `PinchGroup`s so the future fit engine can trim specific zones, not every marker on the same axis.
>
> Current truth: `CuratedWall` is out of active review, but `PinchGroup` + `PinchMarker` are active durable curation data.


**Date:** 2026-05-04  
**Status:** Approved for branch-only refactor

## Goal

Convert Loop 1 review/curation from a curated-wall workflow into a pinch-native workflow whose only durable curation data is:

- rejected extracted candidates
- strategic pinch markers

The algorithmic goal is simple: later, when a floor plan does not fit a site plan, the fit engine should know exactly where width or height may be reduced.

## Product Scope

- **Loop:** Loop 1 floor plan curation
- **Architecture layers:** Domain, Application, Infrastructure, Desktop, Contracts
- **Branch assumption:** This branch is explicitly experimental, so aggressive cleanup is allowed.

## Problem

The current codebase still models review around `CuratedWall`, wall metadata editing, acceptance flows, and prototype pinch groups. That structure is heavier than the actual product goal.

The real curation truth for this MVP is smaller:

1. extracted lines are visible by default
2. invalid lines may be rejected
3. the user marks strategic pinch points
4. each pinch is tagged directly as `Width` or `Height`
5. each pinch stores how much trim it may absorb

Everything else is noise for this branch goal.

## Chosen Approach

### 1. Keep `FloorPlanCuration`, remove curated-wall review semantics

`FloorPlanCuration` remains the draft/publish envelope for review versions.  
What disappears from the active review model is `CuratedWall`.

We will remove `CuratedWall` and its DTO/repository/handlers from the current review flow and from branch code that no longer needs it.

### 2. Remove `PinchGroup`

Named pinch groups add ceremony without adding the essential information the fit engine needs.

Each pinch marker will now carry:

- `SourceCandidateId`
- `GeometryPathId`
- `AxisTag` (`Width` / `Height`)
- `PositionRatio`
- `MaxTrimMm`
- `SortOrder`

### 3. Make pinch markers the only canonical curation payload

The review session will expose:

- template summary
- geometry paths
- wall candidates
- pinch markers

No curated-wall inspector, no wall metadata, no accepted-wall layer.

### 4. Publish based on pinch readiness, not wall metadata

For this branch, publishing a curation will require:

- an existing draft curation
- at least one pinch marker

That keeps publish aligned with the actual business value of the branch.

## Domain Design

### Keep

- `FloorPlanCuration`
- `ExtractedWallCandidate`
- `PinchAxisTag`

### Remove from active branch flow

- `CuratedWall`
- `PinchGroup`

### New pinch marker shape

`PinchMarker`

- `Id`
- `FloorPlanCurationId`
- `SourceCandidateId`
- `GeometryPathId`
- `AxisTag`
- `PositionRatio`
- `MaxTrimMm`
- `SortOrder`

## Application Design

### Keep

- `StartOrResumeCurationHandler`
- `RejectWallCandidateHandler`
- `PublishFloorPlanCurationHandler`
- `GetFloorPlanReviewSessionHandler`
- `OpenFloorPlanReviewSessionHandler`

### Remove

- `AcceptWallCandidateHandler`
- `UpdateCuratedWallMetadataHandler`
- `SyncCuratedWallsFromCandidatesHandler`
- `CreatePinchGroupHandler`

### New/updated behavior

- `AddPinchMarkerHandler`
  - validates candidate exists and belongs to current extracted review set
  - persists pinch directly by candidate/geometry/axis

- `RemovePinchMarkerHandler`
  - deletes a marker by id

- `RejectWallCandidateHandler`
  - rejects the candidate
  - deletes pinch markers attached to that candidate within the draft curation

- `PublishFloorPlanCurationHandler`
  - requires at least one pinch marker instead of at least one curated wall

## Infrastructure Design

### Schema

Add or reshape `pinch_markers` to store:

- `source_candidate_id`
- `geometry_path_id`
- `axis_tag`
- `position_ratio`
- `max_trim_mm`
- `sort_order`

Remove active schema need for:

- `curated_walls`
- `pinch_groups`

We do not need to migrate old local experimental data cleanly on this branch.

### Read model

`SqliteFloorPlanReviewSessionReader` returns:

- geometry paths from candidates + pinch markers
- wall candidates
- pinch markers

No curated walls.

## Desktop / UX Design

### Main screen

The review window becomes a minimal pinch board:

1. **Lines** list
2. **Preview**
3. **Pinch tools**

### The only core actions

- select line
- choose axis: `Width` / `Height`
- enter max trim
- add pinch
- remove selected pinch
- reject line
- publish

### Preview

- selected line highlight
- pinch marker rendering
- visible width handles
- visible height handles
- runtime-only compression preview

No wall inspector, no metadata editing, no stable wall ids.

## Testing Strategy

### Application

- add pinch marker persists `AxisTag` and candidate binding directly
- reject candidate removes attached pinches
- publish requires at least one pinch marker

### Infrastructure

- review session reader returns pinch markers without curated walls
- pinch marker repository stores axis/candidate values correctly

### Desktop

- review VM loads pinch-native session
- add/remove pinch actions work off candidate + axis + trim
- layout test asserts simplified UI labels and absence of wall-metadata controls
- preview geometry tests cover hit-testing detail + preview handles logic

## Tradeoffs

### Pros

- model matches product truth
- much smaller UI and code surface
- less accidental architecture
- easier future fit-engine input

### Cons

- destructive branch refactor
- old curated-wall tests and persistence code will be removed or rewritten
- existing local experimental databases may not reflect the new branch model

## Recommendation

Do the aggressive cleanup in this branch now.  
If the experiment works, this branch becomes the new truth. If not, we fall back to another branch.
