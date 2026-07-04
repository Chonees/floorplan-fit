# Desktop imports dependent sheets into HousePlanSet

## What changed
Added the smallest Desktop onboarding operation for dependent sheets.

## Implemented
- `LibraryViewModel.ImportDependentSheetAsync(...)` imports a dependent plan sheet for the selected floor-plan version.
- The method resolves/creates `HousePlanSet` and `PlanSetVersion`, then calls `ImportPlanSheetHandler`.
- Passing an empty sheet type lets `ImportPlanSheetHandler` use `SheetClassification` to classify by file/name.
- After import, the library refreshes while preserving the selected floor-plan version.

## Why it matters
The HousePlanSet backend had import/classification, but Desktop had no operation to attach electrical/roof/facade sheets to a house plan set. This gives the tool a first tested onboarding seam without inventing registration UI or another fit engine.

## Boundaries
- No XAML button/dialog was added yet.
- No geometric registration UI was added yet.
- No automatic registration transform is guessed.
- Floor plan remains the canonical sheet; dependent sheet import still rejects FloorPlan.

## Verification
- Test-first RED: `LibraryViewModelTests` referenced missing `ImportDependentSheetAsync` before production code existed.
- Added Desktop ViewModel test using real `ImportPlanSheetHandler`, real `ClassifyPlanSheetHandler`, and fake repositories/storage/DXF gateway.
- Scoped `git diff --check` passed.
- No `dotnet test` or `dotnet build` was run due repository rule.