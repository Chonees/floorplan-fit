---
type: implementation
status: active
date: 2026-07-20
project: FloorplanFit
area: Loop 1 automated site fitting and Loop 2 Electrical projection
implements: "[[2026-07-20 - Replace pinches with parametric adaptation profiles]]"
---

# Finite goal for automatic site fitting UX

## Objective

Deliver the smallest production-shaped end-to-end proof that replaces daily pinch/STRETCH interaction with an extremely intuitive automatic site-fitting experience for a commissioned HousePlanSet, initially covering canonical FloorPlan plus ElectricalPlan.

The daily operator flow must require only three primary decisions after entering the Library: select an `Auto-fit ready` house, select a Site Plan, and confirm/export the automatically generated safe proposal. CAD mechanics, entity IDs, vertices, crossing regions, and pinch controls are not exposed.

## Fixed scope

- One-time advanced House commissioning plus simple daily Site fitting.
- First executable proof: SEMINOLE, one Width variable and one Depth variable.
- FloorPlan remains canonical architecture.
- Doors/windows keep immutable dimensions and remain wall-hosted.
- Electrical output is adjusted canonical `ArchitecturalBase + ElectricalOverlay`.
- Electrical devices follow confirmed wall/room hosts; wire graphics regenerate from known connectivity or become explicit manual blockers.
- Existing Library, HousePlanSet registration, export package, comparison, manifest, audit, and fail-closed behavior are preserved where compatible.

## Explicit non-goals

- No RoofPlan or Facade automation in this goal.
- No generic BIM reconstruction.
- No arbitrary-DXF zero-review promise.
- No general optimizer until priority/capacity allocation proves insufficient.
- No new abstraction, dependency, persistence table, or service unless a failing contract requires it.
- Do not restore Pinches V1/V2 as a hidden fallback.

## Phase gates (historical; superseded)

