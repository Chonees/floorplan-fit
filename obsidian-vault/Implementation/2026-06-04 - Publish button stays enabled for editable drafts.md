---
type: Implementation
date: 2026-06-04
project: floorplan-fit
status: active
replaces: "[[2026-06-04 - Publish validation stays in Application but Desktop handles it]]"
tags:
  - floorplan-fit
  - loop1
  - publish
  - desktop
  - fit-curation
---

# Publish button stays enabled for editable drafts

## Change

The Desktop Review header now enables **Publish Curation** whenever the session has an editable draft. It no longer disables the button based on `PinchMarkers.Count` in the ViewModel projection.

## Why

The previous Desktop guard duplicated the Application publish invariant in a cached UI projection. If pinches were persisted but the session projection was stale or not refreshed the way the operator expected, the button stayed disabled and the operator had no way to ask the real use case to publish.

That was the wrong boundary: Desktop should decide whether there is an editable draft; Application remains the source of truth for whether the draft is publishable.

## Design

- `CanPublishCuration = DraftCurationId != Guid.Empty`.
- `PublishAsync` calls `PublishFloorPlanCurationHandler` for editable drafts.
- `PublishFloorPlanCurationHandler` still rejects drafts with zero persisted `PinchMarker` rows.
- Desktop catches the known no-pinch validation exception and shows: `Agreg? al menos un pinche antes de publicar.`
- If pinches are already persisted but the ViewModel projection is stale, publish can still succeed because the Application handler reads the repository state, not the UI cache.

## Verification

- RED first: the new Desktop tests failed while `CanPublishCuration` depended on `PinchMarkers.Count`.
- Green focused tests: `PublishAsync_without_pinch_markers_keeps_button_enabled_and_shows_validation_message` and `PublishAsync_uses_persisted_pinch_markers_even_when_the_session_projection_is_stale` passed 2/2.
- Broader ViewModel slice passed 58/58:
  - `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewViewModelTests|FullyQualifiedName~MeasurementBindingFloorPlanReviewViewModelTests" --artifacts-path ".artifacts-test\publish-enable-viewmodels" --verbosity minimal`
- `git diff --check` exited 0 with only existing LF to CRLF warnings.

## Related

- [[../Bugs/2026-06-04 - Publish stayed disabled after pinches were placed]]
- [[../Bugs/2026-06-04 - Publish without pinches crashed Desktop dispatcher]]
- [[2026-06-04 - Publish validation stays in Application but Desktop handles it]]
