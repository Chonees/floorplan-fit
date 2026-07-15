---
type: implementation
created: 2026-07-04
status: partial
---

# Electrical recipe-aware export slice

## What changed
- Added `ElectricalRecipeProjection.ProjectPoint(...)` as the pure formula for FloorPlan-driven Electrical projection.
- Formula implemented: Electrical source point -> registered FloorPlan source point -> canonical pinch recipe -> site/export point.
- Added optional `ProjectedPlanSheetExportRecipe` to projected sheet DXF export.
- `ExportProjectedPlanSheetHandler` now loads the confirmed sheet registration and canonical adjustment recipe when an Electrical projection has canonical compression steps.
- `ProjectedPlanSheetDxfExporter` applies recipe-aware projection to DXF entity points while keeping vectors/radii/text heights on the existing affine path.

## Files
- `src/FloorplanFit.Application/PlanSets/Projection/ElectricalRecipeProjection.cs`
- `src/FloorplanFit.Application/Abstractions/ProjectedPlanSheetExportRecipe.cs`
- `src/FloorplanFit.Application/PlanSets/Export/ExportProjectedPlanSheetHandler.cs`
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteCanonicalFloorPlanAdjustmentRepository.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Projection/ElectricalRecipeProjectionTests.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Export/ExportProjectedPlanSheetHandlerTests.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`

## Safety boundaries
- No Roof/Facade code was changed.
- No destructive DXF layer filter was added.
- Existing affine export remains the fallback when no local recipe is present.
- Local compression projections still require manual confirmation before export through existing projection status flow.

## Verification done
- `git diff --check` passed. Only CRLF warnings remain.
- No `dotnet build` was run, per repository rule.

## Still missing before final goal completion
- Runtime SEMINOLE verification with a real adjusted FloorPlan and related ElectricalPlan.
- Stronger entity-crossing detection for ARC/CIRCLE/SPLINE when crossing pinch boundaries.
- Manifest wording can still be improved to distinguish â€œrequires reviewâ€ from â€œrecipe-aware export after manual confirmationâ€.

## 2026-07-04 follow-up
- Added a conservative safety guard for recipe-aware `CIRCLE`/`ARC` export: if the curve radius crosses a canonical pinch line after registration into FloorPlan coordinates, export throws a manual-review error instead of writing a broken dependent DXF.
- Updated manual projection confirmation wording so recipe summaries can say `recipe-aware DXF export` after confirmation, instead of continuing to claim the recipe still requires review.
- This still does not complete the goal: SEMINOLE runtime verification and broader crossing semantics remain pending.

## 2026-07-04 follow-up 2
- Export-time unsafe geometry no longer only throws. `ProjectedPlanSheetManualReviewRequiredException` is caught by `ExportProjectedPlanSheetHandler`, which updates the projection back to `RequiresManualConfirmation` with the concrete warning and manual-review recipe summary.
- `SqliteSheetAdjustmentProjectionRepository.UpdateAsync` now persists status, warning, rule summary, and recipe handling summary, so manual review state survives app restart.
- `ExportMultiSheetPlanSetPackageHandler` now continues package auditing when a dependent projection becomes manual during export; the manifest can still be created and show the dependent sheet as `RequiresManualConfirmation` with no exported storage path.

## 2026-07-04 follow-up 3
- `scripts/verify-latest-plan-set-recipe-manifest.ps1` now validates more than recipe text: if ElectricalPlan is `ProjectedAutomatically`, it requires a real StoragePath, checks the DXF has readable `SECTION`/`ENTITIES`/`EOF`, and rejects tiny/empty drawings.
- The verifier now rejects stale contradictory manifests where ElectricalPlan is `ProjectedAutomatically` but the recipe summary still says `requires review before DXF deformation`.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` self-check passed locally without building.
- Running the verifier against the current latest runtime manifest correctly failed because that export predates the recipe-aware export wording and must be regenerated from the app.

