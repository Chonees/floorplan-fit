# Canonical adjustment projects registered sheets before package export

## What changed
Added the missing propagation slice between canonical floor-plan adjustment and multi-sheet package export.

## Implemented
- `ProjectRegisteredPlanSetSheetsHandler` lists registrations for a `PlanSetVersion`, keeps the latest registration per dependent sheet, and delegates to the existing electrical/roof/facade projection handlers.
- Added `ProjectRegisteredPlanSetSheetsRequest` and `ProjectRegisteredPlanSetSheetsResponse` contracts.
- `ISheetRegistrationRepository` now exposes `ListByPlanSetVersionAsync`; SQLite reads registrations by plan-set version ordered by creation time.
- Desktop `SitePlanAdjustmentViewModel` calls the registered-sheet projector after recording the canonical adjustment and before package export.
- `LibraryViewModel` resolves the projector from DI and passes it into the adjustment view model.
- Desktop DI registers `ProjectRegisteredPlanSetSheetsHandler`.

## Why it matters
Before this slice, package export could discover/export only projections that already existed. A user could adjust the floor plan, but registered dependent sheets were not automatically projected during the export flow. Now the canonical adjustment actually propagates to existing dependent registrations before audit/export.

## Boundaries
- No new per-sheet fit engine was introduced.
- No import/registration UI was added yet.
- Repeated exports create fresh projections; package export already selects the latest projection per sheet.

## Verification
- Test-first RED evidence: new projection orchestration test referenced missing handler/request/list method before production code existed.
- Added Application test for projecting latest registrations per dependent sheet.
- Added SQLite persistence coverage for listing registrations by PlanSetVersion.
- Updated Desktop export test so package export depends on a generated projection from registration instead of a preloaded projection.
- Static verification: scoped `git diff --check` passed and new-file whitespace checks passed.
- No `dotnet test` or `dotnet build` was run due repository rule.