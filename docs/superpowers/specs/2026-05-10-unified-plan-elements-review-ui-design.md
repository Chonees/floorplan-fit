# Unified Plan Elements Review UI Design

## Goal

Make the Loop 1 review screen feel intuitive and low-noise by unifying all curable floor-plan artifacts under one review model:

- everything starts **included by default**
- everything can be **excluded directly**
- the user should not need to mentally switch between separate left/right artifact panels or between `Reject` and `Remove`

## Problem

The current review UI mixes two different ideas:

1. **artifact location is split**
   - `Lines` lives on the left
   - `Rooms`, `Openings`, `Opening Labels`, `Fixed Elements`, and `Protected Details` live on the right
2. **artifact actions are split**
   - lines use `Reject`
   - other artifacts use `Remove`
   - room labels currently have no exclusion path at all

This creates unnecessary cognitive load in the exact workflow that should feel like fast technical curation.

## Chosen Approach

Keep the existing three-column window, but change the responsibility of each column:

1. **Plan Elements** (left)
   - all curable plan artifacts live here:
     - Lines
     - Rooms
     - Openings
     - Opening Labels
     - Fixed Elements
     - Protected Details
2. **Preview** (center)
   - the CAD-faithful canvas remains the main visual review surface
3. **Selected Item + Pinch Tools** (right)
   - selected artifact summary
   - one contextual action: `Exclude from Curation`
   - pinch groups / pinch markers workflow

## UX Decisions

### 1. One mental model: included by default

The screen should teach one consistent rule:

> extraction proposes, review excludes false positives

The user does not manually "accept" most items one by one. They review what is already in and remove what should not persist.

### 2. One user-facing action for artifacts

All artifact families use the same UI language:

- `Exclude from Curation`

The UI must stop exposing backend-specific verbs such as `Reject Selected Line` and `Remove Selected Opening`.

### 3. Artifact lists belong together

All plan artifacts belong in the same review zone because they are all part of the floor plan being curated.

The left panel becomes a single scrollable `Plan Elements` surface with compact grouped sections and counts. The right panel stops being a second artifact browser.

### 4. The right panel becomes an inspector, not a duplicate catalog

The right side should answer:

- what is selected?
- why is it here?
- do I keep it or exclude it?
- how do I continue pinch authoring?

It should not keep long side lists for every artifact family.

### 5. Low-noise density over verbose helper copy

The redesign should reduce visual noise by:

- removing repeated explanatory paragraphs from every artifact block
- favoring compact headers with counts
- showing details only for the current selection
- keeping pinch workflow visible but visually separated from artifact exclusion

## Curation Semantics

The user-facing action becomes uniform, but the persistence semantics may remain type-specific in this slice.

### Lines

`Exclude from Curation` maps to the existing **reject** behavior.

- lines stay auditable as rejected
- rejected lines disappear from the active review session
- associated pinch markers are still cleaned up

### Rooms

Room labels need a new exclusion path.

For this slice, the minimum viable behavior is:

- add a remove/exclude handler for room labels
- let the unified UI exclude room labels directly

This is enough to make the screen behavior consistent before a larger correction-backbone refactor exists.

### Openings, Opening Labels, Fixed Elements, Protected Details

`Exclude from Curation` can keep mapping to the current remove behavior internally for now.

That preserves fast implementation while fixing the UX language and interaction model immediately.

## Interaction Model

### Left panel

- grouped sections under `Plan Elements`
- each section shows a compact count
- clicking a row selects the artifact
- canvas selection must keep parity with list selection

### Center panel

- unchanged as the main CAD-faithful review surface
- continues to support direct selection from geometry/text where already implemented

### Right panel

- selected item title, type, source reference, and key metadata
- single artifact action button:
  - `Exclude from Curation`
- pinch groups
- pinch placement
- placed pinches

## Architecture Impact

### Product Loop + Layer

- **Loop 1**: floor plan curation
- **Desktop**: primary impact
- **Application**: new room-label exclusion use case
- **Infrastructure**: room-label repository exclusion support

### Desktop

- `ReviewFloorPlanWindow.axaml`
  - reorganize the layout into `Plan Elements | Preview | Inspector + Pinch`
  - remove artifact-specific action buttons from the left/right lists
- `ReviewFloorPlanWindow.axaml.cs`
  - route one unified exclude action
- `FloorPlanReviewViewModel.cs`
  - add one selected-artifact action path
  - support room-label selection as an actionable artifact
  - expose inspector-friendly selection metadata

### Application

- add a room-label exclusion handler
- keep existing wall reject behavior
- reuse existing remove handlers for the other artifact types

### Infrastructure

- extend `IExtractedRoomLabelRepository`
- add SQLite room-label removal/exclusion support

## Acceptance Criteria

1. The left panel is renamed to `Plan Elements` and contains all curable artifact families.
2. The right panel no longer contains long artifact-family lists for rooms/openings/labels/fixed/protected items.
3. Every artifact family can be excluded directly from the review UI, including room labels.
4. The artifact action language shown to the user is unified as `Exclude from Curation`.
5. Wall exclusion still preserves audit semantics internally through rejection, not destructive deletion.
6. Existing pinch authoring and preview workflow remains available on the right side.

## Testing Strategy

- Desktop layout test for the new panel composition and unified action label
- ViewModel tests for:
  - selected artifact metadata
  - unified exclude action routing
  - room-label exclusion
- Application/Infrastructure tests for the new room-label exclusion path
- regression coverage to ensure wall rejection still clears pinch markers

## Tradeoffs

### Pros

- much clearer review flow
- lower visual noise
- one consistent curation mental model
- easier to teach and demo

### Cons

- ViewModel selection state becomes more explicit
- room labels require new application/infrastructure support
- the backend still keeps mixed persistence semantics until a future correction backbone unifies them

## Recommendation

Implement this redesign as the next review-UI slice.

It gives the user the UX they asked for right now, without prematurely forcing a deeper cross-artifact correction backbone into the same change.

