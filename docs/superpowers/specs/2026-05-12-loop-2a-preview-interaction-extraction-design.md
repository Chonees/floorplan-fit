# Loop 2A Preview Interaction Extraction Design

## Goal

Execute the first slice of **Loop 2** as a **surgical, low-risk refactor** that removes preview interaction coordination from `FloorPlanPreviewControl` without changing Loop 1 product behavior.

The immediate objective is to stop using `FloorPlanPreviewControl` as the place where pointer-state transitions, zoom/pan logic, drag orchestration, and dimension-edit initiation all live at once.

## Product Loop + Architecture Layer

### Product scope

This work belongs to **Loop 1 shared foundation**.

It does **not** add new site-fit behavior, new curation capabilities, or new dimension features. It only restructures the interaction brain behind the existing CAD-faithful review surface.

### Architecture layer

- **Desktop** — primary
- **Documentation / repository truth** — supporting

## Problem

`src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs` is still the main hotspot after Loop 1.

Verified current state:

- file length is **1867 lines**
- renderers are already reasonably extracted into `Controls/Preview/*`
- native-dimension hit-testing and edit math already have dedicated helpers
- but the control still mixes:
  - Avalonia property and observer wiring
  - pointer event entry points
  - interaction state transitions
  - zoom/pan state updates
  - pinch-edge drag preview
  - movable-artifact drag flow
  - dimension-edit initiation and commit
  - hit-resolution priority ordering
  - render orchestration

That is the wrong responsibility boundary.

The next safe move is **not** more renderer fragmentation.

The next safe move is to extract the **interaction coordinator**.

## Chosen Approach

Start Loop 2 with a **surgical interaction-only extraction**.

The first slice will move interaction orchestration out of `FloorPlanPreviewControl` into a focused collaborator while deliberately leaving the following in place for now:

- Avalonia styled properties
- collection observer wiring
- render-layer ordering and drawing composition
- existing renderer files
- existing dimension/projector/helper files

This keeps the blast radius narrow and the regression surface easy to verify.

## Scope

### In scope

- pointer press / move / release / wheel coordination
- state transitions for:
  - preview panning
  - compression-edge preview drag
  - movable-artifact drag
  - dimension edit drag
- hit-priority routing for:
  - pinch handle drag
  - dimension handle hit
  - dimension body hit
  - room label hit
  - opening label hit
  - geometry hit
  - movable-artifact resolution
- zoom / pan transition helpers used by the interaction flow

### Out of scope

- collection observer cleanup
- Avalonia property registration cleanup
- render pipeline decomposition
- changing hit-test semantics or visual behavior
- redesigning dimension editing behavior
- changing published DTOs or Application handlers

## File Structure Direction

### Existing owner that stays

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
  - remains the Avalonia control shell
  - remains the render composition surface
  - remains the event publisher to the outside world

### New focused collaborator

- `src/FloorplanFit.Desktop/Controls/Preview/PreviewInteractionCoordinator.cs`
  - owns interaction decision-making
  - receives current preview state + pointer input + available preview data
  - returns explicit interaction outcomes instead of mutating the whole control directly

### Existing helpers reused instead of reinvented

- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionHitTester.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/NativeDimensionEditor.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PinchMarkerPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewProjector.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/CadTextPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PreviewArtifactGeometryIndex.cs`

The design intentionally **reuses** these helpers so Loop 2A does not become a shadow rewrite.

## Responsibility Split

## `FloorPlanPreviewControl`

After Loop 2A, the control should own:

- Avalonia properties
- event declarations and event invocation
- current interaction fields/state storage
- render composition
- collection observer lifecycle
- conversion between Avalonia event objects and coordinator inputs

It should **not** keep the full conditional interaction tree inline.

## `PreviewInteractionCoordinator`

The new coordinator should own:

- deciding what a pointer press means
- deciding which interaction mode wins when multiple targets are possible
- updating panning / dragging / editing state transitions
- deciding whether a release commits a movement or edit
- producing explicit outcome objects the control can apply

This file should answer one question:

> given the current preview state and pointer input, what interaction transition should happen next?

## Interaction Model

The coordinator should preserve the current priority order already encoded in the control:

1. middle-button preview pan
2. left-button compression-edge drag when pinch placement is not armed
3. dimension handle hit
4. dimension body hit
5. room label hit
6. opening label hit
7. geometry hit
8. movable-artifact drag if the geometry maps to a movable source

That priority order is part of current behavior and must stay stable.

## Proposed Coordinator Shape

The safest design is a coordinator that returns **explicit result objects** rather than calling UI events itself.

Example responsibility direction:

- `HandlePointerPressed(...)` -> returns a result describing:
  - whether the event is handled
  - whether capture should start
  - updated interaction state
  - optional domain-facing action intent:
    - select dimension
    - select room label
    - select opening label
    - select geometry path

- `HandlePointerMoved(...)` -> returns:
  - updated interaction state
  - whether visual invalidation is needed

- `HandlePointerReleased(...)` -> returns:
  - updated interaction state
  - optional committed artifact movement
  - optional committed dimension edit
  - whether capture should be released

- `HandlePointerWheel(...)` -> returns:
  - updated zoom state
  - whether the wheel was handled

The control remains the place that translates those outcomes into:

- `InvalidateVisual()`
- `Pointer.Capture(...)`
- event invocations like `DimensionClicked`, `RoomLabelClicked`, `MovableArtifactMoved`, `DimensionEdited`, etc.

This separation is important because it keeps UI framework effects in the control and interaction decisions in the coordinator.

## Testing Strategy

This slice must stay **TDD-first**.

### New test target

Add a new test file focused on the coordinator, for example:

- `tests/FloorplanFit.Desktop.Tests/Controls/PreviewInteractionCoordinatorTests.cs`

### Red/green priorities

1. press priority is preserved
   - middle button pan beats everything
   - left-button dimension handle beats dimension body
   - label hits beat generic geometry hit where applicable

2. wheel zoom still anchors to the same world point

3. panning still updates zoom state only through pan offset

4. movable-artifact drag still produces the same commit/no-op behavior

5. dimension edit release still emits a change only when the dimension actually changed

6. pinch preview drag still updates preview trim state without committing product changes on move

### Existing regression coverage to preserve

Existing tests around these helpers remain valuable and should keep passing:

- `FloorPlanPreviewControlTests`
- viewport / zoom helper tests
- preview renderer tests
- dimension helper tests

## Acceptance Criteria

1. `FloorPlanPreviewControl` becomes materially smaller.
2. The pointer interaction decision tree no longer lives inline in the control.
3. Existing interaction behavior remains unchanged from the user’s perspective.
4. The new coordinator has focused tests covering priority and transition behavior.
5. Render composition, observer wiring, and Avalonia property setup remain untouched unless required for the extraction itself.

## Tradeoffs

### Why this is the right first slice

Pros:

- smallest safe blast radius
- high confidence regression boundary
- removes the most fragile “brain” logic first
- prepares later Loop 2B cleanup without mixing concerns

Cons:

- `FloorPlanPreviewControl` will still be large after this slice
- observer wiring repetition will remain temporarily
- render orchestration will still need a later pass

That is acceptable because the point of Loop 2A is certainty, not heroics.

## Alternatives Considered

### 1. Extract interaction + viewport helpers immediately

Rejected for the first slice.

Why:

- too easy to blur behavior and math concerns together
- increases regression surface without enough payoff yet

### 2. Extract interaction + observer wiring together

Rejected for the first slice.

Why:

- mixes two unrelated cleanups
- makes failures harder to localize
- violates the “surgical and safe” constraint

### 3. Keep interaction in the control and only add comments/regions

Rejected.

Why:

- fake modularization
- no real ownership change
- does not reduce the hotspot’s cognitive load

## Recommendation

Implement **Loop 2A** as:

- one new interaction coordinator
- one new focused test file
- minimal control changes to delegate interaction decisions
- no observer cleanup yet
- no render-pipeline split yet

After this slice is green and stable, the next follow-up can decide whether **Loop 2B** should attack:

- observer wiring / invalidation repetition, or
- viewport/render composition boundaries

But not both at once.
