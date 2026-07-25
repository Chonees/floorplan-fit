---
type: implementation
status: ready_for_external_verification
date: 2026-07-19
project: FloorplanFit
area: Loop 1 compression and Loop 2 Electrical projection
decision: "[[2026-07-19 - Adopt CAD-style pinch stretch actions]]"
---

# Finite goal for CAD-style pinch deformation

## Objective

Replace the duplicated coordinate-global pinch transforms with the accepted generic CAD-style stretch-action model, preserving the current canonical-to-Electrical pipeline and finishing at a ready-for-external-runtime-proof handoff.

## Required behavior

- Two markers on paired wall faces compile to one logical operation and one total delta.
- Only the exact marked spans shorten; both shorten equally and wall thickness remains unchanged.
- Complete closing-side entities translate rigidly once.
- Fixed/unselected entities remain coordinate-identical.
- Unsupported or unmarked crossings fail closed before partial output.
- New adjustments persist recipe `v2`; historical `v1` remains readable.
- Preview, canonical FloorPlan export, and registered Electrical export use one pure engine.
- Electrical reuses the confirmed registration transform and source-bound proof, resolves its own entity/vertex membership from canonical action geometry, and never copies FloorPlan entity IDs.
- Audit evidence explains selected, stretched, moved, untouched, rejected, requested delta, measured delta, and every invariant result.

## Finite phases

1. Write and self-review the technical design against current code and this accepted decision.
2. Write the smallest RED contracts for paired-face single-delta behavior, rigid translation, untouched geometry, ambiguity failure, preview/export parity, Electrical parity, and `v1` compatibility.
3. Add the minimal recipe `v2` contract and serialization compatibility using existing JSON persistence.
4. Implement one pure entity/vertex-aware deformation engine.
5. Route Desktop preview, canonical FloorPlan export, and Electrical projection/export through that engine; remove or delegate duplicated threshold logic.
6. Add fail-closed validation and operation-level observability.
7. Perform allowed static verification, update documentation, and produce one external `.NET`/Desktop/AutoCAD test sequence.

## Stop contract

- Keep at most one phase in progress.
- Maximum three evidence-bearing attempts per phase.
- Never repeat the same read/check after an unchanged hypothesis, fixture, or implementation.
- Every retry must state what changed and what new evidence it can produce.
- If the same blocker occurs in three consecutive goal turns and no safe repository progress remains, mark the goal blocked with the exact blocker and one required external action.
- Do not expand into RoofPlan, Facade, a whole-building topology graph, new persistence tables, speculative abstractions, or unrelated UI polish.
- Do not run `dotnet`, build, test, restore, watch, or Desktop; do not reset/stash/clean shared changes; do not commit/push without a new explicit request.
- Mark complete when design, tests, implementation, static checks, Obsidian truth, and the single external proof sequence are ready. Do not loop waiting for the user to run external proof.

## Definition of done

- No new `v2` path uses coordinate-only global point thresholds.
- One requested delta is applied exactly once to a paired wall action.
- FloorPlan and Electrical replay equivalent canonical intent with auditable invariant results.
- Existing registration/package/audit infrastructure remains intact.
- The change is generic: no SEMINOLE, coordinate, room, filename, or plan-specific branch.
- One concise external validation path is delivered.

## Execution progress

- Phase 1 `Design` is complete; Phase 2 `RED TDD` is in progress.
- Baseline verified on branch `test` at `4b27d1d`; `src/` and `tests/` have no pre-existing working-tree diff.
- Existing unrelated Obsidian/PDF changes remain preserved and outside implementation ownership.
- Attempt 1 hypothesis: the current recipe JSON and canonical-to-Electrical orchestration can remain while a minimal `v2` action and shared pure engine replace only point-threshold deformation.
- Phase 1 attempt 1 blocker: delegated design/test mapping did not return within the bounded wait and produced no file yet.
- Attempt 2 change: stop further broad exploration and require the agents to return the smallest evidence-backed design/test map from current findings.
- Attempt 2 expected evidence: one concrete design spec plus an exact RED test-location map, without additional codebase expansion.
- Phase 1 attempt 2 blocker: bounded return requests also produced no result within one minute.
- Attempt 3 change: close the non-returning agents and complete the design locally from the already verified code seams; do not spawn replacement exploration.
- Attempt 3 expected evidence: the actual spec file plus a scoped static self-review. This is a progress fallback, not a goal blocker.
- Attempt 3 evidence: `docs/superpowers/specs/2026-07-19-cad-style-pinch-deformation-design.md` now defines the v2 contract, exact side/entity classification, shared engine, Floor/Electrical adapter boundaries, atomic failure, observability, and RED matrix. Required-term review and scoped `git diff --check` passed without running .NET.
- Phase 2 attempt 1 blocker: two bounded test-only workers produced no changed test file within two minutes.
- Phase 2 attempt 2 change: require immediate minimal RED patches from evidence already read; stop further exploration.
- Phase 2 attempt 2 expected evidence: compile-intent test diffs in the assigned files, with no production edits.
- Phase 2 attempt 2 blocker: immediate-return instructions still produced no patch within the bounded minute.
- Phase 2 attempt 3 change: close both workers and author the RED contracts locally against the now-fixed design/API; do not retry delegation in this phase.
- Phase 2 attempt 3 expected evidence: scoped test-only diff covering the required invariants, reviewed without .NET execution.
- Final fail-closed hardening rejects an AI plan that repeats one `PinchGroupId` as multiple logical actions and rejects duplicate/empty action IDs before canonical FloorPlan DXF mutation. This preserves the core invariant: one paired pinch group carries one total delta.

