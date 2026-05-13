# Loop 4C/4D Review Shell Final Cleanup Design

## Goal

Finish the remaining **Loop 4** modularization work by extracting the last residual **apply/notify shell orchestration** out of `FloorPlanReviewViewModel`, then closing the loop with a final documentation and ownership audit so the repository truth matches the refactored Desktop architecture.

## Product Loop + Architecture Layer

### Product scope

This work belongs to **Loop 1 shared foundation**.

It does not change floor-plan curation behavior. It finishes the Desktop shell cleanup that supports CAD-faithful review.

### Architecture layer

- **Desktop** — primary
- **Documentation / architecture map truth** — secondary

## Verified Problem

Verified on **2026-05-12** after closing Loop 4B:

- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` is **1307 lines**
- the ViewModel already delegates:
  - mutations
  - selection routing/presentation
  - review queue projection
  - inspector presentation
  - review session queries/projection
- the remaining hotspot is the **shell apply + notify block**, concentrated in:
  - `ApplySessionProjection(...)`
  - `ApplySelectionPresentationOutcome(...)`
  - `NotifyReviewQueueStateChanged()`
  - `NotifyUxStateChanged()`
  - the editor sync helpers that only exist to support those apply flows

Those methods still mix:

- applying session DTO-derived data into observable collections
- replaying selection after refresh
- applying cross-clears / linked selections / editor sync side effects
- normalizing inspector-tool state
- deciding which Desktop properties must raise `OnPropertyChanged(...)`

That is still the wrong responsibility boundary for the ViewModel shell.

Also verified on **2026-05-12**:

- `docs/explicacion de toda la app/2026-04-30 - mapa completo de arquitectura y archivos.md` still describes `FloorPlanReviewViewModel` as the Loop 3 hotspot
- that map does **not** mention the new Desktop review coordinators created during Loops 3 and 4

So the remaining work is not only code cleanup; it is also a **truth-maintenance** problem.

## Alternatives Considered

### Option A — One big shell coordinator

Create a single new collaborator that owns:

- session apply
- selection presentation apply
- queue property notifications
- UX property notifications

**Pros**
- fast size reduction in one pass
- fewer new files

**Cons**
- creates another too-large coordinator
- mixes apply truth and notification truth together
- harder to test and reason about independently

### Option B — Two focused coordinators plus final documentation audit (**recommended**)

Split the remaining work into:

- **Loop 4C1** — apply orchestration
- **Loop 4C2** — notification orchestration
- **Loop 4D** — final map/ownership audit

**Pros**
- real modularization instead of just moving code around
- low/medium blast radius
- matches the repository’s loop-by-loop cleanup style
- leaves a cleaner final story for `Current State.md` and the architecture map

**Cons**
- requires two code seams plus one documentation pass
- takes slightly longer than a one-shot move

### Option C — Stop at docs only

Update `Current State.md` and the map, but leave the residual ViewModel shell as-is.

**Pros**
- very low code risk

**Cons**
- does not actually finish modularization
- leaves the last Desktop hotspot unresolved

## Chosen Approach

Use **Option B**.

### Loop 4C1 — apply orchestration

Introduce a focused collaborator such as:

- `FloorPlanReviewApplyCoordinator`

This collaborator should own the truth for:

- turning `ReviewSessionProjection` into an apply plan for the ViewModel shell
- replaying resolved selection after session refresh
- turning `SelectionPresentationOutcome` into a final apply plan for cross-clears, linked selections, highlight/preview-label updates, and editor sync actions

The ViewModel should still own:

- observable collections
- final property assignment
- `OnPropertyChanged(...)`
- direct access to current selected observable properties

### Loop 4C2 — notification orchestration

Introduce a focused collaborator such as:

- `FloorPlanReviewNotificationCoordinator`

This collaborator should own the truth for:

- which queue-derived properties need notification fan-out
- which UX/inspector-derived properties need notification fan-out
- normalized inspector tool selection before the ViewModel emits notifications

The ViewModel should remain the one that actually calls `OnPropertyChanged(...)`.

### Loop 4D — final ownership/documentation cleanup

Close the program by:

- updating `Current State.md`
- writing implementation notes for Loop 4C/4D
- refreshing the canonical architecture map so it reflects the review coordinators now living under `src/FloorplanFit.Desktop/ViewModels/Review/`
- explicitly recording that the remaining large Desktop shell files are now mostly composition/applier shells, not mixed procedural hotspots

## Scope

### In scope

- extracting session-apply orchestration out of `FloorPlanReviewViewModel`
- extracting selection-presentation apply orchestration out of `FloorPlanReviewViewModel`
- extracting queue and UX notification fan-out truth
- final documentation audit for the review coordinator cluster

### Out of scope

- new curation features
- changes to Application handlers
- changes to preview rendering
- queue filtering/grouping rules (already owned by `FloorPlanReviewQueueCoordinator`)
- inspector presentation text rules (already owned by `FloorPlanReviewInspectorCoordinator`)

## Recommended Shape

The safest final shape is:

- `FloorPlanReviewMutationCoordinator`
- `FloorPlanReviewSelectionCoordinator`
- `FloorPlanReviewQueueCoordinator`
- `FloorPlanReviewInspectorCoordinator`
- `FloorPlanReviewSessionCoordinator`
- **new:** `FloorPlanReviewApplyCoordinator`
- **new:** `FloorPlanReviewNotificationCoordinator`

Directionally:

- coordinators decide or prepare outcomes
- `FloorPlanReviewViewModel` applies outcomes to observable state

That keeps the ViewModel as a **Desktop shell**, not a procedural god-object.

## Why This Is The Right Finish

At this point the review shell has already been split by concept:

- mutation
- selection
- queue
- inspector
- session query/projection

The final unfinished concept is **shell application and notification fan-out**.

If we stop now, the cleanup is only “mostly done”.

If we finish this seam and refresh the docs, the repository can honestly say that the Loop 4 modularization program is closed.

## Acceptance Criteria

- `FloorPlanReviewViewModel` no longer owns the bulk of session-apply and selection-presentation apply truth inline
- `FloorPlanReviewViewModel` no longer owns the full handwritten queue/UX notification fan-out truth inline
- the ViewModel remains the owner of observable collections/properties and final `OnPropertyChanged(...)` calls
- the Desktop tests remain green
- `Current State.md` reflects Loop 4 closure and the next work returns to product evolution instead of architecture cleanup
- the canonical architecture map is refreshed so the review coordinator cluster appears in the documented repo truth
