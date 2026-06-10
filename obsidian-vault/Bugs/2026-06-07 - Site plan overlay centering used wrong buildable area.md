---
type: Bug
date: 2026-06-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - centering
  - setback
---

# Site plan overlay centering used wrong buildable area

## Symptom

User visual QA showed the published floor plan was not centered inside the orange setback rectangle in **Adjust to Site Plan** preview.

## Root cause

`IxMiliaSitePlanPreviewReader.ResolveBuildableArea(...)` still used the first Step 1 heuristic: choose the second-largest closed geometry bounds from the whole site plan. After the reader was expanded to preserve full CAD content, that heuristic became unstable.

The actual setback/buildable region in `PLANS/originalsSitePlans/158 DAWSON STREET.dxf` is represented by setback render geometry marked with `IsSetback`, not by a single closed setback polyline.

## Fix

Buildable-area detection now:

1. Computes the union bounds of all `SitePlanRenderPathDto` paths where `IsSetback = true`.
2. Uses those bounds as the buildable area when they have positive area.
3. Falls back to the older closed-shape heuristic only when no usable setback geometry exists.

## Evidence

- RED: `ReadAsync_uses_setback_geometry_bounds_as_buildable_area` failed with `Expected MinX: 32.3656860690203` and `Actual MinX: 44.7736704268132`.
- GREEN: the same test passed after the reader used setback geometry bounds.
- Focused reader tests passed 3/3.
- Full Infrastructure tests passed 87/87 after rerun.
- Desktop tests passed 208/208.

## Follow-up

If visual QA still shows offset after this correction, the next debug target is the floor-plan overlay bounding box basis, not the site-plan setback detection.

