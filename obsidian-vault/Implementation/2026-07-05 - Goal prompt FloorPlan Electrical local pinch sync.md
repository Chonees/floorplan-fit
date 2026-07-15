# 2026-07-05 - Goal prompt FloorPlan Electrical local pinch sync

## Decision
For FloorPlan -> ElectricalPlan sync, the safe senior target is not a global rescale. It is a canonical, piecewise FloorPlan recipe replayed in FloorPlan coordinates against registered Electrical geometry.

## Why
When the curated FloorPlan gets local pinches/recortes, such as patio or porch compression, the ElectricalPlan must follow the same structural displacement without becoming a second decision engine and without mutating old exports.

## Core invariant
ElectricalPlan is always regenerated from:

`Electrical original DXF + Electrical->Floor registration + canonical FloorPlan adjustment recipe`

Per safe point:
1. Electrical source point -> FloorPlan source coordinates.
2. Apply the approved canonical FloorPlan recipe in FloorPlan coordinates.
3. Convert the adjusted result to site/output coordinates.

## Scope
- FloorPlan + ElectricalPlan only.
- FloorPlan is canonical.
- ElectricalPlan is dependent.
- No Roof/Facade in this goal.
- No bidirectional sync.
- No destructive filters to hide broken CAD.

## Agent loop design
- Cartographer: map current storage/export flow and stop with concrete files.
- Math Agent: prove the pure point transform with failing/passing tests.
- DXF Safety Agent: define supported entity semantics and manual-review cases.
- Application Flow Agent: wire real canonical recipe into Electrical export and manifest/audit.
- Verification Agent: prove SEMINOLE runtime export with manifest and DXF entity evidence.

## Anti-loop rule
Every loop must produce one durable artifact: failing test, passing test, code diff, verifier output, or root-cause report. After 3 failed attempts for the same symptom, stop and report evidence instead of trying a fourth blind fix.

## Completion condition
The goal is complete only when SEMINOLE proves that a real FloorPlan local pinch propagates to the related ElectricalPlan, while cables/symbols/doors/dimensions/text/ellipses remain CAD-valid or the sheet is explicitly marked RequiresManualConfirmation.
