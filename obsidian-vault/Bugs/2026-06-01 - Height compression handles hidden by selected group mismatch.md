---
type: Bug
date: 2026-06-01
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - fit-preview
  - pinch-groups
  - height-axis
---

# Height compression handles hidden by selected group mismatch

## Verified root cause

The preview handle renderer intentionally requires a selected pinch group with at least one marker on the active preview axis. This is correct because handles need a concrete group/marker set to drive preview compression.

The bug was in ViewModel state: changing the toolbar axis from `Width` to `Height` only updated `SelectedPinchAxis`; it did not update `SelectedPinchGroup`. If the selected group remained a Width group, `CompressionHandlePreviewLayerRenderer.HasPreviewDriver(...)` returned false for `Height`, so the top/bottom handles stayed hidden even when a valid Height group/marker existed elsewhere.

## Fixed

`FloorPlanReviewViewModel.OnSelectedPinchAxisChanged` now resolves a compatible pinch group for the selected axis. It prefers groups that already have a marker on that axis, then falls back to the first group for that axis. This keeps `PreviewAxisTag` and `PreviewPinchGroupId` coherent for the preview control.

## Verification

- RED: `Changing_pinch_axis_to_height_selects_height_group_so_preview_handles_have_a_driver` failed because the selected group stayed Width after switching the axis to Height.
- GREEN: the same test passed after the ViewModel fix.
- Focused Desktop slice passed 98/98:
  `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .testartifacts\dotnet-test-artifacts-height-handle-desktop-slice`
- `git diff --check` exited 0 with only line-ending warnings.
