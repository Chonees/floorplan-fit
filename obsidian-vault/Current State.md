# Current State

## 2026-07-14 - Connected-frame runtime is safely ambiguous because proof remains local

- Runtime evaluated exactly `365 * 1,033 = 377,045` connected-frame pairs; `4,292` quarter-turn hypotheses passed only the uniform-scale dimension gate.
- Exact/tolerance-equivalent transforms are grouped for the final status, but the summary prints the raw evidence-complete evaluations, which explains repeated `scale 1 / 0 degrees / translation (30.479852,63.802324)` entries.
- The blocking RCA is local evidence scope: every connected room-sized rectangle can become a frame, and coverage/residual are normalized only over segments clipped inside that frame. Nonempty two-axis local anchors can therefore be `evidenceComplete` without proving the whole drawing transform.
- Read-only source-DXF evidence for the repeated transform shows near-complete canonical-to-Electrical structural LINE coverage (`H=0.989685`, `V=0.977621`, RMS `0.000675`, max `0.008460`), while reverse coverage is only `H=0.568323`, `V=0.679039` because Electrical contains extra structural runs. Therefore strict whole-sheet bidirectional coverage would reject the true transform.
- Smallest generic correction: group transforms, then gate each unique group by whole-plan **canonical-to-transformed-Electrical** two-axis coverage/residual using the existing metric primitives and unchanged thresholds. Votes remain diagnostic only; more than one whole-plan-complete transform stays ambiguous.
- Current `Ambiguous` result is correct fail-closed behavior: no transform is returned, no registration reaches `PendingConfirmation`, and therefore Confirm is absent.
- No production/test file was changed and no build/test/Desktop command ran. RCA: [[2026-07-14 - Connected frame evidence is local rather than whole-plan]].

## 2026-07-14 - Connected-frame registration repair is statically complete

- `updates`: [[2026-07-14 - Dominant selector ranks disconnected axis pairs]]. The Loop 2 Infrastructure estimator now enumerates only connected four-corner frame candidates from both DXFs and evaluates compatible Floor/Electrical frame pairs rather than committing to independent local winners.
- A cheap per-quarter-turn uniform-scale dimension gate runs before clipping interiors and measuring coverage/residuals. Exactly one non-equivalent evidence-complete transform estimates; zero remains insufficient/manual review; multiple remains ambiguous/manual review.
- The selector's public `Select(...)` API remains available to exporter/audit consumers. No raw-bounding-box fallback, global scale, tolerance change (`0.05` remains), or SEMINOLE-specific branch was introduced.
- Focused regressions now cover a lower-ranked compatible two-source frame pair and distinct compatible-pair transforms. Existing outline-only fixtures were aligned with the interior-anchor proof contract.
- Static source inspection and scoped `git diff --check` passed. No `dotnet`, build, test, restore, watch, Desktop, or runtime command ran; targeted test execution and a SEMINOLE `Register` retry after rebuild remain required.
- Implementation record: [[2026-07-14 - Connected frame pair registration repair]].

## 2026-07-14 - Desktop safe dependent-sheet unlink is statically wired

- `PlanSetSheetDto.CanUnlink` now exposes unlink for every noncanonical sheet; the Application boundary owns canonical rejection and active-workflow cleanup safety.
- `LibraryViewModel.UnlinkDependentSheetAsync` now delegates `UnlinkPlanSheetRequest(sheet.SheetId)` to `UnlinkPlanSheetHandler`, then retains its refresh and selected template/version behavior. Direct ViewModel repository deletion and its separate unit-of-work commit are removed.
- Desktop DI registers `UnlinkPlanSheetHandler` with the other scoped Loop 2 Plan Set Library handlers. Static inspection confirms the adjacent handler removes projections, registrations, then the sheet and commits once.
- Imported documents and historical export/audit snapshots remain outside this change.
- The legacy unregistered-sheet ViewModel test fixture must register `UnlinkPlanSheetHandler` before executable-suite proof; it is outside this owned source-only change.
- Scoped `git diff --check` and source-shape inspection passed. No `dotnet`, build, test, restore, watch, Desktop, or runtime command ran, so executable GREEN proof remains pending.
- `replaces`: [[2026-07-14 - Application unlink RED contract]]. Implementation record: [[2026-07-14 - Desktop safe dependent-sheet unlink]].

## 2026-07-14 - Infrastructure bulk cleanup ports are statically implemented

- Loop 2 persistence now has parameterized `RemoveByDependentSheetIdAsync` deletes for `sheet_adjustment_projections` and `sheet_registrations`; both honor the supplied `CancellationToken` and return a completed `Task` after their single scoped delete.
- `SqlitePlanSheetRepository.RemoveAsync` remains unchanged. Its `plan_sheets` table is keyed by `id`, not `dependent_sheet_id`, so the Application layer must invoke it last after the two dependent-row cleanup calls.
- Cleanup sequencing, canonical-sheet rejection, the one UnitOfWork commit, and any imported-document/export/audit retention policy remain Application responsibilities; the repositories do not coordinate them.
- `git diff --check` and source-shape inspection passed. No `dotnet`, build, test, restore, watch, Desktop, or runtime command ran, so executable GREEN proof remains pending.
- Implementation record: [[2026-07-14 - Infrastructure dependent-sheet cleanup ports]].

## 2026-07-14 - Application unlink has a scoped RED contract

- `UnlinkPlanSheetHandlerTests` now establishes the Loop 2 Application boundary for safe dependent-sheet unlinking.
- Companion RED coverage now requires `PlanSetSheetDto.CanUnlink` for a noncanonical `ElectricalPlan` in `Confirmed / ReadyForExport`, and requires `LibraryViewModel.UnlinkDependentSheetAsync` to hand that sheet to the Application handler, refresh the selected set, and leave no active projection or registration rows.
- The intended minimal API is `UnlinkPlanSheetRequest(SheetId)` plus bulk cleanup ports scoped by `dependent_sheet_id`: projections first, registrations second, the `plan_sheets` row third, then exactly one unit-of-work commit.
- A canonical `FloorPlan` is rejected without any workflow mutation. The test seeds a confirmed `ElectricalPlan` with a `ReadyForExport` projection, verifies every active workflow row for that sheet is removed in order, and leaves another sheet's rows intact.
- Imported documents and historical audit/export evidence remain outside this cleanup boundary. No production code, build, test, restore, watch, or Desktop command ran; executable proof remains intentionally RED.
- Implementation note: [[2026-07-14 - Application unlink RED contract]].

## 2026-07-14 - Confirmed ElectricalPlan unlink is blocked by a safety gap

- `replaces`: [[2026-07-01 - Desktop unlinks misimported dependent sheets]].
- The Library `×` is deliberately hidden for `Confirmed / ReadyForExport`: `CanUnlink` allows only `Unregistered` or `Rejected` plus `NotProjected`.
- The current direct `plan_sheets` delete cannot safely be widened: registrations and projections retain the dependent sheet ID, and SQLite declares no FK/cascade to clean them.
- The smallest safe Loop 2 change is an Application unlink transaction: delete all projections for the sheet, delete all registrations for the sheet, delete the sheet, then commit once. Preserve export/audit snapshots pending an explicit retention decision.
- Discovery and exact RED contract: [[2026-07-14 - Confirmed ElectricalPlan unlink leaves active workflow references]]. No source change, build, test, restore, watch, or Desktop command was run.

## 2026-07-13 - Desktop Electrical registration boundary is statically GREEN

