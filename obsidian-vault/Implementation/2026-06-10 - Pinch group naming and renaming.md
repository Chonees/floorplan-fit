# Pinch group naming and renaming

## Type
Implementation

## Status
Implemented and verified.

## Current truth
Loop 1 edit/review preview now supports human naming for pinch groups:

- Creating a pinch group opens `PinchGroupNameDialog` before persistence.
- The dialog is prefilled with the existing automatic suggestion such as `Ajuste 1`.
- The operator can replace it with a meaningful name such as `Patio`, `Porche`, `Garage`, or `Lateral`.
- Existing selected groups can be renamed from the Fit inspector with `Renombrar grupo`.
- Rename updates only `pinch_groups.name`; it preserves group id, curation id, axis, sort order, markers, and downstream references.

## Product loop
Loop 1: floor plan curation.

This matters for Loop 2 because auto-fit suggestions and apply options name the curated pinch groups. Human-readable group names are part of the fit decision, not cosmetic UI.

## Implementation
- Desktop popup: `src/FloorplanFit.Desktop/PinchGroupNameDialog.axaml(.cs)`.
- UI hooks: `ReviewFloorPlanWindow.axaml(.cs)` opens the dialog for create and rename.
- ViewModel: `FloorPlanReviewViewModel` exposes `SuggestedPinchGroupName`, `CanRenameSelectedPinchGroup`, explicit-name create, and `RenameSelectedPinchGroupAsync`.
- Application: `RenamePinchGroupHandler` validates draft curation and group ownership, then persists an updated `PinchGroup` with the same identity/axis/sort order.
- Infrastructure: `IPinchGroupRepository.UpdateAsync(...)` and `SqlitePinchGroupRepository.UpdateAsync(...)` update the persisted group row.

## Verification
- RED/GREEN Application: `RenamePinchGroupHandlerTests`.
- RED/GREEN Desktop: explicit create-name and rename ViewModel tests; Review window layout/dialog hook tests.
- Infrastructure persistence test: SQLite group update preserves identity/axis/sort order.
- Final focused verification:
  - `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~PinchGroup"` -> 4/4 passed.
  - `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~PinchGroupRepository|FullyQualifiedName~Pinch_repositories_remove"` -> 3/3 passed.
  - `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~ReviewFloorPlanWindowLayoutTests|FullyQualifiedName~DesktopServiceRegistrationTests"` -> 72/72 passed.
  - `git diff --check` -> exit 0 with LF-to-CRLF warnings only.
