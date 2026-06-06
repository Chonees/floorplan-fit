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
  - ux-guidance
replaces: [[2026-06-01 - Height axis change can leave preview using Width handles]]
---

# Height group without pinches should not promise top-bottom handles

## Verified root cause

The screenshots showed `Height` selected and group `Ajuste 3` selected, but the right panel's `Pinches de este grupo` section was empty. This matches the local app DB: `Ajuste 3` is a Height group with 0 pinch markers, while existing markers belong to Width groups.

`CompressionHandlePreviewLayerRenderer` intentionally hides compression handles unless the selected group has at least one marker matching the active axis. That gate is correct: without a Height pinch marker, dragging a top/bottom handle would have no preview driver.

The actual UX bug was the hint text: it still promised “drag the green top or bottom handle” even when the selected Height group had no Height pinches, so the user expected handles that the robust renderer was correctly hiding.

## Fixed

`FloorPlanReviewInspectorCoordinator` now receives whether the selected pinch group has a preview driver for the active axis. If it does not, the hint tells the user to mark at least one pinch for that axis before expecting handles.

## Verification

- RED: `Height_group_without_height_pinches_guides_user_to_place_a_pinch_instead_of_promising_handles` failed because the hint still promised the top/bottom handle.
- GREEN: the same test passed after the hint received selected-group driver state.
- Focused Desktop slice passed 100/100:
  `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .testartifacts\dotnet-test-artifacts-height-no-marker-hint-final`
- `git diff --check` exited 0 with only line-ending warnings.
