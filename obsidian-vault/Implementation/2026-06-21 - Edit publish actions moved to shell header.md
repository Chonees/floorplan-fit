# 2026-06-21 - Edit publish actions moved to shell header

## Type
Implementation

## Replaces
- [[2026-06-18 - Edit screen chrome cleanup]] for the `Editar` / `Publish Curation` placement only.

## What changed
- Loop 1 Edit now puts `Editar` and `Publish Curation` on the same shell row as `← Library`, right-aligned after the status text.
- Removed those two actions from the right-side Actions panel so publish/edit no longer live inside a secondary inspector section.
- The shell buttons bind to `ActiveReviewViewModel.CanEditPublishedCuration` and `ActiveReviewViewModel.CanPublishCuration`, preserving the published-read-only -> click Edit -> draft -> publish flow.
- The Fit toolbar and `Crear grupo de pinches` action are disabled while no draft is open, so a published curation is not visually editable until `Editar` starts an edit draft.

## Why
The user reported the prior placement was weird: once inside Edit, the primary edit/publish workflow belongs on the same navigation line as the Library back button, not buried in the right inspector.

## Verification
- RED source check failed before the change because `MainWindow.axaml` did not expose the four-column review shell row or header buttons.
- GREEN source check passed after the change for header placement, bindings, removed inspector duplicate buttons, and draft edit lock.
- `git diff --check` exited `0` with CRLF warnings only.
- No .NET build was run per repo rule.

## Files
- `src/FloorplanFit.Desktop/MainWindow.axaml`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
