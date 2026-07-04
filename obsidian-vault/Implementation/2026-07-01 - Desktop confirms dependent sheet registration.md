# 2026-07-01 - Desktop confirms dependent sheet registration

## What
- Added `SheetRegistrationId` and `CanConfirmRegistration` to `PlanSetSheetDto`.
- `GetPlanSetLibraryHandler` now carries the latest registration id plus status into dependent sheet rows.
- Added a visible `Confirm` action for rows with `PendingConfirmation` registration.
- `MainWindow.axaml.cs` routes `Confirm` to `LibraryViewModel.ConfirmDependentSheetRegistrationAsync(...)`.
- `LibraryViewModel` calls `ConfirmSheetRegistrationHandler`, refreshes the selected set, and preserves current floor-plan selection.

## Why
After `Register Electrical`, the sheet reached `PendingConfirmation`, but Desktop had no way to approve that registration. Without carrying the registration id in the read model, UI could not call the existing confirmation handler.

## Boundary
- This confirms registration only; it does not confirm adjustment projection.
- The registration may still have low confidence, so projection/export can still require manual review depending on existing projection rules.
- No geometry picker yet.

## Verification
- Added read-model test asserting `SheetRegistrationId` and `CanConfirmRegistration` are surfaced.
- Added layout/cable assertions for `Confirm` button and handler.
- Added ViewModel test confirming a pending registration and refreshing selected sheet status to `Confirmed`.
- Static verification confirmed all symbols and scoped `git diff --check` passed.
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