## 2026-07-04 follow-up 4
- Recipe-aware DXF export now treats `TEXT` and `MTEXT` semantics more safely: insertion points receive the canonical recipe displacement, but text alignment/direction data is preserved as affine vectors so local pinches do not deform text.
- Added regression coverage for `TEXT` alignment point and `MTEXT` direction vector under recipe-aware projection.
- Verification: `git diff --check` passed; manifest verifier self-check passed without building.
## 2026-07-04 follow-up 5
- Recipe-aware Electrical export now refuses unknown coordinate-bearing DXF entities instead of guessing a deformation.
- Supported recipe-aware entity set is intentionally narrow: `LINE`, `LWPOLYLINE`, `SPLINE`, `INSERT`, `TEXT`, `MTEXT`, `CIRCLE`, `ARC`, `ELLIPSE`, `DIMENSION`.
- Example protected case: `HATCH` with coordinate pairs now becomes manual-review instead of being silently pinched.
- Verification: `git diff --check` passed and manifest verifier self-check passed without building.

## 2026-07-04 follow-up 6
- The conservative `CIRCLE`/`ARC` crossing guard now also scans anonymous `DIMENSION` graphic blocks that are actually referenced by modelspace `DIMENSION` entities.
- This closes a safety hole where modelspace curves were protected but dimension-block curves could still be pinched blindly.
- Verification: `git diff --check` passed without build.

## 2026-07-04 follow-up 7
- Recipe-aware Electrical export now treats `ELLIPSE` crossing a canonical pinch line as unsafe, using a conservative bounding-radius guard from the center plus major/minor axis data.
- Non-crossing ellipses still keep the correct semantics: `10`/`20` center is projected as a point, `11`/`21` major-axis vector stays affine-only.
- Verification: `git diff --check` passed and manifest verifier self-check passed without building.

## 2026-07-04 follow-up 8
- The runtime manifest verifier now requires a non-empty valid root `CanonicalAdjustmentId` before accepting a HousePlanSet export.
- The verifier output now prints `CanonicalAdjustmentId`, so the final SEMINOLE proof can show which canonical FloorPlan adjustment drove the Electrical projection.
- The verifier self-check now includes a negative manifest missing `CanonicalAdjustmentId` and expects it to fail.
- Verification: `git diff --check` passed and manifest verifier self-check passed without building.

## 2026-07-04 follow-up 9
- The runtime manifest verifier now inspects projected Electrical DXF entity types, not only total drawable count.
- For `ProjectedAutomatically`, it requires evidence that symbols (`INSERT`), dimensions (`DIMENSION`), ellipses (`ELLIPSE`), and wire/curve carriers (`LWPOLYLINE`/`POLYLINE`/`SPLINE`/`ARC`) are still present.
- The verifier self-check includes a sparse projected DXF with enough total lines to pass the old count gate, but missing key entity classes, and expects it to fail.
- Verification: `git diff --check` passed and manifest verifier self-check passed without building.

## 2026-07-04 follow-up 10
- Added math coverage for Electrical source -> Floor source registration when the dependent sheet has a 90-degree rotation before the canonical FloorPlan recipe is applied.
- Normalized tiny trig noise in both `ElectricalRecipeProjection` and the DXF curve-crossing registration helper so `cos(90)`-style floating dust does not leak into pinch-side decisions.
- Verification: `git diff --check` passed and manifest verifier self-check passed without building.

## 2026-07-04 follow-up 11
- Manual confirmation after an export-time manual-review downgrade now rewrites `manual review required:` to `manual review completed:` in the Electrical recipe summary.
- The runtime manifest verifier now rejects `ProjectedAutomatically` Electrical sheets whose recipe summary still says `manual review required`, preventing contradictory Ready/exportable manifests.
- Verification: `git diff --check` passed and manifest verifier self-check passed without building.

