# Loop 3A Review Command Orchestration Design

## Goal

Start **Loop 3** with a **surgical, low-risk refactor** that extracts review mutation / command orchestration out of `FloorPlanReviewViewModel` before touching broader selection or queue state.

The immediate goal is to stop using one ViewModel as the place where every async review action opens DI scopes, resolves handlers, updates status messages, captures selection, refreshes the session, and decides the post-mutation selection outcome.

## Product Loop + Architecture Layer

### Product scope

This work belongs to **Loop 1 shared foundation**.

It does not add new curation behavior. It restructures the Desktop review shell that supports existing CAD-faithful floor-plan curation.

### Architecture layer

- **Desktop** — primary
- **Application integration boundary** — supporting

## Verified Problem

Verified on **2026-05-12**:

- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs` is **2211 lines**
- the public action / mutation block runs roughly from **`LoadAsync(...)` through `ExcludeSelectedArtifactAsync(...)`**
- the file contains a large amount of repeated orchestration behavior:
  - `StatusMessage = ...`
  - `using var scope = scopeFactory.CreateScope()` or `using (var scope = scopeFactory.CreateScope())`
  - `GetRequiredService<...Handler>()`
  - `await handler.HandleAsync(...)`
  - `await RefreshSessionAsync(...)`
- those patterns appear across review actions such as:
  - reject wall candidate
  - publish curation
  - add pinch group / pinch marker
  - save / restore label text height
  - save / restore artifact positions
  - save / restore curated artifact classification
  - exclude overlay artifacts
  - save dimension overrides
  - export adjusted DXF

That is the wrong responsibility boundary.

`FloorPlanReviewViewModel` should remain the **Desktop state shell**, not the place where every review mutation is manually orchestrated inline.

## Alternatives Considered

### Option A — Start with review command orchestration (**recommended**)

Extract the async mutation / command block into a dedicated collaborator, keeping the ViewModel responsible for state exposure and selection.

**Pros**
- attacks the largest repeated pattern first
- strongest existing test surface already exists across ViewModel tests
- does not require rewriting CommunityToolkit selection partials yet
- cleanly separates "invoke mutation + refresh session" from UI state exposure

**Cons**
- still leaves selection and queue state inside the ViewModel for a later slice
- command outcomes must preserve current selection semantics carefully

### Option B — Start with selection state

Extract the `OnSelectedXChanged(...)` partial methods and preview selection behavior first.

**Pros**
- strong conceptual seam
- no DI or handler orchestration involved

**Cons**
- more fragile because property-changed ordering matters
- touches generated-property semantics and selection cascade rules
- larger risk of subtle UI regressions

### Option C — Start with queue / visible-item orchestration

Extract queue filtering, grouping, counters, and expansion normalization first.

**Pros**
- cohesive cluster
- lower DI complexity

**Cons**
- lower architectural payoff than command extraction
- leaves the ViewModel as a giant mutation orchestrator

## Chosen Approach

Start **Loop 3A** with **review command orchestration**.

Introduce a focused Desktop collaborator that owns the repeated mutation workflow:

- open service scope
- resolve the correct application handler
- run the mutation
- report structured mutation outcomes back to the ViewModel

The ViewModel should keep:

- observable collections and selected items
- computed properties / inspector state
- selection snapshot capture
- session refresh and application of refreshed state

But it should stop hand-writing the same command ceremony in 15+ methods.

## Scope

### In scope

- extracting review mutation orchestration out of `FloorPlanReviewViewModel`
- introducing a dedicated collaborator for command execution / mutation flow
- reducing repeated `scopeFactory + handler + status + refresh` code paths
- preserving the current review behavior and selection persistence semantics

### Out of scope

- rewriting selection partials (`OnSelectedXChanged(...)`)
- rewriting queue filtering / grouping logic
- changing Application handlers
- changing review UX copy or behavior
- touching `FloorPlanPreviewControl`

## Recommended Shape

The safest first shape is:

- one focused collaborator such as `ReviewCommandOrchestrator` or `FloorPlanReviewMutationCoordinator`
- one small result model for mutation outcomes when needed
- `FloorPlanReviewViewModel` methods become thin delegators

Directionally, the collaborator should own mutations like:

- reject candidate
- publish
- add pinch group / marker
- save / restore label text height
- save / restore moved artifact position
- save / restore dimension override
- save / restore curated artifact classification
- exclude curated artifact / remove overlays
- export adjusted DXF

## Why This Slice First

This is the best tradeoff between:

- **blast radius** — lower than selection extraction
- **architectural payoff** — higher than queue-only cleanup
- **testability** — strong existing ViewModel tests already exercise these mutations
- **readability gains** — high, because it removes the biggest procedural block from the ViewModel

## Acceptance Criteria

- `FloorPlanReviewViewModel` no longer owns the bulk of repeated mutation ceremony inline
- repeated `scopeFactory.CreateScope()` / `GetRequiredService<Handler>()` / `RefreshSessionAsync(...)` patterns are materially reduced from the ViewModel
- existing behavior and selection persistence semantics remain unchanged
- current ViewModel tests stay green
- the next slice after Loop 3A can focus on **selection state** with a smaller and safer ViewModel
