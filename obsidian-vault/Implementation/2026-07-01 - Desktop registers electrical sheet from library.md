# 2026-07-01 - Desktop registers electrical sheet from library

## What
- Added `CanRegisterElectrical` to `PlanSetSheetDto` for unregistered/not-projected ElectricalPlan sheets.
- Added a `Register` button in the selected HousePlanSet sheet list for ElectricalPlan rows.
- `RegisterElectricalSheetButton_OnClick` calls `LibraryViewModel.RegisterDependentSheetAsync(...)` with an identity transform and low confidence (`0.25`) so the result requires confirmation instead of pretending to be exact.
- `LibraryViewModel.RegisterDependentSheetAsync(...)` now refreshes the library after registration so the selected sheet list can show the updated registration status.
- `GetPlanSetLibraryHandler` now reads latest sheet registration status via `ISheetRegistrationRepository` and overlays it onto dependent sheet rows.

## Why
After import/show/unlink, the next verified gap was moving an imported ElectricalPlan from `Unregistered / NotProjected` into the registration pipeline. Without updating the read model, registration would happen in the backend but still appear as `Unregistered` in the UI.

## Boundary
- Electrical only for this slice.
- No geometric alignment picker yet.
- The default identity transform is deliberately low confidence and pending confirmation.
- Roof/facade registration UI remains next work.
- Projection/export readiness still depends on confirmation and projection review rules.

## Verification
- Added RED coverage for latest registration status appearing in `GetPlanSetLibraryHandler`.
- Added layout/cable assertions for the `Register` button and `RegisterElectricalSheetButton_OnClick`.
- Updated ViewModel registration test to assert refreshed selected sheet status becomes `PendingConfirmation / NotProjected`.
- Static verification confirmed symbols and scoped `git diff --check` passed.
- No agent-run build/test due repo rule: `Never build after changes`.

## Files
- `src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs`
- `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs`
- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Library/GetPlanSetLibraryHandlerTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/LibraryViewModelTests.cs`
