---
type: Decision
date: 2026-06-01
project: floorplan-fit
status: active
tags:
  - floorplan-fit
  - ux
  - debugging
---

# Do not change preview visual language as a proxy fix

## Decision

When the user reports a missing preview affordance, do not change colors/visual styling unless the verified root cause is visual styling or the user explicitly asks for a visual treatment change.

## Reason

On 2026-06-01, a Height handle issue was first treated as a possible contrast problem. The user clarified that this was not requested and the real symptom was axis behavior: right/left handles appeared while top/bottom did not. The color change was reverted.

## Current rule

For Fit preview handle bugs, verify the state driver first:

1. `SelectedPinchAxis`
2. `SelectedMeasurementCorridorAxis`
3. `SelectedPinchGroupId`
4. selected group markers for the active axis
5. `IsPinchPlacementArmed`
6. renderer output

Only then touch palette/styling.
