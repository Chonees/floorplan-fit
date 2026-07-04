# Anti-loop prompt for canonical adjustment recipe

## Purpose
Create a finite, phase-gated goal for Floorplan Fit that investigates real CAD workflows and applies them to the app macro/micro architecture without looping indefinitely.

## Core stop rule
Stop at the first demonstrable route:
FloorPlan adjustment -> AdjustmentRecipe saved/derived -> dependent projection consumes recipe -> export report says applied vs review.

## Anti-loop rule
Max 3 failed attempts per same symptom. After 3 failures, stop technical work, report evidence, root cause hypotheses, and propose an architecture/design change instead of continuing patches.

## CAD model basis
- Global placement maps to ALIGN/MOVE/ROTATE/reference SCALE.
- Local plan changes map to STRETCH/compression zones, not whole-sheet rescale.
- Dependent sheets register to the canonical FloorPlan and replay recipe operations with per-sheet entity policies.

## Phase artifacts
1. Research note: CAD workflow + current code map.
2. Design: AdjustmentRecipe, anchors, projection, entity policy.
3. Tasks: smallest implementable slice with acceptance criteria.
4. Implementation: approved slice only.
5. Verification: real SEMINOLE floor/electrical package report.
