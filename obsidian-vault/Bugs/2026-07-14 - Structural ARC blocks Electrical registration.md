---
type: bugfix
date: 2026-07-14
status: implemented
---

# Structural ARC blocks Electrical registration

## Root cause
`DxfElectricalFloorRegistrationEstimator` treated every unknown DXF entity on a structural layer as fatal. `ARC` entities therefore stopped both the canonical FloorPlan and ElectricalPlan before registration could be saved, leaving the UI correctly without a Confirm action.

## Fix
ARC is ignored only by the axis-aligned dominant-wall estimator. It contributes no structural-wall evidence; valid LINE/polyline/face evidence still determines registration. Unknown entity types and invalid supported geometry remain fail-closed. A drawing containing only arcs remains insufficient evidence.

## Verification
- Focused regression: matching canonical and electrical drawings with structural-layer ARCs plus sufficient straight walls require an identity registration.
- Static checks only; user must run the external rebuild and click **Register**.
