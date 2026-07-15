---
type: bugfix
date: 2026-07-14
status: implemented
---

# Structural ELLIPSE blocks Electrical registration

## Evidence
Read-only metadata from the real binary DXFs established the source paths and showed that `ELLIPSE` is the only remaining observed unsupported structural curve after `ARC` and `CIRCLE`. The Electrical source contains no structural `SPLINE` or `INSERT` entities.

## Fix and safety boundary
`ELLIPSE` is ignored as non-straight evidence in the dominant wall estimator. Unknown entity types, corrupt geometry, and insufficient straight structural evidence remain fail-closed.

## Retry flow
Let `dotnet watch` rebuild, then use **Register**. **Confirm** appears only after registration state is successfully persisted.
