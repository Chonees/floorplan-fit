---
type: Implementation
status: superseded
date: 2026-07-14
replaced_by: "[[2026-07-14 - Desktop safe dependent-sheet unlink]]"
---

# Application unlink RED contract

> Superseded by [[2026-07-14 - Desktop safe dependent-sheet unlink]]. The RED contract is retained as the historical specification; source is now statically wired, while executable proof remains pending.

## Scope

Loop 2 / Plan Set Library, at the Application layer. This is only the RED specification for a future safe unlink use case; no production implementation changed.

## Contract

- `UnlinkPlanSheetRequest` carries only `SheetId`.
- `UnlinkPlanSheetHandler` rejects `PlanSheetType.FloorPlan` before any write.
- For a dependent sheet, cleanup is scoped solely by `dependent_sheet_id` and occurs in this order: all projections, all registrations, then the sheet row, followed by one `IUnitOfWork.SaveChangesAsync` call.
- Imported source documents and historical audit/export artifacts are intentionally outside the handler's deletion surface.

## Evidence

`tests/FloorplanFit.Application.Tests/PlanSets/Library/UnlinkPlanSheetHandlerTests.cs` seeds two workflow rows for one confirmed `ElectricalPlan` with a `ReadyForExport` projection and one unrelated sheet. The test requires the target rows to disappear while the unrelated rows remain.

`tests/FloorplanFit.Application.Tests/PlanSets/PlanSetSheetDtoTests.cs` requires the noncanonical confirmed/ready-for-export Electrical sheet to expose the unlink capability. `tests/FloorplanFit.Desktop.Tests/ViewModels/LibraryViewModelTests.cs` pins the Desktop handoff to `UnlinkPlanSheetHandler` with `UnlinkPlanSheetRequest(SheetId)`, then requires refreshed UI state and no remaining active workflow rows for that sheet.

## Handoff

Production must add the handler/request and repository bulk-removal ports, then adapt the persistence and Desktop layers in a later owned change. The test suite was not run: repository instructions prohibit `dotnet`, build, test, restore, watch, and Desktop commands.
