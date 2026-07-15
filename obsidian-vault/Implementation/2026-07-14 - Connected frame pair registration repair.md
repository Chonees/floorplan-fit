---
type: implementation
status: superseded-by-runtime-rca
project: floorplan adjustments-to site plan
area: Loop 2 / Infrastructure / DXF registration
date: 2026-07-14
replaces:
  - "[[2026-07-14 - Dominant selector ranks disconnected axis pairs]]"
replaced_by:
  - "[[2026-07-14 - Connected frame evidence is local rather than whole-plan]]"
code_refs:
  - src/FloorplanFit.Infrastructure/Geometry/DominantAxisAlignedOutlineSelector.cs
  - src/FloorplanFit.Infrastructure/Dxf/DxfElectricalFloorRegistrationEstimator.cs
  - tests/FloorplanFit.Infrastructure.Tests/Dxf/DxfElectricalFloorRegistrationEstimatorTests.cs
---

# Connected frame pair registration repair

## What changed

- The selector exposes all connected four-corner candidates through its internal API while retaining public `Select(...)` compatibility for exporter and audit callers.
- Registration evaluates every canonical/electrical connected-frame pair. Each quarter turn first passes the existing uniform-scale dimension fit; only survivors incur interior clipping, coverage, and residual work.
- Accepted evidence-complete candidates are grouped by tolerance-equivalent scale, rotation, and translation. One group estimates, no groups is insufficient evidence, and multiple groups is ambiguous.

## Guardrails retained

- Coordinate tolerance remains `0.05`.
- No raw-sheet bbox fallback, global scale, SEMINOLE branch, or relaxation of numeric fail-closed behavior was added.
- Registration still requires both interior orientations and retains the existing confidence calculation from bidirectional coverage.

## Static validation

- Focused tests include lower-ranked compatible pair success and competing compatible-pair ambiguity.
- `git diff --check` and source-level symbol/signature inspection passed.
- No .NET build, test, restore, watch, Desktop, or runtime command ran.

## Runtime handoff

1. Run the focused Infrastructure estimator test class in CI or locally.
2. Build the application and retry Electrical `Register` for SEMINOLE.
3. Confirm that exactly-one evidence produces `PendingConfirmation`; ambiguous or insufficient evidence remains manual review.
