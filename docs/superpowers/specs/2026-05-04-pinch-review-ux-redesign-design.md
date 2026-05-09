# Pinch Review UX Redesign Design

## Goal

Keep the current single-screen review flow, but make pinch authoring and compression preview obvious enough that a user can discover the workflow without verbal instructions.

## Problem

The current prototype technically supports pinch groups, pinch placement, and edge-drag compression preview, but the UX hides too much state:

- pinch placement mode is only visible through `StatusMessage`
- edge-drag affordance is invisible
- the required order of actions is unclear
- line selection, pinch placement, and preview compete for the same visual focus

## Chosen Approach

Keep the three-column layout, but redesign it as one explicit workflow:

1. **Lines** — select or reject extracted geometry
2. **Preview** — show the highlighted line, pinches, and visible compression handles
3. **Pinch Workflow** — create/select a group, place a pinch, preview the cut, manage existing pinches

## UX Decisions

### 1. One-screen workflow stays

We do not move to a wizard. The user asked to keep the current one-screen model, so we keep it and improve clarity instead of changing navigation.

### 2. Pinch placement becomes explicit

The right panel exposes a primary action button:

- `Place Pinch on Preview`
- toggles to `Cancel Pinch Placement` while armed

Placement no longer requires preselecting the exact same line before clicking in the preview. If a pinch group is active, the next click on a valid line in the preview places the pinch on that line.

### 3. Guidance becomes persistent

The ViewModel exposes explicit UI copy:

- `InteractionHint`
- `PreviewDragHint`
- `PinchPlacementButtonLabel`

This replaces the need for the user to infer the next action from transient status text.

### 4. Edge-drag affordance becomes visible

The preview renders visible handles / highlighted edge zones depending on the selected group axis:

- `Width` → left and right handles
- `Height` → top and bottom handles

When pinch placement mode is armed, edge dragging is temporarily disabled so placement and preview do not conflict.

## Architecture Impact

### Desktop

- `ReviewFloorPlanWindow.axaml`
  - clearer hierarchy and workflow copy
- `ReviewFloorPlanWindow.axaml.cs`
  - pinch action button becomes a toggle
- `FloorPlanReviewViewModel.cs`
  - explicit interaction state properties
  - simpler pinch placement behavior
- `FloorPlanPreviewControl.cs`
  - visible edge affordances
  - no edge-drag while placement is armed

## Testing Strategy

- layout test validates the clearer workflow labels and removal of the old ambiguous button copy
- ViewModel test validates the new interaction-state copy and the ability to arm placement with only an active group
- existing Desktop tests continue to cover preview geometry and session loading

## Tradeoffs

### Pros

- much lower cognitive load
- easier onboarding for first-time users
- no navigation churn
- preserves the prototype's core domain model

### Cons

- more ViewModel-derived UI state
- still not a full CAD-like authoring experience
- preview affordances remain custom-rendered, not native controls

## Recommendation

Implement the redesign now on top of the prototype branch. This is the smallest change that materially improves usability without reopening the whole model.
