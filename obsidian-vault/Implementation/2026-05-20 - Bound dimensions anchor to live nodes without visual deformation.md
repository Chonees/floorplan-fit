---
date: 2026-05-20
type: implementation
status: superseded
replaces:
  - Implementation/2026-05-20 - Restored raw A-B dimension interval bindings.md
replaced_by: Bugs/2026-05-20 - Bound dimensions deform because live nodes drive visual transform.md
---

# Bound dimensions anchor to live nodes without visual deformation

> [!WARNING] Superseded
> Visual QA on 2026-05-20 showed that this implementation still encodes the wrong product semantic: live measurement nodes are used as the visual transform driver for dimensions. The corrected truth is captured in `../Bugs/2026-05-20 - Bound dimensions deform because live nodes drive visual transform.md`.

## What changed
Manual-verified interval-bound dimensions now recompute from the live preview positions of their saved measurement nodes on every active-axis pinch preview, even when the saved interval does not overlap the currently reduced band.

Linear associated dimensions also preserve their authored CAD appearance: the live node delta is projected onto the dimension's original authored axis before rebuilding the dimension geometry. This keeps horizontal dimensions horizontal, vertical dimensions vertical, and free-angle dimensions on their original angle instead of shearing toward arbitrary A/B node misalignment.

## Why
The articulation preview showed some node-bound dimensions staying behind when their numeric span was not reduced by the active pinch band. The old overlap gate skipped those dimensions entirely, so they could not translate with their attached nodes.

A second issue came from rebuilding linear dimensions directly from the raw live A/B points. If two live nodes were not perfectly aligned with the authored dimension orientation, the dimension line and extension geometry could skew/deform. The curation intent is that nodes drive anchoring, while the dimension's visual grammar remains the same CAD dimension.

## Current behavior
- `DimensionIntervalReactiveProjector` still requires a manual-verified binding and an active articulation band on the same axis tag.
- It no longer requires the binding interval to overlap the selected band before recomputing. Bound dimensions on the active axis follow their live nodes even if the measured value stays unchanged.
- `DimensionGeometryProjector.RebuildAssociatedDimension` anchors at the live start node and projects the live end-node delta onto the authored dimension axis.
- Horizontal and vertical dimensions ignore perpendicular live-node drift for visual rebuilds.
- Free-angle dimensions preserve the authored angle by using the dot-product component of the live A/B vector on that authored axis.

## Verification
- RED first: `DimensionIntervalReactiveProjectorTests` failed on non-overlap anchoring and on horizontal/vertical/free-angle visual deformation cases.
- GREEN:
  - `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~DimensionIntervalReactiveProjectorTests|FullyQualifiedName~ReactiveDimensionProjectorTests|FullyQualifiedName~ExportAdjustedDxfHandlerTests" --nologo --artifacts-path .testartifacts\dotnet-test-artifacts-anchor-style-application-final` ? 20/20 passed.
  - `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~MeasurementBindingFloorPlanReviewViewModelTests|FullyQualifiedName~MeasurementBindingPreviewLayerRendererTests|FullyQualifiedName~PreviewRenderComposerTests|FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~NativeDimensionPreviewControlTests" --nologo --artifacts-path .testartifacts\dotnet-test-artifacts-anchor-style-desktop` ? 86/86 passed.

## Important nuance
This supersedes the phrase "raw A/B dimension geometry" from the previous note. The saved nodes are still the source of truth, but linear CAD dimensions now rebuild as style-preserving projections on the authored dimension axis instead of as a visually raw two-point segment.


## Follow-up hardening
A later screenshot showed that basic axis projection was not enough for real CAD dimension shapes: split dimensions, reversed definition-point order, and dimensions whose line sits below/above/left/right could still deform because only the first simple line primitives were rebuilt while additional primitives kept stale authored coordinates.

The reactive linear rebuild now applies one local-coordinate transform to every primitive in the dimension:
- live nodes are sorted along the authored dimension axis so reversed dimensions keep their original definition-point direction,
- all line primitives, line segments, text, inserts, circles, arcs, and solids are transformed together,
- the U coordinate is stretched/translated along the authored axis,
- the V coordinate is translated as a single normal offset, preserving the side/look of the dimension.

Extra RED/GREEN coverage added `Project_preserves_reversed_split_dimension_shape_when_dimension_line_is_below_the_measure`, which failed before the transform because the reversed/below split dimension rebuilt as if binding-node order were definition-point order and left extra primitives stale.
