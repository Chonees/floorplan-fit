---
type: experiment
date: 2026-07-15
status: mixed
---

# Export 2 Floor/Electrical footprint comparison

## Inputs

- Canonical FloorPlan: `D:\PointAIData\test adjust\2.dxf`
- ElectricalPlan: `D:\PointAIData\test adjust\2-plan-set\ELECTRICAL PLAN SEMINOLE 2000-15-465bd2f9709548fb9d54bdc2ec3b972b.dxf`
- Manifest: `C:\Users\lucas\AppData\Local\FloorplanFit\workspace\exports\plan-sets\46654fc797374f349f606d641ffb0c58\manifest.json`

## Result

The automated audit reported a mismatch, but the user subsequently overlaid both exported DXFs in AutoCAD and reported a perfect visual match. The automated result is therefore a **false negative until the footprint selector is corrected**.

- Raw Floor structural bounds: `483.785585957492" × 928.800286462513"`.
- Raw Electrical structural bounds: `470.000000000116" × 928.800286462513"`.
- Raw width mismatch: Electrical is `13.785585957376"` narrower.
- Raw height mismatch: approximately `0"` (`1.41e-13"` numeric noise).
- Native origins also differ: Floor minimum `(78.788603, 95.978754)` versus Electrical `(23.000000, 23.999857)`.
- Manifest verification: `Blocked` by `SegmentCongruenceMismatch` and `FinalOutputCongruenceMismatch`.

Independent `ezdxf` extraction confirmed the raw bounds from both exported files; this is not only a manifest-status inference.

## Manual CAD verification

- User overlaid the exported FloorPlan and ElectricalPlan in AutoCAD.
- Reported result: the plans match perfectly.
- The audit's selected "dominant" bounds (`136" × 6"` and `186" × 4"`) are plainly local/auxiliary wall runs rather than complete house footprints.

## Consequence

The new proof pipeline successfully generated an Electrical DXF that passes manual AutoCAD overlay. The remaining blocker is observability: final-output and segment-congruence audits select non-footprint structural runs and incorrectly block a visually correct package.
