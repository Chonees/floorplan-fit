---
type: Bugs
date: 2026-07-12
status: open
replaces:
  - "2026-07-12 - TEST A width-only HousePlanSet audit"
---

# TEST A verifier missed exported FloorPlan/Electrical visual mismatch

## What happened
The user overlaid the exported ElectricalPlan on the exported FloorPlan for `TEST A` and saw the ElectricalPlan was more compressed than the FloorPlan.

Initial verifier output passed, but follow-up DXF inspection shows the verifier did not compare the final exported FloorPlan DXF geometry against the final exported ElectricalPlan DXF geometry.

## Evidence
Latest TEST A manifest:
`src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/exports/plan-sets/0f11bf0440604eb3bf55c765c82d90b6/manifest.json`

Exported files:
- FloorPlan: `C:\Users\lucas\Downloads\TEST A.dxf`
- ElectricalPlan: `C:\Users\lucas\Downloads\TEST A-plan-set\ELECTRICAL PLAN SEMINOLE 2000-12-35c8a7e0b93d4a769e4083da67adc46c.dxf`

DXF layer bounds found:
- FloorPlan `WALLS`: width `480.186"`, height `930"`
- FloorPlan `SETBACKS`: width `464.400"`, height `948.221"`
- ElectricalPlan `ELECTRICAL WALLS`: width `464.400"`, height `930"`

## Root cause hypothesis
The current audit proves ElectricalPlan against the canonical recipe/expected outline, but it does not prove actual exported FloorPlan visible structural geometry against actual exported ElectricalPlan structural geometry.

Therefore `OutlineSegmentCongruenceStatus=SegmentCongruent` can pass while the user's CAD overlay still shows a mismatch.

## Correct next fix
Add a final exported-vs-exported congruence audit:
- read exported FloorPlan DXF path from manifest
- read exported ElectricalPlan DXF path from manifest
- extract comparable structural outline/layers from both final files
- compare width/height/edges/segments after export
- make `verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic` fail if final exported FloorPlan and ElectricalPlan are not congruent

## Current truth
`TEST A` must not be treated as fully passed until the verifier includes final exported-vs-exported congruence.
