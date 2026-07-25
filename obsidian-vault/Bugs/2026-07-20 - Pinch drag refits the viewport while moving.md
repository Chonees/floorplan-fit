---
type: bug
status: fixed_static_pending_runtime
date: 2026-07-20
project: FloorplanFit
area: Loop 1 interactive pinch preview
---

# Pinch drag refits the viewport while moving

## Verified symptom

The new local CAD-style preview moves, but it can look weak or jittery compared with the retired global preview.

## Causes

1. `OnPointerMoved(...)` obtains a new viewport on every pointer event.
2. `GetPreviewViewport(...)` fits that viewport from `BuildViewportGeometry(axisTag)`, which already contains the current deformed preview.
3. The drawing therefore rescales while it is being shortened, and the next pointer delta is converted with that changing scale. This visually masks part of the movement and can feel unstable.
4. A pinch marker's `MaxTrimMm` is a ceiling, not the amount automatically applied. Dragging requests a total trim in half-inch steps.
5. The paired-wall compiler uses the lower capacity of the two faces. Current SEMINOLE data has patio capacities `4"` and `1"`, so that station can safely move only `1"`; porch is `1"`/`1"`.
6. The four-marker Width group represents two stations with effective capacities `4"` and `1"`; one requested total is distributed between them rather than repeated per marker.

The retired coordinate-global preview could appear to move more because independent marker effects accumulated across the plan. That larger movement was the deformation bug, not desirable behavior.

## Minimal correction

- Capture the viewport when the edge handle is pressed and reuse it for pointer-to-world conversion and rendering until release.
- Keep the half-inch snap and one-total-delta group semantics.
- Surface the effective paired/group capacity in the UI so different marker ceilings such as `4"`/`1"` are not misleading.

## Implemented

- `FloorPlanPreviewControl.GetPreviewViewport(...)` now reuses the base viewport captured by the handle press for the entire active edge drag.
- The same frozen viewport drives both pointer-to-source-unit conversion and rendering, removing the geometry -> auto-fit -> scale -> next-delta feedback loop.
- The viewport is reused only while an edge drag is active and the axis still matches; inactive or stale-axis state falls back to normal viewport calculation.
- Focused source-level contracts cover Width and Height plus the inactive/stale-axis guards.
- No capacity, half-inch snapping, CAD stretch recipe, or DXF behavior changed.
- Static diff validation passes. Executable Desktop proof remains pending under repository policy.
