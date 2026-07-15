---
status: superseded
replaced_by: "[[2026-07-13 - Finite goal prompt for dominant wall registration fix]]"
---

# 2026-07-09 - Goal prompt for Electrical registration fix

> Superseded by [[2026-07-13 - Finite goal prompt for dominant wall registration fix]] after the false two-inch normalization and false-green final gate were proven.

## Purpose
Use this finite goal prompt to fix the remaining FloorPlan -> Electrical sync issue without overlooping.

## Current truth
- The FloorPlan canonical recipe is working and auditable.
- Electrical recipe replay is working mechanically, but it can miss top/patio compression because Electrical -> FloorPlan registration may be identity/misaligned.
- The fix must be generic for N FloorPlans and N ElectricalPlans with shared drawing conventions but varied geometry, not a SEMINOLE/PATIO hardcode.

## Stop rule
The goal must stop when the automatic export proves the Electrical sheet applies the same canonical affected zones as the FloorPlan, or when the same blocker repeats three times with evidence and no safe code-side next step remains.

## Copy/paste goal prompt

```text
GOAL: Finish FloorPlan -> ElectricalPlan zone-sync correctly and generically.

Product intent:
When the user adjusts a canonical FloorPlan to fit setbacks, the system must transmit the same structural shrink recipe to its related ElectricalPlan. The ElectricalPlan must not run an independent fit solver. It receives the approved FloorPlan recipe, maps itself to the FloorPlan coordinate space, applies only the matching affected zones, and exports a valid DXF.

Scope:
- Only FloorPlan + ElectricalPlan.
- Do not touch RoofPlan, Facade/Elevation, broad UI, or unrelated architecture.
- Do not hardcode SEMINOLE, PATIO, exact coordinates, filenames, or one DXF.
- Design for N floor plans and N electrical plans sharing drawing conventions but with different dimensions, rooms, heights, and layer noise.
- Respect AGENTS.md: do not run forbidden build/watch commands.

Known context:
- HousePlanSet, canonical FloorPlan recipe, Electrical recipe-aware export, audit files, and verifier scripts already exist.
- Current failure class: Electrical can miss a patio/top compression because the canonical FloorPlan pinch line lives in FloorPlan coordinates, while Electrical geometry is in a different/misaligned sheet coordinate range.
- A correct fix must distinguish:
  1) geometry truly absent,
  2) geometry present but registration/mapping wrong,
  3) geometry present but unsafe/unsupported,
  4) geometry applied successfully.

Phase 0 — Read current evidence, no code yet:
1. Inspect latest manifest and audit JSON.
2. Report concrete numbers:
   - requested setback dimensions/deltas,
   - canonical recipe operations,
   - FloorPlan applied/missed ops,
   - Electrical applied/missed ops,
   - Electrical bbox/anchor bbox,
   - current registration transform,
   - exact missed operation reason.
3. State the hypothesis in one paragraph.
Stop Phase 0 if there is no failing proof or no fixture/runtime evidence to reproduce.

Phase 1 — Create a failing proof:
1. Add or improve the smallest repo-allowed verifier/fixture that fails when:
   “Electrical geometry exists in the matching structural zone, but the canonical operation is reported as NoGeometryAffected only because coordinate registration/mapping is wrong.”
2. The proof must be generic: bbox/anchors/layers, not SEMINOLE/PATIO strings.
3. Do not change production behavior until this proof exists.

Phase 2 — Fix registration/mapping:
1. Find where Electrical registration is created, stored, and consumed.
2. Prefer existing transform fields: scale, translate X/Y, rotation.
3. If exact transform is missing, implement the smallest generic derived mapping using structural anchors:
   - wall/exterior/structural layer families,
   - registered sheet bounds,
   - edge-equivalent pinches for top/bottom/left/right when canonical coordinates are outside dependent-sheet bounds.
4. Repeated edge operations must stack cumulatively, not collapse into the same coordinate.
5. Keep the algorithm house-agnostic and layer-family based.

Phase 3 — Apply recipe safely to Electrical:
1. Evaluate canonical operations against Electrical geometry through the registration/mapping layer.
2. Move only entities/vertices on the affected side of the operation.
3. Preserve DXF safety:
   - valid handles/owners,
   - no invalid entity reconstruction,
   - unsupported crossing curves downgrade to manual review,
   - existing FloorPlan export behavior unchanged.

Phase 4 — Observability:
1. Improve raw summary/audit so a human can answer:
   - What did the user request?
   - What did FloorPlan shrink?
   - What did Electrical shrink?
   - What did Electrical not shrink?
   - Why?
   - Is this safe to trust?
2. Replace ambiguous “No electrical geometry matched” with a specific category:
   - registered-empty-zone,
   - mapping/registration mismatch,
   - unsupported/manual geometry,
   - applied with dependent-sheet edge anchor.

Phase 5 — Verification:
Run only repo-allowed checks. Do not build/watch.
Required checks:
1. Static/fixture check proving the generic mapping case.
2. Existing manifest verifier self-check.
3. Human-summary contract check.
4. Latest runtime verifier if a fresh Desktop export exists.
5. If runtime proof needs user/Desktop/CAD action, stop and say exactly what the user must do.

Definition of Done:
Complete only when all are true:
1. FloorPlan and Electrical both receive/apply the relevant top/patio-style compression when matching Electrical geometry exists.
2. Electrical no longer reports ambiguous NoGeometryAffected for coordinate-misaligned geometry.
3. FloorPlan exports are not regressed.
4. DXF safety checks pass.
5. Raw summary clearly explains applied/missed operations and trust level.
6. No SEMINOLE/PATIO/coordinate hardcode.
7. Final response includes files changed and exact check outputs.

Anti-loop protocol:
For every iteration print:
- Hypothesis:
- Changed files:
- Check run:
- Result:
- Next decision:

Do not repeat the same check after the same failure unless code, fixture, export, or hypothesis changed.
If the same blocker appears 3 times, stop and mark BLOCKED with exact evidence.
If manual Desktop/CAD action is required, stop and provide exact user steps.
Shortest safe diff wins.
```
