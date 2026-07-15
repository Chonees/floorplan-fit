---
status: superseded
replaces: "[[2026-07-13 - Final output runtime proof blocked on fresh export]]"
replaced_by: "[[2026-07-13 - False two-inch Electrical outline normalization]]"
---

# Fresh final output congruence proof passed

> Superseded: the green result compared a short-fragment Electrical fringe envelope rather than the dominant exterior wall pair. See [[2026-07-13 - False two-inch Electrical outline normalization]].

## Runtime evidence
- Manifest: `b4e8192e70a146f3910a4fc77988b028/manifest.json`
- Status: `ReadyForExport`
- Electrical: `ProjectedAutomatically`
- Final comparison: `FinalExportedSupportedStructuralFootprint`
- FloorPlan footprint: `464.40 x 930.00`
- ElectricalPlan footprint: `464.40 x 930.00`
- Width mismatch: `0.00`
- Height mismatch: `0.00`
- Structural segments: `SegmentCongruent`, missing `0`, extra `0`
- Verifier exit code: `0` with `-RequireAutomatic`

## End-to-end size proof
- Canonical FloorPlan source: `468.00 x 930.00` inches (`39.0 x 77.5` feet).
- Raw ElectricalPlan source: `470.00 x 930.000286` inches.
- ElectricalPlan was normalized to the canonical width `468.00` before applying the recipe (`scaleX = 0.9957446808508176`).
- User request: `464.40 x 930.00` inches (`38.7 x 77.5` feet).
- Required reduction: width `3.60` inches; height `0.00` inches.
- FloorPlan applied `4/4` horizontal operations of `0.90` inches.
- ElectricalPlan applied the same `4/4` canonical operations after normalization.
- Final supported footprints: both `464.40 x 930.00` inches.

This fresh case proves width reduction end to end. It does not prove vertical compression because no height reduction was requested.

## Raw-bounds advisory
Raw visible width mismatch remains `-15.7855859574919502` because the FloorPlan `WALLS` layer contains low-support tail geometry. It is reported for transparency but is not the comparable structural footprint.

## Overlay semantics
- This proof is translation-invariant: it proves equal supported footprint size/coverage after registration, not literal overlap at each file's native coordinates.
- Final supported FloorPlan bounds are `94.55, 95.40 -> 558.95, 1025.40`; Electrical bounds are `-5.50, -39.80 -> 458.90, 890.20`.
- A native-coordinate overlay therefore needs Electrical translated by `+100.05` in X and `+135.20` in Y.
- It also does not claim entity-for-entity equality: FloorPlan and Electrical remain different sheets. The shared structural footprint is the invariant.

## Conclusion
The fresh final FloorPlan DXF and final ElectricalPlan DXF preserve the same supported structural footprint at 1:1 scale for this case. The stale TEST A result is no longer accepted as runtime proof.
