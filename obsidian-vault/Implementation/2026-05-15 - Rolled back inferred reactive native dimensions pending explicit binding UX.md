---
project: floorplan-fit
repo: https://github.com/Chonees/floorplan-fit
status: active
updated: 2026-05-15
loop: shared-foundation
area: dimensions-runtime-rollback
replaces:
  - 2026-05-15 - Loop 3 export reuses reactive session projection
  - 2026-05-15 - Loop 4 native reactive ordinate projection
  - 2026-05-15 - Loop 5 native reactive radial and diameter projection
  - 2026-05-15 - Loop 6 topology-aware anchor remap for reactive dimensions
---

# 2026-05-15 - Rolled back inferred reactive native dimensions pending explicit binding UX

## What
Removed inferred native-dimension reactivity from the active product path. Pinch preview no longer reprojects dimensions from proximity-born associations, adjusted DXF export no longer rewrites dirty dimensions from inferred bindings, and endpoint drags no longer persist proximity-based semantic binding overrides.

## Why
Manual QA on the patio-side pinch proved the runtime was not trustworthy. The system could deform unrelated dimensions because the underlying associations were inferred by geometric proximity rather than by explicit user intent about what tramo or wall pair a dimension actually measures.

## Current truth
- Native dimensions remain CAD-faithful and editable.
- Body drag and endpoint drag still save the authored geometry snapshot override.
- Pinch preview keeps dimensions static/authored instead of pretending to adapt.
- Export writes the saved dirty dimension geometry as-is.
- Association/binding data remains available only as diagnostic groundwork, not as a product-trustworthy runtime driver.

## Reused groundwork kept on purpose
- `ReactiveDimensionProjector` and typed geometry rebuilders were kept in code as reusable groundwork.
- `DimensionBindings` / binding override persistence were not deleted from the model layer.
- Inspector copy now labels associations as diagnostic-only so the UI stops overselling them.

## Files
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/ExportAdjustedDxfHandler.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Desktop/ViewModels/Review/FloorPlanReviewInspectorCoordinator.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/Curation/ExportAdjustedDxfHandlerTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/NativeDimensionPreviewControlTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/DimensionEditingFloorPlanReviewViewModelTests.cs`

## Verification
- `dotnet test .\tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "ExportAdjustedDxfHandlerTests|FloorPlanDimensionOverrideHandlersTests"`
- `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "NativeDimensionPreviewControlTests|DimensionEditingFloorPlanReviewViewModelTests|FloorPlanReviewViewModelTests|FloorPlanPreviewControlTests" --artifacts-path .\.artifacts-test\desktop-rollback-wide`

## Next step
Build an explicit dimension-binding UX where the human selects the dimension and then selects the exact tramo / wall / feature pair it measures. Only those future manual-verified bindings should be allowed to drive pinch-aware adaptation.
