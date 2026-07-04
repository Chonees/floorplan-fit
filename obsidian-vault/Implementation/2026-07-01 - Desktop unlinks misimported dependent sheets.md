# 2026-07-01 - Desktop unlinks misimported dependent sheets

## What
- Added a direct `×` action in the selected HousePlanSet sheet list for dependent sheets that are `Unregistered / NotProjected`.
- The canonical `FloorPlan` row does not expose unlink.
- `LibraryViewModel.UnlinkDependentSheetAsync(...)` removes the dependent sheet record, saves, refreshes the library, and preserves the selected floor-plan version.
- `IPlanSheetRepository.RemoveAsync(...)` and `SqlitePlanSheetRepository.RemoveAsync(...)` delete from `plan_sheets`.

## Why
Manual smoke showed an ElectricalPlan from Seminole was accidentally imported into Santa Barbara. The UI needed a simple way to undo the wrong association without deleting the canonical floor plan or building a full wizard.

## Boundary
- This does not delete physical DXF files from managed storage.
- This does not delete imported document/measurement rows yet.
- The `×` is intentionally limited to unregistered/not-projected dependent sheets. Registered/projected sheets should get a confirm flow later.

## Verification
- Added layout/cable assertions for the `×` button, `CanUnlink` visibility, and click handler.
- Added ViewModel test for removing an unregistered dependent sheet and refreshing the selected set.
- Static RED check confirmed unlink production code was missing before implementation.
- Static verification confirmed contract, SQLite delete, ViewModel, XAML, code-behind, and tests are wired.
- Scoped `git diff --check` passed.
- No agent-run build/test due repository rule: `Never build after changes`.

## Files
- `src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs`
- `src/FloorplanFit.Application/Abstractions/IPlanSheetRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSheetRepository.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/LibraryViewModelTests.cs`
