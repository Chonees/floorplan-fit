# 2026-07-01 - Next gap is dependent sheet registration UI

## Verified state
Desktop now supports selecting a floor-plan version, importing dependent sheets explicitly, showing associated HousePlanSet sheets, and unlinking unregistered/not-projected mistakes.

Application layer already has:
- `RegisterElectricalSheetHandler`
- `RegisterRoofSheetHandler`
- `RegisterFacadeElevationSheetHandler`
- `ConfirmSheetRegistrationHandler`
- `ProjectRegisteredPlanSetSheetsHandler`
- package export/project wiring in SitePlan adjustment

Desktop ViewModel already has `RegisterDependentSheetAsync(...)`, but visible XAML does not expose a registration/alignment action yet.

## Next product gap
Expose a minimal Desktop registration action for an imported dependent sheet so it can move from `Unregistered / NotProjected` to registered/aligned, and later participate in projection/export.

## Recommended next slice
Start with ElectricalPlan only and use a simple transform/default registration UI or action before generalizing to roof/facade.

## Superseded
Status: Superseded
Replaced by: [[2026-07-01 - Desktop registers electrical sheet from library]], [[2026-07-01 - Desktop confirms dependent sheet registration]], [[2026-07-01 - Desktop confirms dependent sheet projection]]

The original gap was a visible registration UI. That gap has since moved forward through register, confirm registration, and confirm projection slices.
