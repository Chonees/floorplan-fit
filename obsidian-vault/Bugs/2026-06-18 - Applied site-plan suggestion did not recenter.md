# 2026-06-18 - Applied site-plan suggestion did not recenter

## Type
Bugfix

## Symptom
In Loop 2 Adjust-to-Site-Plan, applying an auto-fit suggestion could make the resized floor plan fit, but leave it visually off-center. The user then had to manually move the adjusted plan so it sat correctly inside the buildable area.

## Root cause
The initial projection centered the original structural footprint in the buildable area. A one-sided compression changes the adjusted footprint center, but `ApplyAutoFitPlan(...)` replaced the preview geometry without recentering the adjusted footprint.

## Fix
After applying the compression steps, `ApplyAutoFitPlan(...)` now:
- resolves the adjusted structural footprint using the same placement/fit geometry ids,
- computes the delta from adjusted footprint center to buildable-area center,
- translates preview geometry, labels, dimensions, and the auto-fit baseline,
- updates `ManualOffsetX/Y` so export placement matches the preview.

Compression markers are still recorded before this final recenter translation, so source-coordinate export remains stable.

## Verification
- Added layout-independent source test contract: `ApplyAutoFitPlan_recenters_adjusted_floor_plan_inside_buildable_area`.
- Source check confirmed recentering happens before replacing preview items and after source compression markers are recorded.
- `git diff --check` passed with CRLF warnings only.
- No .NET build was run per repo rule.

## Files
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs`

## Follow-up: compile fix
`ResolveAutoFitCenteringDelta(...)` initially referenced `GeometryBounds` and `TryBoundsOf`, which are scoped inside `SitePlanAdjustmentPreviewProjector` and not visible from `SitePlanAdjustmentViewModel`. The fix replaced that with a local nullable tuple fallback bounds resolver inside the method.

Verification:
- Source check confirmed no `GeometryBounds`/`TryBoundsOf` references remain in the ViewModel recenter helper.
- `git diff --check` passed for the touched ViewModel file.
- No .NET build was run by the agent per repo rule.
