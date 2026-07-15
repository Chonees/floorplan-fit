---
type: bug
status: read-only-rca
project: floorplan adjustments-to site plan
area: Loop 2 / Infrastructure / DXF registration
date: 2026-07-14
replaces:
  - "[[2026-07-14 - Connected frame pair registration repair]]"
code_refs:
  - src/FloorplanFit.Infrastructure/Geometry/DominantAxisAlignedOutlineSelector.cs
  - src/FloorplanFit.Infrastructure/Dxf/DxfElectricalFloorRegistrationEstimator.cs
  - tests/FloorplanFit.Infrastructure.Tests/Dxf/DxfElectricalFloorRegistrationEstimatorTests.cs
---

# Connected frame evidence is local rather than whole-plan

## RCA

The connected-frame repair fails closed but overproduces locally valid transforms. `SelectConnectedFrames` emits every four-corner rectangle assembled from supported axis-run pairs. The estimator then clips both drawings to each candidate rectangle and calls a candidate evidence-complete when the clipped interiors satisfy two-axis coverage and residual thresholds. There is no minimum absolute support and no whole-plan coverage gate, so small repeated room/grid frames can prove incompatible local transforms.

Exact/tolerance-equivalent transforms are already grouped for status. The diagnostic summary is misleading because it renders the raw evidence-complete evaluations rather than the unique groups.

## Read-only SEMINOLE evidence

- Candidate count arithmetic is exact: `365 * 1,033 = 377,045` frame pairs.
- `4,292` is the count surviving uniform-scale dimension pruning, not necessarily the evidence-complete count.
- A read-only audit of axis-aligned structural LINE entities at transform `scale=1, rotation=0, translation=(30.479852,63.802324)` measured canonical-to-Electrical coverage `H=0.989685`, `V=0.977621`, combined RMS `0.000675`, maximum residual `0.008460`.
- Reverse coverage was `H=0.568323`, `V=0.679039`; strict whole-sheet bidirectional coverage at `0.75` would therefore reject the apparent true transform because Electrical carries extra structural runs.
- Uncertainty: this metric audit intentionally covered structural LINE entities only; the source estimator also accepts straight LWPOLYLINE edges and finite planar face edges. The large margin above the canonical-direction threshold is strong but not executable proof.

## Recommended finite correction

1. Keep connected frames as hypothesis generators and preserve the current uniform-scale prune.
2. Group evidence-complete hypotheses by equivalent transform before final selection and summarize each group once with its support count.
3. For every unique group, transform all Electrical structural segments and evaluate whole-plan canonical-to-Electrical horizontal and vertical coverage plus residual using the existing metric primitives and unchanged `0.75` / `0.05` gates.
4. Authorize only if exactly one unique transform passes. Zero is insufficient; multiple remains ambiguous. Never select by vote count.

This is a Loop 2 Infrastructure correction. The selector need not change; the minimal source/test scope is the estimator and its focused test file.

## Safety state

The current runtime screenshot is correct fail-closed behavior. `Ambiguous` returns no transform, so Application cannot persist a `PendingConfirmation` registration and Desktop has nothing safe to Confirm.
