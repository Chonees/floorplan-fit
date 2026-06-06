---
type: Bug
date: 2026-06-04
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - publish
  - desktop
  - fit-curation
---

# Publish without pinches crashed Desktop dispatcher

## Follow-up

The crash fix remains valid, but the original button-gating detail was superseded by [[../Implementation/2026-06-04 - Publish button stays enabled for editable drafts]]. `Publish Curation` is now enabled for editable drafts; missing-pinch validation stays in the Application handler and Desktop maps that validation to the Spanish status message.

## Symptom

Clicking **Publish Curation** could crash the Avalonia desktop app and make `dotnet watch` exit with code `-532462766`.

The visible tail of the stack ended in Avalonia's dispatcher/main loop, but that was only the UI thread exit path.

## Root Cause

`PublishFloorPlanCurationHandler` correctly rejects publishing a draft with no pinch markers:

```text
InvalidOperationException: A curation must contain at least one pinch marker before publish.
```

Before this fix, `FloorPlanReviewViewModel.PublishAsync` called the handler without guarding or catching that validation failure. Because the click handler is `async void`, the exception escaped into Avalonia's dispatcher and crashed the desktop process.

## Fix

- `CanPublishCuration` now requires:
  - an editable draft curation, and
  - at least one `PinchMarker` in the current review session.
- `PublishAsync` now short-circuits with a Spanish user message when there are no pinches:
  - `Agregá al menos un pinche antes de publicar.`
- `PublishAsync` also catches Application-layer `InvalidOperationException` validation failures and turns them into `StatusMessage` instead of letting them crash the app.
- Session refresh now raises `CanPublishCuration` after replacing `PinchMarkers`, so adding/removing pinches updates the Publish button state.

## Verification

- RED first: `PublishAsync_without_pinch_markers_shows_validation_message_instead_of_throwing` failed with the exact `InvalidOperationException`.
- Green focused Desktop test passed after the fix.
- Application publish handler tests still pass and keep the domain guard intact.
- Related Desktop publish/edit/group-delete focused tests pass.
- `git diff --check` exits 0, with only existing LF→CRLF warnings.

## Related

- [[../Implementation/2026-06-04 - Publish validation stays in Application but Desktop handles it]]
- [[../Implementation/2026-06-04 - Fit pinch groups can be removed with their pinches]]
