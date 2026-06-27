---
type: Bugs
date: 2026-06-27
status: fixed
scope: Loop 1 measurement binding
related:
  - [[Current State]]
---

# Measurement corridor allowed more than two nodes

## Problem
Loop 1 curation could still arm measurement-node placement and persist a third node in the selected measurement corridor/franja.

## Product Rule
A measurement corridor/franja represents one interval, so it can have at most two nodes: start and end.

## Root Cause
The Desktop ViewModel only required a selected corridor before arming node placement, and `AddMeasurementNodeHandler` created the next node with `existing.Count + 1` without enforcing the max-two-node invariant.

## Fix
- Desktop now refuses to arm node placement when the selected franja already has two nodes.
- Desktop also re-checks before saving a preview click, then disarms and shows: `La franja ya tiene dos nodos. Elimin? uno antes de marcar otro.`
- Application handler now rejects any third node for the same corridor.

## Verification
- RED: `ToggleMeasurementNodePlacement_does_not_arm_when_the_selected_corridor_already_has_two_nodes` failed because placement armed.
- RED: `AddMeasurementNodeHandler_rejects_a_third_node_for_the_same_corridor` failed because no exception was thrown.
- GREEN: focused regression tests passed `2/2`.
- GREEN: `MeasurementBindingFloorPlanReviewViewModelTests` passed `22/22`.
- `git diff --check` passed.

## Files
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/AddMeasurementNodeHandler.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/MeasurementBindingFloorPlanReviewViewModelTests.cs`
