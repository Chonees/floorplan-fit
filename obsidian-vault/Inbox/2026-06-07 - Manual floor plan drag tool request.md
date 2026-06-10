---
type: Inbox
date: 2026-06-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - manual-adjustment
---

# Manual floor plan drag tool request

## User intent

User confirmed automatic centering improved the overlay but is not precise enough. Desired next tool: allow dragging/moving the **floor plan overlay only** while keeping the **site plan fixed**.

## Current scale behavior

The overlay is not scale-to-fit. The site plan is rendered in its DXF source coordinates, and the floor plan is converted into the site-plan unit system with:

`scale = floorPlanMeasurementContext.ToMillimetersFactor / sitePlan.ToMillimetersFactor`

Examples:

- Floor inches over site feet: `25.4 / 304.8 = 1/12`, so 12 floor inches become 1 site foot.
- Same unit on both files: scale is `1`.

After scaling, the projector applies only translation offsets.

## Likely next design

Add a preview-only “Move floor plan” tool in `SitePlanAdjustmentWindow` that captures pointer drag deltas and applies them to the transformed floor-plan geometry/labels/dimensions. The site-plan render paths/text remain unchanged.