> Replaced for execution by [[#Authoritative micro-gated completion plan — 2026-07-21]]. The product scope remains unchanged.

1. **Baseline and executable contract:** map the actual active paths and dirty worktree; write focused failing contracts for commissioning readiness, exact Width/Depth fit, opening preservation, simple daily UX, Electrical composition, fail-closed behavior, and audit output.
2. **Minimum control model:** add only the smallest model required for commissioned variables, immutable hosted openings, deterministic entity roles, capacities/priorities, and invariants.
3. **Deterministic Floor fit:** calculate exact site deficit, try existing rigid placement first, allocate reduction by priority/capacity, apply the compiled profile, and prove exact envelope change with unrelated geometry unchanged.
4. **Electrical consequence:** use canonical adjusted architecture as output base, project hosted Electrical overlay entities, regenerate only supported wire routes, and block unsupported routes explicitly.
5. **Minimal UX:** Library `Auto-fit ready` gate; one automatic proposal; Floor/Electrical before-after overlay; concise outcome explanation; confirm/export. Keep advanced commissioning controls out of the daily path.
6. **Safety and observability:** prove doors/windows unchanged, walls/invariants valid, device counts/hosts preserved, exact final footprints, explicit blockers, DXF/package outputs, and human-readable audit.
7. **Static closeout and runtime handoff:** remove newly dead pinch entry points only when replacement coverage exists; run allowed static checks; update Obsidian; provide one exact user runtime script. Repository policy forbids agent-run `.NET` build/test/restore/watch/Desktop commands.

## Acceptance criteria

- The daily flow has no manual stretch/pinch authoring and no more than three primary decisions from Library to export.
- A commissioned SEMINOLE profile automatically resolves one Width-only, one Depth-only, and one combined safe case exactly.
- Door/window sizes, swings/orientations, wall thickness, protected elements, and unrelated geometry remain unchanged.
- Insufficient capacity or any invariant violation blocks export with a concrete reason.
- Electrical structural background is the adjusted canonical FloorPlan, making Floor/Electrical footprint congruence true by construction.
- Supported Electrical devices remain hosted and preserve size/count; supported wires regenerate; unsupported routes are explicit blockers.
- Export package contains named FloorPlan, ElectricalPlan, comparison, manifest, and readable audit artifacts with no extra loose duplicate export.
- Focused tests/contracts and static checks cover every changed branch; user receives a reproducible external runtime test because the agent cannot execute .NET in this repository.

## Anti-loop rules (historical; superseded)

> Replaced by the measurable stop contract in [[#Execution and anti-overloop contract]].

- Each phase must finish with new evidence: a failing contract, implementation diff, static proof, or explicit external handoff.
- Never repeat the same hypothesis/change more than twice without new evidence; revert or choose the next smallest alternative.
- Do not repeatedly inspect the same files or rewrite plans instead of implementing the next dependency-ready slice.
- Do not broaden scope to solve future plan types or arbitrary DXFs.
- Continue through every unblocked phase. External runtime validation does not stop static progress.
- Mark complete only when all repository-executable acceptance criteria are satisfied and the remaining user runtime proof is exact and finite.
- Mark blocked only under the goal protocol after the same external blocker recurs for three consecutive goal turns and no other meaningful work remains.

## Current completion estimate (historical snapshot)

Phase 1 baseline is complete and the first Phase 2 slice now exists statically. `CommissionedHouseFitPlanner` consumes a versioned commissioned profile made only of semantic Width/Depth variables and precompiled entity-aware actions. It tries rigid placement first, converts the exact inch deficit to source units, allocates Width then Depth deterministically by priority/capacity, preserves the commissioned targets/roles unchanged, and fails closed without partial actions for missing capacity or unsafe templates. Focused RED/GREEN source contracts cover rigid fit, combined exact allocation, missing capacity/axis, and immutable/protected entities.

The profile is now durable per `FloorPlanVersionId` through schema v3 JSON persistence and carries a non-empty `PublishedCurationId`. Application save/readiness handlers keep persistence behind the workflow boundary; a profile is `Auto-fit ready` only when both Width and Depth have safe positive capacity and the stored curation matches the active expected curation. A dedicated profile query returns the exact persisted profile together with readiness, while stale curation returns no profile. The SQLite repository round-trips nested action targets/roles plus both identities, upserts one document per version, removes selectively, and rejects corrupt or incomplete JSON identity. Desktop DI wiring has focused source contracts.

The active Desktop Library route now performs an exact commissioned-profile lookup using the selected version's non-empty `ActivePublishedCurationId`; missing, stale, not-ready, unit-mismatched, or structurally incomplete inputs stop before any legacy fallback. A distinct `BuildCommissioned` path requires measurement context and accepted structural IDs, creates one deterministic plan, projects all SOURCE actions once for preview, recenters the final footprint, and stores the unchanged SOURCE actions for export. A rejected preview restores the untouched projected FloorPlan and disables export. The legacy builder remains unchanged for existing tests/callers, but the daily Library route no longer resolves `IAutoFitPlanSuggester` or enters Pinches.

Focused Desktop contracts now specify exact one-time projection (`SOURCE delta 2` at preview scale `2` becomes a visual delta `4` while export remains `2`), active-curation identity, required measurement/structural evidence, no Pinches/suggester dependency, and blocked export with untouched geometry after preview rejection. No `.NET` command was run, so executable GREEN proof remains external. One-time profile commissioning, complete opening/annotation role projection, Electrical base-plus-overlay composition, the final three-decision shell, package/audit closeout, and legacy pinch entry-point removal remain pending.

## Verified Desktop integration seam

- The active daily route now goes `MainWindow -> LibraryViewModel.ShowVersionSitePlanAdjustmentAsync -> OpenVersionSitePlanAdjustmentAsync -> SitePlanAdjustmentPreviewProjector.BuildCommissioned -> SitePlanAdjustmentViewModel`. It supplies the exact active published-curation identity and never silently falls back to legacy `Build`.
- `GetCommissionedHouseAdaptationProfileHandler` returns the exact profile plus readiness and Desktop DI registers it together with the SQLite repository.
- The safe seam is to load the exact profile in the existing Library scope, fail closed on missing/stale/not-ready state, and call a dedicated commissioned builder. That builder projects the untouched canonical Floor once, creates the fit request, runs `CommissionedHouseFitPlanner`, projects the resulting SOURCE actions once for preview, and injects the unchanged SOURCE actions into `SitePlanAdjustmentViewModel` for export.
- The commissioned runtime must not call `SuggestAutoFitPlanAsync`, `AutoFitSuggestionOptionGenerator`, `ApplyAutoFitPlan`, `CadStretchRecipeCompiler`, or `ToFloorSourceStretchAction`; those are the current Pinches runtime chain and would re-infer intent or double-convert coordinates.
- The commissioned document binds both `FloorPlanVersionId` and `PublishedCurationId`. Exact reads compare the caller's active published curation and fail closed with no profile when the stored recipe is stale.
- Existing `OpeningLabelDto` has a source reference but no host-opening relationship. One-time commissioning must persist an explicit label/annotation role or host binding; the daily runtime must never infer it by proximity. Missing, duplicate, crossing, stretched, or unresolved opening/fixed/protected/annotation roles block Auto-fit readiness/export.
- Existing curated Pinch groups may seed a profile only in an explicit one-time commissioning command. Width/Height groups, closing edges, priorities, capacities, target walls, rigid/fixed hosted entities, and annotation bindings are compiled and persisted there; the daily path reads only the persisted profile and has no Pinches fallback.

## Desktop commissioned slice status

- `BuildCommissioned` has no suggester, PinchGroup, or PinchMarker parameter and does not read those collections from the review model.
- Preflight requires the exact version/curation pair, profile readiness, equal positive Floor unit factor, positive Site Plan factor, and nonempty accepted structural path IDs whose geometry still exists.
- Preview receives projected actions once; export receives the planner's original SOURCE actions. Recentering changes only placement offset and projected display coordinates.
- Planner or preview failure yields no partial actions. Preview failure keeps a blocked ViewModel so the user can see the reason, while `CanExportAdjustedSitePlan` remains false.
- **Resolved 2026-07-21:** `CommissionedHouseFitPreviewProjector` now returns geometry paths, room labels, opening labels, and dimensions through one coherent result. Source-only `Fixed` annotations remain value-equivalent, `RigidMove` offsets accumulate exactly once in projected coordinates, invalid/incomplete roles fail closed, and productive commissioning includes only exact supported `DIMENSION` identities with representable bounds. Runtime GREEN remains external because repository policy forbids agent-run `.NET` commands.

## Minimal commissioned presentation

- `SitePlanAdjustmentWindow` now keeps the legacy suggestion/options/manual-confirm controls only for the legacy path.
- In commissioned mode the operator sees the automatic status/summary and one primary `Confirmar y exportar` action wired to the existing safe export gate.
- The change is presentation-only: it does not fake readiness, add a fallback, or merge the separate manual Electrical-confirmation command. A future transactional ViewModel command is required only if that manual path must become one click too.

## Exact Library readiness slice

- Library refresh now evaluates each actively published version through `GetCommissionedHouseAdaptationReadinessHandler` using the exact version/curation pair, sequentially inside the existing scope.
- `CanAdjustToSitePlan` requires the enriched `IsAutoFitReady` result plus a nonempty active published curation; published-only is no longer sufficient.
- The version row displays `Auto-fit ready` or `Setup required` without adding a decision. Missing/stale/invalid state and individual probe failures stay disabled; cancellation is rethrown.
- Focused Desktop contracts and static checks are present. `.NET` execution remains part of the final external handoff.

## Authoritative micro-gated completion plan — 2026-07-21

This section is the active execution contract. Earlier estimates and broad phase gates are historical context only.

### Already proved statically — do not reopen without contradictory evidence

- [x] `CommissionedHouseFitPlanner` computes exact Width/Depth deficit, tries rigid placement first, allocates deterministically by priority/capacity, and rejects insufficient capacity without partial actions.
- [x] `CommissionedHouseFitRequestFactory` performs the physical-unit conversion and structural-envelope request.
- [x] Published curation commissions an exact version/curation profile; Library readiness uses that exact identity and fails closed.
- [x] The commissioned daily path does not invoke Pinches V1/V2 or the legacy AI suggester.
- [x] Electrical composition uses adjusted canonical FloorPlan architecture and excludes `ELECTRICAL WALLS` from the overlay.
- [x] A successful package has named FloorPlan, ElectricalPlan, comparison JSON, manifest, and readable audit, then removes the loose canonical source.
- [x] Automatic Electrical export exceptions raised during dependent generation abort atomic publication. The earlier discovery-filter gap remains open in Phase 6.

### Phase 1 — restore one coherent commissioned Floor preview

- [x] **1.1** Expand `CommissionedHouseFitPreviewResult` minimally to carry geometry paths, room labels, opening labels, and dimensions.
- [x] **1.2** Add/use the overload already specified by focused tests; do not create a second projector abstraction.
- [x] **1.3** Keep every source-only `Fixed` label/dimension value-equivalent to its input.
- [x] **1.4** Apply each source-only `RigidMove` exactly once using the signed Width/Depth vector scaled to preview coordinates.
- [x] **1.5** Preserve text, angle, measurement, size, render primitives, and unrelated payload while moving coordinates.
- [x] **1.6** Reject missing, duplicate, unsupported, contradictory, or incomplete annotation roles with the exact entity/action reason.
- [x] **1.7** Make `SitePlanAdjustmentViewModel` consume the projected annotations instead of restoring baseline annotations after a successful commissioned projection.
- [x] **1.8** Include supported `DimensionDto` inventory during productive commissioning only when exact identity and safe bounds are available; otherwise leave the house `Setup required`.
- [x] **1.9** Inspect source/test signatures and run only allowed static syntax/XAML/diff checks. Phase gate: production and focused contracts describe one API and no branch silently drops annotations.

#### Phase 1 evidence — 2026-07-21

- `CommissionedHouseFitPreviewProjector.Apply` has one expanded production overload plus one compatibility forwarder; the only production call supplies all four preview inventories.
- Focused contracts cover fixed identity, signed/scaled single movement, cumulative Width + Height movement, complete dimension-coordinate translation, payload preservation, and fail-closed missing/duplicate/unsupported/contradictory/incomplete evidence.
- `SitePlanAdjustmentViewModel.ApplyCommissionedHouseFit` consumes and recenters the projected labels and dimensions with the projected geometry.
- `FloorPlanReviewViewModel.TryBuildCommissioningAuxiliaryBindings` accepts only exact `DIMENSION` identities with complete representable bounds and otherwise reports `Setup required`.
- Scoped `git diff --check` passed for all Phase 1 production/test files; only line-ending warnings were emitted. No `.NET` command was run, so executable proof stays in Phase 8's external handoff.

### Phase 2 — close canonical Floor auxiliary identity and hosted-opening safety

- [x] **2.1** Separate structural and auxiliary source-reference matching; an auxiliary `LINE:n` must never match a structural wall solely because the text is equal.
- [x] **2.2** Reuse existing extractor identity/handle conventions; do not invent a parallel indexing scheme.
- [x] **2.3** Add one collision contract proving structural/global aliases reject or resolve uniquely before output creation.
- [x] **2.4** Add one successful unambiguous `DIMENSION` `RigidMove` contract; preserve its non-coordinate payload.
- [x] **2.5** Define `GeometryPathId + SegmentSortOrder` as the opening's wall-segment host only if it resolves to exactly one accepted structural wall segment; otherwise persist the smallest missing host reference in the existing profile JSON.
- [x] **2.6** Validate every commissioned door/window host during readiness and again before export; stale, missing, duplicate, crossing, or non-wall hosts fail closed.
- [x] **2.7** Prove `Fixed` keeps the complete raw entity unchanged and `RigidMove` changes only coordinates, preserving door/window size, swing/orientation, block name, scale, and rotation.
- [x] **2.8** Prove wall thickness, protected entities, and unrelated geometry are unchanged. Phase gate: no ambiguous auxiliary identity and no opening can export without one accepted host.

#### Phase 2.1-2.4 evidence — 2026-07-21

- Structural spans resolve exclusively through `StructuralSourceEntityRef`; persisted auxiliary `Fixed`/`RigidMove` roles resolve exclusively through `AuxiliarySourceEntityRef`.
- The reader retains the established wall-layer structural ordinals, type-global auxiliary ordinals, handle-first `DIMENSION` identity, and extractor-backed `INSERT` identity/bounds. No parallel indexing scheme or DTO namespace was added.
- A focused `LINE:3` collision contract proves that a global auxiliary alias cannot silently bind a structural target: the exporter rejects the alias before creating output.
- A focused handle-backed `DIMENSION` contract translates all seven X/Y coordinate pairs exactly once and verifies the complete non-coordinate raw payload remains equivalent.
- Scoped `git diff --check` passed. Repository policy still reserves executable `.NET` proof for the finite external handoff.

#### Phase 2.5 evidence and remaining 2.6 correction — 2026-07-21

- `GeometryPathId + SegmentSortOrder` keeps its original meaning: the opening artifact's own preview/replay geometry. Optional `HostGeometryPathId + HostSegmentSortOrder` now persists the distinct accepted structural wall segment in the same JSON document; legacy JSON deserializes with null host fields and remains `Setup required`.
- Productive commissioning resolves a host once from translated opening geometry and accepted wall segments. It accepts exactly one fully collinear supporting segment; zero candidates, multiple candidates, crossing-only evidence, missing geometry, or arithmetic overflow returns one actionable `Setup required` reason.
- Compiler/readiness reject incomplete, conflated, non-wall, duplicate/ambiguous, stale, or mismatched binding/role host evidence. The commissioned planner repeats auxiliary/host validation before returning either rigid or deforming output, so a legacy opening with only its own geometry cannot reach export.
- Focused contracts cover successful distinct own/host identity, missing host, zero/multiple/crossing-only commissioning evidence, non-wall/duplicate accepted host, stale/ambiguous readiness, JSON round-trip/backward compatibility, and pre-output rigid-path rejection.
- Adversarial review found one missing fail-closed branch: `TryResolveOpeningWallHost` currently accepts `matches.Count == 1` even when `crossingCount > 0`. A valid collinear host plus a second crossing accepted wall must reject and needs one focused Desktop contract.
- Scoped `git diff --check` passed; `.NET` execution remains external by repository policy.

##### 2.6 contradictory branch resolved

- The only success condition is now `matches.Count == 1 && crossingCount == 0`.
- A focused productive-publish fixture combines one valid collinear host with one additional accepted perpendicular crossing wall. It proves no commissioned profile is persisted and the house remains `Setup required` with the opening reference and explicit crossing reason.
- Targeted source/test inspection and scoped `git diff --check` passed. With this correction, microsteps `2.5-2.6` are statically green.

#### Phase 2.7-2.8 evidence — 2026-07-21

- Raw DXF replay now has paired opening contracts: a `Fixed` block-backed opening remains byte-record equivalent, while `RigidMove` changes only its insertion coordinate and leaves the complete remaining record equivalent. Explicit assertions retain block name, X/Y scale, rotation, and therefore the commissioned opening's size/orientation payload.
- The final bounded composition contract applies Width and Depth actions cumulatively to the same `INSERT`: only DXF coordinate groups `10/20` change, exactly by the two requested deltas, while the complete remaining block record stays equivalent.
- The paired-wall replay contract keeps the two adjusted faces parallel at their original 4-unit separation while shortening both equally, preserving wall thickness. Its fixed wall remains at identical coordinates.
- Protected/source-only `Fixed` text is raw-record equivalent, the compiler retains the protected role as `Fixed`, and unrelated geometry remains unchanged in the same replay contract.
- Combined with the corrected mixed-crossing guard, no commissioned opening can produce output without one distinct accepted structural host. Phase 2 is statically closed; runtime execution remains part of Phase 8's finite external handoff.

### Phase 3 — prove the canonical Floor result with real SEMINOLE evidence

- [x] **3.1** Reference the authoritative SEMINOLE Floor fixture only from tests/harness configuration; production code must remain house-agnostic.
- [x] **3.2** Build one commissioned test profile containing exactly one Width variable and one Depth variable from real curated identities.
- [x] **3.3** Width-only case: verify requested deficit, allocated delta, final width, unchanged depth, and exact affected-role list.
- [x] **3.4** Depth-only case: verify requested deficit, allocated delta, final depth, unchanged width, and exact affected-role list.
- [x] **3.5** Combined case: verify deterministic allocation order/capacity and exact final width/depth.
- [x] **3.6** For all three cases, verify opening dimensions/orientation, wall thickness, protected entities, and unrelated geometry.
- [x] **3.7** Add insufficient-capacity and invariant-violation cases that produce no partial output and one actionable blocker.
- [x] **3.8** Phase gate: all three real-fixture contracts share the same generic production path and contain zero SEMINOLE-specific production branches.

#### Phase 3 evidence — 2026-07-21

- New `tests/FloorplanFit.Infrastructure.Tests/SitePlanAdjustment/SeminoleCommissionedHouseFitContractTests.cs`: 7 contracts building the commissioned profile from real `IxMiliaWallExtractor`/`IxMiliaRoomLabelExtractor` identities of `SEMINOLE2000.dxf` (env override `FLOORPLANFIT_SEMINOLE_FIXTURE`, fallback `D:\PointAIData\PLANS\originalFloorPlans\SEMINOLE2000.dxf`, clean skip when absent).
- Cases: Width-only 2", Depth-only 1.5", combined with deterministic [Width, Depth] order, exact rigid fit with zero actions, insufficient capacity blocker, protected-Stretch-target blocker, and real-profile readiness with exact capacities. Zero production changes.
- 3.6 scope is planner-level (roles/spans/protected identity); byte-level DXF deformation proof is owned by the Phase 2.7-2.8 replay contracts; visual proof is Phase 8.7.
- Audited pre-existing production SEMINOLE string: `DxfExtractionProfile.cs:117` names known vendor plans inside the "Pointe Homes CAD" extraction profile (layer-mapping metadata). It is not a commissioned-fit branch; the commissioned planner never reads it. Accepted under 8.4 with this explicit note.

### Phase 4 — make Electrical devices follow explicit hosts

- [x] **4.1** Inventory the existing registration/profile evidence before adding data. Reuse the current JSON document/repository; add no persistence table.
- [x] **4.2** Define the minimum supported device as an `INSERT` with exactly one confirmed wall or room host established during commissioning/registration, never inferred daily by proximity.
- [x] **4.3** Persist or consume that host identity and bind it to the same canonical variable action that moves the host.
- [x] **4.4** Project the device by the host's exact cumulative transform, not by comparing its coordinate to a global cut line.
- [x] **4.5** Preserve device count, block name, scale, rotation, attributes, and size.
- [x] **4.6** Reject missing, duplicate, stale, incompatible, or multiply-matched hosts before writing output.
- [x] **4.7** Add focused contracts for one wall-hosted device, one room-hosted device if existing evidence supports it, one fixed device, and one invalid host.
- [x] **4.8** Phase gate: supported devices move only because their confirmed host moved and all payload/count invariants remain true.

#### Phase 4 evidence — 2026-07-21

- Inventory confirmed zero persisted host/wire evidence anywhere; the consumption seam is `ProjectedPlanSheetExportRecipe.OverlayReconciliation` with `ElectricalDeviceHostBinding` (HostKind `Wall`|`Room`, `HostSourceEntityRef`, precompiled delta validated against the recipe axis by `EnsureHostDeltaMatchesRecipeAxis`). No new table, no Contracts change.
- Discriminating contracts prove evidence-only movement: a device at x=4 with a cut at 6 moves only because its host binding says so; a `Fixed` device crossing the cut stays put. Missing/duplicate/stale/incompatible bindings reject before any output file exists.
- 4.7 honest scope: wall-hosted, fixed, and invalid-host are contracted; `Room` hosts are accepted by the model but no producer emits them yet, so room-hosted stays fail-closed by design.
- Open producer seam: one-time commissioning does not yet author device bindings, so today every locally-deforming commissioned Electrical export fails closed with an actionable reason (see Phase 6 evidence).

### Phase 5 — regenerate only supported Electrical wire routes

- [x] **5.1** Inventory current Electrical route carriers and registration evidence; do not treat entity-type support as connectivity.
- [x] **5.2** Define the smallest supported route shape: known ordered endpoints/vertices whose endpoint hosts are explicit and whose carrier can be regenerated safely.
- [x] **5.3** Store/consume the route-to-endpoints/hosts evidence in the existing registration/profile document; no generic graph framework.
- [x] **5.4** Rebuild a supported route from final transformed endpoints while preserving its layer and supported visual payload.
- [x] **5.5** Reject ARC/SPLINE/unknown topology or any route lacking complete connectivity as `UnsupportedWireRoute` before output creation.
- [x] **5.6** Add one supported regeneration contract, one fixed/unaffected route contract, and one unsupported-connectivity blocker contract.
- [x] **5.7** Phase gate: every exported wire is either regenerated from known connectivity or the complete export is blocked explicitly.

#### Phase 5 evidence — 2026-07-21

- Supported route = `ElectricalWireRouteBinding(carrier, startDevice, endDevice)` where both endpoints are commissioned device bindings; LINE carriers regenerate from the final device endpoints, preserving layer and non-coordinate payload.
- Under a locally-deforming recipe: non-LINE route carriers, carriers with no route and no explicit static declaration, and route endpoints without device bindings all reject as `UnsupportedWireRoute` (or the specific binding reason) before output creation.
- 12 new contracts in `ProjectedPlanSheetDxfExporterTests` cover device movement, fixed devices crossing cuts, missing/duplicate/stale bindings, route regeneration, fixed routes, unsupported carriers/topology, endpoint validation, and axis-mismatched deltas.

### Phase 6 — close final Electrical composition and automatic package atomicity

- [x] **6.1** Keep the already-proved adjusted canonical `ArchitecturalBase`; do not revive or deform `ELECTRICAL WALLS`.
- [x] **6.2** Compose only reconciled hosted devices and regenerated supported wires into `ElectricalOverlay`.
- [x] **6.3** Verify final Floor/Electrical structural footprints are congruent by construction and device/wire counts match their supported contracts.
- [ ] **6.4** Make the before/after comparison display the real Electrical overlay as well as canonical architecture; label what is intentionally unsupported. *(Partial: honest labeling of the current device/wire exclusion is contracted Desktop-side; displaying the real overlay is blocked on the commissioning producer seam below.)*
- [x] **6.5** Require current source-bound registration hashes/evidence; stale or missing proof disables confirmation/export.
- [x] **6.6** In automatic discovery, inspect each required dependent sheet before filtering. Require exactly the commissioned Electrical sheet in `ReadyForExport`.
- [x] **6.7** If required Electrical is missing or non-ready, abort staging, preserve the loose Floor source, publish no package, and report the Electrical reason.
- [x] **6.8** Preserve explicit legacy projection-ID behavior only where its existing manual audit is still required.
- [x] **6.9** Prove success publishes exactly five named artifacts and no loose duplicate; prove every failure leaves no final/staging package.
- [x] **6.10** Phase gate: automatic commissioned export is all-or-nothing Floor + Electrical and its comparison/audit describes the actual final files.

#### Phase 6 evidence — 2026-07-21

- Composition trigger is honesty-preserving: an affine-only recipe keeps the proven legacy whole-sheet path (devices/wires safe by rigid construction); a locally-deforming recipe requires `OverlayReconciliation` and otherwise fails closed with an actionable reason — exactly where V1 corrupted geometry.
- `ArchitecturalBase` remains the adjusted canonical FloorPlan; `ELECTRICAL WALLS` stays excluded; all 9 pre-existing composition contracts pass untouched by inspection.
- 6.5 was verified as already enforced: `BuildExportRecipeAsync` requires Confirmed registration plus source-bound SHA-256 proof.
- **Open producer seam (the one honest runtime boundary):** `ExportProjectedPlanSheetHandler` currently builds recipes with `OverlayReconciliation = null`, so a deforming commissioned Electrical export fails closed today (atomic: no package, explicit reason) until one-time commissioning authors device-host and wire-route evidence. This is the designed safety behavior, not a regression; authoring that evidence is the next implementation slice after 8.7.

#### Phase 6.6-6.7 evidence — 2026-07-21

- `ExportMultiSheetPlanSetPackageRequest.RequireReadyElectricalPlan` declares the commissioned invariant; `SitePlanAdjustmentViewModel` sets it from `isCommissionedAutoFit` on both package call sites. The legacy manual confirm-then-re-export flow (empty projection IDs, flag off) keeps its intermediate manual-audit package because the existing Desktop contract proves the operator needs that audit to confirm.
- Automatic discovery no longer pre-filters non-ready latest projections (`ResolveRequestedProjectionsAsync`); `EnsureRequiredElectricalProjectionIsReadyAsync` inspects every latest projection's sheet type before any dependent export and fails closed at `DependentSheetGeneration` inside the staging callback for a non-ready or absent required Electrical.
- "Required Electrical" semantics: every auto-discovered `ElectricalPlan` latest projection must be `ReadyForExport` and at least one must exist; profile-level Electrical sheet identity binding does not exist yet and remains future work if commissioning ever names a specific sheet.
- Focused contracts: fail-closed non-ready, fail-closed absent, no-over-block ready, legacy flag-off audit retention, and the commissioned Desktop rigid-fit export rejection. Static checks passed; `.NET` execution remains reserved for Phase 8.
- Bug record: [[2026-07-21 - Automatic package could publish without required Electrical]].

### Phase 7 — prove the three-decision daily UX

- [x] **7.1** Decision 1 is selecting one `Auto-fit ready` Library version; `Setup required` remains disabled with a reason.
- [x] **7.2** Decision 2 is selecting the Site Plan through the existing direct picker.
- [x] **7.3** The proposal is calculated automatically; no Pinch, vertex, entity ID, crossing-window, commissioning, or legacy suggestion control appears.
- [x] **7.4** Show one recommended outcome: exact requested/allocated Width and Depth changes, preservation status, Electrical status, and any blocker.
- [x] **7.5** Show trustworthy Floor/Electrical before/after evidence before confirmation.
- [x] **7.6** Decision 3 is the sole `Confirmar y exportar` action; unsafe/missing evidence disables it.
- [x] **7.7** Add one focused Desktop contract enumerating those three decisions and proving forbidden controls/actions are absent in commissioned mode.
- [x] **7.8** Remove only newly dead commissioned pinch entry points after replacement coverage exists; retain unrelated legacy paths rather than broad cleanup.
- [x] **7.9** Phase gate: the commissioned path needs no fourth primary decision and cannot enter runtime Pinches.

#### Phase 7 evidence — 2026-07-21

- New `tests/FloorplanFit.Desktop.Tests/Layout/SitePlanAdjustmentWindowLayoutTests.cs` (5 XAML/source contracts) plus one Library contract and one commissioned-integration contract: single `Confirmar y exportar` action, every legacy control gated behind `!IsCommissionedAutoFit`, zero pinch/vertex/entity/crossing controls in the adjustment window, exact-delta recommendation with honest preservation and Electrical labeling, and `Setup required` blocked with a visible reason (Library ToolTip + StatusMessage).
- 7.8 honest finding: no dead commissioned pinch entry point exists to remove — the commissioned path was born clean (`Commissioned.*Pinch` grep = 0); the remaining pinch paths are retained legacy (review/advanced commissioning and legacy `Build`).
- 7.5 shows the contracted honest labeling; the real device/wire overlay upgrade follows the Phase 6 producer seam.
- Independent sweep confirmed `CommissionedHouseFitPlanner`/`RequestFactory`/`Handlers`/`CommissionedHouseFitPreviewProjector` contain zero references to `CadStretchRecipeCompiler`, `IAutoFitPlanSuggester`, `ToFloorSourceStretchAction`, `PinchMarker`, or `PinchGroup`; hits in `CommissionExistingCurationProfileCompiler` are the sanctioned one-time commissioning reads.

### Phase 8 — static closeout and one finite external proof

- [x] **8.1** Audit every acceptance criterion against an authoritative source/test/artifact; uncertain evidence remains red.
- [x] **8.2** Inspect all changed branches for source/test API consistency and cancellation/fail-closed behavior.
- [x] **8.3** Run allowed static checks only: scoped `git diff --check`, XAML XML parsing, forbidden-symbol/hardcode searches, and PowerShell parser/static contracts that do not invoke `.NET`.
- [x] **8.4** Confirm the commissioned runtime has no Pinches/suggester fallback and production contains no SEMINOLE path/name hardcode.
- [x] **8.5** Update Current State, decisions/bugs, and this checklist with exact evidence; do not mark runtime green from static inspection.
- [x] **8.6** Provide one external script/command sequence that runs focused `.NET` tests, creates Width-only/Depth-only/combined SEMINOLE exports, validates all three packages, and names their audit files.
- [ ] **8.7** User opens the three Floor/Electrical outputs in AutoCAD and confirms final overlay, openings, devices, and supported wires; failures return to the exact numbered micro-step they contradict.
- [ ] **8.8** Completion gate: every checkbox above is evidenced green, the external sequence is reproducible, and no required acceptance item remains missing or merely inferred.

#### Phase 8 evidence — 2026-07-21

- 8.3: scoped `git diff --check` green on every touched file (CRLF warnings only); `MainWindow.axaml` and `SitePlanAdjustmentWindow.axaml` parse as XML; the new proof script parses with the PowerShell language parser.
- 8.4: commissioned runtime symbol sweep is clean (see Phase 7 evidence). The only production SEMINOLE string is the pre-existing "Pointe Homes CAD" extraction-profile plan-name list (`DxfExtractionProfile.cs:117`) — vendor layer-mapping metadata never read by the commissioned planner; accepted with this note.
- 8.6: `scripts/run-commissioned-autofit-proof.ps1` — Stage 1 runs the focused suites (`SeminoleCommissionedHouseFitContractTests`, planner/readiness/compiler/preview suites, package handler, projected/canonical exporters, Desktop layout/integration suites); Stage 2 validates each exported user package (exactly five named artifacts, coherent stem, no `.staging-` leakage, green manifest); Stage 3 chains `verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.
- 8.7 honest expectation: the rigid-fit case proves the full five-artifact package end to end; deforming cases currently prove the fail-closed contract (no package + exact Electrical reason) because commissioning does not yet author overlay reconciliation evidence. Runtime green is NOT claimed from static inspection.
- 8.8 remains open until the user completes 8.7; the only intentionally-unfinished acceptance item is the real deforming Electrical overlay, gated fail-closed behind the Phase 6 producer seam.

## Execution and anti-overloop contract

1. Maintain exactly one `in_progress` micro-step. Independent agents may work in parallel only with disjoint files and explicit numbered ownership.
2. Every micro-step must end with at least one new artifact: focused RED contract, minimal production diff, static proof, explicit blocker evidence, or runtime evidence. Reading/replanning alone is not progress.
3. Never repeat the same hypothesis or materially identical edit more than twice. After the second failed attempt, record why it failed and pivot to the next smallest alternative.
4. Never reopen an `[x]` item without new contradictory evidence tied to a named acceptance criterion. If reopened, mark the old claim superseded in Obsidian and Engram.
5. A blocked micro-step does not pause the goal while another dependency-independent step can advance. Continue the next unblocked numbered item.
6. Pause only when **all** are true:
   - the same blocker has recurred for three consecutive goal turns;
   - it requires unavailable external state/user action or no safe repository change can produce new evidence;
   - no other meaningful micro-step is unblocked;
   - the blocker, two attempted hypotheses, evidence, and one exact unblock action are documented.
7. Never pause because work is difficult, a test is red, a build is forbidden, context is large, or more investigation would be useful.
8. Never mark complete because a subset passes, token/time use is high, or only static checks are green. Complete only at **8.8**.
9. Scope stays fixed: SEMINOLE proof fixture without production hardcoding, one Width, one Depth, Floor + Electrical. Roof, Facade, generic BIM, arbitrary-DXF automation, and a general optimizer remain excluded.
10. Repository policy remains absolute: the agent never runs `dotnet`, build, restore, watch, or Desktop. It must finish all static work first and request the single external sequence only at 8.6.
