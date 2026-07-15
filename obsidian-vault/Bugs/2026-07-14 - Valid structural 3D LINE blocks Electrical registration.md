---
type: Bugs
status: resolved
date: 2026-07-14
---

# Valid structural 3D LINE blocks Electrical registration

## Symptom

Registering the freshly imported SEMINOLE Electrical sheet leaves it `Unregistered` and shows: `Canonical FloorPlan structural entity extraction failed closed: LINE is non-planar`.

## Diagnosis

No confirmation is available because no registration was created. The estimator's hardening incorrectly requires both Z coordinates of every structural `LINE` to match. A LINE remains a valid projected XY segment when its endpoint elevations differ; the unsafe non-planar rule is required for face-based geometry, not for a line.

## Required fix

Add a regression with a structural LINE whose endpoints have different Z but valid XY footprint. Extract its XY segment while retaining finite-coordinate checks and the stricter non-planar checks for SOLID/3DFACE.

## Implemented

`AddLine` now validates finite XYZ and convertible XY values but accepts different finite endpoint elevations. The regression requires an estimated scale-one registration for that case. SOLID and 3DFACE continue to use the existing planar-only extraction path.

## UI clarity

The Desktop status now says manual review is required and no registration was saved, instead of incorrectly implying that a Confirm action is available.
