# 2026-06-22 - SitePlanDawson unit context is site-plan feet not floor-plan inches

> Correction / scope note: this note describes the accidentally imported `SitePlanDawson.dxf` row in the floor-plan library DB, not the real Dawson/site-plan picker library the user meant. For the active synthetic-site-plan issue, see `Bugs/2026-06-22 - Synthetic site plans are Seminole-calibrated.md`.

## Type
Discovery / Data-unit gotcha

## Context
The user questioned why Dawson/Santa Barbara appear to fit a site plan and whether the `12:1` conversion is backwards or should apply everywhere.

## Verified evidence
- `SANTA-BARBARA.dxf` has `$INSUNITS = 1` and is imported as `Inch` (`to_millimeters_factor = 25.4`).
- `SEMINOLE2000.dxf` has `$INSUNITS = 1` and is imported as `Inch`.
- `SitePlanDawson.dxf` has `$INSUNITS = 2` and is imported/read as `Foot` (`to_millimeters_factor = 304.8`).
- Dawson setback/buildable bounds from `SETBACKS` are about `63.814 x 78.319` site units, i.e. feet.
- Current non-rejected floor-plan structural footprints:
  - Santa Barbara: about `38.0 x 65.6 ft`.
  - Seminole: about `39.0 x 77.5 ft`.
- Therefore these floor plans can fit Dawson's buildable area after converting floor inches to site feet.

## Root cause / explanation
The `1/12` scale is correct when projecting a floor plan whose source coordinates are inches into a site plan whose source coordinates are feet:

`floorFactor / siteFactor = 25.4 / 304.8 = 1/12`.

The reverse (`12`) is only correct when converting site feet back into floor-plan inches.

## Data gotcha
`SitePlanDawson.dxf` also contains embedded floor-plan-looking geometry/dimensions. If it is imported as a floor plan, the app treats those coordinates as feet because the file header says feet; e.g. a dimension displayed as `66'-0"` has `792` source units, which becomes `792 ft` instead of `66 ft`. That is a document-context issue, not a curation rule.

## Implication
Do not curate `SitePlanDawson.dxf` as a floor plan. Use it as a site plan. If the product must support mixed-unit files with embedded inch floor-plan blocks inside foot site-plan drawings, it needs an explicit import/unit override or a site-plan-only import path.
