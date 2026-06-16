---
type: bug
project: floorplan-fit
date: 2026-06-11
topic_key: bugs/width-deficit-uses-non-footprint-geometry
status: fixed-verified
replaces: Bugs/2026-06-11 - Width setback examples used wall bbox instead of preview bbox.md
---

# 2026-06-11 - Width deficit still uses non-footprint geometry

## Current finding

The previous correction over-shifted the fit basis from `WALLS`-only to total preview geometry. That made the app report a width deficit such as `2"` when the visible architectural footprint appears to fit inside the orange setback.

## Evidence

The UI shows buildable X:

```txt
101 -> 660.245
```

That is `559.245"` wide. The app reports `ancho 2"` because the current code computes:

```txt
widthDeficit = floorPlanBBoxWidth - buildableWidth
```

after passing all projected preview geometry into `AutoFitSuggestionFactBuilder`.

## Root cause hypothesis

The correct basis is neither:

- raw `WALLS` bbox only; nor
- all preview geometry including annotations/cotas/pinches/fixtures/outliers.

The correct basis should be an explicit **fit footprint/building envelope**: exterior architectural footprint used for setback compliance, excluding measurement annotations, pinch markers, labels, and non-footprint geometry that can extend outside the house.

## Product truth

If the orange setback visually contains the actual building footprint, the UI must not show a width deficit only because non-footprint preview artifacts extend the bbox.

## Fix applied

Restored Loop 2 projection/facts to use `floorPlanPlacementGeometryPathIds` derived from accepted wall candidates:

- initial centering uses accepted wall candidate geometry;
- `AutoFitSuggestionFactBuilder` receives the same structural fit footprint geometry;
- visible preview can still render other geometry, but those artifacts do not define width deficit.

## Verification

- RED: `Build_uses_wall_candidate_footprint_not_total_preview_outliers_for_width_deficit` failed while total-preview geometry was used. The projected structural wall footprint landed at `70..170` instead of `100..200`.
- GREEN: the same test passed after restoring the wall-candidate footprint basis.
- Exported DXF parse verified:
  - Example 01: footprint `483.785586 x 930.000286`, setback `482.785586 x 930.000286`, deficit `1 x 0`, centers equal.
  - Example 02: footprint `483.785586 x 930.000286`, setback `481.785586 x 930.000286`, deficit `2 x 0`, centers equal.
  - Example 03: footprint `483.785586 x 930.000286`, setback `483.785586 x 929.000286`, deficit `0 x 1`, centers equal.
  - Example 04: footprint `483.785586 x 930.000286`, setback `483.785586 x 928.000286`, deficit `0 x 2`, centers equal.
