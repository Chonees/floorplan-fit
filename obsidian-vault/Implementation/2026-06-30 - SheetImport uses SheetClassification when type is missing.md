# 2026-06-30 - SheetImport uses SheetClassification when type is missing

## Type
Implementation

## What
`ImportPlanSheetHandler` now accepts an optional `ClassifyPlanSheetHandler`. When `ImportPlanSheetRequest.SheetType` is empty, import asks the classifier for a sheet type before persisting the dependent sheet.

## Why
HousePlanSet import should not permanently depend on external/manual sheet classification. The import module can now consume the SheetClassification module while still refusing uncertain or unknown classifications.

## Where
- `src/FloorplanFit.Application/PlanSets/Import/ImportPlanSheetHandler.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Import/ImportPlanSheetHandlerTests.cs`

## Boundary
Classification does not override safety. Unknown, ambiguous, or manual-confirmation-required results still fail import and require explicit user classification. FloorPlan still remains in the canonical floor-plan import flow.

## Verification
- RED evidence: import tests requested empty `SheetType` with classifier available; production had no classifier dependency or sheet-type resolution path.
- GREEN evidence: `ResolveSheetTypeAsync(...)` now uses explicit type first, classifier second, and rejects uncertain classifications.
- Static check: `git diff --check` passed for the touched import/classification files.
- No `dotnet test` / no `dotnet build`, per repo rule.
