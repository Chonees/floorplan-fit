# 2026-06-24 - High zoom manual floor-plan move stalled

## Type
Bugfix

## Symptom
In Loop 2 Adjust-to-Site-Plan, when the preview was zoomed in heavily, the manual floor-plan move tool could feel stuck: dragging the plan did not visibly move it.

## Root cause
`FloorPlanPreviewControl.CalculateFloorPlanMoveDelta(...)` converted screen pixels to source units and then reused the generic 3-decimal `RoundModelValue(...)`. At high zoom, a normal drag becomes a tiny source-unit delta below `0.001`, so it rounded to `0`.

The pointer move handler then updated the previous pointer position even when the emitted delta was zero. That dropped the sub-precision residual on every pointer event, so many small pixel moves could never accumulate into a model-visible move.

## Fix
Manual floor-plan movement now uses 6-decimal movement precision, matching the Adjust placement/projector precision. The active drag state stores the drag-start pointer plus the already-applied cumulative delta, so tiny high-zoom pointer movement accumulates until it crosses model precision and then dispatches the correct delta.

## Product loop / architecture
- Product loop: Loop 2 Adjust-to-Site-Plan.
- Architecture layer: Desktop control/input handling only.
- ViewModel/application contracts stayed unchanged: `FloorPlanMoveDeltaRequested` still raises decimal deltas and `SitePlanAdjustmentViewModel.MoveFloorPlanBy(...)` still owns geometry translation.

## Verification
- RED: focused tests first failed because the new high-zoom dispatch contract did not exist yet.
- GREEN: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~CalculateFloorPlanMove" --no-restore --output .testartifacts\desktop-tests-out -v minimal` passed `2/2`.
- Additional focused coverage: same project with `--filter "FullyQualifiedName~FloorPlanPreviewControlTests&FullyQualifiedName!~sources&FullyQualifiedName!~clips"` passed `61/61` using the separate output folder.
- Full class run against the normal bin was blocked by the currently running `FloorplanFit.Desktop` process locking DLLs; the separate-output run avoids that lock. Source-path-only tests were excluded from the separate-output run because they assume `AppContext.BaseDirectory` is under the normal test bin.
- `git diff --check` passed with CRLF warnings only.
- No `dotnet build` command was run.

## Files
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
