---
type: bugfix
date: 2026-07-14
status: implemented
---

# Nonplanar structural 3DFACE blocked Electrical registration

## Actual-input policy completion
Read-only analysis of the original SEMINOLE DXFs found two finite, in-range, nonplanar diagonal `3DFACE` entities unique to `ELECTRICAL WALLS`. Their XY traces provide no horizontal or vertical wall-run evidence.

## Fix and safety boundary
They are ignored only after finite and decimal-range validation succeeds; no projection, flattening, or face-edge extraction occurs. Planar `3DFACE` behavior remains intact. `SOLID`, unknown types, corrupt values, and out-of-range coordinates remain fail-closed.

## Current proof state
All observed original structural classes are now classified. External runtime proof is still required: let watch rebuild and retry **Register**.
