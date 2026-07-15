# 2026-07-14 - Confirmed ElectricalPlan unlink leaves active workflow references

`replaces`: [[2026-07-01 - Desktop unlinks misimported dependent sheets]] for confirmed/projected-sheet unlink policy.

## Status

Read-only investigation. No source, schema, or test file was changed.

## What

A dependent ElectricalPlan with `RegistrationStatus = Confirmed` and `ProjectionStatus = ReadyForExport` cannot expose the Library unlink action. The current direct sheet deletion would leave the active registration and projection workflow rows behind.

## Exact evidence

- `PlanSetSheetDto.CanUnlink` requires `!IsCanonical`, an editable registration (`Unregistered` or `Rejected`), and `ProjectionStatus == "NotProjected"`.
- `MainWindow.axaml` shows the `×` only when `CanUnlink`; `LibraryViewModel.UnlinkDependentSheetAsync` enforces the same predicate.
- `SqlitePlanSheetRepository.RemoveAsync` executes only `DELETE FROM plan_sheets WHERE id = $id`.
- `sheet_registrations.dependent_sheet_id` and `sheet_adjustment_projections.dependent_sheet_id` / `sheet_registration_id` retain the removed identifiers. A projection always requires a registration ID in the Domain entity.
- The Electrical projector stores the registration's dependent-sheet ID and registration ID on the projection; it resolves `ReadyForExport` only for a confirmed registration at or above the automatic-export confidence threshold.
- SQLite connections enable `foreign_keys`, but the current table definitions declare no foreign keys or cascades for `plan_sheets`, `sheet_registrations`, `sheet_adjustment_projections`, or `plan_set_exported_sheets`.
- The v1-to-v2 registration repair explicitly treats a missing dependent sheet as invalid; current v2 databases do not have a delete trigger that prevents or cleans this state.

## Safe minimum

Implement a Loop 2 Application unlink use case that uses the scoped `SqliteSession` transaction and deletes, in order, **all** projections by `dependent_sheet_id`, **all** registrations by `dependent_sheet_id`, then the `plan_sheets` row, followed by one `IUnitOfWork.SaveChangesAsync`.

Keep immutable export/audit rows unless a separate retention policy says historical package evidence must be erased. `plan_set_exported_sheets` is an add-only historical snapshot today; deleting it is not required to restore the active registration/projection workflow and would silently discard export history.

## Product boundary

Confirmed registrations with `NotProjected` can be unlinked after registration cleanup. That smaller policy does **not** solve the requested `Confirmed + ReadyForExport` ElectricalPlan case: supporting that case requires projection cleanup too.

## RED contract

1. `PlanSetSheetDtoTests.Confirmed_ready_for_export_dependent_sheet_can_unlink`.
2. `UnlinkPlanSheetHandlerTests.HandleAsync_removes_all_projections_and_registrations_before_the_confirmed_ready_sheet_and_commits_once`.
3. `LibraryViewModelTests.UnlinkDependentSheetAsync_unlinks_confirmed_ready_for_export_sheet_through_the_handler_and_refreshes`.
4. `PlanSheetUnlinkPersistenceTests.Unlink_cleanup_removes_every_active_registration_and_projection_for_the_sheet`.

## Files

- `src/FloorplanFit.Contracts/PlanSets/PlanSetSheetDto.cs`
- `src/FloorplanFit.Application/PlanSets/Library/UnlinkPlanSheetHandler.cs` (new)
- `src/FloorplanFit.Application/Abstractions/IPlanSheetRepository.cs`
- `src/FloorplanFit.Application/Abstractions/ISheetRegistrationRepository.cs`
- `src/FloorplanFit.Application/Abstractions/ISheetAdjustmentProjectionRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqlitePlanSheetRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetRegistrationRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteSheetAdjustmentProjectionRepository.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/Composition/DesktopServiceRegistration.cs`