## 2026-07-04 follow-up 12
- `ExportMultiSheetPlanSetPackageHandler` now audits explicitly selected manual dependent projections instead of trying to export them and aborting the package.
- This keeps unsafe Electrical projections in `RequiresManualConfirmation` with no storage path, matching the goal rule: manual, not broken.
- Verification: `git diff --check` passed and manifest verifier self-check passed without building.
## 2026-07-05 - Explicit projection ownership guard
- Added an Application-layer guard before exporting explicitly selected dependent projections.
- The handler now rejects a selected projection if its `PlanSetVersionId` or `CanonicalAdjustmentId` does not match the package request.
- This prevents writing a wrong Electrical DXF before the audit layer gets a chance to reject inconsistent input.
## 2026-07-05 - PlanSet mismatch coverage and stale runtime manifest
- Added regression coverage for explicit dependent projections that belong to another `PlanSetVersionId`; such projections must be rejected before export and must not write a DXF.
- Current runtime verifier evidence is still negative: the latest workspace manifest is stale and says ElectricalPlan still requires DXF deformation review while marked `ProjectedAutomatically`.
- Required next external proof: re-export SEMINOLE from the desktop app and rerun `scripts/verify-latest-plan-set-recipe-manifest.ps1`.
## 2026-07-05 - Stale summary sanitized on successful recipe-aware export
- Found that old `ReadyForExport` Electrical projections could still carry `local recipe requires review before DXF deformation` even when the current exporter applies the canonical recipe.
- `ExportProjectedPlanSheetHandler` now detects that stale phrase after a successful recipe-aware export, rewrites it to `recipe-aware DXF export applied canonical operations`, persists via `ISheetAdjustmentProjectionRepository.UpdateAsync`, and saves `IUnitOfWork` when available.
- This matters because `CreateMultiSheetExportAuditHandler` reloads projections from the repository before writing the manifest; the new export path can therefore clean stale audit text during re-export.
## 2026-07-05 - Runtime verifier passes as manual fallback due unsupported POINT
- Latest runtime verifier now passes against manifest `cc8f919791a940689fe04342e010f3d4/manifest.json`.
- The package status remains `RequiresManualConfirmation`; Electrical is not exported automatically because the recipe-aware DXF path encountered unsupported `POINT` geometry.
- This is correct safety behavior: the app refuses to generate a potentially broken Electrical DXF. It is not the final automatic displacement proof because no `ElectricalStoragePath` exists in manual status.
## 2026-07-05 - POINT support for recipe-aware Electrical export
- The latest runtime manual fallback was caused by unsupported `POINT` entities in the Electrical DXF.
- `POINT` is now included in the recipe-aware supported entity set. Its `10`/`20` coordinates are transformed as a normal point through the canonical FloorPlan recipe.
- Added focused infrastructure test coverage for a `POINT` after a right-side FloorPlan compression.
## 2026-07-05 - SOLID support for recipe-aware Electrical export
- Latest SEMINOLE runtime manifest moved past 3DFACE and now downgraded Electrical to `RequiresManualConfirmation` because `SOLID` was unsupported.
- Verified in the runtime DXF that `SOLID` is an `AcDbTrace`-style filled entity with point vertices in `10/20`, `11/21`, `12/22`, `13/23`; Z values are separate and should not be locally pinched.
- `ProjectedPlanSheetDxfExporter` now treats `SOLID` as supported by the same coordinate-pair projection path used by 3DFACE/HATCH/POINT.
- Added focused test coverage that projects SOLID XY vertices through the canonical recipe and preserves Z values.
- Verification: `git diff --check` passed and manifest verifier self-check passed without building.

## 2026-07-05 - Binary DXF support in runtime verifier
- Latest runtime export was `ProjectedAutomatically` but affine-only, so it correctly failed the default final-proof gate because there was no `HorizontalCompression` or `VerticalCompression` in the Electrical recipe summary.
- Running `-AllowAffineOnly` exposed a verifier bug: the Electrical output was AutoCAD Binary DXF, while the verifier only checked text DXF syntax.
- `verify-latest-plan-set-recipe-manifest.ps1` now recognizes the binary DXF sentinel and counts null-delimited entity names for the same evidence checks used on text DXF.
- `test-verify-latest-plan-set-recipe-manifest.ps1` now includes an automatic binary compression manifest fixture.
- Verification: self-check passed, latest runtime manifest passes with `-AllowAffineOnly`, and `git diff --check` passed without building.
