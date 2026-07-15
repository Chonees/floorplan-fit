---
type: Implementation
status: static-implemented
date: 2026-07-14
replaces: "[[2026-07-14 - Application unlink RED contract]]"
---

# Desktop safe dependent-sheet unlink

## Scope

Loop 2 / Plan Set Library. Contracts and Desktop delegate unlinking to the Application boundary; imported documents and historical export/audit snapshots remain untouched.

## Implementation

- `PlanSetSheetDto.CanUnlink` is now true for every noncanonical sheet, including confirmed `ReadyForExport` dependents.
- `LibraryViewModel.UnlinkDependentSheetAsync` resolves `UnlinkPlanSheetHandler` and sends `UnlinkPlanSheetRequest(sheet.SheetId)`.
- The ViewModel retains its existing refresh plus selected template/version restoration, but no longer reads, deletes, or commits through `IPlanSheetRepository` or `IUnitOfWork` directly.
- `DesktopServiceRegistration` registers the handler as scoped alongside the existing Plan Set Library handlers.

## Safety boundary

Static inspection of the adjacent Application handler confirms the required cleanup order: projections by `dependent_sheet_id`, registrations by `dependent_sheet_id`, the plan-sheet row, and one unit-of-work commit. Canonical FloorPlan rejection remains in that Application boundary.

## Verification

Scoped `git diff --check` and source-shape checks passed. No `dotnet`, build, test, restore, watch, Desktop, or runtime command ran; executable GREEN proof is pending.

The existing unregistered-sheet `LibraryViewModelTests` fixture has not registered `UnlinkPlanSheetHandler`; that test setup needs the same transient registration before the executable suite can prove this change.
