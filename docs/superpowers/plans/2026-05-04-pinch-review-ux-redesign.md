# Pinch Review UX Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the single-screen pinch review workflow visually obvious and reduce the hidden-state friction in pinch placement and preview.

**Architecture:** Keep the existing review window and domain model, but push more explicit UX state into the Desktop ViewModel and add visible affordances in the preview control. Do not change the persistence model.

**Tech Stack:** Avalonia UI, MVVM, xUnit

---

### Task 1: Add UX-state regression tests

**Files:**
- Modify: `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
- Modify: `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

- [ ] Add layout expectations for the clearer one-screen workflow labels
- [ ] Add ViewModel expectations for explicit interaction copy and placement-toggle behavior
- [ ] Run the targeted Desktop tests and confirm they fail for the expected missing UX state

### Task 2: Refactor ViewModel interaction state

**Files:**
- Modify: `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`

- [ ] Add explicit UX-facing properties like `InteractionHint`, `PreviewDragHint`, and `PinchPlacementButtonLabel`
- [ ] Allow pinch placement mode to arm from the selected group without requiring a preselected line
- [ ] Update preview-click handling so armed placement uses the clicked line directly
- [ ] Keep refresh and selection behavior coherent after pinch placement/removal

### Task 3: Redesign the review window layout

**Files:**
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml`
- Modify: `src/FloorplanFit.Desktop/ReviewFloorPlanWindow.axaml.cs`

- [ ] Reframe the three-column layout as `1. Lines`, `2. Preview`, `3. Pinch Workflow`
- [ ] Replace the ambiguous arm button with a primary placement-toggle button
- [ ] Surface the new hints and current selection summaries directly in the UI
- [ ] Keep publish and reject actions accessible but less visually dominant than the workflow

### Task 4: Improve preview affordances

**Files:**
- Modify: `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`

- [ ] Disable edge-drag activation while pinch placement mode is armed
- [ ] Render visible handles/highlight zones for the active width/height edges
- [ ] Preserve existing pinch rendering and preview deformation logic

### Task 5: Verify Desktop behavior

**Files:**
- Verify only

- [ ] Run targeted Desktop tests for layout + ViewModel state
- [ ] Run the full Desktop test project
- [ ] Summarize the new manual UI flow in the final response
