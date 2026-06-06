---
type: Bug
date: 2026-06-03
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - seminole2000
  - publish
  - fit-curation
  - measurement-bindings
  - pinch-groups
---

# Opening published Seminole hides Fit data behind empty draft

## Verified symptom

After publishing `SEMINOLE2000`, reopening the review session can show no related dimensions/franjas and no user-created pinch groups in the active Fit UI.

This is reproducible from the local workspace database:

- Published curation `06c496a5-e8c8-45f7-900d-3bb3319d9343`
  - `pinch_groups`: 11
  - `pinch_markers`: 1
  - `measurement_corridors`: 93
  - `measurement_nodes`: 187
  - `floorplan_dimension_interval_bindings`: 87
- New draft curation `faebd608-0b33-41ac-8a97-e1c4b482b11f`
  - `based_on_curation_id`: `06c496a5-e8c8-45f7-900d-3bb3319d9343`
  - `pinch_groups`: 0
  - `pinch_markers`: 0
  - `measurement_corridors`: 0
  - `measurement_nodes`: 0
  - `floorplan_dimension_interval_bindings`: 0

## Root cause

`OpenFloorPlanReviewSessionHandler` always calls `StartOrResumeCurationHandler` before reading the session. Once a curation has been published, `StartOrResumeCurationHandler` creates a new empty draft whose `based_on_curation_id` points to the published curation.

`SqliteFloorPlanReviewSessionReader.GetCurationContext` correctly returns lineage `[published, draft]` for override-style data, but the Fit structures are loaded only from `ActiveCurationId`:

- `GetPinchGroups(activeDraftId)`
- `GetPinchMarkers(activeDraftId)`
- `GetMeasurementCorridors(activeDraftId)`
- `GetMeasurementNodes(activeDraftId)`
- `GetDimensionIntervalBindings(activeDraftId)`

Because the active draft is empty, the review UI hides the already-published Fit data even though the published rows still exist in SQLite.

## Product impact

Loop 1 publish is not losing the data. The data remains attached to the published curation. The bug is the edit/review reopening model: after publish, the reader prioritizes the new empty draft for Fit data instead of inheriting/copying published Fit state.

## Candidate fixes

1. **Copy-on-draft creation**
   - When creating a draft from a published curation, clone pinch groups, pinches, franjas, nodes, and dimension interval bindings into the new draft.
   - Tradeoff: simpler reader and edit semantics, but needs careful ID remapping for group/corridor/node/binding foreign keys.

2. **Lineage-aware Fit reader**
   - Read Fit data from the newest lineage curation that has rows, then overlay draft edits.
   - Tradeoff: less duplication, but delete/edit semantics become more complex because the UI must distinguish inherited rows from draft-owned rows.

3. **Open published read-only unless user explicitly starts a new edit draft**
   - Reopening a published plan shows the published curation directly; a new draft is created only when the user chooses to edit.
   - Tradeoff: clean product semantics, but requires UI state for read-only vs edit mode.

## Current recommendation

Prefer **copy-on-draft creation** for the current MVP because the existing mutation handlers assume rows belong to the active curation id. It keeps the Application/Desktop mutation model simple and makes reopened sessions behave like the user expects: published Fit work is visible and editable immediately.

## Fixed

The review/open flow now treats an active published curation as the display source of truth.

- `OpenFloorPlanReviewSessionHandler` checks the review session before creating/resuming a draft. If the selected template/version already has an active published curation, it returns that session with `DraftCurationId = Guid.Empty` instead of creating a new draft.
- `SqliteFloorPlanReviewSessionReader` now reports `Published` before `Curated Draft` when `floorplan_templates.active_published_curation_id` points to the selected version.
- `SqliteFloorPlanReviewSessionReader.GetCurationContext` now prefers the active published curation over any leftover draft, so stale empty drafts no longer hide published pinch groups, franjas, nodes, or dimension interval bindings.

## Verification

- RED confirmed first:
  - `OpenFloorPlanReviewSessionHandlerTests.HandleAsync_returns_the_active_published_session_without_creating_a_new_draft` failed because a new draft id was returned.
  - `FloorPlanReviewSessionReaderIntegrationTests.GetByTemplateAsync_returns_latest_published_fit_data_when_an_empty_post_publish_draft_exists` failed because status was `Curated Draft` and Fit data came back empty.
- GREEN after fix:
  - `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter "FullyQualifiedName~OpenFloorPlanReviewSessionHandlerTests" --nologo --verbosity minimal` -> 3/3 passed.
  - `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests.GetByTemplateAsync_returns_latest_published_fit_data_when_an_empty_post_publish_draft_exists" --nologo --verbosity minimal` -> 1/1 passed.
  - `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~FloorPlanReviewSessionReaderIntegrationTests|FullyQualifiedName~OpenFloorPlanReviewSessionIntegrationTests" --nologo --verbosity minimal` -> 3/3 passed.
  - `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --nologo --verbosity minimal` -> 86/86 passed.
  - `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --nologo --verbosity minimal` -> 77/77 passed.
  - `git diff --check` exited 0, with only existing line-ending warnings.
