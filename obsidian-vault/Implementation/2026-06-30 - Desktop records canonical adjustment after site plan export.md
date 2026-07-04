# 2026-06-30 - Desktop records canonical adjustment after site plan export

## Status
Connected the Desktop Adjust-to-Site-Plan export flow to the durable CanonicalFloorPlanAdjustment module.

## What changed
- `SitePlanAdjustmentViewModel` now accepts optional plan-set/floor-plan ids and `RecordCanonicalFloorPlanAdjustmentHandler`.
- After `ExportAdjustedSitePlanAsync` writes the combined adjusted DXF, it records a canonical adjustment using the exported path and current placement.
- `LastCanonicalAdjustmentId` exposes the recorded adjustment id for follow-on package/projection workflows.
- `SitePlanAdjustmentPreviewProjector.Build` passes the current floor-plan version id as the temporary PlanSetVersionId bridge and canonical floor-plan version id.
- `LibraryViewModel` resolves the recorder from DI and passes it into the adjustment view model when available.
- Added a Desktop ViewModel test proving the canonical adjustment is recorded after export.

## Boundary
- The flow records the canonical adjustment; it does not yet auto-project dependent sheets or auto-trigger package export from the UI.
- The bridge still uses `FloorPlanVersion.Id` as `PlanSetVersionId` until real PlanSetVersion records exist.
- No per-sheet fit engine was introduced.

## Verification
- RED evidence: `SitePlanAdjustmentViewModel` had no recorder/ids/LastCanonicalAdjustmentId before the test.
- `git diff --check` passed for touched files; no `dotnet test` or `dotnet build` was run due repo rule.
