# Desktop shows selected HousePlanSet sheets

## What changed
The Desktop Library now shows the sheets belonging to the selected HousePlanSet/version.

## Implemented
- `LibraryViewModel` keeps a `SelectedPlanSetSheets` collection and `SelectedPlanSetSheetsLabel`.
- `RefreshItemsAsync` now also uses `GetPlanSetLibraryHandler` when available, then maps the selected floor-plan version to the matching `PlanSetLibraryItemDto`.
- `MainWindow.axaml` shows a compact `Plan Set Sheets` section with sheet type, name, registration status, and projection status.
- Importing a dependent sheet refreshes the PlanSet sheets list so the imported electrical/roof/facade sheet becomes visible.

## Why it matters
The app had backend support for HousePlanSet sheets, but the Library screen still hid the related sheets from the user. This made dependent-sheet import feel like a black box. Now the tool shows the canonical floor plan plus dependent sheets and their registration/projection state.

## Boundary
No registration wizard, no transform picker, and no per-sheet fit engine. This is a read-model/visibility slice over the existing PlanSet library handler.

## Verification
- Added ViewModel test assertion that dependent import updates `SelectedPlanSetSheets` and label count.
- Added layout assertions that `MainWindow.axaml` binds `SelectedPlanSetSheets` and displays registration/projection statuses.
- `git diff --check` passed for touched files.
- No `dotnet test` or `dotnet build` was run due repository rule.
