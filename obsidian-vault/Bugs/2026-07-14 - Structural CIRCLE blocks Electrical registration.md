---
type: bugfix
date: 2026-07-14
status: implemented
---

# Structural CIRCLE blocks Electrical registration

## Runtime sequence
After `ARC` and finite-bulged `LWPOLYLINE` were accepted, the next real ElectricalPlan entity was `CIRCLE` on a structural layer. It blocked extraction before a `PendingConfirmation` registration could be saved.

## Fix and safety boundary
`CIRCLE` is ignored only as non-straight evidence for the dominant wall estimator. It does not enable any other curve type and does not relax fail-closed handling of unknown entities, invalid geometry, or insufficient straight structural evidence.

## User action
Retry **Register** after `dotnet watch` recompiles. **Confirm** appears only after registration state is successfully saved.
