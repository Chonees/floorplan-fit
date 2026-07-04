# Desktop registers dependent sheets against canonical floor plan

## What changed
Added the smallest Desktop registration operation for dependent sheets.

## Implemented
- `LibraryViewModel.RegisterDependentSheetAsync(...)` routes an imported dependent sheet to the correct registration handler by sheet type.
- Electrical uses `RegisterElectricalSheetHandler` with full similarity transform.
- Roof uses `RegisterRoofSheetHandler` with the same transform plus overhang inches.
- Facade/elevation uses `RegisterFacadeElevationSheetHandler` with horizontal scale/offset plus optional horizontal reference name.
- The method resolves `HousePlanSet` and `PlanSetVersion` from the selected floor-plan version before registering.

## Why it matters
Dependent sheet import alone is not enough. The canonical adjustment can only propagate to sheets that have a registration against the floor-plan model. This gives Desktop a tested seam for registering imported electrical/roof/facade sheets without inventing automatic geometry matching.

## Boundaries
- No automatic transform estimation was added.
- No XAML button/dialog was added yet.
- No per-sheet fit engine was introduced.
- Registration still depends on explicit transform values from future UI or operator input.

## Verification
- Test-first RED: Desktop test referenced missing `RegisterDependentSheetAsync` before production code existed.
- Added Desktop ViewModel test for electrical registration using real `RegisterElectricalSheetHandler` and fake repositories.
- Scoped `git diff --check` passed and whitespace checks passed.
- No `dotnet test` or `dotnet build` was run due repository rule.