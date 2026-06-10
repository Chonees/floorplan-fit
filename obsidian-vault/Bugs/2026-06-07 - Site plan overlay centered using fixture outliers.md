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
  - placement-bounds
---

# Site plan overlay centered using fixture outliers

## Symptom

After correcting setback buildable-area detection, user visual QA still showed the floor-plan overlay offset inside the setback rectangle.

## Root cause

The projector centered the overlay using the bbox of every `reviewViewModel.GeometryPaths` item. That collection includes structural walls/openings, but also fixed components and protected/detail geometry. Fixture/component paths can extend beyond the exterior wall shell and skew the placement center.

Current local SEMINOLE2000 evidence:

- All selected preview geometry bbox center: `X = 359.4108749017802`.
- Wall candidate geometry bbox center: `X = 320.6813958468742`.
- Difference: about `38.73` source inches, or about `3.23 ft` when projected into the foot-based site plan.

## Fix

`SitePlanAdjustmentPreviewProjector.Project(...)` now accepts optional placement geometry path ids. The Library `Adjust to Site Plan` flow passes wall candidate geometry ids, so centering uses the building structure instead of fixture/component outliers.

The preview still renders and transforms all geometry, labels, and dimensions.

## Verification

- RED: `Project_centers_floor_plan_by_structural_placement_geometry_not_fixture_outliers` failed because the projector did not expose placement geometry ids.
- GREEN: the same test passes after the projector uses wall placement ids.
- Projector tests passed 4/4.
- Site-plan reader tests passed 3/3.
- Desktop tests passed 209/209.
- `git diff --check` exited 0 with LF-to-CRLF warnings only.

## Follow-up

If visual QA still shows an offset, the next candidate is not fixture skew; inspect site-plan orientation/rotation and whether the setback geometry should produce an oriented placement rectangle rather than an axis-aligned bbox.
