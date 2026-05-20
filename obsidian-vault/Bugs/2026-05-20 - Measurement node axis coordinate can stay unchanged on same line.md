# Measurement node axis coordinate can stay unchanged on same line

## What
When marking two measurement nodes on the same vertical geometry while the selected corridor is `Width`, both node cards can show the same `AxisCoordinate` even though they were clicked at different positions on the line.

## Evidence
- `FloorPlanReviewViewModel.AddMeasurementNodeFromPreviewAsync(...)` resolves the clicked `worldPoint` with `FloorPlanPreviewGeometry.GetPointAtRatio(path, positionRatio)`.
- It then stores `axisCoordinate` as `worldPoint.Y` only for `Height`; otherwise it stores `worldPoint.X`.
- The node still persists both real anchor coordinates (`AnchorX`, `AnchorY`) and `PositionRatio`.
- The Review UI labels `AxisCoordinate` as `Coordenada eje` and `PositionRatio` as `Posición sobre la línea`.

## Why it matters
This is expected for the current axis-aligned corridor model, but confusing in the UX: two points on one vertical wall have the same X, so a `Width` corridor shows the same axis coordinate. The different `PositionRatio` values prove the clicks are different points along the wall.

## Direction
Do not treat this as data loss. Treat it as a UX/model clarity issue:
- show `AnchorX`/`AnchorY` in the node card or debug detail,
- label the coordinate as `X del corridor` / `Y del corridor`,
- warn when the selected corridor axis is perpendicular to the clicked line and the resulting interval would be zero,
- for true line-local A-to-B measurement, add explicit line-local or angle-aware corridor semantics instead of reusing global Width/Height.

## 2026-05-20 update
Raw A/B dimension rebuilding has been restored, so repeated AxisCoordinate is now explicitly a UI/semantics issue, not a rebuild blocker. The actual A/B line uses GeometryPathId + PositionRatio to recover the clicked world points.