- `updates`: [[#2026-07-13 - Evidenced Electrical registration Application/Contracts GREEN is statically approved]].
- Loop 2 Desktop composition now resolves the stateless DXF Electrical estimator through `IElectricalFloorRegistrationEstimator` with singleton lifetime.
- Electrical registration bypasses `RegistrationTransformDialog`, sends an identifier-only request into Application, and reports inconclusive estimator evidence as `requires manual confirmation` without a registration result.
- RoofPlan and FacadeElevation retain the existing dialog-driven transform/confidence flow unchanged.
- Scoped `git diff --check` passed. No `dotnet`, build, test, restore, watch, Desktop, or runtime command was run, so executable proof remains pending.
- Implementation record: [[2026-07-13 - Desktop automatic Electrical registration boundary]].

## 2026-07-13 - Evidenced Electrical registration Application/Contracts GREEN is statically approved

- `replaces`: [[#2026-07-13 - Evidenced Electrical registration has a RED contract (superseded)]].
- A read-only quality review of the four scoped Application/Contracts files found no blockers against the authored test contract.
- The public request now carries only `PlanSetVersionId` and `ElectricalSheetId`; geometry truth comes from the estimator rather than Desktop-supplied transform values.
- The handler resolves the canonical Floor source by `CanonicalFloorPlanVersionId`, resolves the Electrical source by the owned sheet ID, and passes the exact managed/source paths to the Application-owned estimator boundary.
- Ambiguous, insufficient, or otherwise non-conclusive estimates throw before registration, audit, or unit-of-work writes. Conclusive estimates persist `PendingConfirmation`, preserve estimator confidence and `RuleSummary`, and emit structured transform, coverage, residual, edge, candidate, and reason audit data.
- Dependency direction is inward (`Contracts` stays identity-only; `Application` owns the estimator port and depends on `Domain` value types). The reviewed C# shapes are internally consistent by static inspection.
- Evidence is static only: scoped whitespace verification passed, but no `dotnet`, build, test, restore, watch, Desktop, or runtime command was run.
- Implementation record: [[2026-07-13 - Dominant wall registration and native overlay fix plan#2026-07-13 - Application/Contracts GREEN static review approved]].

## 2026-07-13 - Electrical registration estimator remains under adversarial review

- Application/Contracts and Desktop are statically approved, but the first Infrastructure estimator is NOT accepted yet.
- Active fail-closed blockers: partial/invalid structural entities can be silently dropped; substring layer matching can classify `NONSTRUCTURAL` as structural; curved/non-default-OCS polylines and non-planar/invisible-edge faces can be flattened into fabricated walls; decimal overflow can escape; dominant-selector ambiguity is downgraded to insufficient evidence.
- Discriminating RED coverage must also prove the scale-1 snap, reverse/bidirectional coverage, all four quarter-turns, and a true competing short fringe edge pair.
- These are bounded parser/registration correctness fixes, not expansion into arbitrary CAD geometry. No `.NET` command has run.

## 2026-07-13 - Dominant-wall Electrical fix goal is active

- Active goal thread: `019f1a8b-bea0-7513-ac35-849b1cd734d3`.
- Objective: remove false bbox normalization, implement evidenced dominant-wall registration, preserve canonical recipe projection, and replace translation-invariant false-green verification with native-coordinate structural proof.
- Finite stop contract: one active phase, at most three evidence-bearing attempts per phase, no unchanged reruns, implementation completes at the external dotnet/Desktop/AutoCAD handoff instead of looping on forbidden proof.
- Full prompt: [[2026-07-13 - Finite goal prompt for dominant wall registration fix]].

## 2026-07-13 - Fresh final congruence proof is a false green

- `replaces`: [[2026-07-13 - Fresh final output congruence proof passed]] and the earlier Current State claim that manifest `b4e8192e70a146f3910a4fc77988b028` proved final congruence.
- Original dominant Floor and Electrical exterior wall pairs are both exactly `468"` wide after translation; scale is `1`.
- The Electrical DXF contains short fragments one inch outside each dominant edge. The pipeline selected those fragments as min/max, reported a false `470"` width, and applied `scaleX=0.9957446808508176`.
- Final dominant widths are Floor `464.400000"` versus Electrical `463.308510"`; Electrical is approximately `1.091490"` too narrow. AutoCAD overlay correctly exposes the mismatch.
- The final audit passed because its selected Electrical fringe envelope is `464.400000"`. It compared the same wrong reference before and after, so `FinalOutputCongruent` and the current `-RequireAutomatic` green result are not trustworthy for this case.
- Active bug: [[2026-07-13 - False two-inch Electrical outline normalization]].
- Proposed correction: [[2026-07-13 - Dominant wall registration and native overlay fix plan]]. The required contract is direct Floor/Electrical structural overlay in one canonical coordinate system, not translation-invariant bbox equality.
- Finite implementation goal with phase gates and explicit anti-overloop terminal states: [[2026-07-13 - Finite goal prompt for dominant wall registration fix]].

## 2026-07-13 - Raw comparison authority is D:\PointAIData\PLANS

- Original-vs-adjusted CAD analysis must use `D:\PointAIData\PLANS\originalFloorPlans\SEMINOLE2000.dxf` and `D:\PointAIData\PLANS\original electrical plans\ELECTRICAL PLAN SEMINOLE 2000.dxf` as the authoritative raw pair.
- Earlier source measurements were taken from the app-managed import snapshots in LocalAppData, not directly from D:.
- The managed FloorPlan is byte-identical to the D: original. The Electrical files have equal length (`569,896` bytes), but the D: original is currently locked, so byte identity has not yet been proven.
- Decision note: [[2026-07-13 - PointAIData DXFs are comparison authority]].

## 2026-07-13 - Latest canonical extraction lineage verified, manifest remains incomplete

- The canonical source behind the latest SEMINOLE flow is original file `SEMINOLE2000.dxf`, persisted as `SEMINOLE2000-21.dxf` with SHA-256 `2C8ACFBE120668844D803A0ACA482B8B75C8AA72A9536D35EB86C62C9C80C8A4`.
- The last successful extraction is `c2496c35-ad07-4846-a68c-6bcc34027d69` (`ixmilia-line-segments`, `Completed`, `2026-05-20T04:27:55.7853173Z`). A later recovery run `fc119d23-fe8f-4323-ac5e-8c9b849aee9a` was ignored and is not the active extraction.
- The latest adjusted canonical output is a different artifact: `C:\Users\lucas\Downloads\fresh final test.dxf`, adjustment `67d4f5e5-675e-44bb-9f7a-c29384a2e7b6`, manifest `b4e8192e70a146f3910a4fc77988b028`.
- Old build-workspace and durable LocalAppData copies of the managed source both exist and have identical length/hash, so no source-content drift was detected. The DB path is nevertheless stale because it still names the build workspace.
- Current observability gap: the manifest does not join original source, managed source/hash, active extraction, active curation, canonical version, adjustment and final export in one lineage record.
- Bug note: [[2026-07-13 - Canonical extraction lineage missing from latest manifest]].

## 2026-07-09 - Electrical patio top compression root cause is registration mismatch
- Latest runtime export `c9f3a5381daa4b64ab64165de74a4577/manifest.json` proves the remaining issue: FloorPlan applied 8/8 operations, but Electrical applied 6/8 and missed the two top vertical compressions.
- Local DXF inspection confirmed the Electrical patio exists, but Electrical max Y is about `961.577`, while canonical top pinch lines are `1000.460736` and `1001.669227`.
- Current Electrical registration is identity (`Scale=1`, `Rotation=0`, `TranslateX=0`, `TranslateY=0`), so Electrical points are compared against FloorPlan pinch coordinates without being aligned.
- This is a general N-plans problem, not a SEMINOLE-only patch: dependent sheets need real Electrical -> FloorPlan registration/anchors before reliable recipe replay.
- Note: [[2026-07-09 - Electrical patio top compression missed by identity registration]]
- A finite goal prompt for the next fix is documented in [[2026-07-09 - Goal prompt for Electrical registration fix]].
- The goal prompt now explicitly requires phase-gated evidence, generic structural anchors, cumulative edge-equivalent pinches, clear raw observability categories, and anti-loop stop rules.

## 2026-07-09 - Electrical edge-anchor fallback implemented, fresh export pending
- `ProjectedPlanSheetDxfExporter` now computes registered dependent-sheet anchor bounds from generic wall/exterior/structural layer families, falling back to all modelspace coordinates.
- When a canonical edge pinch lies outside those registered Electrical bounds, the exporter replays the operation at the equivalent dependent-sheet edge and records an audit reason containing `dependent-sheet edge anchor`.
- Human summary now explains when Electrical used the registered edge because the canonical pinch was outside the electrical bbox.
- Added `ExportAsync_anchors_out_of_bounds_top_recipe_to_electrical_wall_edge` regression coverage and `scripts/test-electrical-edge-anchor-contract.ps1`.
- Checks passed without build: human summary contract, manifest verifier self-check, edge-anchor contract, and `git diff --check`.
- Runtime proof is still pending: the desktop app must re-export because latest manifest `c9f3a538...` predates this code path.
- `verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic` now intentionally rejects stale manifests that still use the ambiguous Electrical `NoGeometryAffected` reason; current live verifier fails on `c9f3a538...` until a fresh export is produced.
- Read-only real-data dry run predicts the new edge-anchor logic will map the two top operations to Electrical effective coordinates `Y 961.577` and `Y 961.277`, with matching Electrical geometry present.
- Note: [[2026-07-09 - Electrical edge-anchor fallback]]

## 2026-07-09 - Electrical zone-sync goal blocked on fresh Desktop export
- Repo-allowed checks still pass: `test-plan-set-human-summary-contract.ps1`, `test-verify-latest-plan-set-recipe-manifest.ps1`, and `test-electrical-edge-anchor-contract.ps1`.
- Latest runtime manifest is still `c9f3a5381daa4b64ab64165de74a4577/manifest.json` from `2026-07-09 15:26:42`, so it predates the edge-anchor export path.
- Live verifier now fails intentionally because that stale manifest still contains the old ambiguous Electrical `NoGeometryAffected` reason.
- This is the same external blocker after repeated goal turns: Desktop must regenerate a new SEMINOLE compression export, then `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic` can prove or disprove the current code.

## 2026-07-09 - Electrical zone-sync runtime proof passed
- Fresh runtime manifest `763c5f00d4f14631ae570f2866da3f9c/manifest.json` passes `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.
- ElectricalPlan is `ProjectedAutomatically`, with entity evidence preserved: `3348` DXF entities, `93` INSERT, `17` DIMENSION, `4` ELLIPSE, `313` wire/curve carriers.
- Electrical projection audit now reports `8/8` operations `Applied`, `0` `NoGeometryAffected`, `2` top operations using `dependent-sheet edge anchor`, and both top operations applied.
- FloorPlan impact audit reports `8/8` operations applied, and the canonical FloorPlan DXF exists at `C:\Users\lucas\Downloads\ultimo test 3.dxf` with status `Exported`.
- DXF safety passed: output exists, `3425` entities before/after, `0` missing handles, `0` missing owners, `0` unsupported crossings.
- Raw summary now says Electrical applied all operations and explains that two operations used the registered edge because the canonical pinch was outside the electrical bbox.

## 2026-07-09 - Electrical edge-anchor CAD jitter fixed, re-export pending
- User visually caught that Electrical did not perfectly match the expected reduced silhouette even after `763c5f00...` passed operation-level verification.
- Geometry check confirmed a real precision issue: exported Electrical `ELECTRICAL WALLS` bbox height was `927.8"` while raw `930.000286"` minus expected `2.4"` should be about `927.600286"`.
- Root cause: one top wall line was `0.000286"` below the registered max edge, so strict edge comparison made it receive only one of the two top compressions.
- Fix: recipe application/audit now uses `0.01"` CAD coordinate tolerance around pinch lines.
- Static checks passed; fresh Desktop re-export is needed to produce the corrected DXF.
- Note: [[2026-07-09 - Electrical edge-anchor CAD jitter caused silhouette mismatch]]

## 2026-07-09 - ULTIMO TEST 4 proves height jitter fixed but exposes outline mismatch
- Fresh manifest `f407546184564e2f9dc70c7d53da0fd0/manifest.json` passes `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.
- User request in summary: `468" x 930"` -> `464.4" x 928.8"`; Electrical applied `8/8` operations and DXF safety is OK.
- Height is now correct: raw Electrical `ELECTRICAL WALLS` bbox height `930.000286"` -> exported `928.800286"`, matching the `1.2"` vertical shrink and FloorPlan `WALLS` height `928.800286"`.
- Width still explains the visual mismatch: raw Electrical `ELECTRICAL WALLS` bbox width is `470"`, exported width is `466.4"` after applying the `3.6"` shrink, while the canonical requested width is `464.4"`.
- Conclusion: recipe replay now applies deltas correctly, but exact overlay/silhouette requires a separate outline congruence/registration audit because the dependent Electrical sheet starts from a different envelope than the canonical FloorPlan.

## 2026-07-10 - Electrical outline congruence normalization implemented, fresh export pending
- Electrical recipe export now receives canonical source width/height from the FloorPlan input audit.
- Exporter derives dependent Electrical structural bounds from generic structural layer families and creates axis-specific outline normalization before applying the canonical recipe.
- For the known width case, expected scale is `468 / 470 = 0.995744680851`, so Electrical should normalize to `468"` before applying `3.6"` shrink, yielding `464.4"`.
- New artifact `outline-congruence-audit.json` is written and required by the verifier.
- Static/script checks passed; latest old runtime manifest `f407546...` correctly fails because it predates the new audit artifact.
- Note: [[2026-07-10 - Electrical outline congruence audit and normalization]]

## 2026-07-06 - c534 verifier and DXF structural proof pass after file unlock
- After closing the process that locked the DXF, `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic` passed against manifest `c5341f1042fc4dd093c8c4354ef0d898/manifest.json`.
- Proof: `CompressionRecipeSheets = 1`, `ElectricalStatus = ProjectedAutomatically`, Electrical DXF path exists and is `569896` bytes.
- Binary DXF structural scan succeeded: 3195 drawable entities; `LWPOLYLINE` count 41; zero `LWPOLYLINE` entities missing required identity/subclass evidence (`5`, `330`, `AcDbPolyline`).
- Repo-allowed checks also passed: `git diff --check` and `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`.
- Remaining product confirmation: user should open the latest Electrical DXF in AutoCAD and confirm visual display, because a previous verifier-passing file surfaced a CAD viewer issue.
## 2026-07-06 - Final verifier passes for c534, AutoCAD visual confirmation pending
- Fresh manifest `c5341f1042fc4dd093c8c4354ef0d898/manifest.json` satisfies the final script gate: `CompressionRecipeSheets = 1`, `ElectricalStatus = ProjectedAutomatically`, and recipe summary contains VerticalCompression operations.
- Electrical DXF path: `C:\Users\lucas\Downloads\fghftdjtdh-plan-set\ELECTRICAL PLAN SEMINOLE 2000-12-719fa2f0df394e84997d0e3f9afc1a27.dxf`.
- DXF exists and is non-empty (`569896` bytes). When re-verified from Codex, the file was locked by another process, likely AutoCAD, so byte-level re-read could not complete inside the agent.
- Because a previous verifier-passing DXF was invalid in AutoCAD, final completion still needs user confirmation that this newly exported Electrical DXF opens visually in AutoCAD.
## 2026-07-06 - Final verifier passed but AutoCAD invalid exposed LWPOLYLINE identity bug
- Runtime manifest `19595bf5a2f749118b9f7a3e2291d79e/manifest.json` passed `-RequireAutomatic`: CompressionRecipeSheets 1, ElectricalStatus ProjectedAutomatically, entity evidence preserved.
- User opened the Electrical DXF and AutoCAD reported/behaved as invalid black screen. Local inspection confirmed the file exists and is non-empty AutoCAD Binary DXF.
- Root cause evidence: the generated `LWPOLYLINE` replacements for crossing ARC/CIRCLE were missing group `5` handle and group `330` owner, while the original ARC/CIRCLE entities had them.
- Fix: preserve `5` and `330` when converting crossing ARC/CIRCLE to LWPOLYLINE; tests now assert those pairs survive.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed.
- Still not final: re-export again and manually open the new Electrical DXF in AutoCAD; verifier passing alone is not enough for completion after this bug.
- Note: [[2026-07-06 - Generated LWPOLYLINE identity fix]]
## 2026-07-06 - Electrical CIRCLE crossing support implemented, runtime re-export pending
- Fresh compression export `2cf2ae537a974e41bdd1ae11212b7594/manifest.json` proved the previous ARC blocker moved forward and exposed the next blocker: `CIRCLE crosses a canonical recipe pinch line`.
- The user output folder `C:\Users\lucas\Downloads\testYALOHIZE-plan-set` was empty because the FloorPlan exported separately as `testYALOHIZE.dxf`, while Electrical was downgraded to manual and therefore had no StoragePath.
- Implemented the same safe strategy for crossing CIRCLE: convert only crossing CIRCLE to a closed sampled LWPOLYLINE, then let existing recipe-aware point projection deform those vertices.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed.
- Still not final: restart/reload app and create another fresh SEMINOLE compression export, then run the verifier with -RequireAutomatic.
- Note: [[2026-07-06 - Electrical CIRCLE crossing polyline export]]
## 2026-07-06 - Fresh re-export f116 is automatic but affine-only
- User generated fresh package `f11616387daf49b989ddba1de7b6e862/manifest.json` after the ARC-crossing fix.
- This proves a fresh ElectricalPlan export can be `ProjectedAutomatically` and writes `C:\Users\lucas\Downloads\nuevotest4-plan-set\ELECTRICAL PLAN SEMINOLE 2000-12-3f943fc32f4646aa87fdb300bbd0161f.dxf`.
- However `CompressionRecipeSheets = 0` and RecipeHandlingSummary says `affine placement applied; no local compression operations`, so it is not the final local compression proof.
- `-AllowAffineOnly` passes with entity evidence: INSERT 93, DIMENSION 17, ELLIPSE 4, wire/curve 311.
- Next proof must generate a FloorPlan adjustment that actually says HorizontalCompression/VerticalCompression / recortar patio-porche-living before export.
## 2026-07-06 - Active goal blocked waiting for fresh runtime re-export
- Third consecutive goal continuation still reads the same latest manifest `0afa3c787fb34a98b63c10ba175913e3/manifest.json`, last written 2026-07-06 21:02:54.
- ElectricalStatus remains `RequiresManualConfirmation` with warning `ARC crosses a canonical recipe pinch line`.
- Code-side ARC crossing support has already been implemented and repo-allowed checks passed, but the active goal Definition of Done requires a fresh runtime manifest proving `ProjectedAutomatically` with `-RequireAutomatic`.
- No further code change is aligned until the desktop app regenerates a new SEMINOLE compression export after the ARC fix.
## 2026-07-06 - Electrical ARC crossing support implemented, runtime re-export pending
- Implemented the minimal safe path for Electrical ARC entities crossing canonical FloorPlan recipe pinch lines: convert only crossing ARC to sampled LWPOLYLINE, then let existing recipe-aware point projection deform those vertices.
- Non-crossing ARC remains semantic ARC; CIRCLE/ELLIPSE crossing still requires manual review.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed.
- Still not final: the app must regenerate a fresh SEMINOLE compression export, then the verifier must pass with -RequireAutomatic.
- Note: [[2026-07-06 - Electrical ARC crossing polyline export]]
## 2026-07-06 - Fresh compression export blocks on real ARC crossing
- Fresh runtime manifest 0afa3c787fb34a98b63c10ba175913e3/manifest.json now has CompressionRecipeSheets 1, so the previous affine-only blocker is resolved.
- Electrical still downgraded to RequiresManualConfirmation because an ARC crosses a canonical recipe pinch line.
- Meaning: the system correctly refused to automatically export a curved electrical entity that crosses a local compression boundary, because projecting only center/radius would distort the CAD semantics.
- Next implementation slice, if automatic final proof is required: support crossing ARC recipe projection safely, likely by tessellating only crossing ARC entities into projected LWPOLYLINE geometry, or keep manual review if visual fidelity cannot be guaranteed.
- Note: [[2026-07-05 - Goal prompt final FloorPlan Electrical compression proof]]
## 2026-07-05 - Goal blocked after third affine-only runtime check
- Third consecutive active-goal check still finds latest manifest 1ca1c1fe9f8f4837a9b4135590cdab19/manifest.json as affine-only.
- Evidence: verifier passes only with -AllowAffineOnly; ElectricalStatus is ProjectedAutomatically; CompressionRecipeSheets is 0; RecipeHandlingSummary says no local compression operations; Electrical entity evidence is preserved.
- This satisfies the anti-loop blocked threshold for same_affine_only / external app action needed.
- Required unblock: create a fresh desktop app export whose FloorPlan adjustment produces HorizontalCompression or VerticalCompression, then rerun the verifier without -AllowAffineOnly and with -RequireAutomatic.
- Note: [[2026-07-05 - Goal prompt final FloorPlan Electrical compression proof]]
## 2026-07-05 - Compression manifests are stale; post-fix latest exports are affine-only
- Rechecked the active goal state: latest manifest 1ca1c1fe9f8f4837a9b4135590cdab19/manifest.json still passes only with -AllowAffineOnly and has CompressionRecipeSheets 0.
- Recent history shows the last real compression manifests are older and pre-final entity support: 7ea... blocked on HATCH, d957... blocked on 3DFACE, e814... blocked on SOLID.
- Since HATCH/3DFACE/SOLID support was added after those compression exports, they are stale evidence. A fresh compression export from the desktop app is required to prove the current code path.
- No code change is aligned while latest is affine-only; next action remains external app export with HorizontalCompression or VerticalCompression.
- Note: [[2026-07-05 - Goal prompt final FloorPlan Electrical compression proof]]
## 2026-07-05 - Latest goal check is affine-only, external compression export needed
- Latest runtime manifest 1ca1c1fe9f8f4837a9b4135590cdab19/manifest.json passes the verifier with -AllowAffineOnly.
- Evidence: ElectricalStatus ProjectedAutomatically; Electrical entity evidence preserved (INSERT 93, DIMENSION 17, ELLIPSE 4, wire/curve 311); CompressionRecipeSheets is 0.
- Classification against the active goal: affine-only smoke OK, but not final FloorPlan -> Electrical local compression proof.
- No code should be changed for this state; the next required action is a desktop app export where the FloorPlan adjustment actually creates HorizontalCompression or VerticalCompression.
- Note: [[2026-07-05 - Goal prompt final FloorPlan Electrical compression proof]]
## 2026-07-05 - Goal prompt ready for final FloorPlan Electrical compression proof
- Created a finite anti-loop goal prompt for finishing the remaining FloorPlan -> Electrical local compression proof.
- Current truth: affine-only Electrical package export works as smoke, but final proof needs a real HorizontalCompression/VerticalCompression export from SEMINOLE and ProjectedAutomatically Electrical output.
- Stop rule: complete only with verifier evidence; block instead of looping when the same blocker repeats three times or when desktop/CAD user action is required.
- Note: [[2026-07-05 - Goal prompt final FloorPlan Electrical compression proof]]
## 2026-07-05 - Runtime verifier distinguishes affine smoke from local compression proof
- Latest manifest bd661da5e4714e8ea22fad99a11b0874/manifest.json is ProjectedAutomatically but affine-only: RecipeHandlingSummary says no local compression operations, so it is not the final FloorPlan pinch sync proof.
- The exported ElectricalPlan DXF is AutoCAD Binary DXF. The verifier previously only understood text DXF, so -AllowAffineOnly falsely failed on a valid binary export.
- Minimal fix: scripts/verify-latest-plan-set-recipe-manifest.ps1 now detects binary DXF sentinel and counts null-delimited entity names for the same symbol/dimension/ellipse/wire evidence gates.
- Added verifier self-check coverage for an automatic binary compression manifest.
- Checks run without build: scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed; scripts/verify-latest-plan-set-recipe-manifest.ps1 -AllowAffineOnly passed against the latest runtime manifest; git diff --check passed.
- Still not final: re-export a real patio/porch/living compression case, then run the verifier without -AllowAffineOnly and finally with -RequireAutomatic.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - SEMINOLE runtime now blocks on SOLID after 3DFACE support
- Fresh manifest e8142f5b7a414cff93190f74c220c90a/manifest.json proves the 3DFACE blocker moved forward; the new blocker is SOLID is not supported for recipe-aware electrical export.
- Inspected runtime DXF content: SOLID entities appear as AcDbTrace-style filled shapes with vertex XY pairs 10/20, 11/21, 12/22, 13/23 and separate Z values 30/31/32/33.
- Minimal fix: SOLID is now included in the recipe-aware Electrical supported entity set; XY vertex pairs are projected through the canonical recipe while Z values are preserved.
- Added infrastructure coverage for SOLID vertices and kept unknown coordinate entities guarded via the LEADER rejection test.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed.
- Still not final: app must be restarted/reloaded and SEMINOLE re-exported again, then pass scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - SEMINOLE runtime now blocks on 3DFACE after HATCH support
- Fresh manifest d9579f448ff34e5fad76264bf941c8f4/manifest.json proves the HATCH blocker moved forward; the new blocker is 3DFACE is not supported for recipe-aware electrical export.
- Inspected runtime DXF content: 3DFACE stores vertex XY pairs as 10/20, 11/21, etc., with Z values (30/31) separate.
- Minimal fix: 3DFACE is now included in the recipe-aware Electrical supported entity set; XY vertex pairs are projected through the canonical recipe while Z values are preserved.
- Added infrastructure coverage for 3DFACE vertices and kept unknown coordinate entities guarded via the LEADER rejection test.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed.
- Still not final: app must be restarted/reloaded and SEMINOLE re-exported again, then pass scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - SEMINOLE runtime now blocks on HATCH, not POINT
- Fresh SEMINOLE export 7ea8135315974dabb5f094344e68558d/manifest.json proves the previous POINT blocker is gone.
- New runtime blocker: HATCH is not supported for recipe-aware electrical export.
- Minimal fix: HATCH is now included in the recipe-aware Electrical supported entity set so its coordinate pairs flow through Electrical source -> FloorPlan recipe -> output like other safe point geometry.
- Added infrastructure coverage for simple HATCH boundary points and kept the unknown-coordinate guard by moving the rejection test to LEADER.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed.
- Still not final: app must pick up this code and SEMINOLE must be re-exported again, then pass scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - Final automatic verifier also requires recipe-aware summary
- Strengthened the final -RequireAutomatic verifier: an automatic ElectricalPlan is not enough; its RecipeHandlingSummary must explicitly mention
ecipe-aware DXF export.
- Why: final goal proof must show the Electrical DXF used the canonical FloorPlan recipe path, not only that a DXF file exists with preserved entities.
- Self-check updated with a failing ProjectedAutomatically manifest that has compression text but no recipe-aware summary.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed; live -RequireAutomatic still fails as expected against stale manual runtime output.
- Still not final: SEMINOLE must be re-exported from the app and then pass scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - Final Electrical proof requires ProjectedAutomatically gate
- Added -RequireAutomatic to scripts/verify-latest-plan-set-recipe-manifest.ps1 so final goal verification cannot pass on the safe manual fallback path.
- Default verifier still accepts RequiresManualConfirmation as a safety proof, but final SEMINOLE proof now runs with -RequireAutomatic and requires ElectricalStatus: ProjectedAutomatically plus DXF entity evidence.
- Self-check updated: automatic compression passes with -RequireAutomatic; manual compression fails with -RequireAutomatic.
- Current runtime manifest correctly fails the final gate because it is still the stale manual export cc8f919791a940689fe04342e010f3d4/manifest.json.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed; live -RequireAutomatic failed as expected against stale manual runtime output.
- Next step: confirm/re-export existing manual package or create a fresh adjusted SEMINOLE export, then run the verifier with -RequireAutomatic.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - Recipe-ready wording is scoped to Electrical only
- Found and fixed a scope leak: changing AdjustmentRecipeSummaryDto.ToSheetReviewSummary(...) would also affect Roof/Facade wording, which is outside the current FloorPlan -> Electrical goal.
- Reverted the common DTO to its conservative local recipe requires review before DXF deformation wording.
- Electrical now converts that wording locally in ProjectElectricalSheetAdjustmentHandler.BuildRecipeHandlingSummary(...) to
ecipe-aware DXF export will apply canonical operations.
- Why: Electrical has recipe-aware DXF export support in this slice; Roof/Facade remain outside scope and should not receive optimistic wording from a shared contract helper.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed.
- Still not final: runtime SEMINOLE export must be regenerated from the app to prove ProjectedAutomatically.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - Electrical compression projections can reach ReadyForExport
- Verified current goal state: the active goal is not complete yet; the latest runtime manifest on disk is still an old/manual export.
- ProjectElectricalSheetAdjustmentHandler no longer blocks high-confidence confirmed Electrical projections just because the canonical FloorPlan recipe has compression/pinch operations.
- New rule: Electrical can be ReadyForExport when registration is confirmed and confidence is above the auto-export threshold; unsafe geometry is caught later by the recipe-aware DXF exporter and downgraded to RequiresManualConfirmation.
- Why: the goal requires proving automatic FloorPlan -> Electrical local-pinch sync when entities are supported; forcing all compression recipes to manual made that proof impossible.
- Checks run without build: git diff --check passed; scripts/test-verify-latest-plan-set-recipe-manifest.ps1 passed.
- Live verifier still reports the stale cc8f919791a940689fe04342e010f3d4/manifest.json as RequiresManualConfirmation with old POINT warnings, so SEMINOLE must be re-exported from the app before final proof.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - POINT entities are supported in recipe-aware Electrical export
- Runtime manifest showed the automatic Electrical export was blocked by `POINT is not supported for recipe-aware electrical export`.
- `POINT` is now treated as a safe point entity: its `10`/`20` coordinates flow through the same Electrical source -> FloorPlan recipe -> output projection as other point geometry.
- Added infrastructure coverage: `ExportAsync_applies_electrical_recipe_projection_to_point_entities` expects a POINT after a right-side compression to move from `(60,10)` to `(216,220)`.
- Checks run without build: `git diff --check` passed; `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` passed.
- Still not final: latest manifest on disk was created before this change; SEMINOLE must be re-exported from the app to prove `ProjectedAutomatically` with DXF entity evidence.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - Latest runtime manifest verifies safe manual fallback for Electrical recipe export
- `scripts/verify-latest-plan-set-recipe-manifest.ps1` now passes against latest runtime manifest `cc8f919791a940689fe04342e010f3d4/manifest.json`.
- Result is `Status: RequiresManualConfirmation`, `ElectricalStatus: RequiresManualConfirmation`, not automatic export.
- Reason: Electrical recipe-aware export hit unsupported `POINT` geometry and correctly refused to write a projected Electrical DXF automatically.
- This proves the safety invariant: unsafe Electrical geometry becomes manual review instead of broken CAD. It does not yet prove full automatic Electrical displacement/preservation because `ElectricalStoragePath` is empty for manual status.
- Next minimal step to complete automatic proof: support or explicitly classify safe `POINT` entities in recipe-aware Electrical DXF export, then re-export SEMINOLE and require `ProjectedAutomatically` with DXF entity evidence.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - Successful recipe-aware Electrical export sanitizes stale review summary
- Found a real stale-data path: old `ReadyForExport` Electrical projections can still carry `local recipe requires review before DXF deformation` even after the exporter becomes recipe-aware.
- `ExportProjectedPlanSheetHandler` now rewrites that stale phrase after a successful recipe-aware export to `recipe-aware DXF export applied canonical operations`, persists the projection, and saves the unit of work.
- Why: the package audit reloads the projection from SQLite before writing the manifest, so a fresh SEMINOLE re-export can now produce a clean manifest without requiring manual reconfirmation just to clean old text.
- Checks run without build: `git diff --check` passed; `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` passed.
- Still not final: the existing latest runtime manifest remains stale until the desktop app re-exports SEMINOLE.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - Runtime manifest is still stale; explicit PlanSet mismatch is covered
- Latest runtime verifier against the current app workspace failed on `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/exports/plan-sets/92e21933ac104fcd8dbd8dca50c398b8/manifest.json` because ElectricalPlan is `ProjectedAutomatically` but still says `recipe requires review before DXF deformation`.
- Meaning: the final SEMINOLE proof is still missing; a fresh app re-export is required after the recipe-aware export changes.
- Added regression coverage for explicitly selected dependent projections from another `PlanSetVersionId`, matching the existing canonical-adjustment mismatch guard.
- Checks run without build: `git diff --check` passed; `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` passed.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - Explicit dependent projection ownership is validated before export
- Package export now rejects explicitly selected dependent projections whose `PlanSetVersionId` or `CanonicalAdjustmentId` does not match the requested HousePlanSet export.
- Why: the audit layer already validates ownership, but export must not write an Electrical DXF before that later validation catches a wrong projection.
- Scope: Application layer only, `ExportMultiSheetPlanSetPackageHandler.ResolveExportableProjectionsAsync`.
- Still not final: SEMINOLE runtime re-export remains required.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]
## 2026-07-05 - FloorPlan to Electrical local pinch sync goal refined
- Current design truth: ElectricalPlan should not run its own adjustment engine and should not receive a global rescale. It must replay the canonical FloorPlan adjustment recipe in FloorPlan coordinates through its Electrical->Floor registration.
- Required regeneration source: Electrical original DXF + SheetRegistrationTransform + canonical FloorPlan recipe.
- Stop rule: every loop must produce evidence; after 3 failed attempts on the same symptom, stop with root-cause report instead of blind fixes.
- Note: [[2026-07-05 - Goal prompt FloorPlan Electrical local pinch sync]]
## 2026-07-04 - Package export audits explicit manual projections safely
- Current implementation truth: when a dependent projection is explicitly selected for package export but is not `ReadyForExport`, `ExportMultiSheetPlanSetPackageHandler` sends it directly to audit as manual instead of trying to export and aborting.
- This reinforces the invariant: unsafe Electrical projections stay `RequiresManualConfirmation`, not broken package exports.
- Still not final: SEMINOLE runtime re-export remains required.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Projected Electrical manifests reject manual-review contradictions
- Confirmation after export-time manual downgrade rewrites `manual review required` to `manual review completed`.
- Runtime verifier rejects `ProjectedAutomatically` Electrical sheets that still say `manual review required`.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Electrical registration rotation normalizes trig noise
- `ElectricalRecipeProjection.ProjectPoint(...)` has coverage for rotated Electrical -> Floor registration before applying the canonical FloorPlan recipe.
- Tiny trigonometric near-zero noise is clamped in both the Application helper and Infrastructure DXF curve guard.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Runtime verifier checks Electrical entity classes
- For `ProjectedAutomatically` ElectricalPlan exports, `scripts/verify-latest-plan-set-recipe-manifest.ps1` checks key DXF entity classes, not just total drawable count.
- Required evidence includes `INSERT`, `DIMENSION`, `ELLIPSE`, and at least one wire/curve carrier from `LWPOLYLINE`/`POLYLINE`/`SPLINE`/`ARC`.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Runtime verifier requires CanonicalAdjustmentId
- Runtime verifier rejects manifests missing a valid root `CanonicalAdjustmentId` and prints it in output.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Recipe-aware Electrical export guards ellipse crossings
- `ELLIPSE` entities that cross a canonical FloorPlan pinch line require manual review.
- Non-crossing ellipses keep semantics: `10`/`20` center gets recipe-aware point projection; `11`/`21` major-axis vector stays affine/vector-only.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Recipe-aware Electrical export protects dimension-block curves
- `CIRCLE`/`ARC` crossing guard scans referenced anonymous `DIMENSION` graphic blocks, not only modelspace entities.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Recipe-aware Electrical export rejects unknown coordinate entities
- Unknown coordinate-bearing entities degrade to manual review instead of being projected blindly.
- Supported recipe-aware entities: `LINE`, `LWPOLYLINE`, `SPLINE`, `INSERT`, `TEXT`, `MTEXT`, `CIRCLE`, `ARC`, `ELLIPSE`, `DIMENSION`.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Recipe-aware Electrical export preserves text semantics
- `TEXT`/`MTEXT` insertion points move through the canonical FloorPlan recipe.
- Text alignment/direction values stay affine vectors so local pinches do not deform text.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Unsafe Electrical recipe export downgrades to manual
- Unsupported crossing geometry raises `ProjectedPlanSheetManualReviewRequiredException`.
- Export handler persists projection status `RequiresManualConfirmation`, warning, and recipe summary.
- Multi-sheet package audit continues instead of aborting.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Electrical recipe-aware export slice implemented partially
- Electrical projected DXF export can receive canonical recipe context and project entity points through `Electrical source -> FloorPlan source -> recipe -> site/export`.
- Pure helper: `ElectricalRecipeProjection.ProjectPoint(...)`.
- Export handler loads confirmed registration and canonical adjustment recipe for ready Electrical projections with compression steps.
- Safety boundary: no Roof/Facade code changed; no destructive DXF filters added.
- Still not final: SEMINOLE runtime re-export remains required.
- Note: [[2026-07-04 - Electrical recipe-aware export slice]]

## 2026-07-04 - Current blocker to final goal completion
- Code/static evidence has advanced, but the full goal cannot be marked complete until SEMINOLE is re-exported from the app and `scripts/verify-latest-plan-set-recipe-manifest.ps1` passes against the new runtime manifest.

## 2026-07-08 - HousePlanSet observability audit artifacts wired
- HousePlanSet package exports now write an `audit/` folder next to `manifest.json` with six JSON artifacts: input, canonical recipe, floorplan impact, electrical registration, electrical projection, and DXF safety.
- Electrical projected DXF export now returns operation-level audit evidence: affected entities, affected vertices, measured deltas, and Applied/NoGeometryAffected status per canonical recipe operation.
- DXF safety evidence now includes output file existence/bytes, entity counts, INSERT/DIMENSION/ELLIPSE/wire counts, and missing handle/owner counts.
- Runtime verifier now requires the six audit artifacts and fails `-RequireAutomatic` if Electrical projection audit contains `Failed` operations.
- Limitation: explicit user dimensions (e.g. 39 -> 38.7 / 77.5 -> 77.4) are not persisted in the current export contract; input audit honestly marks them unavailable and derives deltas from the canonical recipe.
- Verification without build: `scripts/test-plan-set-observability-audit-files.ps1`, `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`, and `git diff --check` passed.
- Note: [[2026-07-08 - HousePlanSet observability audit artifacts]]

## 2026-07-08 - Anti-loop goal prompt for HousePlanSet observability finish
- Defined the finite goal shape for finishing FloorPlan -> Electrical projection/observability: explicit scope, proof gates, stop rules, and blocker rules.
- The next loop must stop on verified Definition of Done, not continue refactoring without new failing evidence.
- Scope remains FloorPlan + Electrical only; FloorPlan is canonical and Electrical receives the canonical placement/recipe.

## 2026-07-08 - HousePlanSet input audit dimensions
- Added captured input dimensions to the HousePlanSet audit path: original/requested width/height and required width/height deltas in inches.
- `input-audit.json` now writes `dimensionsInches` from `AdjustedSitePlanPlacementDto.InputAudit`; the verifier rejects manifests without it.
- Added `scripts/test-plan-set-input-audit-contract.ps1` and kept repo-allowed checks green. No build/test was run due AGENTS.md.
- Latest old runtime manifest still fails because it predates the audit folder; fresh desktop re-export is required.

## 2026-07-08 - Electrical projection operation audit gate
- Strengthened `verify-latest-plan-set-recipe-manifest.ps1`: compression exports now fail if `electrical-projection-audit.json` has fewer operation entries than `canonical-recipe-audit.json.operationCount`.
- Added verifier self-test coverage for missing Electrical projection operation audit.
- This closes the silent-failure class where the FloorPlan had compression operations but Electrical had no per-operation evidence.

## 2026-07-08 - FloorPlan impact audit rows
- `AdjustedSitePlanPlacementDto` now carries `FloorPlanImpactAudit` rows.
- Auto-fit records per-operation FloorPlan impact while applying compression: affected entities, affected vertices, measured delta, and status.
- `floorplan-impact-audit.json` now uses persisted impact rows, not the old placeholder.
- The verifier rejects compression manifests when FloorPlan impact operation rows are missing compared with canonical recipe operation count.
- Latest old runtime manifest still fails because it predates the audit folder; fresh desktop export is required.

## 2026-07-08 - HousePlanSet human summary
- `manifest.json` now includes `HumanSummary` lines through `MultiSheetExportAuditDto`.
- The summary reports FloorPlan operations applied, Electrical operations applied, affected entities/vertices, and warnings/failures.
- The Desktop audit panel now shows those lines before the per-sheet rows.
- Added `scripts/test-plan-set-human-summary-contract.ps1` and kept repo-allowed checks green; no build/test was run.
- Latest runtime manifest is still stale and must be regenerated from the app before final goal completion.

## 2026-07-08 - DXF safety audit verifier gate
- `verify-latest-plan-set-recipe-manifest.ps1` now validates the ElectricalPlan row inside `audit/dxf-safety-audit.json` for `ProjectedAutomatically` exports.
- The verifier rejects unsafe audit evidence: missing/empty output, missing entity counts/classes, missing handles/owners, or unsupported crossing entities.
- Added a self-check case where a valid-looking DXF fails because `dxf-safety-audit.json` reports `UnsupportedCrossingEntityCount = 1`.
- Latest runtime manifest is still stale and must be regenerated from the app before final goal completion.

## 2026-07-08 - Electrical projection operation audit verifier gate
- `verify-latest-plan-set-recipe-manifest.ps1` now rejects incomplete ElectricalPlan projection operation rows.
- Electrical operation audit rows must include operation identity, recipe coordinates/delta, affected entity/vertex counts, measured delta range, and a valid status.
- Non-`Applied` Electrical operations must include a reason, closing the silent no-op gap.
- Added a self-check fixture where `Status`-only Electrical operations fail.
- Latest runtime manifest is still stale and must be regenerated from the app before final goal completion.

## 2026-07-08 - Electrical canonical operation index verifier gate
- `verify-latest-plan-set-recipe-manifest.ps1` now rejects Electrical projection audits that do not cover every canonical operation index exactly once.
- This prevents a false pass where Electrical reports the right number of rows but duplicates one operation and silently omits another, including vertical compression operations.
- Added a self-check fixture for duplicated `OperationIndex = 0` with missing canonical operation `1`.
- Latest runtime manifest is still stale and must be regenerated from the app before final goal completion.

## 2026-07-08 - Canonical recipe operations verifier gate
- `verify-latest-plan-set-recipe-manifest.ps1` now rejects `canonical-recipe-audit.json` files that expose `operationCount` without listing matching `recipe.Operations`.
- Canonical recipe operations must include kind, axis, edge, coordinate, and delta source units.
- Added a self-check fixture for missing canonical recipe operations.
- PowerShell single-item JSON arrays are forced to `[object[]]` in the verifier so `.Count` is reliable.
- Latest runtime manifest is still stale and must be regenerated from the app before final goal completion.

## 2026-07-08 - FloorPlan impact status warning verifier gate
- `verify-latest-plan-set-recipe-manifest.ps1` now rejects FloorPlan impact operations missing `Status`.
- FloorPlan impact status must be `Applied` or `NoGeometryAffected`.
- A FloorPlan operation with zero affected vertices must include a warning, closing the silent no-op gap.
- Added a self-check fixture for `NoGeometryAffected` without warning.
- Latest runtime manifest is still stale and must be regenerated from the app before final goal completion.

## 2026-07-08 - Anti-overloop goal framing
- Current HousePlanSet observability work should be run with finite goals: closed FloorPlan + Electrical scope, explicit audit artifacts, verifier gates, and hard stop states.
- A goal is complete only with passing allowed script checks plus a fresh runtime export proof; stale manifests are not valid final proof.
- Anti-loop rule: do not rerun the same check after the same failure unless a file, fixture, export, or hypothesis changed. Stop and report a blocker if the same failure repeats without new evidence.

## 2026-07-09 - ExportProjectedPlanSheetHandler compile fix
- Fixed CS0103 in `ExportProjectedPlanSheetHandler`: `exportAudit` is now declared outside the `try` before being returned in `ExportProjectedPlanSheetResponse`.
- Root cause was C# block scope, not CAD/export logic.
- No build was run per AGENTS.md; dotnet watch should pick up the changed file in the user's running session.

## 2026-07-09 - HousePlanSet canonical audit context pass-through
- Real SEMINOLE export `ba64fb54686c44ee8d40e86eb034b7a4` proved package audits could be created with `input-audit.dimensionsInches = null`, `canonical-recipe.recipe = null`, and empty FloorPlan impact operations, even while Electrical projection had 8 operations.
- Root cause: package export request did not carry the in-memory canonical placement/recipe from Desktop to audit creation; repository fallback returned null for the real run.
- Fix: pass optional `CanonicalPlacement` and `CanonicalRecipe` through package export/audit requests and use them before repository fallback.
- UI human summary now reports operation warnings/failures instead of saying none when Electrical operation warnings exist.
- Fresh Desktop re-export is required; old manifests cannot prove the fix.

## 2026-07-09 - HousePlanSet observability runtime proof passed
- Latest runtime manifest `39a5188e145a4e899723235c7272b07e/manifest.json` passed `verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.
- SEMINOLE export captured input dimensions: original `468 x 930` inches, requested `464.4 x 927.6`, required delta `3.6 x 2.4`.
- Canonical recipe has `8` operations and FloorPlan impact audit has `8` operations.
- UI summary reports FloorPlan `8/8` applied, Electrical `6/8` applied, and `2` Electrical warnings/failures.
- The two Electrical non-applied operations are explicit `NoGeometryAffected` Top vertical compressions with reasons, so they are no longer silent.
- DXF safety passed with ProjectedAutomatically Electrical export.

## 2026-07-09 - HousePlanSet raw AI summary
- UI audit panel now uses `HumanSummary` as raw explanatory text instead of dumping `RecipeHandlingSummary`, sheet storage paths, and full manifest paths.
- Summary explains: what the user asked to shrink, what FloorPlan shrank, what Electrical received/applied, what did not shrink and why, DXF safety, and missing-data warnings.
- Technical detail remains in manifest/audit JSON files for verification.

## 2026-07-09 - HousePlanSet summary polish
- UI summary now uses `Resumen AI` instead of `AI raw`.
- Known Electrical no-geometry reason is translated in UI while raw reason remains available in JSON.
- DXF safety and quality report lines are now concise Spanish text instead of mixed English technical dumps.

## 2026-07-10 - Electrical outline congruence normalization awaiting runtime proof
- ElectricalPlan export now has code and verifier coverage for outline congruence normalization before recipe replay.
- The exporter refuses to call fallback/all-entity bounds congruent; missing structural outline data becomes `InsufficientData`.
- Repo-allowed scripts pass; latest manifest `f407546...` is stale and missing `outline-congruence-audit.json`.
- Next required proof: fresh Desktop export, then run `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.

## 2026-07-10 - Nullable decimal compile fix
- Fixed CS0173 in `ProjectedPlanSheetDxfExporter.cs` by declaring outline mismatch locals as `decimal?`.
- User's running `dotnet watch` should rebuild after the file change.

## 2026-07-10 - PlacementJson export crash fixed
- Fixed runtime crash in `ExportProjectedPlanSheetHandler.BuildExportRecipeAsync` where System.Text.Json could not deserialize `AdjustedSitePlanPlacementDto` due multiple constructors.
- The handler now reads only `InputAudit.OriginalWidthInches` / `OriginalHeightInches` from `PlacementJson` via `JsonDocument`, preserving outline congruence data without constructing the DTO.
- User should restart/rebuild `dotnet watch` and retry a fresh package export.

## 2026-07-10 - JsonElement out parameter compile fix
- Fixed CS0177 in `ExportProjectedPlanSheetHandler.TryGetProperty` by assigning `value = default` on the false path.
- User's running `dotnet watch` should rebuild after the file change.

## 2026-07-10 - FloorPlan to Electrical outline congruence goal proven
- Fresh runtime export `ULTIMO TEST 8` produced manifest `bcab33898c764363b96f10fa20a3263f` and passed `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.
- Electrical outline started `2"` wider than canonical FloorPlan, was normalized before recipe, then exported with width mismatch `0"` and height mismatch `0.000286"` within tolerance.
- FloorPlan and Electrical both applied `8/8` canonical operations; DXF safety passed with no missing handles/owners and no unsupported crossings.
- This closes the FloorPlan -> ElectricalPlan outline congruence slice for the tested case; Roof/Facade remain out of scope.

## 2026-07-10 - Segment congruence gate implemented, fresh export pending
- Added `outline-segment-congruence-audit.json` to compare structural FloorPlan vs Electrical wall segments, not just bbox.
- The verifier now requires segment congruence and fails automatic proof unless status is `SegmentCongruent`.
- Static/contract checks pass; latest `ULTIMO TEST 8` manifest is stale for this new artifact and fails until a fresh export is created.
- Next required proof: export `ULTIMO TEST 9` from Desktop and run `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.

## 2026-07-10 - Segment congruence goal blocked on fresh export
- Segment-congruence code/checks are wired, but runtime proof is blocked because latest manifest remains `bcab33898c764363b96f10fa20a3263f` from `ULTIMO TEST 8`.
- The latest manifest predates `outline-segment-congruence-audit.json`; verifier correctly fails until a fresh Desktop export is created.
- Required next action: export `ULTIMO TEST 9` and run `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.

## 2026-07-10 - ULTIMO TEST 9 exposed raw wall-edge segment false positive
- Fresh runtime export `03ee564a6d4a477c92badd8307ed4305/manifest.json` produced `outline-segment-congruence-audit.json`, so the stale-export blocker is resolved.
- The verifier correctly failed `-RequireAutomatic`, but inspection showed the segment audit was comparing raw `WALLS` vs `ELECTRICAL WALLS` drafting edges too literally: FloorPlan had `125` segments, Electrical had `282`, and many mismatches were small wall-line offsets/fragments rather than proven recipe failure.
- Fix: segment audit now normalizes paired wall edges into structural wall centerline runs, merges collinear fragments, reports true total mismatch counts separately from the first 20 samples, and keeps verifier blocking unless centerline runs are congruent.
- Fresh Desktop re-export is required because `ULTIMO TEST 9` was generated before this centerline-run audit change.

## 2026-07-10 - ULTIMO TEST 10 moved segment audit to outline coverage
- Fresh runtime export `f2df4b2006c04164afd03b5a009e2755/manifest.json` proved the new artifact is being written, but the gate still failed: `SegmentMismatchRequiresManualReview`, `missing=22`, `extra=116`.
- Inspection showed the remaining failure is not safe evidence of bad FloorPlan->Electrical deformation: the audit was still treating every internal/drafting wall-run difference as automatic blocker, while Electrical has many more structural wall fragments than FloorPlan (`183` vs `92`).
- Fix: the segment gate now blocks only on required canonical outline coverage missing from Electrical; internal FloorPlan/Electrical wall-run differences remain observable as advisory counts and samples.
- Fresh Desktop re-export is required because `ULTIMO TEST 10` was generated before this outline-coverage gate change.

## 2026-07-10 - UI segment congruence summary reads live audit JSON
- The Desktop audit panel no longer leaves segment evidence as a vague `revisar outline-segment-congruence-audit.json` line when the artifact exists.
- `SitePlanAdjustmentViewModel` now reads `audit/outline-segment-congruence-audit.json` from the package manifest folder and replaces the generic line with a human summary: OK / NO OK / insufficient data, required outline runs, missing outline runs, and advisory internal wall-run differences.
- This keeps raw details in JSON but makes the UI explain what the verifier is enforcing.
- Repo-allowed checks passed; fresh Desktop export is still required to produce a new audit with the outline-coverage gate.

## 2026-07-10 - Verifier now rejects stale segment audit shape
- The runtime verifier now requires the new outline-coverage segment audit fields: `ComparisonMode`, `RequiredOutlineSegmentCount`, and advisory internal wall-run counts.
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1` fixtures were updated so passing automatic manifests include `StructuralOutlineCoverageWithWallRunAdvisory` and required outline evidence.
- Latest runtime manifest remains `f2df4b2006c04164afd03b5a009e2755` from `ULTIMO TEST 10`; it now fails earlier because its segment audit predates the outline-coverage gate and lacks `RequiredOutlineSegmentCount`.
- Required next action: generate a fresh Desktop export after these verifier/audit-shape changes, then run the verifier with `-RequireAutomatic`.

## 2026-07-10 - Segment audit now reports explicit edge and corner coverage
- `outline-segment-congruence-audit.json` now includes explicit `OutlineEdges` for Left/Right/Bottom/Top and `CornerCoverage` for the four principal corners.
- The automatic gate still blocks only on missing required outline coverage, but the JSON and UI can now explain which edge/corner failed instead of dumping anonymous segment mismatches.
- The verifier requires `OutlineEdges` and `CornerCoverage` so stale or incomplete segment audits cannot pass.
- Latest runtime manifest is still `f2df4b2006c04164afd03b5a009e2755` from `ULTIMO TEST 10`; it predates these fields and correctly fails. Fresh export required.

## 2026-07-10 - Verifier requires named outline edges and principal corners
- The segment-congruence verifier now validates not just counts but exact named outline evidence: `Left`, `Right`, `Bottom`, `Top` edge rows and `BottomLeft`, `BottomRight`, `TopLeft`, `TopRight` corner rows.
- Each edge row must include `RequiredCount`, `MissingCount`, and `Mismatches`; each corner row must include `DeltaX`, `DeltaY`, and `IsCovered`.
- This closes the loophole where any four anonymous rows could satisfy the edge/corner requirement.
- Repo-allowed checks passed. Runtime proof still requires a fresh export after these audit-shape changes.

## 2026-07-10 - Segment congruence goal blocked on fresh ULTIMO TEST 11 export
- Third consecutive resumed goal turn still finds latest runtime manifest `f2df4b2006c04164afd03b5a009e2755/manifest.json` from `ULTIMO TEST 10` (`2026-07-10 15:28:08`).
- The segment audit is stale: `ComparisonMode = StructuralWallCenterlineRuns`, `RequiredOutlineSegmentCount` is missing, and `OutlineEdges` / `CornerCoverage` are absent.
- Current code-side verifier/audit checks pass, but the Definition of Done requires a fresh Desktop export generated after the outline-coverage + named edge/corner audit changes.
- Anti-loop decision: goal is blocked until the user generates `ULTIMO TEST 11` from the app and reruns `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.

## 2026-07-10 - FloorPlan -> ElectricalPlan segment congruence proof complete

Latest verified runtime export: `ea194766c40646248be23bcd1fbebec7`.

The package verifier passes with `-RequireAutomatic`: ElectricalPlan is `ProjectedAutomatically`, outline normalization is applied, exported width mismatch is `0`, segment congruence is `SegmentCongruent`, and required outline missing/extra counts are both `0`.

The active comparison mode is `StructuralOutlineCoverageWithWallRunAdvisory`: required exterior/structural outline must match, while internal electrical wall-run differences are advisory only.

Next product work should focus on richer visual QA/overlay UX or expanding the same relationship model to RoofPlan/Facade, not on re-solving the Electrical segment proof.

## 2026-07-12 - HousePlanSet E2E manual audit checklist

Established the manual audit checklist for proving FloorPlan canonical compression propagates to ElectricalPlan end-to-end: width-only, height-only, combined, different setback, and another FloorPlan/Electrical pair before generic confidence.


## 2026-07-12 - TEST A width-only audit passed

TEST A ( f11bf0440604eb3bf55c765c82d90b6) passed the HousePlanSet automatic verification for width-only compression: 4 horizontal operations totaling 3.6 inches were applied to both FloorPlan and ElectricalPlan, outline export width mismatch is 0, and segment congruence is SegmentCongruent.


## 2026-07-12 - TEST A exposed missing final exported-vs-exported congruence gate

TEST A should not be considered fully passed. The current verifier proves Electrical against canonical recipe/expected outline, but not against the final exported FloorPlan DXF geometry. User overlay found Electrical visually more compressed. DXF evidence: FloorPlan WALLS width 480.186, Electrical ELECTRICAL WALLS width 464.400. Next fix: add final FloorPlan-export-vs-Electrical-export congruence audit and make -RequireAutomatic fail on mismatch.


## 2026-07-12 - Final output congruence gate added

HousePlanSet verification now requires
inal-output-congruence-audit.json. -RequireAutomatic must fail unless final exported FloorPlan DXF and final exported ElectricalPlan DXF are structurally congruent. Old TEST A is stale/mismatch evidence; a fresh export is required before this goal can be declared complete.


## 2026-07-13 - Final output audit footprint reference discovery
- TEST A raw FloorPlan WALLS bounds include a low-support tail that inflates width; final automatic proof should compare supported structural footprint/huella and report raw-bounds mismatch as observability, not mix it into the gate.

## 2026-07-13 - Final output supported footprint gate implemented
- Final output automatic proof now compares supported structural footprint/huella (FinalExportedSupportedStructuralFootprint) and reports raw visible-bounds mismatch separately.
- FloorPlan exporter now compresses HATCH boundary points so visible hatch geometry is not left in stale source coordinates.
- Latest runtime manifest is still stale and must be regenerated from Desktop before the goal can be marked complete.

## 2026-07-13 - Final output no-hardcodes guard added
- Added scripts/test-plan-set-final-output-no-hardcodes-contract.ps1 to keep the final-output proof generic: no SEMINOLE/TEST A/Downloads/GUID/known-coordinate hardcodes in the production final-output pipeline files.

## 2026-07-13 - Verifier final output detail fields added
-
erify-latest-plan-set-recipe-manifest.ps1 now outputs final Floor/Electrical paths, supported footprint W/H for both, final mismatch values, and optional raw visible-bounds mismatch values.

## 2026-07-13 - Verifier output fields self-check added
- scripts/test-verify-latest-plan-set-recipe-manifest.ps1 now asserts the verifier prints final-output reason, tolerance, paths, W/H, and mismatch fields.

## 2026-07-13 - Final output runtime proof blocked on fresh export
- Static/code-side checks pass for the final-output gate, no-hardcodes guard, HATCH compression, and verifier output fields.
- Runtime proof is blocked because latest manifest remains  f11bf0440604eb3bf55c765c82d90b6 from 2026-07-12 and lacks
inal-output-congruence-audit.json.
- Required next action: generate a fresh Desktop export and run scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic.

## 2026-07-13 - Fresh final output congruence runtime proof passed
- Fresh manifest `b4e8192e70a146f3910a4fc77988b028` passes `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic` with exit code `0`.
- `ElectricalStatus = ProjectedAutomatically` and final output comparison mode is `FinalExportedSupportedStructuralFootprint`.
- Final FloorPlan and ElectricalPlan supported footprints are both `464.40 x 930.00`; width and height mismatch are both `0.00`.
- Structural outline coverage is congruent: missing `0`, extra `0`.
- Raw width mismatch `-15.7855859574919502` remains visible as a non-gating advisory caused by low-support FloorPlan `WALLS` tail geometry.
- The prior fresh-export blocker is superseded; FloorPlan + ElectricalPlan final-output congruence goal is runtime-proven for this case.
- End-to-end width proof: canonical FloorPlan started at `468 x 930`, Electrical raw source at `470 x 930.000286` was normalized to canonical width `468`, the user requested `464.4 x 930`, and both final supported footprints measured `464.4 x 930` after applying all `4/4` horizontal operations.
- Scope warning: this fresh run proves width compression only; height delta was `0`, so vertical compression still needs its own fresh runtime case if required as separate evidence.
- Overlay boundary: `FinalOutputCongruent` proves translation-invariant supported-footprint equality, not direct overlap at native file coordinates or entity-for-entity identity. The final Electrical footprint is offset `-100.05` in X and `-135.20` in Y relative to FloorPlan; direct native-coordinate overlay needs the inverse translation.

## 2026-07-13 - Whole-app robustness audit
- The five-project layered modular monolith remains the correct architecture; do not split into microservices or more csproj files now.
- Static audit found production-safety blockers before broader modularization: destructive fallback migration for `pinch_markers`, canonical FloorPlan deletion without PlanSet protection, registration persisting PlanSetVersionId as CanonicalFloorPlanVersionId, and `ReadyForExport` being decided before final output verification.
- ~~Roof/Facade manual confirmation can promote canonical compression that is not implemented.~~ Superseded by [[#2026-07-13 - Honest sheet capabilities now fail closed]].
- Runtime data currently lives under `AppContext.BaseDirectory/workspace`; a stable user-data root plus backup/restore is required.
- Import/export are not DB+filesystem atomic; Desktop lacks a global operation error boundary, structured logging, and cancellation lifetimes.
- Current `STAGING` baseline is high risk for a broad refactor: 30 modified tracked files, 73 untracked files, and about 6,485 insertions / 1,346 deletions before the audit note.
- Recommended order: data/identity safety -> in-product verification gate -> CI/E2E baseline -> atomic workflows and operational resilience -> internal modularization -> Roof/Facade expansion.
- Full evidence and roadmap: [[2026-07-13 - Whole app robustness and modularization audit]].

## 2026-07-13 - Finite P0 hardening goal activated
- The active goal intentionally closes only P0 data safety and HousePlanSet trust; broad modularization and P1/P2 work are deferred to later goals.
- Phases: baseline -> durable data/migrations -> canonical identity/references -> honest sheet capabilities -> typed in-product verification -> atomic package -> evidence-based closure.
- Anti-loop: two attempts per hypothesis, three hypotheses per gate, one runtime request, no side work, and completion only from explicit gates.
- Decision note: [[2026-07-13 - Finite P0 hardening goal]].

## 2026-07-13 - P0 hardening Phase 0 green
- Captured authoritative baseline on `STAGING` at `afebe7c`: 30 tracked modified files and 73 untracked files before the first P0 check.
- Verified all six P0s and mapped each to a runnable red check, minimum target files, and binary green gate.
- First red proof is real: `test-p0-pinch-marker-migration-safety-contract.ps1` exits `1` because `SqliteSchemaInitializer` can drop unknown `pinch_markers` data.
- No production file was changed before the red proof.
- Phase 0 evidence: [[2026-07-13 - P0 hardening baseline and gates]].

## 2026-07-13 - Unknown pinch-marker schemas now fail closed
- Startup no longer drops and recreates an unrecognized `pinch_markers` table.
- Unsupported column shapes now raise an explicit error while preserving the original table and rows.
- Focused regressions cover unknown-schema preservation and current-schema idempotency.
- The PowerShell safety contract passes; xUnit execution is intentionally pending because repository policy forbids running `dotnet test` in this session.

## 2026-07-13 - SQLite compatibility boundary v1 (superseded)
> Superseded by [[#2026-07-13 - Canonical HousePlanSet identity is enforced at schema version 2]].
- Schema version became explicit through `PRAGMA user_version`; transitional version `1` established the boundary.
- Unversioned databases are upgraded through the existing restartable bootstrap and receive version `1` only after success.
- A database from a newer app version is rejected without mutation.
- Initializer, application sessions, and startup cleanup now share one connection policy with foreign keys enabled and a 5-second busy timeout.

## 2026-07-13 - Runtime state moved to durable user storage
- The active workspace root is `%LOCALAPPDATA%/FloorplanFit/workspace`; build/reinstall paths are no longer the source of truth.
- The prior executable-relative workspace is copied once through staging and left intact; a non-empty stable target is never silently merged or overwritten.
- Before an older schema advances to the current target, SQLite and all files inside the managed workspace are snapshotted to `%LOCALAPPDATA%/FloorplanFit/backups/pre-schema-v{target}`; current target is `v2`.
- Backup publication uses staging plus atomic directory rename; SQLite itself is captured with `BackupDatabase`.

## 2026-07-13 - Canonical HousePlanSet identity is enforced at schema version 2
- `SheetRegistration.CanonicalFloorPlanVersionId` now comes from its owning `PlanSetVersion`, never from the PlanSet version ID itself.
- Version-1 databases are repaired transactionally; orphan or cross-version registration ownership fails closed without advancing `user_version`.
- SQLite triggers enforce registration ownership and prevent both soft and hard deletion of a FloorPlan version referenced as a HousePlanSet canonical source.
- The application delete path also rejects the operation before writing.
- Cleanup captures geometry ownership before deleting curation/extraction rows, includes measurement and dimension-binding tables, and preserves any geometry still referenced by surviving data.
- This section `replaces` the transitional v1 current-version claim above.

## 2026-07-13 - Honest sheet capabilities now fail closed
- `replaces`: the Roof/Facade promotion risk recorded in [[#2026-07-13 - Whole-app robustness audit]].
- A typed Domain policy is now the single capability truth: Electrical supports affine placement plus canonical compression; Roof and Facade support affine placement only.
- A compressed Roof/Facade projection is persisted as `Unsupported`, with a stable reason stating that manual confirmation cannot make it exportable.
- Confirmation checks capabilities before its legacy `ReadyForExport` early return, and projected export performs the same check; therefore old Ready rows cannot bypass the restriction.
- Electrical confirmation and recipe-aware compression export remain supported, including the existing confirmed-summary rewrite.
- All five P0 PowerShell contracts and `git diff --check` pass. Executable xUnit tests were authored but not run because this repository forbids `dotnet` execution in this session.
- Phase 3 is complete. Typed final verification (Phase 4) and atomic package publication (Phase 5) remain pending and were not touched.

## Fase 4 - verificacion tipada como unico gate (2026-07-13)

- `PlanSetVerificationReportDto` schema v1 es la unica fuente de `ReadyForExport`.
- El reporte tipado cubre outputs, DXF safety, operaciones FloorPlan/Electrical, outline, segmentos, congruencia final y capabilities.
- Missing, mismatch, insufficient-data y unsupported bloquean el package como `RequiresManualConfirmation`.
- Manifest schema v2, Desktop y verifier PowerShell reflejan la misma decision tipada.
- Evidencia permitida: contrato Fase 4 y contratos P0 Fases 1-3 verdes; `git diff --check` verde.
- Pendiente externo: los tests xUnit fueron escritos pero no ejecutados porque el repo prohibe comandos `dotnet` en esta goal.
- Fase 5 se completo con publicacion atomica y compensacion fail-closed; ver la seccion siguiente.

## Fase 5 - publicacion atomica del paquete (2026-07-13, superseded)

> Superseded by [[#2026-07-13 - Fase 5 publication order gate corrected]].

- Los DXF dependientes se generan en un staging sibling y `PackageDirectory` aparece mediante un unico `Directory.Move`; un destino previo se rechaza sin borrarlo ni sobrescribirlo.
- Si exporter, cancelacion, verificacion, writer o persistencia fallan, se limpia staging y se retira cualquier paquete nuevo ya publicado; el error original se preserva.
- El paquete de observabilidad del workspace tambien usa staging: primero escribe los audits, luego `manifest.json` como ultimo archivo, y finalmente publica el directorio por rename.
- Los fallos pre-persistencia se registran best-effort como `PlanSetExportStatus.Failed` con `PlanSetExportFailureDto` JSON tipado; si esa persistencia tambien falla no se reemplaza el error original.
- El reporte tipado de Fase 4 sigue siendo la unica fuente de `ReadyForExport`; un paquete bloqueado puede publicarse atomicamente pero conserva `RequiresManualConfirmation`.
- Evidencia permitida verde: contratos Fases 5, 4 y 3; manifest verifier self-check; `git diff --check`. Los xUnit focalizados fueron escritos pero no ejecutados por la prohibicion de `dotnet`.

## 2026-07-13 - Fase 5 publication order gate corrected

- `replaces`: la seccion Fase 5 superseded de este archivo y [[2026-07-13 - P0 hardening baseline and gates#Fase 5 - publicacion atomica]].
- El `PackageDirectory` final permanece ausente durante `BuildVerificationReport` y durante la publicacion atomica de workspace audits + `manifest.json`.
- Los DXF dependientes conservan `StoragePath` final para manifest/DB y usan `VerificationPath` tipado con `[JsonIgnore]` exclusivamente para leer el staging.
- Orden efectivo: stage dependientes -> verificacion tipada -> workspace audits -> workspace manifest ultimo -> rename del paquete de usuario -> persistencia success.
- El callback atomico revierte el final nuevo si falla DB; el audit handler revierte el workspace si falla rename/DB; finales preexistentes nunca se sobrescriben ni eliminan.
- RED observado: `P0 RED: atomic publication still has no callback boundary between staging and final rename.`
- GREEN observado: contratos Fases 5, 4 y 3, manifest self-check y `git diff --check` pasan; xUnit no se ejecuto por prohibicion explicita de `dotnet`.
- Bug note: [[2026-07-13 - Fase 5 package rename preceded verification gate]].

## 2026-07-13 - P0 HousePlanSet hardening reached static closure

- `replaces`: the open P0 items from [[#2026-07-13 - Whole-app robustness audit]] for destructive migration, runtime storage, canonical identity, capability gating, verification truth and partial package publication.
- Phases 0 through 5 are complete in the current worktree: safe/versioned data startup, durable workspace and backup, canonical ownership guards, honest sheet capabilities, typed verification, and atomic HousePlanSet package publication.
- Final allowed evidence is green: seven focused P0 contracts, the manifest-verifier self-check, and `git diff --check` all exit `0`.
- `FloorPlan` remains the canonical geometry source. `ElectricalPlan` remains the only dependent sheet with canonical-compression projection in this scope. Roof and Facade deformation remain explicitly out of scope and fail closed when unsupported.
- The worktree remains intentionally dirty (`58 files changed` in the tracked diff at closure); no reset, stash, cleanup, commit or push was performed.
- No `.NET` compilation, xUnit execution, application launch, SQLite runtime migration or end-to-end export was run in this goal. Static closure MUST NOT be described as runtime proof.
- External proof still required: run `dotnet test .\FloorplanFit.sln`; then launch `.\scripts\dev-desktop.bat`, export to a fresh destination, and run `powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic`.
- Broad app modularization and new Roof/Facade geometry are P1/P2 work, not hidden follow-ups inside this closed P0 goal.

## 2026-07-13 - Executable P0 proof is red

- `replaces`: the earlier statement that `.NET` proof was merely pending in [[#2026-07-13 - P0 HousePlanSet hardening reached static closure]].
- The user executed `dotnet test .\FloorplanFit.sln`; all five production projects compiled, but the solution failed because two test projects have four compile errors and Infrastructure has 11 failing tests out of 164.
- Static P0 contracts remain green, but the application MUST NOT be described as fully runtime-verified.
- Active failure groups are test API drift, locked workspace-backup staging, fixtures rejected by canonical identity guards, legacy pinch-marker migration fixtures, and DXF exporter regressions/expectation drift.
- Active bug note: [[2026-07-13 - Executable P0 proof is red]].

## 2026-07-13 - Transient backup destination bypasses SQLite pooling

- `replaces`: the locked workspace-backup staging failure group in [[2026-07-13 - Executable P0 proof is red]].
- `SqliteConnectionPolicy.Open` keeps pooling enabled by default for normal production and source connections, but now accepts an explicit opt-out.
- Only the transient pre-migration backup destination opens with `pooling: false`, so disposing it closes the physical connection before the staging directory is published or cleaned up on Windows.
- No global pool clearing was added, and the migration, backup, identity and DXF safety gates remain unchanged.
- Targeted static inspection and `git diff --check` are green. The supplied failing `.NET` check was not rerun because this task explicitly forbids all `dotnet` commands; compile/runtime proof remains pending.
- Bug note: [[2026-07-13 - Windows backup destination pooling lock]].

## 2026-07-13 - Registration and pinch migration fixture REDs have static fixes

- `replaces`: the fixture-rejected-by-identity and legacy-pinch-migration failure groups in [[#2026-07-13 - Executable P0 proof is red]].
- The two stale sheet-registration persistence fixtures now seed typed canonical `FloorPlanVersion`, `PlanSetVersion`, and owned dependent `PlanSheet` rows; confirmation preserves the original identity.
- Exactly three fixtures that fabricate legacy pinch-marker tables after current initialization now reset `PRAGMA user_version = 0`, allowing the real initializer migration path to run.
- Production migration, backup, canonical identity, trigger, and DXF safety gates remain unchanged.
- Targeted static inspection and `git diff --check` are green. The supplied failing `.NET` check was not rerun because this task explicitly forbids all `dotnet` commands; compile/runtime proof remains pending.
- Bug note: [[2026-07-13 - Stale registration and pinch migration fixtures]].

## 2026-07-13 - Projected DXF exporter regressions have static fixes

- `replaces`: the DXF exporter regression/expectation-drift failure group in [[2026-07-13 - Executable P0 proof is red]].
- Pre-R13 one-byte and R13+ two-byte binary group-code formats are now detected from the required opening `0/SECTION` pair and preserved on output; legacy `255` escapes are supported, while groups `290-299` always retain one-byte boolean payloads and synthetic group code `21248` remains invalid.
- Short fixed-length binary reads and streams without a terminal `0/EOF` pair now fail closed with `InvalidDataException`; focused malformed-input regressions cover both paths.
- Recipe-crossing circular curves are converted only in top-level `ENTITIES`. Curves inside referenced anonymous `DIMENSION` blocks remain behind the typed manual-review gate and raise `ProjectedPlanSheetManualReviewRequiredException`.
- Focused exception expectations and the HEADER assertion helper now match production behavior; the existing binary AutoCAD-corruption regression was not changed.
- Static inspection and `git diff --check` are green. No `dotnet`, build, test, restore, watch, or app command was run, so executable proof remains pending.
- Bug note: [[2026-07-13 - Projected DXF binary and manual-review regressions]].

## 2026-07-13 - Supplied executable failures have static repairs pending rerun

- `updates`: [[#2026-07-13 - Executable P0 proof is red]]. The supplied RED was classified and every reported group now has a minimal current-worktree repair.
- Test-project compilation drift was fixed with two existing Contracts namespace imports; no production API was changed for those diagnostics.
- Shared-foundation fixes cover the transient SQLite backup lock and faithful legacy migration fixtures. Loop 2 fixes cover valid PlanSet fixture ownership and fail-closed binary/manual-review DXF behavior.
- A static review found and corrected two defects in the first binary patch: boolean payload width and acceptance of truncated/no-EOF streams.
- Final current-state evidence: all eight allowed PowerShell scripts and `git diff --check` exit `0`.
- Executable proof remains RED/pending because this session did not rerun `dotnet`. The only next verification action is a fresh `dotnet test .\FloorplanFit.sln` by the user or CI.

## 2026-07-13 - Dominant-wall fix has a synthetic RED baseline

- Active Loop 2 work now has three focused Infrastructure regressions covering short outboard fragments, ambiguous dominant wall pairs, and a false-green final footprint caused by fringe bounds.
- Production remains unchanged in this phase. The current raw min/max selectors are expected to fail those contracts: they see width `101` instead of dominant width `100`, silently normalize tied candidates, and accept equal fringe envelopes although dominant walls differ.
- No `.NET` command was run. `git diff --check` is green; executable RED/GREEN proof remains external.
- Implementation plan: [[2026-07-13 - Dominant wall registration and native overlay fix plan]].

## 2026-07-13 - Dominant selector is still under fail-closed review (superseded)

> Superseded by [[#2026-07-13 - Dominant outline and native audit reached static closure]].

- Loop 2 Infrastructure now has a shared dominant-axis selector, hierarchical actual-maximum evidence ranking, native-edge final congruence, and closed `LWPOLYLINE`/classic `POLYLINE` segment extraction in the current worktree.
- Static adversarial review found one remaining false-green topology: span overlap alone does not prove the independently selected vertical and horizontal pairs form a connected structural outline.
- The next bounded step is a RED fixture with non-touching interior stubs, followed by an actual corner/connectivity gate. Evidenced Electrical-to-Floor registration remains pending after that selector review passes.
- No `.NET` command has been run; this is code-side/static progress, not executable proof.

## 2026-07-13 - Automatic manifest proof requires native dominant edges

- `-RequireAutomatic` now accepts only `FinalNativeDominantStructuralOutlineEdgesAndSize` final evidence; the prior normalized/size-only comparison mode is rejected as stale.
- Automatic proof requires congruent status, size and native-edge aggregate flags, width/height residuals within the artifact tolerance, and exactly four individually covered native edge rows within tolerance.
- The verifier self-test includes an equal-size translated false green and exact missing/mismatched-field diagnostics. Its focused RED-to-GREEN PowerShell cycle and scoped `git diff --check` exit `0`.
- Existing exports produced with the old audit contract must be re-exported; they cannot be promoted by compatibility fallback.

## 2026-07-13 - Dominant outline and native audit reached static closure

- `replaces`: [[#2026-07-13 - Dominant selector is still under fail-closed review (superseded)]].
- Dominant X/Y pairs are ranked hierarchically against each evidence stage's actual maximum, ambiguity fails closed, and all selected endpoints require shared support at the four outline corners; disconnected interior stubs cannot synthesize a rectangle.
- Closed lightweight/classic polylines contribute their implicit closing edge. `SOLID` uses perimeter order `p1,p2,p4,p3` with triangle collapse; `3DFACE` ordering remains unchanged.
- Final output proof compares dominant width, height, and all four native edges. Success, insufficient-data, and exception artifacts report the exact resolved verification paths used by the reader.
- Focused RED fixtures exist for fringe bounds, tied candidates, disconnected axes/stubs, tolerance-chain ranking, closed polylines, SOLID order, native translation, false final greens, and both success/failure provenance paths.
- Allowed evidence: focused verifier PowerShell self-test and scoped `git diff --check` exit `0`. No `.NET` command was run, so executable proof remains external.
- Next active phase: derive and persist evidenced Electrical-to-Floor source registration; request/dialog identity is not accepted as geometry truth.

## 2026-07-13 - Evidenced Electrical registration has a RED contract (superseded)

> Superseded by [[#2026-07-13 - Evidenced Electrical registration Application/Contracts GREEN is statically approved]].

- Application REDs require an identifier-only request, exact canonical-version Floor source resolution, exact Electrical source resolution, estimator-owned transform/confidence/evidence, `PendingConfirmation`, structured candidate/residual audit data, and zero registration/unit-of-work writes on ambiguous or insufficient evidence.
- Infrastructure REDs cover identity orientation with outboard fringe, a unique quarter-turn, two-axis uniform scale, anisotropic rejection, symmetric rotation ambiguity, and failure with only one interior-anchor orientation.
- Desktop REDs require Electrical to bypass the manual transform dialog, while Roof/Facade retain it, and require DI to resolve the DXF estimator.
- These tests are code-side RED because the production API/estimator do not yet exist. No `.NET` command was run; only scoped `git diff --check` was used.

## 2026-07-13 - Insufficient final-output audits retain inspected-path provenance

- Loop 2 Infrastructure now carries the resolved FloorPlan and ElectricalPlan `VerificationPath ?? StoragePath` values through every insufficient-data return and the final-output audit catch.
- `InsufficientFinalOutput` reports the exact files selected for inspection instead of null Floor provenance plus Electrical `StoragePath`; reason/status/gating behavior is unchanged.
- A focused regression reaches the no-comparable-structural-segments branch after reading two valid minimal verification DXFs whose storage paths differ.
- Scoped `git diff --check` exits `0`; no build or test ran, so runtime proof remains pending.
- Bug note: [[2026-07-13 - False two-inch Electrical outline normalization#2026-07-13 - Insufficient-output provenance repaired statically]].

## 2026-07-13 — Estimator attempt 2: one fail-open blocker remains

Static spec review passed, but the independent quality review found one concrete blocker: an unsupported geometry entity on a structural layer still falls through the estimator switch and is silently ignored. That can preserve partial LINE evidence and falsely produce an automatic registration. Before closing Loop 2, add a RED with a structural ARC and then reject unsupported structural geometry with an entity-specific diagnostic. No runtime/.NET verification has been run.
## 2026-07-13 — Electrical registration estimator statically closed

Attempt 3 closed the last reviewed fail-open path. A new RED appends a structural ARC to otherwise sufficient matching evidence; automatic registration must return `InsufficientEvidence`, no transform and no candidates. The estimator now rejects every unsupported entity on an accepted structural layer with an entity-specific diagnostic, so partial evidence cannot survive silently. Independent static re-review approved the complete estimator contract, including IxMilia `EntityTypeString` API shape. Runtime/.NET proof remains intentionally external because repository rules forbid running it here.
## 2026-07-13 — Final static gate exposed a stale normalization contract (superseded)

> Superseded by [[#2026-07-14 - Outline congruence contract migrated to evidenced registration]].

Three final non-.NET contract checks and all whitespace/hardcode checks passed. `test-outline-congruence-contract.ps1` failed because it still requires the removed `BuildOutlineNormalization` symbol. That expectation contradicts the current goal: hidden bbox/min-max normalization was intentionally replaced by evidenced Electrical-to-Floor registration and native dominant-edge congruence. The contract must be migrated to assert the new invariant, not revived with dead compatibility code.

## 2026-07-14 - Outline congruence contract migrated to evidenced registration

- `replaces`: [[#2026-07-13 — Final static gate exposed a stale normalization contract (superseded)]].
- The Loop 2 static contract now rejects `BuildOutlineNormalization`, requires dominant-outline selection plus one evidence-complete registration candidate, and proves candidate scale is persisted in `SheetRegistrationTransform`.
- Export assertions now require the shared projection path, persisted `RegistrationTransform`, disabled legacy normalization, identity/native final inspection, fail-closed exported dominant-outline selection, and the verifier's `FinalNativeDominantStructuralOutlineEdgesAndSize`/native-edge gate.
- TDD evidence moved from exit `1` (`DXF exporter must derive dependent Electrical outline normalization.`) to exit `0` (`Outline congruence contract is wired.`). The untracked script's no-index whitespace check is green.
- No production file, build, `.NET` command, test runner, restore, watch, Desktop, AutoCAD, npm, external app, commit, or worktree cleanup was used.
- Implementation record: [[2026-07-13 - Dominant wall registration and native overlay fix plan#2026-07-14 - Outline congruence contract migrated to evidenced registration]].

## 2026-07-14 — Remaining blocker: registration tolerance uses raw DXF units

The end-to-end static integration audit verified that imported canonical Floor and dependent Electrical paths retain their raw DXF coordinate spaces. The estimator currently applies a numeric `0.05` tolerance as if every raw unit were an inch, while the import contract supports inch, foot, millimeter, centimeter, meter and unitless files resolved through `$INSUNITS`/`$MEASUREMENT`. This can authorize a false automatic registration for non-inch inputs. Available SEMINOLE managed copies report inches, so this is a generic robustness blocker rather than evidence for the historical two-inch mismatch. The finite next step is one unit regression followed by source-specific raw tolerances and selector tolerance plumbing; then rerun the already-defined non-.NET gates. The stale outline-normalization contract has already been migrated and now passes.
## 2026-07-14 — Desktop startup CS0136 corrected

The Electrical registration estimator had three branch-local `rationale` declarations colliding with its final method-scope declaration. They were mechanically renamed without changing geometry or registration behavior. Static inspection passed; external `dotnet watch` must provide the compile proof because repository policy forbids running a build after changes.

## 2026-07-14 — Confirmed Electrical replacement is blocked by unsafe unlink

The UI correctly hides the unlink cross for `Confirmed + ReadyForExport`, but the underlying reason is a product gap: Desktop currently deletes only the `plan_sheets` row, while confirmed registrations and projections remain separate non-cascading records. Exposing the control alone would create logical orphans. The required safe operation is transactional: remove every projection for the dependent sheet, then every registration, then the sheet; retain imported documents and historical export/audit snapshots. This is now the immediate prerequisite to create a fresh Electrical registration for the one-to-one SEMINOLE proof.

## 2026-07-14 — Confirmed Electrical can now be safely replaced

The hidden unlink control was repaired without allowing orphaned workflow state. `CanUnlink` now exposes every noncanonical dependent sheet, but Desktop delegates removal to the new Application `UnlinkPlanSheetHandler`. In one UnitOfWork commit it removes all projections by `dependent_sheet_id`, then all registrations by `dependent_sheet_id`, then the sheet. Canonical FloorPlan protection remains; imported documents and historical exports/audits are preserved. Static RED/implementation reviews and whitespace checks passed; a user build is still the external runtime proof.
## 2026-07-14 — SEMINOLE registration currently fails before confirmation

The UI state is `Unregistered`, not pending confirmation: the estimator rejected a canonical structural `LINE` because its endpoint Z values differ. Therefore there is no Confirm button to press. This is a false fail-closed condition introduced by treating a LINE like a face; valid XY footprint extraction must allow differing finite endpoint elevations while SOLID/3DFACE remain planar-only. This is the immediate blocker to the fresh registration proof.
## 2026-07-14 - LINE endpoint elevation has a focused registration RED

- Loop 2 Infrastructure now has a synthetic asymmetric registration contract where one Electrical structural `LINE` endpoint has finite `Z = 3` while its XY segment remains identical to the Floor structure.
- The contract requires an `Estimated`, conclusive identity transform at scale `1`; the existing non-planar `3DFACE` rejection remains untouched.
- Static source inspection proves the current failure: `AddLine` delegates to `GetPlanarPoints`, which rejects different endpoint Z values with a non-planar diagnostic before XY registration can run.
- Scoped `git diff --check` passed. No `dotnet`, build, test, restore, watch, Desktop, or runtime command ran; executable RED proof remains pending.
- Bug record: [[Bugs/2026-07-14 - LINE endpoint elevation rejects valid registration]]. Implementation record: [[Implementation/2026-07-14 - LINE endpoint elevation registration RED]].

## 2026-07-14 — Finite-Z LINE registration rejection corrected

The structural extractor now accepts a finite 3D LINE as XY footprint evidence even when its endpoint elevations differ. The new regression proves that this remains an estimated scale-one registration. SOLID and 3DFACE stay fail-closed for nonplanar geometry. The screenshot's Unregistered state had no Confirm button because no registration was created; after an external rebuild, Register should proceed past this false rejection.

- Desktop now distinguishes manual review with no saved registration from a real PendingConfirmation state, so users do not look for a nonexistent Confirm button.

- 2026-07-14: Library Register feedback is now explicit: clicking Register shows progress, manual Electrical failures explain that no Confirm button exists until PendingConfirmation is saved, and long status text wraps under the toolbar instead of being clipped.

## 2026-07-14 — Structural ARC no longer blocks registration

`ARC` on a structural layer is decorative/curved geometry for the dominant axis-aligned wall estimator, not straight-wall evidence. It previously caused both FloorPlan and ElectricalPlan extraction to fail before a registration could be persisted, so the UI rightly had no Confirm button. The estimator now ignores only `ARC`; it still fails closed for unknown entity types and invalid supported geometry, and drawings with no straight evidence remain insufficient. A focused synthetic regression covers matching Floor/Electrical ARC-containing drawings with adequate straight walls. User rebuild/retry is the pending runtime proof.

- Bug record: [[Bugs/2026-07-14 - Structural ARC blocks Electrical registration]].

- **Current truth:** `Register` must be retried after rebuild; `Confirm` appears only if that retry saves a `PendingConfirmation` registration.

- **Current truth:** finite LWPOLYLINE bulges now skip only their curved edge; retry `Register`, and expect `Confirm` only after a registration is persisted.

- **Current truth:** ARC and finite-bulge LWPOLYLINE pass; CIRCLE now also skips as non-straight evidence. Retry `Register` after watch recompiles; `Confirm` requires saved registration state.

- **Current truth:** binary-DXF metadata confirmed ELLIPSE as the final observed structural curve; it now skips as non-straight evidence. Let watch rebuild, retry `Register`; `Confirm` requires persisted state.

- **Current truth:** the two finite nonplanar Electrical 3DFACEs now skip only after finite/range validation; all observed original structural types are classified, but runtime `Register` proof remains required.

- **Current truth (read-only):** SEMINOLE registration now reaches dominant-outline selection, where the selector correctly rejects independent Floor/Electrical axis-pair winners that lack four shared corners. The remaining generic fix is connected-frame candidate selection and pairwise evidence evaluation, not another entity-type patch, tolerance relaxation, raw whole-sheet bbox, or arbitrary scale. See [[Bugs/2026-07-14 - Dominant selector ranks disconnected axis pairs]].

## 2026-07-14 — Dominant structural outline selection

- DXF extraction completes and reaches dominant structural outline selection.
- The selector independently ranks vertical and horizontal edge pairs before checking connected four-corner support, which can reject valid lower-ranked frames.
- Read-only reconstruction found 365 canonical and 1,033 electrical connected H/V frame candidates: this is a selection defect, not missing geometry.
- Required generic fix: construct only connected four-corner candidates, then compare compatible frames through the existing uniform-scale/residual gates; no bbox fallback, tolerance relaxation, or SEMINOLE-specific rule.
- Runtime proof is pending implementation and retry.

## 2026-07-14 — Connected frame-pair registration

- **Loop 2 / Infrastructure:** SEMINOLE was caused by independent local axis/frame selection, not unsupported DXF entities. The selector now emits only connected four-corner frames; registration evaluates canonical × electrical connected-frame pairs, prunes nonuniform fits before proof, and authorizes only one tolerance-distinct evidence-complete transform. Zero or multiple candidates require manual review.
- No tolerance relaxation, raw-bbox global scale, or SEMINOLE-specific case was added. Static review found no blockers; build/tests and `Register` runtime proof are still pending.

## 2026-07-14 - Pending Electrical registration now has a bounded visual confirmation gate

- **Loop 2 / Desktop:** a `PendingConfirmation` sheet row now keeps status, method, and confidence compact and moves actions onto their own wrapping row, so long estimator evidence cannot clip the confirmation path.
- `Revisar y confirmar` opens a dedicated overlay of canonical FloorPlan WALL geometry and transformed ElectricalPlan WALL geometry in the same coordinate system. The preview reuses the existing viewport, workspace, and clipped-line rendering primitives, uses separate translucent colors, and fits both layers together.
- Full warning/rule evidence is shown in a wrapping, scrollable diagnostics panel. Confirm and Cancel/Back remain in a fixed bottom row.
- Confirmation still calls `ConfirmDependentSheetRegistrationAsync`, which owns the existing Application confirmation and Library refresh. Closing/canceling the dialog performs no mutation.
- Missing/stale registration, unresolved DXF sources, extraction errors, empty WALL geometry, and invalid transforms produce a visible error model with Confirm disabled.
- Focused source/layout/transform/failure checks were added. XML parsing and scoped `git diff --check` passed; no `dotnet`, build, test, restore, watch, Desktop, or runtime command ran, so compile and executable UX proof remain external.
- Implementation record: [[Implementation/2026-07-14 - Pending registration visual review overlay]].

## 2026-07-14 - Visual registration review is Electrical-only

- The pending-registration overlay is now strictly gated to `SheetType == "ElectricalPlan"` in both visibility and handler defense.
- Pending RoofPlan and FacadeElevation rows retain the direct `Confirm` action and call the existing `ConfirmDependentSheetRegistrationAsync` workflow without opening the Electrical overlay.
- A single Desktop converter splits the two actions while retaining `CanConfirmRegistration` as the shared pending-state gate.
- Focused source/layout coverage proves both handler boundaries. Scoped `git diff --check` and MainWindow XAML parsing passed; no .NET command ran.
- Updates: [[Implementation/2026-07-14 - Pending registration visual review overlay]].

## 2026-07-15 - Nullable HousePlanSet quality JSON is fail-safe (superseded)

> Superseded by [[#2026-07-15 - Desktop nullable audit summary preserves manual package result]]. The Application quality-event RCA below was incorrect.

- **Loop 2 / Application:** the package/audit pipeline's only production `JsonElement` numeric accessor was the optional quality-event `confidence` read. It no longer calls `GetDecimal` on nullable JSON: missing/null maps to `decimal? null`, while malformed non-numeric values fail with event/property context.
- Required quality fields (`method`/`source`, `status`) no longer degrade to empty strings. Malformed JSON now aborts package publication instead of being swallowed as absent observability metadata.
- Package abort after audit startup cleans staging/final package files and persists exactly one `Failed` export with no manifest path; the standalone canonical DXF remains as designed.
- Focused source tests cover nullable confidence, actionable required-field failure, and package failure-state cleanup. Runtime compile/test proof remains external because repository policy forbids executable .NET commands.
- Bug record: [[Bugs/2026-07-15 - Nullable quality confidence crashed HousePlanSet export]].
- **Evidence caveat:** the pre-change source already guarded `JsonValueKind.Null`; without the runtime stack trace, the Number/Null incident is consistent with a stale/different assembly. The raw accessor is now gone, but external rebuild/reproduction is required for conclusive closure.

## 2026-07-15 - Desktop nullable audit summary preserves manual package result

- **Current truth / Loop 2 Desktop:** manifest `07f2352e44504bb2a510feee5117fe5f` was created successfully as `RequiresManualConfirmation`; the Electrical output is absent because projected export required manual review. Package generation itself did not throw the Number/Null error.
- The crash happened afterward while Desktop summarized nullable numeric fields from final/segment audit JSON. `GetJsonDecimal` and `GetJsonInt` called `TryGet...` on `JsonValueKind.Null`, masking the real blocked/manual result with a generic package-failed message.
- Both helpers now require `JsonValueKind.Number`; null/missing remains `null` for decimals and `0` for integers. No broad catch was added.
- The focused Desktop regression preserves manual/missing-data lines and the real `HousePlanSet NO listo: MissingExpectedOutput` UI status.
- The speculative Application/package parser and failure-state changes were manually removed; earlier atomic package work remains intact.
- External compile/test/runtime proof remains required because executable .NET commands are forbidden.
- Bug record: [[Bugs/2026-07-15 - Nullable Desktop audit summary masked manual HousePlanSet result]].

## 2026-07-15 - Confirmed whole-plan Electrical proof is now authoritative at export

- **Current truth / Loop 2 Domain → Application → Infrastructure:** the estimator's conclusive whole-plan coverage/residual result now survives as an optional typed `WholePlanRegistrationProof` on `SheetRegistration`, is preserved by confirmation, and round-trips through nullable SQLite JSON.
- Recipe-aware export accepts canonical registered coordinates only when the owning registration is `Confirmed` and the proof is current, passed, and range-valid. It keeps normalization null and canonical operation coordinates unchanged instead of selecting a locally dominant frame again.
- Missing, legacy, failed, malformed, or unsupported proof remains fail-closed/manual; `RuleSummary` is not an authority source.
- Existing unsupported-entity, curve-crossing, deformation/operation audit, DXF safety, congruence-dimension, and atomic-package gates remain active. No source fingerprint was added because current canonical version/dependent sheet identities do not support in-place source replacement.
- Focused TDD contracts and scoped static checks are present. No `.NET`, build, test, restore, watch, Desktop, commit, or push command ran, so compile/runtime proof and a fresh export remain external.
- Bug: [[Bugs/2026-07-15 - Export discarded whole-plan Electrical registration proof]]. Implementation: [[Implementation/2026-07-15 - Whole-plan Electrical proof reaches export]].

### Final static-review corrections

- The typed proof now carries policy version/status, canonical/dependent identities, both source SHA-256 values, coverage, and residuals. Estimator acceptance and persisted-proof authority share one versioned domain policy; `Passed=true` with weak coverage or excessive residual is invalid.
- SQLite v1-to-v2 migration adds `whole_plan_registration_proof_json` before identity repair advances `user_version`, with migrated add/read/update proof round-trip coverage.
- Recipe-aware export validates projection-registration-canonical-adjustment ownership, proof IDs, and the caller Electrical DXF hash before using canonical coordinates. Canonical FloorPlan/import records are append-only and managed copies never overwrite, so canonical version identity is the immutable binding.
- The outline stage now reports `RegistrationProofAuthorized` and leaves unmeasured bounds, residuals, anchors, and scales null. It no longer fabricates `Congruent` or zero measurements; measured segment congruence, final-output congruence, operation, DXF-safety, and atomic-package gates remain required.
- The synthetic regression parses exported DXF vertices: the canonical outer frame deforms at the canonical operation coordinate while the stronger interior decoy remains non-authoritative. Missing or policy-invalid proof remains manual and creates no output.
- Static review only; no .NET execution. External compile/tests and a fresh runtime export remain pending.

- Fresh compile diagnostics in proof authorization were corrected: nullable string anchor labels use `is null`, and the failed `TryReadPoint` reference-record out value uses an unconsumed `null!` sentinel rather than fabricated geometry. The user subsequently confirmed the build succeeds.

- **Verified runtime migration truth:** external build succeeded, but an existing LocalAppData database already marked schema v2 lacked `whole_plan_registration_proof_json`, causing startup SQLite Error 1 before repository reads. Current-version initialization now idempotently ensures the additive sheet-registration schema before returning; it does not delete/reset data or relax future-version rejection. Focused runtime migration execution remains external.

## 2026-07-15 - Export 2 automated footprint audit is a false negative

- Fresh output `D:\PointAIData\test adjust\2.dxf` and its packaged Electrical DXF were inspected directly with `ezdxf` and through manifest `46654fc797374f349f606d641ffb0c58`.
- Electrical generation succeeds. Raw structural bounds differ because they include wall-like auxiliary geometry and are not a reliable house-footprint measurement.
- User manually overlaid both outputs in AutoCAD and reported a perfect match.
- The audit selected local bounds of only `136" × 6"` for Floor and `186" × 4"` for Electrical, proving it did not select the complete house footprint. `SegmentCongruenceMismatch` and `FinalOutputCongruenceMismatch` are therefore false negatives for this export.
- Current blocker is the observability selector, not the exported transformation. Do not relax the gate; fix the audit to compare the registered whole-plan structural footprint.
- Experiment: [[Experiments/2026-07-15 - Export 2 Floor Electrical footprint comparison]].
