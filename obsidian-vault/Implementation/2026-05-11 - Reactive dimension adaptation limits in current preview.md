---
created: 2026-05-11
project: floorplan-fit
type: implementation
status: active
replaces:
replaced_by:
---

# Reactive dimension adaptation limits in current preview

## What
Verified the current root-cause limits for why dragged preview geometry does not always make native dimensions adapt in real time.

## Why
The user reported that moving handles in preview does not reliably make dimensions follow or recalculate, so the codepath had to be traced before proposing any fix.

## Where
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionAssociationProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/MeasurableEdgeProjector.cs`
- `src/FloorplanFit.Domain/FloorPlans/FloorPlanArtifactPositionSourceKinds.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/FloorPlanArtifactPositionTests.cs`

## Verified limits
1. Reactive dimensions only update when `BuildRenderedDimensions(...)` can recompute live `MeasurableEdges` and the dimension has a fully resolved `DimensionAssociationDto`.
2. Associations are inferred only from edge endpoint proximity, not from arbitrary points along a wall or extension-line projections.
3. Live measurable edges are built only from `WallCandidate` and `OpeningCandidate`, not from `FixedPlanComponent` or `ProtectedDetailAssembly` geometry.
4. Geometry translation support explicitly excludes `WallCandidate`; wall translation is not yet part of the canonical edit model.
5. Dimensions marked `IsEdited` are intentionally excluded from reactive recomputation.
6. Dimensions with non-empty `RawTextOverride` keep their visible text instead of regenerating measurement text.
7. Compression handles only deform preview geometry when there are active pinch markers and a positive requested trim.

## Learned
- The current system is a partial associative preview, not yet a full CAD-style associative geometry engine.
- “Move” and “resize” are different concepts here: rigid translation should move dimension graphics, while a true span change should alter measurement value.
