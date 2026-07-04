---
type: Bug
date: 2026-06-01
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - fit-preview
  - height-axis
  - axis-state
replaces: [[2026-06-01 - Height compression handles visually invisible on dark workspace]]
---

# Height axis change can leave preview using Width handles

## Verified root cause

The remaining user-visible symptom was not handle color. The user clarified that Width/right-left handles still appeared while Height/top-bottom handles did not.

The code had two axis states in the Fit workflow:

- `SelectedPinchAxis` drives `FloorPlanPreviewControl.PreviewAxisTag`, therefore it decides whether compression handles are left/right (`Width`) or top/bottom (`Height`).
- `SelectedMeasurementCorridorAxis` drives the right-panel `Tipo de franja` selector and previously only updated `CanChangeSelectedMeasurementCorridorAxis`.

Changing a selected franja/corridor axis to `Height` did not update the preview/pinch axis, so the canvas could keep using `Width` and keep showing right/left handles.

Additional data check on the local app DB showed the active curation has a Height pinch group (`Ajuste 3`) with 0 markers, while existing markers belong to Width groups. Because handle gating intentionally requires a selected group with at least one marker for the active axis, Height top/bottom handles need Height pinch markers to drive a real preview.

## Fixed

`FloorPlanReviewViewModel.OnSelectedMeasurementCorridorAxisChanged` now syncs a valid corridor axis change into `SelectedPinchAxis`. Existing `OnSelectedPinchAxisChanged` then selects a compatible pinch group for that axis, preferring groups with matching markers.

The prior color-only change was reverted because it was not the root cause of this symptom.

## Verification

- RED: `Changing_measurement_corridor_axis_to_height_updates_the_preview_axis_and_height_group` failed because `SelectedPinchAxis` stayed `Width` after changing `SelectedMeasurementCorridorAxis` to `Height`.
- GREEN: the same test passed after syncing corridor axis into preview axis.
- Focused Desktop slice passed 99/99:
  `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .testartifacts\dotnet-test-artifacts-height-axis-sync-final`
- `git diff --check` exited 0 with only line-ending warnings.
