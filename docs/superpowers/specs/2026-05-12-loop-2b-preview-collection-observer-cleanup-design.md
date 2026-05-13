# Loop 2B Preview Collection Observer Cleanup Design

## Goal

Execute the second slice of **Loop 2** as a **surgical, low-risk refactor** that removes collection-observer wiring and invalidation repetition from `FloorPlanPreviewControl` without changing Loop 1 product behavior.

The immediate objective is to stop using `FloorPlanPreviewControl` as the place where eleven property-change handlers, eleven attach helpers, eleven detach helpers, and eleven `CollectionChanged` callbacks all repeat the same observer lifecycle pattern.

## Product Loop + Architecture Layer

### Product scope

This work belongs to **Loop 1 shared foundation**.

It does **not** add new curation behavior, new preview rendering features, or new site-fit logic. It only restructures the observer plumbing behind the existing CAD-faithful review surface.

### Architecture layer

- **Desktop** — primary
- **Documentation / repository truth** — supporting

## Problem

After Loop 2A, the interaction brain already moved out to `PreviewInteractionCoordinator`, but `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` is still a hotspot.

Verified current state on **2026-05-12**:

- file length is **1747 lines**
- pointer move/release/press/wheel coordination now lives behind `PreviewInteractionCoordinator`
- but the control still repeats the same collection-observer pattern across:
  - `OnGeometryPathsChanged`
  - `OnPinchMarkersChanged`
  - `OnRoomLabelsChanged`
  - `OnWallCandidatesChanged`
  - `OnOpeningCandidatesChanged`
  - `OnOpeningLabelsChanged`
  - `OnDimensionsChanged`
  - `OnDimensionAssociationsChanged`
  - `OnFixedPlanComponentsChanged`
  - `OnProtectedDetailAssembliesChanged`
  - `OnCuratedPlanArtifactsChanged`
- plus matching `Attach...CollectionObserver(...)`
- plus matching `Detach...CollectionObserver(...)`
- plus matching `...CollectionChanged(...) => InvalidateVisual()`

That is the wrong responsibility boundary.

The next safe move is **not** render decomposition yet.

The next safe move is to extract the **collection observer hub**.

## Chosen Approach

Start Loop 2B with a **focused observer-plumbing extraction**.

The slice will move collection subscription lifecycle and shared invalidation callbacks out of `FloorPlanPreviewControl` into a dedicated collaborator while deliberately leaving the following in place for now:

- Avalonia styled properties
- pointer event entry points
- `PreviewInteractionCoordinator`
- render-layer ordering and drawing composition
- existing renderer files
- existing DTOs and Application contracts

This keeps the blast radius narrow and the regression surface easy to verify.

## Scope

### In scope

- collection observer registration lifecycle for all preview collections
- replace-on-property-change behavior
- attach-all / detach-all behavior for visual tree lifecycle
- shared invalidation callback wiring
- removing repeated attach/detach/collection-changed boilerplate from the control

### Out of scope

- render pipeline decomposition
- preview interaction changes
- styled property registration changes
- changing which collections trigger invalidation
- changing product-visible preview behavior
- touching `FloorPlanReviewViewModel`

## File Structure Direction

### Existing owner that stays

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - remains the Avalonia control shell
  - remains the interaction state owner
  - remains the render composition surface
  - remains the event publisher to the outside world

### New focused collaborator

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewCollectionObserverHub.cs`
  - owns observer registration and unregistration
  - owns replace-on-property-change logic
  - owns a shared `CollectionChanged -> invalidate` bridge
  - does **not** know anything about rendering, geometry, or pointer interaction

### Existing tests to extend

- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs`
  - keeps shell-facing regression checks

### New focused test target

- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewCollectionObserverHubTests.cs`
  - pins attach / replace / detach / invalidate behavior directly

## Responsibility Split

## `FloorPlanPreviewControl`

After Loop 2B, the control should own:

- Avalonia properties
- event declarations and event invocation
- interaction state storage
- render composition
- conversion between property changes / visual tree lifecycle and hub commands

It should **not** keep the full repeated attach/detach/invalidate plumbing inline.

## `PreviewCollectionObserverHub`

The new hub should own:

- remembering which observable collection is currently registered for each preview slot
- attaching only when the incoming collection is observable
- detaching previous subscriptions on replacement
- detaching everything when the control leaves the visual tree
- invoking one supplied invalidation callback when any observed collection changes

This file should answer one question:

> for each preview collection slot, which observable instance is currently subscribed, and how do we keep that subscription correct?

## Proposed Hub Shape

The safest design is a **small stateful helper** with explicit slot names rather than reflection or dynamic property scanning.

Directionally:

- `PreviewObservedCollectionSlot` enum
  - `GeometryPaths`
  - `PinchMarkers`
  - `RoomLabels`
  - `WallCandidates`
  - `OpeningCandidates`
  - `OpeningLabels`
  - `Dimensions`
  - `DimensionAssociations`
  - `FixedPlanComponents`
  - `ProtectedDetailAssemblies`
  - `CuratedPlanArtifacts`

- `PreviewObservedCollections` snapshot record
  - carries the current set of lists from the control for attach-all

- `PreviewCollectionObserverHub`
  - constructor takes `Action invalidateVisual`
  - `AttachAll(PreviewObservedCollections collections)`
  - `Replace<T>(PreviewObservedCollectionSlot slot, IReadOnlyList<T>? value)`
  - `DetachAll()`

Internally, the hub can store a dictionary from slot -> `INotifyCollectionChanged` and use one shared handler that calls the injected invalidation callback.

The control remains the place that decides **when** to call:

- `AttachAll(...)`
- `Replace(...)`
- `DetachAll()`

This separation is important because it keeps Avalonia lifecycle ownership in the control and pure observer bookkeeping in the hub.

## Testing Strategy

This slice must stay **TDD-first**.

### New hub tests

`PreviewCollectionObserverHubTests` should pin:

1. attach subscribes an observable collection and invalidates on change
2. replace detaches the previous observable collection
3. replace ignores non-observable lists without throwing
4. detach-all removes all active subscriptions
5. replacing the same instance does not double-subscribe

### Existing shell regression to preserve

`FloorPlanPreviewControlTests` should keep thin shell-facing coverage, for example:

- the control still delegates zoom/pan helpers correctly
- the control source no longer carries the old per-collection change callback boilerplate, if a source-inspection assertion is useful

## Acceptance Criteria

1. `FloorPlanPreviewControl` becomes materially smaller again.
2. The repeated collection observer lifecycle no longer lives as 33 nearly-identical methods in the control.
3. Any observed collection mutation still invalidates the preview exactly once per event.
4. Replacing an observed collection detaches the old instance before subscribing the new one.
5. `OnAttachedToVisualTree` / `OnDetachedFromVisualTree` still preserve existing preview behavior.
6. Render composition and interaction behavior remain unchanged from the user’s perspective.

## Tradeoffs

### Why this is the right next slice

Pros:

- removes the next largest mechanical hotspot after Loop 2A
- keeps the control moving toward a true shell role
- isolates a highly testable responsibility
- prepares a later render-cleanup slice without mixing concerns

Cons:

- adds one more helper file to the preview subsystem
- `FloorPlanPreviewControl` will still remain large after this slice
- styled property change registration will still stay verbose for now

## Alternatives Considered

### 1. Keep everything in `FloorPlanPreviewControl` but add a few generic helper methods

This would reduce repetition, but ownership would still stay in the wrong file. It is safer than a broad rewrite, but less modular.

### 2. Split observer methods into a `partial` file only

This improves file-size optics, but mostly relocates the noise instead of extracting the responsibility. It is a cosmetic improvement, not a real architectural boundary.

### 3. Jump straight to render decomposition

Too early. The current verified hotspot is observer plumbing, and mixing that with render cleanup would increase risk for no good reason.

## Follow-up After This Slice

If Loop 2B lands green and stable, the next follow-up can decide whether a later Loop 2C should attack:

- render composition / layer orchestration cleanup, or
- moving on to `FloorPlanReviewViewModel` in Loop 3

The point is to finish the preview shell cleanup in ordered slices, not to reopen a big-bang refactor.
