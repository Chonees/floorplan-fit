# Looped Architecture Modularization Design

## Goal

Refactor the active Floorplan Fit working tree into a cleaner, more legible, more professional architecture by executing modularization in **ordered loops** instead of a single big-bang rewrite.

The specific goals are:

- keep the codebase understandable file-by-file
- move toward **one clear responsibility per file**
- reduce architectural drift between docs and code
- isolate visual-system concerns from preview behavior
- isolate preview composition from preview interaction
- isolate review orchestration from feature-specific state

## Problem

The repo has already improved in important ways, especially in `src/FloorplanFit.Desktop/Controls/Preview/*`, but the current architecture is still uneven.

Verified issues:

1. `FloorPlanPreviewControl.cs` is still too large and responsibility-dense.
2. `FloorPlanReviewViewModel.cs` is still too large and mixes orchestration with feature state.
3. the visual system is only partially centralized:
   - some brushes are tokenized in `App.axaml`
   - multiple hex values are still inlined in styles
   - preview rendering still has localized color decisions
4. the architecture map is behind the real codebase after the native-dimension milestone
5. if refactoring starts directly from the largest files without first re-establishing canonical truth, the repo risks becoming cleaner in code while becoming less trustworthy in docs

This is exactly the kind of problem that gets worse when treated as "just split the big files."

## Chosen Approach

Refactor in **vertical loops ordered by architectural risk**, not by convenience.

Each loop must leave the codebase in a valid, more readable state, with its own documentation and verification.

The loops are:

1. **Loop 0 - Canonical architecture truth**
2. **Loop 1 - Shared visual system**
3. **Loop 2 - Preview composition and tooling**
4. **Loop 3 - Review orchestration decomposition**
5. **Loop 4 - Cleanup, naming, ownership, and final drift pass**

This keeps the refactor professional and auditable:

- documentation stays aligned
- code moves in bounded slices
- each loop has a clear product and architecture purpose
- later loops build on stabilized earlier boundaries

## Core Design Principle

Do **not** modularize by chopping files randomly.

Modularize by answering this question for every extracted unit:

> what single responsibility does this file own, and can a future engineer understand that without reading three other files first?

That means:

- one visual token file should define shared visual language
- one renderer should render one family or one rendering concern
- one interaction tool should own one interaction concern
- one view-model helper should own one state or command cluster

If a file becomes a "miscellaneous coordination drawer," the modularization failed.

## Product Loop + Architecture Layer

### Product scope

This work belongs to **Loop 1 shared foundation**, not to Loop 2 fit behavior.

It improves the operability and maintainability of the CAD-faithful floor-plan curation surface that Loop 1 already depends on.

### Architecture layers impacted

- **Desktop** - primary impact
- **Contracts** - secondary impact where DTO ownership or view-facing grouping needs clarification
- **Application** - only where orchestration boundaries must be clarified for Desktop consumption
- **Documentation / repository truth** - mandatory supporting layer

## Loop Design

## Loop 0 - Canonical architecture truth

### Goal

Make the repository truthful before deeper refactoring starts.

### Scope

- refresh the canonical architecture map
- update it for the native-dimension milestone
- document the current ownership of:
  - visual system
  - preview shell
  - preview renderers
  - preview interaction helpers
  - dimension-editing helpers
  - review orchestration

### Why first

Without this loop, the repo would refactor on top of stale documentation that still claims dimensions are pending.

That is architectural dishonesty.

### Exit criteria

- map no longer claims native dimensions are pending
- new dimension files are represented
- hotspots and responsibilities are documented explicitly

## Loop 1 - Shared visual system

### Goal

Separate visual language from behavior.

### Scope

- centralize color tokens and semantic brushes
- reduce inline hex usage in `App.axaml`
- move preview color semantics behind shared palette abstractions where missing
- define a clear convention for iconography / action visuals if the current UI uses repeated inline visual decisions

### Principle

Behavior files should not quietly become the place where design language lives.

### Exit criteria

- visual tokens live in a small set of intentional files
- Desktop and preview stop inventing colors ad hoc
- repeated style intent is named semantically

## Loop 2 - Preview composition and tooling

### Goal

Turn `FloorPlanPreviewControl` into a true composition shell instead of a giant mixed-control brain.

### Scope

Separate the preview by concerns such as:

