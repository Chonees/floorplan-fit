# Pinch group naming and renaming request

## Type
Inbox / product request

## Status
Requested; design pending approval before implementation.

## Context
The edit/review preview currently creates pinch groups with automatic names such as `Ajuste N`. The user wants human-facing group names to be editable because Loop 2 fit suggestions depend on meaningful names like patio, porch, garage, lateral, etc.

## Requested behavior
- When creating a pinch group from the edit preview, show a naming popup/dialog before persisting the group.
- Allow renaming an existing pinch group from the edit preview.
- Preserve the existing group identity, axis, markers, and sort order when only the name changes.
- Keep the flow human-readable so generated site-plan fit options name the meaningful curated groups.

## Verified current code facts
- `ReviewFloorPlanWindow.axaml` currently has a `Crear grupo de pinches` button but no naming input/popup in the visible UI.
- `FloorPlanReviewViewModel.AddPinchGroupAsync(...)` currently calls `CreateNextPinchGroupName()` and creates names like `Ajuste 1` automatically.
- `PinchGroup.Name` already exists in Domain and is displayed through `PinchGroupDto.Name`.
- `IPinchGroupRepository` currently supports add/get/list/remove but no rename/update operation.

## Product loop
Loop 1: floor plan curation. This improves the named curation source that Loop 2 uses for human-readable adjustment plans.
