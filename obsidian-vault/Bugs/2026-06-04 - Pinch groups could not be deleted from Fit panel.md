---
type: Bug
date: 2026-06-04
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - fit-curation
  - pinch-groups
---

# Pinch groups could not be deleted from Fit panel

## Symptom

The operator could delete individual pinches, but could not delete a whole group of pinches from the Fit panel.

## Root Cause

The delete flow only existed for `PinchMarker`:

- `RemovePinchMarkerHandler` deleted one marker.
- `IPinchMarkerRepository.RemoveAsync` / `SqlitePinchMarkerRepository.RemoveAsync` deleted one marker row.
- `FloorPlanReviewViewModel.RemoveSelectedPinchAsync` returned early unless `SelectedPinchMarker` was set.
- The XAML only had a group creation button plus an individual `Quitar pinch` action.

There was no Application use case, repository delete operation, ViewModel command, or XAML button for deleting a selected `PinchGroup`.

## Fix

Implemented a proper group-level delete:

- Added `RemovePinchGroupHandler`.
- Added `IPinchGroupRepository.RemoveAsync`.
- Added `IPinchMarkerRepository.RemoveByGroupAsync`.
- SQLite deletes group markers first, then deletes the group.
- Desktop exposes `CanRemoveSelectedPinchGroup` and `RemoveSelectedPinchGroupAsync`.
- Fit right panel now has `Eliminar grupo de pinches`, enabled when a draft group is selected.

## Verification

- RED first: `RemovePinchGroupHandlerTests` failed because `RemovePinchGroupHandler` did not exist.
- Application focused test passed: `RemovePinchGroupHandlerTests`.
- Infrastructure focused test passed: `Pinch_repositories_remove_a_group_and_only_its_markers`.
- Desktop ViewModel focused test passed through isolated artifacts path because the running desktop app locked normal output DLLs.
- Desktop layout + DI focused tests passed through isolated artifacts path.

## Related

- [[../Implementation/2026-06-04 - Fit pinch groups can be removed with their pinches]]
- [[../Implementation/2026-05-22 - Fit pinch groups panel and handle gating]]
- [[../Implementation/2026-06-01 - Fit franja nodes can be removed individually]]
