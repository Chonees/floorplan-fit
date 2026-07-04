# Loop 3B Selection State Design

## Goal

Continue **Loop 3** by extracting **selection state orchestration** out of `FloorPlanReviewViewModel` now that review mutation orchestration already lives in `FloorPlanReviewMutationCoordinator`.

The immediate goal is to stop using one ViewModel as the place where preview-hit routing, selection snapshot capture/replay, cross-clearing of selected artifacts, highlight/preview-label updates, and editor sync side effects all live mixed together.

## Product Loop + Architecture Layer

### Product scope

This work belongs to **Loop 1 shared foundation**.

It does not add new curation behavior. It restructures the Desktop review shell that supports existing CAD-faithful floor-plan curation.

### Architecture layer

- **Desktop** — primary
- **Application integration boundary** — unchanged

## Verified Problem

Verified on **2026-05-12** after closing Loop 3A:

- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` is **2133 lines**
- the ViewModel already delegates review mutations, and only load/refresh query handlers remain inline
- the next hotspot is the **selection state block**, spread across:
  - `SelectPreviewPath(...)`
  - `CaptureSelection(...)`
  - `BuildSelectionSnapshotForMovedArtifact(...)`
  - replay of selection inside `ApplySession(...)`
  - artifact-specific partials like:
    - `OnSelectedCandidateChanged(...)`
    - `OnSelectedCuratedArtifactChanged(...)`
    - `OnSelectedRoomLabelChanged(...)`
    - `OnSelectedPinchMarkerChanged(...)`
    - `OnSelectedOpeningCandidateChanged(...)`
    - `OnSelectedOpeningLabelChanged(...)`
    - `OnSelectedFixedPlanComponentChanged(...)`
    - `OnSelectedProtectedDetailAssemblyChanged(...)`
    - `OnSelectedDimensionChanged(...)`
    - `OnSelectedPinchGroupChanged(...)`
- those blocks repeatedly mix:
  - deciding **what becomes selected**
  - deciding **what gets cleared**
  - deciding **what the preview should highlight**
  - deciding **what label the preview should show**
  - deciding **which editor must sync or clear**
  - triggering `NotifyUxStateChanged()`

That is the wrong responsibility boundary.

`FloorPlanReviewViewModel` should remain the **Desktop state shell**, not the place where all selection truth and presentation side effects are handwritten inline.

## Alternatives Considered

### Option A — Split selection work into routing first, presentation second (**recommended**)

Treat Loop 3B as two internal sub-slices:

- **Loop 3B1** — extract selection routing and snapshot/replay truth
- **Loop 3B2** — extract selection presentation side effects

**Pros**
- keeps the refactor surgical
- first slice attacks the truth model before touching UI side effects
- second slice can reuse a cleaner routing seam
- lower blast radius than a full one-shot extraction

**Cons**
- needs two coordinated steps instead of one
- the ViewModel stays partially selection-heavy until both slices close

### Option B — Move selection code to partial files only

Split the same code into `FloorPlanReviewViewModel.Selection.cs` or similar.

**Pros**
- fast visual cleanup
- low immediate risk

**Cons**
- mostly cosmetic
- does not create real ownership
- the same class still owns the same mixed responsibilities

### Option C — Extract one big SelectionState object in one shot

Create a single owner for all selection truth and all selection side effects at once.

**Pros**
- conceptually clean on paper
- could yield a smaller ViewModel quickly

**Cons**
- too much blast radius for a first pass
- harder to preserve subtle ordering and UI semantics
- easier to break CommunityToolkit-driven property change behavior

## Chosen Approach

Use **Option A**.

Loop 3B should advance in **two internal sub-slices**:

### Loop 3B1 — selection routing

Extract the logic that answers:

- what artifact should be selected for a preview hit
- what snapshot represents the current selection
- what snapshot should be preserved after moved-artifact operations
- how a stored selection snapshot resolves back to live items after refresh

This slice should introduce a focused collaborator such as:

- `FloorPlanReviewSelectionCoordinator`

and keep the ViewModel responsible for applying the resolved output to observable properties.

### Loop 3B2 — selection presentation

Extract the repeated side effects that answer:

- what other selections get cleared when one item becomes active
- what `HighlightGeometryPathId` should be
- what `PreviewSelectionLabel` should be
- when curated editors or label text editors must sync/clear
- when `NotifyUxStateChanged()` should be triggered

This slice should leave the ViewModel as the property shell while removing the repeated handwritten selection side effects.

## Scope

### In scope

- extracting selection routing out of `FloorPlanReviewViewModel`
- extracting selection snapshot capture/replay truth
- extracting repeated selection presentation side effects
- keeping current selection semantics unchanged
- preserving existing preview-selection behavior and inspector/editor behavior

### Out of scope

- queue/filter orchestration
- mutation orchestration (already handled by Loop 3A)
- changes to Application handlers
- changes to preview rendering
- UX wording changes beyond preserving current labels

## Recommended Shape

The safest shape is:

- one focused collaborator: `FloorPlanReviewSelectionCoordinator`
- small DTO/result shapes when needed, for example:
  - preview-hit resolution result
  - snapshot capture input/output
  - replay resolution result
  - selection presentation outcome

Directionally, the collaborator should own:

- preview-hit priority resolution
- selection snapshot capture
- moved-artifact snapshot derivation
- selection replay matching after session refresh
- derived selection presentation outcomes

The ViewModel should keep:

- `[ObservableProperty]` ownership
- observable collections
- final property assignment
- `NotifyUxStateChanged()`
- `StatusMessage`
- query/load/refresh flow

## Why This Slice Next

This is the best tradeoff after Loop 3A between:

- **blast radius** — lower than queue + selection together
- **architectural payoff** — high, because selection truth is currently spread across many methods
- **testability** — strong Desktop ViewModel tests already exercise preview selection and selected-item behavior
- **readability gains** — high, because this removes the next biggest conceptual knot from the ViewModel

## Acceptance Criteria

- `FloorPlanReviewViewModel` no longer owns the bulk of selection routing truth inline
- selection snapshot capture/replay no longer lives entirely inside the ViewModel
- repeated artifact-specific selection side effects are materially reduced from the ViewModel
- current preview selection, inspector state, and editor sync semantics remain unchanged
- existing Desktop ViewModel tests stay green
- after Loop 3B, the next decision can cleanly evaluate **queue/filter orchestration** versus going straight to **Loop 4**
