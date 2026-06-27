# 2026-06-26 - Adjust synthetic site plan used rejected walls for fit

## Type
Bugfix

## Symptom
When creating a synthetic site plan from the app with explicit width/height, the floor plan could appear badly off-center and the Adjust panel could report a huge height deficit instead of the small expected mismatch.

## Root cause
`SitePlanAdjustmentPreviewProjector.Build(...)` built the placement/fit geometry id set from all `WallCandidates`, including candidates already curated as `Rejected`. A rejected outlier wall could therefore define the structural footprint used for centering and auto-fit facts.

## Fix
Adjust now uses only `WallCandidateDto.Status == "Accepted"` geometry ids for the placement/fit footprint. If no accepted wall ids exist, the existing fallback still uses all geometry.

## Product loop / architecture
- Product loop: Loop 2 Adjust-to-Site-Plan.
- Architecture layer: Desktop ViewModel projection boundary.
- Synthetic DXF generation and DXF reading did not need changes.

## Verification
- RED: `Build_ignores_rejected_wall_candidates_when_centering_floor_plan_on_site_plan` first failed with accepted wall center `-527` instead of buildable center `73`.
- GREEN: focused regression test passed `1/1`.
- Broader focused suite: `SitePlanAdjustmentPreviewProjectorTests` passed `32/32` using `.testartifacts\desktop-tests-out` to avoid the running desktop app's locked bin folder.
- `git diff --check` passed with CRLF warnings only.
- No `dotnet build` command was run.

## Files
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs`
