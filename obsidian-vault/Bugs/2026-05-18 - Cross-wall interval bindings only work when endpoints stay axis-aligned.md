# 2026-05-18 - Cross-wall interval bindings only work when endpoints stay axis-aligned

## What
The current measurement interval runtime allows start/end nodes from different geometry paths as long as they belong to the same corridor, but it rebuilds the dimension from the raw live 2D points of each node instead of projecting both endpoints onto the corridor axis/guide.

## Why it matters
This means same-wall vertical/horizontal cases work, and cross-wall cases only work when both clicks are already collinear on the measurement axis (same Y for Width, same X for Height). Total-width bindings across two different walls become diagonal/incorrect if the user clicks different heights.

## Evidence
- `SaveDimensionIntervalBindingHandler.cs` only requires both nodes to belong to the same corridor.
- `DimensionIntervalReactiveProjector.cs` resolves each node from its own `GeometryPathId` and calls `RebuildAssociatedDimension(...)` with those raw points.
- `DimensionGeometryProjector.cs` computes `ComputeDistance(startPoint, endPoint)` (Euclidean distance), not axis-projected distance.
- `MeasurementBindingPreviewLayerRenderer.cs` draws the active interval directly between the two raw node world points, which visually exposes the diagonal mismatch.
- Existing tests cover cross-path bindings only in a perfectly aligned case (`Y=120` on both endpoints), so they do not guard the misaligned cross-wall use case.

## Implication
The codebase partially supports "different walls in the same corridor", but it does not robustly support the stronger product use case "measure total width/height across different walls regardless of where on each wall the user clicks".
