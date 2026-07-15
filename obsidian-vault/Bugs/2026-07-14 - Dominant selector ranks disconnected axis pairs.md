---
type: bug
status: static-repair-pending-runtime
project: floorplan adjustments-to site plan
area: Loop 2 / Infrastructure / DXF registration
source_of_truth: read-only source and original SEMINOLE DXFs
code_refs:
  - src/FloorplanFit.Infrastructure/Geometry/DominantAxisAlignedOutlineSelector.cs
  - src/FloorplanFit.Infrastructure/Dxf/DxfElectricalFloorRegistrationEstimator.cs
  - src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs
  - src/FloorplanFit.Infrastructure/Storage/PlanSetOutlineSegmentCongruenceAuditBuilder.cs
updated: 2026-07-14
replaced_by: "[[2026-07-14 - Connected frame pair registration repair]]"
---

# Dominant selector ranks disconnected axis pairs

> Static repair recorded in [[2026-07-14 - Connected frame pair registration repair]]. Runtime/CI proof is still required before this issue can be closed.

## Confirmed root cause

`DominantAxisAlignedOutlineSelector` ranks vertical and horizontal parallel-edge pairs independently, then checks their four cross-axis corners. The SEMINOLE raw DXFs contain strong independent pairs that do not form the same rectangle, so the corner gate correctly fails closed but cannot choose an available connected frame.

## Input-backed evidence

- Floor: selected X `[94.5741888255331, 562.5741888255767]`, selected Y `[177.3787537985202, 929.3787537985603]`. The vertical-pair shared Y support starts at `193.3787537984329`, so it misses MinY; the horizontal-pair shared X support starts at `365.4990400068387`, so it misses MinX. Relevant source layer: `WALLS`.
- Electrical: selected X `[64.0943369432345, 70.0943369431909]`, selected Y `[859.5764294379516, 865.5764294379516]`. The vertical-pair shared support ends at `859.5764294379698`, so it misses MaxY; the horizontal-pair shared support begins at `70.0943369432418`, so it misses MinX. Relevant source layer: `ELECTRICAL WALLS`.
- Read-only reconstruction found connected H/V frame candidates in both originals (365 Floor, 1033 Electrical). This proves the immediate failure is pair selection, not a lack of extracted straight evidence; it does not by itself prove a unique registration frame.

## Recommended generic correction

Build and retain connected four-corner `FrameCandidate`s from the Cartesian product of vertical and horizontal pair candidates before ranking. In the registration estimator, compare Floor/Electrical frame candidates per quarter turn; require uniform-scale compatibility and the existing two-axis interior coverage/residual proof. Pick exactly one evidence-complete candidate; otherwise retain `Ambiguous` or `InsufficientEvidence`.

Do not loosen the `0.05` tolerance, accept a three-corner shape, use a raw sheet bbox, or add a SEMINOLE exception.

## Implementation tests

- Independent axis winners disconnected but a lower-ranked connected frame exists.
- No complete frame, three-corner frame, tolerance boundary, and tied complete frames all fail closed appropriately.
- Different Floor/Electrical footprints with one uniquely evidenced connected-frame similarity succeeds; unrelated frames, anisotropic scale, and multiple valid frame/rotation matches do not auto-register.
- Exporter and final-output audit retain manual review when a connected/expected frame cannot be uniquely evidenced.