- viewport / camera math
- preview composition lifecycle
- hit-test coordination
- selection routing
- pinch interaction coordination
- native dimension interaction coordination
- preview render-layer orchestration

### Non-goal

This loop does not redesign Loop 1 product behavior. It restructures the same behavior into cleaner ownership boundaries.

### Exit criteria

- `FloorPlanPreviewControl` is materially smaller
- support logic moves into focused collaborators
- files under `Controls/Preview/*` or adjacent tooling folders have names that match their responsibility

## Loop 3 - Review orchestration decomposition

### Goal

Reduce `FloorPlanReviewViewModel` to orchestration and composition, not feature sprawl.

### Scope

Break responsibilities into focused units such as:

- selected artifact state
- artifact action routing
- pinch tools state
- dimension editing state
- publish workflow state
- display summary helpers

### Principle

A review VM should coordinate user flows, not hoard every rule and every state bucket itself.

### Exit criteria

- `FloorPlanReviewViewModel` is materially smaller
- commands and state clusters are easier to test independently
- feature-specific logic stops piling up in one class

## Loop 4 - Cleanup and final architecture pass

### Goal

Close the refactor cleanly instead of stopping at "it kind of works now."

### Scope

- naming cleanup
- folder placement cleanup
- eliminate leftover hybrid files
- refresh architecture docs again
- record ownership and next boundaries clearly

### Exit criteria

- no obvious architectural drift between code and docs
- naming is coherent
- future work can land without re-inflating the same hotspots immediately

## File Structure Direction

The target structure should be responsibility-first, not vanity-folder-first.

Examples of the intended direction:

- visual tokens / palettes in clearly named visual-system files
- renderers grouped by rendering concern
- interaction coordinators grouped by interaction concern
- state helpers grouped by review concern

The structure should help answer:

- where do colors live?
- where does hit-testing live?
- where does dimension editing live?
- where does pinch interaction live?
- where does artifact-action routing live?

If those answers still require reading the two largest files, the refactor is incomplete.

## Testing Strategy

This refactor must stay test-first where behavior changes, and regression-first where behavior is preserved.

### Loop 0

- documentation self-check
- no code verification beyond file integrity

### Loop 1

- Desktop tests for any behavior touched by visual-token extraction if bindings/styles move in a way that affects expectations
- targeted regression checks for palette-dependent preview rendering helpers if they change public behavior

### Loop 2

- preview-control regression tests
- hit-test / selection / dimension-interaction tests
- renderer orchestration tests where needed

### Loop 3

- ViewModel tests for selection state
- command routing tests
- publish / pinch / dimension workflow tests

### Loop 4

- fresh full targeted test pass over touched Desktop, Application, and Infrastructure slices
- documentation drift check against working tree

## Acceptance Criteria

1. The repository has an updated canonical architecture map aligned with the native-dimension reality.
2. Shared visual language is more centralized and less dependent on inline values.
3. `FloorPlanPreviewControl` is no longer the hidden home of multiple unrelated responsibilities.
4. `FloorPlanReviewViewModel` becomes primarily an orchestration surface instead of a responsibility sink.
5. The refactor leaves the codebase easier to navigate by responsibility, not just split into more files.
6. Documentation remains aligned after each architectural loop.

## Tradeoffs

### Pros

- safer than big-bang refactoring
- clearer review and rollback points
- documentation stays useful
- architecture becomes easier to teach and extend
- future Loop 1 and Loop 2 work lands on cleaner foundations

### Cons

- slower initial momentum because docs come first
- some intermediate loops improve structure more than visible user behavior
- requires discipline to avoid opportunistic unrelated cleanups

## Alternatives Considered

### 1. Big-bang file splitting

Rejected.

Why:

- too much risk
- easy to create fake modularity
- docs drift immediately

### 2. Only update docs and leave code alone

Rejected.

Why:

- truthful docs are necessary but not sufficient
- the code hotspots are real and will keep absorbing future complexity

### 3. Only split the two largest files

Rejected.

Why:

- treats symptoms before structure
- ignores visual-system drift
- ignores architecture-map drift

## Recommendation

Execute this refactor as a **looped architectural cleanup program**:

- docs first
- visual system second
- preview decomposition third
- review orchestration fourth
- cleanup and drift pass last

That is the most professional path for turning the current branch into a codebase that is legible, teachable, and ready for future CAD-faithful growth without re-collapsing into giant files.
