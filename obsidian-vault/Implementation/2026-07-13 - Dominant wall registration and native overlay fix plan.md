---
type: Implementation
status: in-progress
date: 2026-07-13
replaces: []
replaced_by: null
---

# Dominant wall registration and native overlay fix plan

## Product contract

For FloorPlan + ElectricalPlan, both exported DXFs must use the same canonical coordinate system. Inserting both at `0,0` must align the shared structural footprint; nonstructural Electrical content remains sheet-specific.

## Phases

1. Add a RED synthetic DXF case containing a dominant `468"` wall pair plus short one-inch-outboard fragments. It must prove the current false `470"` normalization and false-green final gate.
2. Stop deriving hidden global scale from structural-layer min/max. Only an explicit, evidenced registration may scale a dependent sheet; otherwise fail closed.
3. Select dominant corresponding wall pairs by separation, cumulative support and coverage. Canonical dimensions validate candidates but do not force a scale.
4. Estimate and persist Electrical-to-Floor registration from at least two non-collinear structural anchors. For SEMINOLE the expected transform is scale `1`, rotation `0`, translation approximately `+30.479852"` X / `+63.802325"` Y.
5. Replay the canonical local recipe after registration, then place Electrical in the same final canonical coordinate system as FloorPlan.
6. Make final verification require native-coordinate edge/anchor residuals within tolerance. Translation-invariant equal width/height is advisory only and cannot produce `ReadyForExport`.

## Required evidence

- Candidate edges, support, coverage, selected/rejected reason.
- Registration scale/rotation/translation and anchor residuals.
- Deformation decomposition: registration versus canonical recipe.
- Final dominant Floor/Electrical bounds in the same coordinates.
- Width, height, left/right/top/bottom residuals and mismatch samples.

## Tests

- Synthetic unit regression for outboard fragments; selected width must be `468"`, scale must remain `1`.
- Ambiguous candidate case must require manual review.
- Final-gate regression must reject equal fringe bboxes when dominant walls differ.
- No-op, width-only, height-only and width+height integration cases.
- Fresh SEMINOLE runtime proof from the authoritative D: inputs. Width-only acceptance: both dominant widths `468" -> 464.4"`; native-coordinate residuals within tolerance; no hidden normalization.

## Rejected shortcut

Do not hardcode SEMINOLE coordinates, ignore exactly one-inch fragments, or merely increase the `24"` support threshold. Those patches do not generalize to N plans.

## 2026-07-13 - Synthetic RED baseline added

- `ExportAsync_uses_dominant_outline_instead_of_short_outboard_fragments` requires the dominant `100 x 200` rectangle to win over two short outboard fragments, keep scale `1`, and finish at width `90` after the canonical compression.
- `ExportAsync_requires_manual_review_for_ambiguous_dominant_wall_pairs` requires tied supported wall pairs to fail closed instead of silently normalizing the full min/max envelope.
- `BuildVerificationReport_rejects_matching_fringe_bounds_when_dominant_walls_differ` requires the final verification gate to reject equal fringe envelopes when the real dominant footprints differ.
- These tests are intentionally RED against the current min/max implementation. They were not executed because this goal prohibits every `dotnet` command; only static inspection and `git diff --check` were used.

## 2026-07-13 - Evidenced registration boundary

- Estimation belongs in Loop 2 at `RegisterElectricalSheetHandler`: Application resolves the exact canonical Floor version and owned Electrical source, while Infrastructure interprets DXF geometry behind one new `IElectricalFloorRegistrationEstimator` boundary.
- Reuse `SheetRegistration`, `SheetRegistrationTransform`, the existing repository, and `RuleSummary`; no SQLite migration or second registration store is needed.
- A conclusive estimate persists transform, confidence, and evidence as `PendingConfirmation`. Ambiguous or insufficient evidence performs no registration write; request/dialog identity must never become automatic geometry truth.
- Candidate quarter-turns are `0/90/180/270`. Uniform scale is accepted only when both axes support it; dimensions already equal within tolerance snap to exactly `1`. Translation is derived after rotation/scale, then bidirectional structural coverage and residuals must select one unique candidate.
- Dominant rectangular bounds alone cannot distinguish opposite rotations. Symmetric or multiply valid candidates fail closed rather than inventing orientation.
- `RuleSummary` carries concise durable evidence; audit output carries observed X/Y scales, selected transform, per-edge residuals, RMS/max residual, coverage, and rejection reasons.

## 2026-07-13 - Application/Contracts GREEN static review approved

- Verdict: **APPROVED** with no blockers in the four-file review scope.
- `RegisterElectricalSheetRequest` exposes exactly the two identity properties required by the test contract.
- `RegisterElectricalSheetHandler` obtains the owning `PlanSetVersion`, checks sheet ownership/type, requests the exact canonical Floor version and exact Electrical sheet source, and passes their exact paths to `IElectricalFloorRegistrationEstimator`.
- `ElectricalFloorRegistrationEstimate.IsConclusive` fails closed unless status is `Estimated` and a transform exists; the handler checks it before every registration, audit, and unit-of-work write.
- Successful registration persists the owning canonical Floor version, estimator transform/confidence/evidence, `WholeSheetSimilarity`, and `PendingConfirmation`, with no confirmed timestamp. `RuleSummary` is returned in the DTO.
- The audit payload is structured and includes registration identity/status, selected transform, observed scales, coverage, RMS/max residuals, and per-candidate acceptance, transform, coverage, residual, edge-residual, and reason fields.
- Static C# review found constructor/fake/record signatures internally aligned and dependency direction correct. Scoped whitespace verification passed.
- This is not executable proof: no `dotnet`, build, test, restore, watch, Desktop, or runtime command was run.

