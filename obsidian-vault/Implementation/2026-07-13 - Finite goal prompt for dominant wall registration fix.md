---
type: Implementation
status: ready
date: 2026-07-13
replaces: "[[2026-07-09 - Goal prompt for Electrical registration fix]]"
replaced_by: null
---

# Finite goal prompt for dominant wall registration fix

Copy/paste this as the implementation goal:

```text
GOAL: Fix the false Electrical outline normalization and leave FloorPlan -> ElectricalPlan ready for one external end-to-end runtime proof.

Outcome:
For every related FloorPlan/ElectricalPlan pair, the system must identify corresponding dominant structural walls, persist an evidenced Electrical -> FloorPlan registration, replay the canonical local adjustment recipe, and export both DXFs in the same canonical coordinate system. Inserting both final files at 0,0 with scale 1 and rotation 0 must align the shared structural footprint.

Verified starting evidence:
- Current SEMINOLE dominant source footprints are both 468" x 930" after translation.
- Electrical short fringe fragments at X=63.094337 and X=533.094337 caused a false 470" bbox.
- The exporter applied false scaleX=0.9957446808508176.
- Final dominant widths are Floor=464.400000" and Electrical=463.308510".
- The current final audit returned a false green because the fringe envelope also measured 464.4".
- Authoritative raw inputs live under D:\PointAIData\PLANS.
- Do not trust manifest b4e8192e70a146f3910a4fc77988b028 as final congruence proof.

Scope:
- FloorPlan + ElectricalPlan only.
- Fix registration, projection, final congruence and observability.
- Do not touch RoofPlan, Facade/Elevation, unrelated UI, broad modularization or architecture.
- Do not hardcode SEMINOLE, 468, 470, one-inch fragments, coordinates, filenames, room names or layer names beyond existing generic structural layer-family conventions.
- Preserve FloorPlan curation/export behavior and DXF validity.
- Respect AGENTS.md: do not run dotnet/build/test/watch/restore/app; do not reset/stash/clean unrelated worktree changes.

Non-negotiable rules:
1. Canonical dimensions validate candidate wall pairs; they never force a hidden global scale.
2. Global dependent-sheet scale may come only from an explicit registration supported by multiple independent anchors.
3. Ambiguous or insufficient geometry fails closed as RequiresManualConfirmation.
4. Equal normalized width/height is not final proof. Automatic approval requires dominant-wall and native-coordinate residuals.
5. Use the smallest implementation. No new interface/factory/plugin/dependency unless an existing seam cannot do the job.
6. Strict TDD ordering: establish the failing regression before changing production behavior.
7. Never weaken a verifier or tolerance merely to make the fixture pass.

Phase 0 — Baseline, no edits:
- Re-read the active bug note and current code paths:
  - ProjectedPlanSheetDxfExporter.TryCollectRegisteredAnchorBounds / BuildOutlineNormalization
  - RegisterElectricalSheetHandler and Desktop registration caller
  - PlanSetOutlineSegmentCongruenceAuditBuilder.SupportedBounds / BuildFinalOutputSheet
- Record exact current hypothesis, selected wrong edge candidates and existing test coverage.
- Do not rediscover unrelated architecture.

Gate 0:
Proceed only if the false 470" selection and final dominant mismatch remain reproducible from current evidence. Otherwise stop with the contradictory evidence.

Phase 1 — Executable RED:
Add the smallest generic synthetic regressions:
A. Dominant walls separated by target width plus short outboard fringe fragments. Expected selected width=target and registration scale=1.
B. Final fringe envelope matches Floor, but dominant Electrical walls are narrower. Expected final status=MismatchRequiresManualReview.
C. Ambiguous equal-scoring wall pairs. Expected RequiresManualConfirmation.
Do not modify production behavior before these tests express the failure.
Do not add the real D: files to the repository.

Gate 1:
List test names and explain exactly why each fails on current production logic. Since dotnet execution is forbidden here, mark executable RED as written-but-not-run; do not claim runtime RED.

Phase 2 — P0 safety correction:
- Remove hidden bbox/min-max-derived outline scaling from export.
- If no trusted registration evidence exists, do not normalize: fail closed.
- The old manifest/verifier evidence must no longer qualify as automatic final proof when dominant/native-coordinate evidence is absent.
- Preserve all DXF safety gates.

Gate 2:
Static inspection must prove no path can generate scaleX from raw structural min/max alone.

Phase 3 — Generic dominant-wall matcher:
Implement one small deterministic matcher, not duplicate heuristics:
- collect axis-aligned structural runs;
- cluster coordinates within existing CAD tolerance;
- calculate cumulative support and coverage;
- enumerate parallel edge pairs;
- rank pairs by canonical-size compatibility, support, continuity and cross-axis consistency;
- return selected pair, rejected candidates and confidence;
- return ambiguity/insufficient-data instead of guessing.
Canonical dimensions are a validator/tie-breaker, not a resize instruction.

Gate 3:
Synthetic fringe case selects the dominant pair; ambiguous case fails closed; no fixture-specific constants exist.

Phase 4 — Electrical -> Floor registration:
- Derive scale/rotation/translation from at least two non-collinear matched anchors.
- Persist the transform through the existing SheetRegistration model.
- Scale must remain 1 when corresponding anchor distances agree.
- Surface suggested transform and evidence through the existing confirmation flow; do not silently confirm uncertain registration.
- For the current imported SEMINOLE snapshot, expected evidence is approximately scale=1, rotation=0, translateX=+30.479852", translateY=+63.802325". Use this only as runtime evidence, never as production constants.

Gate 4:
Registration audit contains anchor IDs/coordinates, transform, residuals, confidence and ambiguity reason.

Phase 5 — Recipe projection and coordinate contract:
- Register Electrical into Floor coordinates first.
- Replay the canonical local operations second.
- Apply the same final canonical/site placement third.
- Final Floor and Electrical structural outputs must share native coordinates.
- Existing dependent-edge fallback may not hide a failed registration; downgrade to manual when its structural correspondence is unproven.

Gate 5:
A no-op synthetic case overlays at 0,0; width-only, height-only and combined cases apply the same canonical deltas to both dominant footprints.

Phase 6 — Independent observability/final gate:
Expose:
- every candidate edge with support/coverage;
- selected/rejected reasons;
- registration transform and anchor residuals;
- registration deformation versus canonical recipe deformation separately;
- final dominant bounds for Floor and Electrical in the same coordinate system;
- left/right/top/bottom native-coordinate deltas;
- missing/extra dominant wall samples.
ReadyForExport requires all mandatory residuals within tolerance. Translation-invariant bbox equality is advisory only.

Gate 6:
The old false-green shape must fail even when fringe envelopes have equal width and height.

Phase 7 — Verification and handoff:
Run only repository-allowed non-dotnet checks once each after the final change:
- relevant PowerShell contract/self-check scripts;
- git diff --check;
- focused source inspection for fixture-specific constants.
Write the focused xUnit tests but do not execute dotnet.
Provide one external verification sequence:
1. user/CI runs dotnet test .\FloorplanFit.sln;
2. user opens Desktop from fresh code;
3. unlink/re-register the stale Electrical registration;
4. export no-op, width-only, height-only and combined fresh destinations;
5. run verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic;
6. overlay Floor/Electrical at 0,0 in AutoCAD.

Implementation Definition of Done:
- All phases 0-7 gates are satisfied with recorded evidence.
- Focused executable tests exist.
- Allowed static checks pass.
- No hidden bbox normalization remains.
- No hardcodes or unrelated refactor.
- The app is ready for exactly one external runtime verification sequence.
At this point MARK THE IMPLEMENTATION GOAL COMPLETE. Do not loop waiting for AutoCAD or forbidden dotnet execution.

Product Acceptance (external, separate from implementation completion):
- source dominant Floor/Electrical dimensions agree;
- registration scale=1 for the SEMINOLE pair;
- no hidden normalization;
- every expected operation applies safely;
- final dominant dimensions agree;
- all native edge residuals are within tolerance;
- direct 0,0 overlay aligns;
- DXF safety passes.

Anti-overloop protocol:
- Maintain exactly one in-progress phase.
- Every iteration must print:
  Phase:
  Hypothesis:
  Evidence inspected:
  Files changed:
  Check executed:
  Result:
  Next decision:
- Never rerun an unchanged check after the same result.
- Never reread the same files without a new hypothesis.
- Maximum three evidence-bearing attempts per phase.
- A retry must change the hypothesis, fixture or implementation.
- If the same blocker repeats for three consecutive goal turns and no safe code-side action remains, mark BLOCKED with the exact blocker, evidence and one required external action.
- If only Desktop/AutoCAD/dotnet proof remains, mark implementation COMPLETE and return the external sequence once. Do not keep looping.
- Do not commit or push unless explicitly requested.
- Shortest safe diff wins.
```
