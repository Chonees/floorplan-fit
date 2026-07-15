---
type: Experiments
date: 2026-07-12
status: passed
---

# TEST A width-only HousePlanSet audit

User exported `C:\Users\lucas\Downloads\TEST A.dxf` with a width-only setback adjustment.

Latest manifest:
`src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/exports/plan-sets/0f11bf0440604eb3bf55c765c82d90b6/manifest.json`

## Result
Passed automatic pipeline verification.

## Evidence
- Manifest status: `ReadyForExport`
- Electrical status: `ProjectedAutomatically`
- Canonical recipe operations: 4 horizontal compressions, all `Right`, each `0.9"`; total width reduction `3.6"`
- No vertical compression operations were generated
- FloorPlan operations: 4/4 applied
- Electrical operations: 4/4 applied
- Electrical affected entities: operation set affected 456/456/93/93 entities respectively
- DXF safety: output exists, 3425 entities after export, 93 INSERTs, 17 DIMENSIONs, 4 ELLIPSEs, 313 wire/curve entities, 0 missing handles, 0 missing owners
- Outline normalization: applied because Electrical source outline was about `2"` wider than canonical FloorPlan source outline
- Export outline width mismatch after normalization and recipe: `0"`
- Export outline height mismatch: about `0.000286"`, inside tolerance
- Segment congruence: `SegmentCongruent`
- Required structural missing in Electrical: `0`
- Required structural extra in Electrical: `0`

## Non-blocking observations
- Advisory internal wall-run differences remain: 19 missing internal floor runs and 113 extra electrical wall runs.
- These are advisory because the current gate proves required structural outline coverage, not exact internal drafting equality.

## Interpretation
For width-only setback propagation, TEST A proves the FloorPlan canonical recipe was propagated to ElectricalPlan and produced a safe, automatically projected Electrical DXF.

## Superseded
Superseded by [[2026-07-12 - TEST A verifier missed exported FloorPlan Electrical visual mismatch]]. The verifier passed, but later overlay/DXF evidence showed it did not compare final exported FloorPlan geometry against final exported Electrical geometry.

