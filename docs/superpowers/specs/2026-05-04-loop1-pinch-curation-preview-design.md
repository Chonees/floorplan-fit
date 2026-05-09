# Loop 1 Pinch Curation Preview Design


> [!WARNING] SUPERSEDED ? 2026-05-09
> This prototype design is historical. It correctly introduced pinch-first curation, but its auto-staged `CuratedWall` implementation detail is no longer active.
>
> Current truth: pinches belong to named `PinchGroup`s, the group owns the Width/Height axis, and Loop 1 curation also includes CAD-faithful artifacts such as room labels, openings, fixed components, protected details, and future dimensions.


**Date:** 2026-05-04
**Status:** Approved for prototype implementation

## Goal

Replace the current accept-first wall curation UX with a pinch-first review flow that lets the user keep extracted geometry visible by default, reject bad lines, place grouped pinch markers on strategic lines, and preview grouped compression directly in the review canvas.

## Product Scope

- **Loop:** Loop 1 floor plan curation
- **Purpose:** Teach the future site-plan fit engine where width or height can be reduced without globally scaling the floor plan.
- **Promise:** The user marks intentional compression zones once; later adaptation reuses those zones.

## MVP Rules

1. Extracted wall candidates remain visible by default in review.
2. The user rejects invalid candidates instead of manually accepting every good one.
3. A pinch is authored on a selected line.
4. Each pinch belongs to a named group tagged with `Width` or `Height`.
5. Each pinch stores a recortable maximum in millimeters.
6. Preview drag is runtime-only; no persisted anchor side is required.
7. The preview may use a simple grouped compression transform for the prototype.
8. Existing rich wall metadata editing is removed from the main UX for this prototype.

## Domain Design

### Existing reality to preserve

- A draft curation still exists per floor plan version.
- Publishing still happens from a `FloorPlanCuration`.
- The system still needs a persistent representation per reviewed line so rejects and future pinch attachment have a stable home.

### New prototype model

#### Auto-staged curated walls

When a review session opens, the draft curation auto-stages one `CuratedWall` per non-rejected extracted candidate if it does not already exist.

This is an implementation detail, not the primary UX.

Why:
- keeps publish semantics stable
- gives pinches a stable curation-owned target
- avoids a breaking rewrite of the whole Loop 1 persistence story

#### PinchGroup

A pinch group represents one compressible intent axis.

Fields:
- `Id`
- `FloorPlanCurationId`
- `Name`
- `AxisTag` (`Width` or `Height`)
- `SortOrder`

#### PinchMarker

A pinch marker represents one strategic editable cut on one reviewed line.

Fields:
- `Id`
- `FloorPlanCurationId`
- `PinchGroupId`
- `CuratedWallId`
- `GeometryPathId`
- `PositionRatio`
- `MaxTrimMm`
- `SortOrder`

## Application Design

### New behavior

- Open review session:
  - start/resume draft curation
  - sync auto-staged curated walls from non-rejected candidates
  - load review session including pinch groups and pinch markers
- Reject candidate:
  - mark candidate rejected
  - remove staged curated wall for that candidate
  - remove pinches attached to that staged wall
- Create pinch group
- Add pinch marker to selected reviewed line
- Remove pinch marker

### Publish rule for prototype

Keep publish simple:
- at least one staged curated wall must remain after rejects
- pinches are optional for publish, but when present they are persisted as part of the curation

## Desktop / UX Design

### Main review screen

Keep only the UI that matters for this prototype:
- candidate list
- pinch group list
- pinch editor actions
- preview canvas
- reject + publish actions

Remove from the primary UX:
- accept candidate button
- stable wall id editing
- wall role/mobility/protection metadata editor
- curated wall inspector list

### Pinch authoring flow

1. User selects a line in the candidate list or preview.
2. User creates/selects a pinch group.
3. User enters max trim mm.
4. User clicks `Add Pinch`.
5. The pinch is saved on the selected line using the current selection point or default line midpoint.

### Preview flow

1. User selects a pinch group.
2. User drags near a matching outer canvas edge.
3. Runtime preview computes requested compression amount.
4. Compression is distributed equally across the group's pinches, capped by each pinch's `MaxTrimMm`.
5. Preview uses a simple piecewise transform by pinch coordinate along the selected axis.

## Preview transform for prototype

### Width group

Pinches belong on vertical lines.
Dragging left/right requests width reduction.
The preview applies grouped X-axis compression using pinch X coordinates.

### Height group

Pinches belong on horizontal lines.
Dragging top/bottom requests height reduction.
The preview applies grouped Y-axis compression using pinch Y coordinates.

### Prototype tradeoff

This transform is intentionally simple and may not fully solve wall-thickness or topology-perfect adaptation. That is acceptable for the branch goal because the branch is a proof of concept for grouped pinch curation and interactive compression preview.

## Files expected to change

- Domain: new pinch entities/enums
- Application: new repos + handlers + review/session sync
- Contracts: new DTOs in review session
- Infrastructure: schema + repositories + review reader
- Desktop: simplified review VM/window + preview control/geometry helper
- Tests: application, infrastructure, desktop geometry, desktop VM/layout

## Risks

1. Preview transform may look correct for many straight-line cases but still be approximate.
2. Auto-staged curated walls are a transitional implementation device, not the final long-term domain model.
3. Existing review tests will need to move from accept-first assumptions to pinch-first assumptions.
