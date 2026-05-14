---
created: 2026-05-13
updated: 2026-05-13
project: floorplan-fit
type: implementation
status: fixed
replaces: 2026-05-13 - Native dimension edge drag now preserves C-shape.md
replaced_by:
---

# Native dimensions now preserve authored CAD families without a center grip

## What

Completed the larger native-dimension rewrite so preview and curation now treat AutoCAD linear dimensions as one authored CAD entity instead of collapsing them to a generic editable `C`.

## Why

The earlier C-shape fix solved a visible mirroring symptom, but it still left five product bugs alive:

1. preview rendering truncated authored dimensions to three line primitives
2. a fake center/body grip was still visible
3. body/text clicks started edit immediately instead of after a drag threshold
4. split / leader dimensions were rebuilt as if they were generic three-line dimensions
5. selecting a dimension still enabled the Text Tool even though the dimension text is part of the CAD entity

## Where

- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionShape.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionEditor.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionHitTester.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewProjector.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewInspectorCoordinator.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

## Implementation

- Renderer: `CreateProjectedSegments(...)` now preserves the full authored `LinePrimitives` list instead of truncating/deduping to three segments.
- Handles: only the two visible terminal extents are exposed as grips; the center/body control is implicit and no longer rendered as a third handle.
- Hit semantics: text and line hits now resolve to semantic body editing with a world-space reference point, instead of guessing a nearest visible handle.
- Interaction: body clicks select the dimension immediately, but body drag only starts after a movement threshold, matching AutoCAD-like intent better.
- Geometry editing: the native editor now resolves authored linear families from terminal inserts plus local axis/normal space, and preserves topology for:
  - `StandardLinearC`
  - `SplitLinear`
  - `LeaderLinear`
- Inspector/VM wiring: dimensions no longer enable the Text Tool; the inspector hint now describes endpoint stretch + implicit body drag + restore.

## Result

- idle preview now matches authored dimension geometry much more faithfully
- no more central visible grip on dimensions
- body/text drags move the dimension line implicitly without treating text as a free label
- split and leader dimensions survive edits without collapsing into a generic `C`
- dimension selection no longer routes the user into the Text Tool

## Verification

- Focused slice:
  - `dotnet test .\\tests\\FloorplanFit.Desktop.Tests\\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~NativeDimensionPreviewControlTests|FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~PreviewInteractionCoordinatorTests|FullyQualifiedName~FloorPlanReviewViewModelTests" --artifacts-path .\\.artifacts-test\\desktop-dimension-green2`
  - **93/93 PASS**
- Full desktop suite:
  - `dotnet test .\\tests\\FloorplanFit.Desktop.Tests\\FloorplanFit.Desktop.Tests.csproj --artifacts-path .\\.artifacts-test\\desktop-full-after-dimensions`
  - **141/141 PASS**
- Pre-existing warning still present and out of scope:
  - `src/FloorplanFit.Application/FloorPlans/Review/OpenFloorPlanReviewSessionHandler.cs(20,41) CS8625`
