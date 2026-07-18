---
type: bug
status: implemented-static-runtime-pending
date: 2026-07-17
updated: 2026-07-17
project: FloorplanFit
area: Loop 1 Desktop
source_of_truth: code
code_refs:
  - src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs
  - tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs
---

# Pinch selection mixed marker identity with group highlight

## Symptom

- Navigating between pinch markers can leave the capacity panel visually stale.
- The preview colors multiple markers when the user intends to edit exactly one.
- This makes a single-marker persistence update look like several markers changed.

## Verified cause

- Persistence is already scoped to one `PinchMarkerId`: SQLite updates `WHERE id = $id` and requires exactly one affected row.
- The preview has group/axis highlighting but no selected-marker identity or pinch-marker hit test.
- `SelectedPinchGroupMarkers` is rebuilt as a new array and re-notified during marker selection, causing avoidable ListBox source churn.
- Save reads the current selection instead of enforcing the marker ID captured when editing began, so stale UI selection could target the wrong single marker.

## Required correction

1. Keep group identity for compression semantics, but introduce a separate selected marker ID for highlight and click selection.
2. Highlight only the exact selected marker; render every other pinch neutrally.
3. Stop invalidating the marker collection during a marker-only selection change.
4. Cancel or reject an edit if selection identity changes; save by the captured marker ID.

## Evidence boundary

Diagnosis and implementation are source-verified. Runtime verification remains external because repository rules prohibit running the Desktop app or .NET commands from the agent.

## Implemented correction

- The ViewModel exposes the exact selected `PinchMarkerId`; marker-only navigation no longer invalidates the ListBox item source.
- Changing marker cancels an in-progress draft, and save refuses to write if the selected identity differs from the edit-start identity.
- The preview receives the exact marker ID, renders only that marker active, and keeps every other marker neutral.
- Pinch hit testing runs before generic geometry hit testing and selects the marker by ID.
- Session refresh restores the exact marker ID, while group/axis state remains available for compression semantics.

## P2 follow-up correction

- A focused regression starts on a Width group, defines Height groups H1/H2, selects a marker owned by H2, and requires both the exact marker ID and H2 group ID to survive.
- Root cause: `ApplySelectionPresentationPlan` applied the target axis before the target group, so the reactive axis callback temporarily selected H1 and cleared the H2 marker identity.
- Minimal correction: apply `SelectedPinchGroup` before `SelectedPinchAxis`; the group callback establishes the matching axis without introducing the wrong intermediate group.
- Static verification: scoped `git diff --check` passes. No .NET build, test, restore, watch, or Desktop process was run by request.
