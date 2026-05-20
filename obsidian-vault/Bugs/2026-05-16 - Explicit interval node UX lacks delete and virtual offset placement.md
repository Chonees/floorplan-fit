---
date: 2026-05-16
type: bug
area: native-dimensions
---

# Explicit interval node UX lacks delete and virtual offset placement

## Verified truth

- Current Desktop UX for explicit interval bindings is still **corridor-first** and not intuitive for end users.
- Creating a corridor requires selected preview geometry (HighlightGeometryPathId) plus a typed name.
- Creating a node requires:
  1. selecting a corridor,
  2. arming add-node mode,
  3. clicking supported preview geometry.
- The current node-placement UI only persists nodes **directly on clicked geometry** with:
  - offsetAlongAxis = 0
  - offsetNormal = 0
  so there is no user-facing support yet for virtual interior-gap nodes.
- There is currently **no Desktop delete/remove UX** for:
  - measurement corridors
  - measurement nodes
- Restore exists only for the dimension interval binding itself (Restore Authored/Static Binding).

## Evidence

- FloorPlanReviewViewModel.AddMeasurementCorridorAsync(...)
- FloorPlanReviewViewModel.ToggleMeasurementNodePlacement(...)
- FloorPlanReviewViewModel.HandlePreviewInteractionAsync(...)
- FloorPlanReviewViewModel.AddMeasurementNodeFromPreviewAsync(...)
- FloorPlanReviewViewModel.ResolveSelectedMeasurementSourceArtifact(...)
- repository/app search showed no remove corridor/node handlers or UI actions

## Product implication

The explicit binding architecture is stronger than proximity, but the current manual authoring UX is still QA-grade rather than admin-friendly product UX. The next healthy improvement is a dimension-first binding wizard plus remove actions for corridors/nodes, and eventually virtual node offsets for interior clear spans.
