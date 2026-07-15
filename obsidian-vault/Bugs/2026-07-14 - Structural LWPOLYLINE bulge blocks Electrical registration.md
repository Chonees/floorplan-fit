---
type: bugfix
date: 2026-07-14
status: implemented
---

# Structural LWPOLYLINE bulge blocks Electrical registration

## Root cause
After `ARC` was accepted, retrying **Register** exposed a finite nonzero `LWPOLYLINE` bulge in the ElectricalPlan. The estimator rejected the whole polyline even though a bulge belongs only to its outgoing edge.

## Fix
The axis-aligned wall estimator now ignores only finite curved edges and retains zero-bulge straight edges from that same polyline. It does not flatten curves into invented straight walls. Nonfinite bulges and unknown geometry remain fail-closed.

## User impact
Retry **Register** after rebuilding. **Confirm** still appears only after registration is successfully persisted as `PendingConfirmation`.
