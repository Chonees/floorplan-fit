---
created: 2026-05-11
project: floorplan-fit
type: implementation
status: active
replaces:
replaced_by:
---

# Live dimension recalculation now uses shared rebuild logic plus projected edge anchors

## What
Implemented a shared dimension rebuild pipeline so native dimension endpoint edits recalculate measurement/text in real time, and measurable-edge associations now resolve points projected onto wall/opening segments instead of only exact edge endpoints.

## Why
Pinch preview could deform wall geometry without updating many dimensions because endpoint-only association was too weak, and manual endpoint edits could move cota geometry without recalculating the visible measurement.

## Where
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionGeometryProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionAssociationProjector.cs`
- `src/FloorplanFit.Application/FloorPlans/Review/ReactiveDimensionProjector.cs`
- `src/FloorplanFit.Contracts/FloorPlans/DimensionAnchorReferenceDto.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionEditor.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/ReactiveDimensionProjectorTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Review/DimensionAssociationProjectorTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`

## Learned
- A single canonical rebuild path prevents drift between reactive dimensions and manual dimension editing.
- Projecting anchors onto measurable segments is enough to unlock many architectural dimensions that do not land exactly on segment endpoints.
- Persisted edited dimensions are still intentionally excluded from reactive associativity until edit semantics are split more explicitly (presentation-only vs measurement-geometry edits).
