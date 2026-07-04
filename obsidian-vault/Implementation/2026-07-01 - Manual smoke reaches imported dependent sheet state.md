# 2026-07-01 - Manual smoke reaches imported dependent sheet state

## What
Manual Desktop smoke test reached the HousePlanSet sheet list after importing an ElectricalPlan dependent sheet.

Observed UI:
- `FloorPlan` row shows the canonical floor plan with `Canonical / CanonicalSource`.
- `ElectricalPlan` row shows the imported dependent sheet with `Unregistered / NotProjected`.

## Meaning
The dependent sheet import is now visible and associated with the selected HousePlanSet/PlanSetVersion, but it has not been registered/aligned to the canonical floor plan and has not received a projected adjustment.

## Current gap
Desktop has `LibraryViewModel.RegisterDependentSheetAsync(...)` and DI registrations for electrical/roof/facade registration handlers, but the visible XAML does not yet expose a registration/review action for dependent sheets.

## Files checked
- `src/FloorplanFit.Application/PlanSets/Library/GetPlanSetLibraryHandler.cs`
- `src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
