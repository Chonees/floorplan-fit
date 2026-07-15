---
type: implementation
date: 2026-07-14
loop: 2
layers:
  - Desktop
status: static-complete-runtime-unverified
---

# Pending registration visual review overlay

## Problem

The HousePlanSet sheet row rendered the full registration evidence inline. Long evidence consumed the row width and hid the existing Confirm action, while users had no visual proof of the estimated ElectricalPlan-to-FloorPlan transform.

## Implemented

- Replaced inline quality prose with compact status/method/confidence fields and tooltips; actions now live on a separate wrapping row.
- Replaced direct pending confirmation with `Revisar y confirmar`.
- Added a bounded modal review surface with:
  - canonical FloorPlan WALL geometry;
  - ElectricalPlan WALL geometry transformed with the persisted registration scale, rotation, and translation;
  - shared initial fit, distinct translucent colors, and a legend;
  - wrapping/scrollable diagnostics;
  - fixed Cancel/Back and Confirm actions.
- Preview loading reads the exact canonical version and dependent-sheet source through existing Application ports, reuses `IWallExtractor`, and reuses Desktop viewport/workspace/line-clipping primitives.
- Load failures remain visible and disable Confirm. Cancel closes with `false` and does not call a mutation. Confirm delegates to the pre-existing confirmation workflow and refresh behavior.

## Files

- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `src/FloorplanFit.Desktop/ViewModels/LibraryViewModel.cs`
- `src/FloorplanFit.Desktop/RegistrationReviewDialog.axaml`
- `src/FloorplanFit.Desktop/RegistrationReviewDialog.axaml.cs`
- `src/FloorplanFit.Desktop/Controls/RegistrationOverlayPreviewControl.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/RegistrationReviewLayoutTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`

## Verification boundary

- Passed static XML parsing for both touched XAML surfaces.
- Passed scoped `git diff --check`.
- Tests were authored before production logic but intentionally not executed under repository policy.
- Compile, test, and Desktop runtime behavior remain unverified until the user runs them externally.

## Boundary correction: Electrical only

Static review found that `CanConfirmRegistration` alone also includes pending RoofPlan and FacadeElevation registrations. The row now splits actions with a Desktop visibility converter:

- Electrical pending registration -> `Revisar y confirmar` -> overlay.
- Roof/Facade pending registration -> existing direct `Confirm` -> existing confirmation workflow.

Both handlers reject the opposite sheet category defensively. Focused source/layout coverage was added. Scoped whitespace and XAML parsing passed; compile/runtime remain external.