## 2026-07-13 - Estimator adversarial review blockers

The first Infrastructure estimator is not yet accepted. Before this phase closes it must:

- reject the whole automatic estimate when a supported structural entity is partially or invalidly parsed;
- tokenize layer semantics so `NONSTRUCTURAL` cannot match `STRUCT` accidentally;
- reject curved or non-default-OCS structural polylines and non-planar structural faces, while honoring `3DFACE` invisible edges;
- convert arithmetic overflow into `InsufficientEvidence`;
- preserve dominant-selector `Ambiguous` as registration ambiguity;
- add discriminating REDs for scale-1 snap, bidirectional coverage, rotations `0/90/180/270`, and a real competing fringe pair.

These are fail-closed correctness requirements, not a new general-purpose CAD engine.

## 2026-07-13 — Estimator attempt 2: one fail-open blocker remains

Static spec review passed, but the independent quality review found one concrete blocker: an unsupported geometry entity on a structural layer still falls through the estimator switch and is silently ignored. That can preserve partial LINE evidence and falsely produce an automatic registration. Before closing Loop 2, add a RED with a structural ARC and then reject unsupported structural geometry with an entity-specific diagnostic. No runtime/.NET verification has been run.
## 2026-07-13 — Electrical registration estimator statically closed

Attempt 3 closed the last reviewed fail-open path. A new RED appends a structural ARC to otherwise sufficient matching evidence; automatic registration must return `InsufficientEvidence`, no transform and no candidates. The estimator now rejects every unsupported entity on an accepted structural layer with an entity-specific diagnostic, so partial evidence cannot survive silently. Independent static re-review approved the complete estimator contract, including IxMilia `EntityTypeString` API shape. Runtime/.NET proof remains intentionally external because repository rules forbid running it here.
## 2026-07-13 — Final static gate exposed a stale normalization contract (superseded)

> Superseded by [[#2026-07-14 - Outline congruence contract migrated to evidenced registration]].

Three final non-.NET contract checks and all whitespace/hardcode checks passed. `test-outline-congruence-contract.ps1` failed because it still requires the removed `BuildOutlineNormalization` symbol. That expectation contradicts the current goal: hidden bbox/min-max normalization was intentionally replaced by evidenced Electrical-to-Floor registration and native dominant-edge congruence. The contract must be migrated to assert the new invariant, not revived with dead compatibility code.

## 2026-07-14 - Outline congruence contract migrated to evidenced registration

- `replaces`: [[#2026-07-13 — Final static gate exposed a stale normalization contract (superseded)]].
- Removed positive requirements for `ProjectedPlanSheetOutlineNormalization`, `BuildOutlineNormalization`, `NormalizeRegisteredPoint`, `MismatchRequiresNormalization`, and the three hidden-normalization regression names.
- Added a negative `BuildOutlineNormalization` assertion and positive evidence assertions for `DominantAxisAlignedOutlineSelector.Select`, `accepted.Length == 1 && accepted[0].EvidenceComplete`, and `estimated.Evidence.Scale.GetValueOrDefault()`.
- Export projection must use `ElectricalRecipeProjection.ProjectPoint` with `recipe.RegistrationTransform`; the effective recipe sets `OutlineNormalization = null`.
- Final audit proof must inspect projected output with identity registration, fail closed through `ThrowDominantOutlineManualReview("Exported Electrical"...)`, and retain the verifier's `FinalNativeDominantStructuralOutlineEdgesAndSize` plus `NativeEdgeResidualsWithinTolerance` gates.
- Preserved useful audit DTO, canonical-dimension read, safe deserialization, manifest artifact, human summary, and verifier output assertions.
- RED: `& './scripts/test-outline-congruence-contract.ps1'` exited `1` with `DXF exporter must derive dependent Electrical outline normalization.`
- GREEN: the same command exited `0` with `Outline congruence contract is wired.`
- Whitespace: `git -c core.autocrlf=false diff --no-index --check -- NUL scripts/test-outline-congruence-contract.ps1` reported no errors. The default Git conversion warning was not a whitespace defect.
- Scope remained Loop 2 static verification. No production edit or prohibited execution occurred; executable/runtime proof remains external.

## 2026-07-14 — Remaining blocker: registration tolerance uses raw DXF units

The end-to-end static integration audit verified that imported canonical Floor and dependent Electrical paths retain their raw DXF coordinate spaces. The estimator currently applies a numeric `0.05` tolerance as if every raw unit were an inch, while the import contract supports inch, foot, millimeter, centimeter, meter and unitless files resolved through `$INSUNITS`/`$MEASUREMENT`. This can authorize a false automatic registration for non-inch inputs. Available SEMINOLE managed copies report inches, so this is a generic robustness blocker rather than evidence for the historical two-inch mismatch. The finite next step is one unit regression followed by source-specific raw tolerances and selector tolerance plumbing; then rerun the already-defined non-.NET gates. The stale outline-normalization contract has already been migrated and now passes.