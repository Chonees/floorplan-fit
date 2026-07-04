# 2026-06-30 - SheetClassification module introduced

## Type
Implementation

## What
Introduced the minimal `SheetClassification` module for HousePlanSet sheets.

## Why
The HousePlanSet architecture cannot rely forever on the user pre-classifying every dependent sheet before import. The system now has a small application use case that can classify a sheet as FloorPlan, ElectricalPlan, RoofPlan, FacadeElevation, or Unknown from file/name hints with confidence and manual-confirmation state.

## Where
- `src/FloorplanFit.Contracts/PlanSets/ClassifyPlanSheetRequest.cs`
- `src/FloorplanFit.Contracts/PlanSets/ClassifyPlanSheetResponse.cs`
- `src/FloorplanFit.Application/PlanSets/Classification/ClassifyPlanSheetHandler.cs`
- `tests/FloorplanFit.Application.Tests/PlanSets/Classification/ClassifyPlanSheetHandlerTests.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`

## Boundary
This is intentionally filename/title heuristic classification only. It does not parse DXF title blocks, does not auto-import sheets, and does not override manual confirmation for ambiguous matches.

## Verification
- RED evidence: no `Classification` module or `ClassifyPlanSheetHandler` existed; existing import tests only covered user-classified sheets.
- GREEN evidence: classification contracts, handler, tests, and DI registration exist.
- Static check: `git diff --check` passed for the touched classification files.
- No `dotnet test` / no `dotnet build`, per repo rule.
