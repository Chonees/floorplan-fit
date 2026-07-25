---
type: bug
status: fixed_static_pending_runtime
date: 2026-07-18
project: FloorplanFit
area: Loop 1 compression
source_of_truth: code
replaced_by: "[[2026-07-19 - Adopt CAD-style pinch stretch actions]]"
---

# Pinch compression is coordinate-global instead of topology-local

## Symptom

Reducing one selected double-line wall assembly can move, shorten, lengthen, or change the angle of unrelated geometry and entities on other layers.

## Verified cause

The preview and DXF export apply a piecewise coordinate threshold to every supported point. A point is shifted when its X or Y is beyond a pinch marker, without checking structural connectivity, wall membership, layer role, or a local deformation corridor.

The two faces of a double-line wall are also treated as independent additive markers. If a requested `2"` reduction is split across markers at X=100 and X=106, points between them move `1"` and points beyond both move `2"`; the represented wall thickness therefore changes from `6"` to `5"`.

## Required behavior

- Treat the two faces of one wall as one structural assembly, not two independent reductions.
- Reduce only the exact line selected by each pinch, centered on its marker, by the exact requested amount.
- Preserve every non-target entity byte-for-byte under the user's latest literal rule.
- Never transform geometry merely because its coordinate lies beyond a marker.
- Audit requested reduction, achieved reduction, target paths changed, and non-target paths unchanged.

## Open product invariant

Resolved on 2026-07-19: preserve a closed shrinking footprint. Only the selected span deforms; a connected downstream component may translate rigidly. Literal endpoint gaps are not accepted.

## Evidence

- `FloorPlanPreviewGeometry.CreatePreviewGeometry` maps every segment endpoint in every geometry path.
- `FloorPlanPreviewGeometry.TransformPoint` decides movement only from axis coordinate and marker thresholds; marker deltas accumulate.
- `IxMiliaAdjustedSitePlanExporter.ApplyCompression` patches every supported entity in `ENTITIES` and dimension blocks, with no topology or layer-scope gate.

## Boundary

The replacement is implemented statically through recipe `v2` for interactive handle preview, AutoFit-plan preview/application, canonical Floor DXF, and registered Electrical DXF. The coordinate-global overload remains only for historical `v1` compatibility and is no longer called by the interactive handle route. See [[2026-07-20 - Handle drag preview still uses legacy global compression]]. Runtime build, real DXF export, verifier, and AutoCAD overlay are still required externally.

Implementation: [[2026-07-19 - Finite goal for CAD-style pinch deformation]].
