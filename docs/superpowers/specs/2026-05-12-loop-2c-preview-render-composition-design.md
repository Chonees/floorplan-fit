# Loop 2C Preview Render Composition Design

## Goal

Execute the third slice of **Loop 2** as a **surgical, low-risk refactor** that removes preview render composition and layer orchestration from `FloorPlanPreviewControl` without changing Loop 1 product behavior.

The immediate objective is to stop using `FloorPlanPreviewControl` as the place where workspace chrome, compression handles, base geometry strokes, curated-vs-detected artifact branches, dimensions, text labels, pinch markers, and active dimension handles are all composed inline inside one render method.

## Product Loop + Architecture Layer

### Product scope

This work belongs to **Loop 1 shared foundation**.

It does **not** add new curation features, new DXF semantics, or new site-fit behavior. It only restructures the render composition behind the existing CAD-faithful review surface.

### Architecture layer

- **Desktop** — primary
- **Documentation / repository truth** — supporting

## Problem

After Loop 2A and Loop 2B, `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` is materially smaller, but it still owns the preview render composition brain.

Verified current state on **2026-05-12**:

- `FloorPlanPreviewControl.cs` is now **1363 lines**
- `Render(DrawingContext context)` spans about **122 lines**
- the file already delegates:
  - interaction transitions to `PreviewInteractionCoordinator`
  - collection observer lifecycle to `PreviewCollectionObserverHub`
- but `Render(...)` still mixes:
  - workspace background and border chrome
  - compression-handle layer
  - preview-scene preparation
  - base geometry ordering and stroke drawing
  - curated-artifact vs opening/fixed/protected branch selection
  - dimension line rendering
  - text label rendering
  - pinch marker rendering
  - active dimension handle rendering

That is the wrong responsibility boundary.

The next safe move is **not** `FloorPlanReviewViewModel`.

The next safe move is to extract the **preview render composer**.

## Chosen Approach

Start Loop 2C with a **composition-only extraction**:

- introduce a small immutable `PreviewRenderScene` snapshot
- introduce one `PreviewRenderComposer` that owns render-layer ordering and drawing orchestration
- let `FloorPlanPreviewControl` keep building render inputs with its existing helpers for now

This is intentionally narrower than a full render rewrite. The control will still prepare:

- preview geometry
- moved artifact geometry
- rendered labels
- rendered dimensions
- artifact index

but it will stop deciding the final layer order inline.

## Scope

### In scope

- extracting render-layer orchestration out of `FloorPlanPreviewControl.Render(...)`
- introducing a render-scene snapshot object
- moving base geometry stroke drawing into the composer
- moving the curated-vs-detected artifact branch into the composer
- keeping preview layer order explicit in one dedicated file

### Out of scope

- changing render behavior or visual semantics
- changing preview data-preparation helpers such as:
  - `BuildPreviewGeometry(...)`
  - `ApplyActiveArtifactMoveToGeometry(...)`
  - `BuildRenderedRoomLabels()`
  - `BuildRenderedOpeningLabels()`
  - `BuildRenderedDimensions()`
- reworking renderer internals
- touching `PreviewInteractionCoordinator`
- touching `FloorPlanReviewViewModel`

## Verified Hotspot

The current `Render(...)` method does all of this in order:

1. workspace render
2. border rectangle
3. compression handles
4. geometry/viewport existence checks
5. preview geometry + artifact move projection
6. room/opening label and dimension preparation
7. artifact index creation
8. base path filtering and ordering
9. raw geometry line drawing
10. curated-artifact branch OR opening/fixed/protected branch
11. dimension line rendering
12. room/opening/dimension text rendering
13. pinch marker rendering
14. dimension handle rendering

That sequencing is part of current behavior and must stay stable, but it should live in a dedicated composition file, not in the control shell.

## File Structure Direction

### Existing owner that stays

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - remains the Avalonia control shell
  - remains the owner of preview state and helper methods
  - remains the place where the render scene is assembled from current state

### New focused collaborators

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderScene.cs`
  - immutable snapshot of everything the render composer needs

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewRenderComposer.cs`
  - owns render-layer order
  - owns branch selection between curated artifacts and detected artifact layers
  - owns base-geometry stroke rendering

### Existing renderers reused

- `PreviewWorkspaceRenderer`
- `CompressionHandlePreviewLayerRenderer`
- `CuratedArtifactPreviewLayerRenderer`
- `OpeningPreviewLayerRenderer`
- `FixedPlanComponentPreviewLayerRenderer`
- `ProtectedDetailPreviewLayerRenderer`
- `DimensionPreviewLayerRenderer`
- `CadTextPreviewLayerRenderer`
- `PinchMarkerPreviewLayerRenderer`

This design reuses existing renderers instead of re-inventing them.