## Static completion state

All seven finite phases are complete at the repository-allowed static boundary:

1. The approved design and finite implementation plan define recipe `v2`, role selection, Electrical resolution, invariants, compatibility, and fail-closed behavior.
2. Focused TDD contracts cover one delta for two faces, equal shortening, rigid movement, untouched geometry, preserved spacing, duplicate/missing identity, unsupported crossing, multi-action composition, Preview/Floor parity, registered Electrical IDs, observability, and `v1` JSON compatibility.
3. `AdjustedSitePlanPlacementDto` and `AdjustmentRecipeSummaryDto` carry optional `StretchActions`; historical `v1` payloads deserialize with an empty action list.
4. `CadStretchDeformationEngine` is the single pure deformation engine. It has no Avalonia or IxMilia dependency and returns an edit plan only after all invariants pass.
5. Preview, canonical Floor DXF, and registered Electrical DXF adapt their own entity/vertex IDs into that engine. The v2 branches do not call the legacy coordinate-threshold point transforms.
6. Canonical and dependent audits count one paired action once, record requested/measured delta and role evidence, and keep package/manifest verification intact. Unsupported or ambiguous crossings reject before a new output file is written.
7. Scoped static inspection and whitespace checks pass. No `.NET`, build, test, restore, watch, Desktop, commit, or push command ran.

Final static evidence is concrete: tracked `git diff --check` passed; all four new C# files have no trailing whitespace; the three v2 adapters contain no call to their legacy point-threshold functions; source contains exactly three calls to `CadStretchDeformationEngine.Apply` (Preview, Floor, Electrical); changed production source contains no plan-name hardcode and no persistence-schema expansion; required contract names are present. A Roslyn parser could not be loaded into the legacy Windows PowerShell host, so this is not represented as compile proof.

## Architecture result

- **Loop 1 / Desktop:** compiles two authored wall-face markers into one source-bound action and renders Preview from the shared edit plan.
- **Loop 1 / Infrastructure:** patches raw canonical Floor DXF pairs by entity/vertex identity, then preserves the existing dimension patch and site-plan injection pipeline.
- **Loop 2 / Application:** preserves canonical adjustment persistence, confirmed/source-hash-bound registration, projection state, and package orchestration.
- **Loop 2 / Infrastructure:** registers Electrical geometry into the canonical frame, resolves Electrical's own matching wall entities, executes the same action, then maps the result to final output coordinates.
- **Contracts:** recipe `v2` lives in the existing JSON; no SQLite table or topology graph was added.

## Deliberate safety boundary

- A target face currently must resolve to one `LINE` or one straight two-vertex `LWPOLYLINE`; a multisegment target rejects before Preview.
- An unselected ARC, curve, dimension, or other entity that intersects the cut rejects rather than being guessed or distorted.
- Historical recipe `v1` remains on its legacy path for old adjustments only; newly compiled paired-wall adjustments use `v2`.
- Runtime compile/API behavior, real-file block/dimension rendering, and AutoCAD visual parity remain the single external proof. They are not silently claimed from static evidence.

## One external proof sequence

1. Stop the current watcher, restart the source launcher, and wait for one successful build.
2. Select a curated FloorPlan whose pinch group has exactly two markers on two single-segment wall faces and whose Electrical registration is confirmed/current.
3. Apply one width or height reduction and export to a fresh HousePlanSet folder.
4. From the repository root run `powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic`.
5. Confirm the manifest/audit reports one `CadStretch` action, equal requested/measured delta, two stretched targets, preserved pair spacing, and zero rejected crossings.
6. Overlay the packaged FloorPlan and ElectricalPlan in AutoCAD; verify the two shortened faces, moved closing-side symbols/blocks, untouched fixed side, dimensions, wires, and DXF validity.

If step 4 reports a concrete crossing, that is the intended fail-closed result: inspect the named entity/action instead of retrying the same export or weakening the gate.
