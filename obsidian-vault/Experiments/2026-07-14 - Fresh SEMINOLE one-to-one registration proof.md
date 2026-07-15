---
type: Experiments
status: pending
date: 2026-07-14
---

# Fresh SEMINOLE one-to-one registration proof

## Purpose

Prove with a newly computed registration that the curated canonical FloorPlan and dependent ElectricalPlan begin at structural scale `1:1`, receive the same canonical adjustment, and export with coincident dominant structural walls.

## Procedure

1. Select the curated SEMINOLE FloorPlan version in Library.
2. Unlink only the existing ElectricalPlan using the cross on its Plan Set Sheets row.
3. Use **Import Electrical** and choose the original SEMINOLE Electrical DXF. Do not reimport the FloorPlan.
4. On the new ElectricalPlan row, click **Register**. The estimator must compute the transform; no manual scale/translation dialog is valid for Electrical.
5. Continue only if registration becomes `PendingConfirmation` and does not require manual review. Confirm it with **Confirm**.
6. Run **Adjust to Site Plan** on the selected curated FloorPlan, apply a known width-only adjustment, and export to a new filename.
7. Confirm that the canonical Floor DXF and sibling `<name>-plan-set` folder were created, with the Electrical DXF inside.
8. Run `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.
9. In AutoCAD, copy the exported Electrical geometry and use `PASTEORIG` into the exported Floor drawing. Compare dominant exterior walls, not title blocks or fringe entities.

## Pass criteria

- Initial dominant footprints are both `468 x 930` inches and registration scale is exactly `1` after tolerance snapping.
- For a `39' -> 38.7'` width-only case, both exported dominant widths are `464.4` inches.
- The verifier reports automatic projection and native final structural congruence with zero width/height mismatch within tolerance.
- The four dominant exterior walls overlap in AutoCAD at original coordinates.

## Stop conditions

- `RequiresManualReview` or missing registration transform.
- A scale different from `1` for the known SEMINOLE originals.
- Missing plan-set folder/Electrical output.
- Verifier failure or any dominant-wall mismatch in AutoCAD.

