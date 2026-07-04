---
type: Bug
date: 2026-05-10
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - fixed-components
  - dxf
  - layers
---

# Missing sink symbols live on L1 so fixed-component extractor skips them

## What happened

The user inspected a missing sink symbol directly in AutoCAD. The selected sink geometry is not on `CABS-FLOORPLAN` or `FIXTURES`; it is on layer `L1` and is composed of `3DFACE` (3), `ELLIPSE` (1), `CIRCLE` (4), and `ARC` (3).

## Why this matters

`IxMiliaFixedPlanComponentExtractor` already supports `Dxf3DFace`, `DxfEllipse`, `DxfCircle`, and `DxfArc`, so entity support is not the blocker for this sink. The blocker is semantic gating: `DxfExtractionProfile.PointeHomes` only treats `CABS`, `CABS-FLOORPLAN`, and `FIXTURES` as fixed-component layers. Anything living only on `L1` is ignored before geometry extraction ever runs.

## Consequence

The preview cannot show this sink because the extractor never emits it as a fixed component at all. This likely explains other missing bath/kitchen symbols if they also live on `L1` or another currently unprofiled layer.

## Resolution status

Mitigated experimentally on 2026-05-10 by admitting `L1` as `Fixture` in `DxfExtractionProfile.PointeHomes`. After relaunching onto the updated binary and doing a clean reimport + extraction, the latest runtime extraction persisted `28` fixed components from `L1`, confirming that the earlier invisibility was caused by profile-level filtering.

## Remaining risk

Opening all of `L1` is intentionally broad and may add review noise. The next refinement is still to audit missing families (`TUB`, `SHWR`, `COOKTOP`, `OVEN`, `DISHWASHER`, `WASH/DRY`, etc.) and tighten extraction around the meaningful connected symbols rather than treating all `L1` geometry as equally important.
