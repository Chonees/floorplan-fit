---
type: Implementation
date: 2026-06-04
project: floorplan-fit
status: superseded
replaced_by: "[[2026-06-04 - Publish button stays enabled for editable drafts]]"
tags:
  - floorplan-fit
  - loop1
  - publish
  - desktop
---

# Publish validation stays in Application but Desktop handles it

## Superseded

Superseded by [[2026-06-04 - Publish button stays enabled for editable drafts]]. The crash-handling lesson remains valid, but the Desktop button-gating detail was replaced: Publish is enabled for editable drafts, and the Application handler remains the source of truth for missing-pinch validation.


## Change

The Desktop Review ViewModel now prevents and handles publish attempts when the draft has no pinch markers.

## Why

The Application layer already has the correct product guard: a curation without pinches is not fit-ready and must not be published. The bug was Desktop allowing the exception to escape through an `async void` Avalonia click handler, crashing the app instead of teaching the operator what is missing.

## Design

- Keep `PublishFloorPlanCurationHandler` strict.
- Reflect the same rule in Desktop:
  - `CanPublishCuration = DraftCurationId != Guid.Empty && PinchMarkers.Count > 0`.
- When a publish is attempted without pinches, set:
  - `StatusMessage = "AgregÃ¡ al menos un pinche antes de publicar."`
- Catch `InvalidOperationException` from the mutation call and map known no-pinch validation to the same Spanish message.
- Notify `CanPublishCuration` after session state is applied, because `PinchMarkers` can change after adding/removing pinches or groups.

## Verification

- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~PublishFloorPlanCurationHandlerTests" --no-restore` => 2/2.
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~PublishAsync_without_pinch_markers_shows_validation_message_instead_of_throwing|FullyQualifiedName~StartEditingPublishedCurationAsync_creates_editable_draft_and_allows_creating_measurement_corridor|FullyQualifiedName~Published_review_header_exposes_explicit_edit_action_before_publish_action|FullyQualifiedName~RemoveSelectedPinchGroupAsync_removes_selected_group_and_all_its_markers" --artifacts-path ".artifacts-test/publish-crash-final"` => 4/4.
- `git diff --check` => exit 0, with only existing line-ending warnings.

## Related

- [[../Bugs/2026-06-04 - Publish without pinches crashed Desktop dispatcher]]
- [[2026-06-04 - Fit pinch groups can be removed with their pinches]]