## Responsibility Split

## `FloorPlanPreviewControl`

After Loop 2C, the control should own:

- calling `base.Render(context)`
- reading current shell state (`Bounds`, `PreviewAxisTag`, selected ids, active edit state, DTO collections)
- building a `PreviewRenderScene`
- passing that scene to the composer

It should **not** keep final layer-order decisions inline.

## `PreviewRenderScene`

The scene should carry the prepared inputs needed by the composer, for example:

- `Rect Bounds`
- `PinchAxisTag? AxisTag`
- `bool IsPinchPlacementArmed`
- `FloorPlanPreviewGeometry.PreviewViewport? Viewport`
- `IReadOnlyList<GeometryPathDto> PreviewGeometry`
- `IReadOnlyList<RoomLabelDto> RoomLabels`
- `IReadOnlyList<OpeningLabelDto> OpeningLabels`
- `IReadOnlyList<DimensionDto> Dimensions`
- `PreviewArtifactGeometryIndex ArtifactIndex`
- `IReadOnlyList<OpeningCandidateDto>? OpeningCandidates`
- `IReadOnlyList<FixedPlanComponentDto>? FixedPlanComponents`
- `IReadOnlyList<ProtectedDetailAssemblyDto>? ProtectedDetailAssemblies`
- `IReadOnlyList<CuratedPlanArtifactDto>? CuratedPlanArtifacts`
- `IReadOnlyList<PinchMarkerDto>? PinchMarkers`
- `Guid? HighlightGeometryPathId`
- `Guid? HighlightRoomLabelId`
- `Guid? HighlightOpeningLabelId`
- `Guid? HighlightDimensionId`
- `Guid? PreviewPinchGroupId`
- `string? PreviewAxisTag`
- `FloorPlanPreviewControl.DimensionHandleKind? ActiveDimensionHandleKind`

The scene should be render-only data. No behavior beyond simple helpers.

## `PreviewRenderComposer`

The composer should own:

- workspace + border + compression-handle sequence
- early-return behavior when there is no geometry or viewport
- base geometry ordering and stroke rendering
- curated-artifact branch versus opening/fixed/protected branch
- dimensions, labels, pinch markers, and active handles order

This file should answer one question:

> given a prepared preview render scene, in what exact order should the CAD-faithful layers be drawn?

## Recommended Shape

The safest shape is:

- one immutable scene record
- one static composer

Directionally:

```csharp
internal sealed record PreviewRenderScene(...);

internal static class PreviewRenderComposer
{
    public static void Render(DrawingContext context, PreviewRenderScene scene) { ... }
}
```

Inside the composer, the only non-trivial helper should be something like:

- `ResolveOrderedBasePaths(scene)`

That helper can stay pure and testable.

## Why This Is the Right Slice

This is the right next step because it:

- extracts a real remaining hotspot
- keeps the render-layer truth explicit
- avoids reopening interaction or observer concerns
- prepares a later optional slice if we want to move scene preparation out of the control too

## Alternatives Considered

### 1. Extract only a base-geometry stroke renderer

Too shallow. It would remove the manual `DrawLine(...)` loop but would leave layer orchestration and branching in the control.

### 2. Extract both scene preparation and composition at once

Too broad for the current low-risk sequence. The preparation helpers already work and are not the sharpest risk today.

### 3. Jump straight to `FloorPlanReviewViewModel`

Too early. The verified next Desktop hotspot is still inside the preview shell, not the review VM.

## Testing Strategy

This slice should stay **TDD-first**.

### New tests

- `PreviewRenderComposerTests`
  - should pin base path ordering and exclusion logic
  - should pin curated-artifact branch selection vs detected-artifact branch selection

### Existing shell regression to preserve

- `FloorPlanPreviewControlTests`
  - should keep a source-facing architectural guard that the control now delegates to `PreviewRenderComposer`
  - can assert that the inline `context.DrawLine(...)` loop no longer lives in the control source

### Existing renderer tests reused

Current renderer-specific tests remain valid and should continue passing without modification unless the new files change invocation boundaries only.

## Acceptance Criteria

1. `FloorPlanPreviewControl` becomes materially smaller again.
2. `Render(...)` no longer contains the full layer-order brain inline.
3. Base geometry ordering and curated-vs-detected artifact branching are owned by the composer.
4. User-visible preview behavior remains unchanged.
5. Renderers stay reused; this slice does not duplicate their logic.

## Follow-up After This Slice

If Loop 2C lands green and stable, the next follow-up can decide whether to:

- extract render-scene preparation from the control into a builder, or
- move on to **Loop 3** and start decomposing `FloorPlanReviewViewModel`

The point is to finish the preview-shell cleanup in ordered slices, not to reopen another mixed refactor.
