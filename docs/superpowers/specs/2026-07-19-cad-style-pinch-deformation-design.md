# CAD-Style Pinch Deformation Design

**Date:** 2026-07-19  
**Status:** Approved for implementation  
**Decision:** `obsidian-vault/Decisions/2026-07-19 - Adopt CAD-style pinch stretch actions.md`

## 1. Purpose

Replace the current coordinate-global compression rule with one deterministic CAD-style stretch action per logical paired-wall pinch group.

The product contract is:

- the two markers on the two faces of one wall represent one action and one total delta;
- only the two authorized wall spans may change length;
- complete entities selected on the closing side translate rigidly once;
- fixed and unselected entities remain coordinate-identical;
- wall thickness and rigid entity shape are preserved;
- unsupported or unselected geometry crossing the cut fails closed before output;
- Preview, canonical FloorPlan DXF, and registered ElectricalPlan DXF replay the same deformation semantics.

This affects **Loop 1** (curation, site-fit preview, canonical export) and **Loop 2** (registered Electrical projection/export). It does not redesign registration or HousePlanSet packaging.

## 2. Verified Current State

### Contracts

`AdjustedSitePlanPlacementDto.cs` persists compression as `AdjustedCompressionStepDto -> AdjustedCompressionMarkerDto(Coordinate, TrimSourceUnits)`. `AdjustmentRecipeSummaryDto.FromPlacement` flattens every marker into an independent recipe-v1 operation containing only kind, axis, edge, coordinate, and delta.

That format cannot express:

- that two markers are one paired wall;
- which exact entity/segment is allowed to stretch;
- which entities move rigidly;
- which side remains fixed;
- which crossings must reject the operation.

### Desktop preview

`FloorPlanPreviewGeometry.CreatePreviewGeometry` calls its private `TransformPoint` for every segment endpoint. `TransformPoint` accumulates every marker delta solely from endpoint coordinate and edge. Entity identity and role are absent.

### Canonical FloorPlan export

`IxMiliaAdjustedSitePlanExporter.ApplyCompressionPoint` repeats the coordinate/edge threshold over raw DXF points. Two wall-face markers can therefore be counted twice, and unrelated entities can have only one endpoint moved.

### Electrical projection/export

`ElectricalRecipeProjection.ApplyRecipe` repeats the same point-threshold behavior after registration. `ProjectedPlanSheetDxfExporter` already owns extensive DXF entity handling, source-bound registration proof, unsupported-curve checks, operation audits, and DXF safety validation, but the final point deformation still delegates to the global recipe rule.

### Reusable infrastructure

The following remain authoritative and unchanged in responsibility:

- canonical adjustment persistence and versioned recipe JSON;
- confirmed Electrical-to-canonical registration transform;
- source-SHA-bound whole-plan registration proof;
- projection status and manual-review gates;
- HousePlanSet package orchestration, manifest, audit artifacts, and DXF safety checks.

## 3. Non-Goals

- No RoofPlan or Facade/Elevation support in this change.
- No whole-building persisted topology graph.
- No general geometric constraint solver.
- No plan-, room-, coordinate-, filename-, or SEMINOLE-specific rule.
- No new SQLite table unless a RED compatibility contract proves the existing recipe JSON cannot carry the required data.
- No automatic deformation of arbitrary curves that cross a cut.
- No unrelated UI redesign.

## 4. Core Model

### 4.1 One logical action

Markers are grouped by `PinchGroupId`, not flattened independently. A valid v2 group has exactly two markers for the same axis, each bound to a distinct wall-face geometry path. The action delta is allocated once to the group. Both target faces receive that same delta.

Multiple logical groups may still share a requested width/height deficit. Allocation occurs between groups according to their capacities; it never occurs between the two faces inside one group.

### 4.2 CAD roles

Every resolved entity has exactly one operation-local role:

| Role | Behavior |
|---|---|
| `Stretch` | Move only the explicitly listed closing-side vertex/vertices by the action vector. Initially limited to LINE and straight LWPOLYLINE segments. |
| `RigidMove` | Translate every position/control point of the complete entity by one identical vector. Radius, angle, scale, text metrics, block content, layer, and other shape properties do not change. |
| `Fixed` | No coordinate or property changes. This is the implicit default. |
| `Rejected` | Preflight failure; no output is written. |

The pure engine does not infer roles from coordinates. Adapters resolve roles first, then the engine validates and executes the explicit plan.

### 4.3 Cut and closing side

Each action stores:

- axis (`Width` or `Height`);
- closing edge (`Left`, `Right`, `Top`, or `Bottom`);
- canonical cut coordinate;
- requested delta in Floor source units;
- maximum allowed delta;
- two canonical target span geometries;
- exact canonical Floor role bindings;
- source bounds/tolerance needed for dependent resolution.

The closing edge defines one translation vector:

- `Right`: `(-delta, 0)`;
- `Left`: `(+delta, 0)`;
- `Top`: `(0, -delta)`;
- `Bottom`: `(0, +delta)`.

There is no second additive marker transform. Each target face moves its closing-side endpoint by this same vector, shortening both faces by exactly `delta` and preserving their separation.

### 4.4 Operation-scoped resolution, not global topology

At authoring/placement time, the compiler resolves the marker ratio against its `GeometryPathDto` to identify the containing `GeometrySegmentDto`. It joins `SourceCandidateId` to `WallCandidateDto.SourceEntityRef` for raw-DXF replay.

The compiler performs only an operation-scoped adjacency/selection scan:

1. validate two distinct parallel target spans and one shared cut station within source-unit tolerance;
2. classify each entity envelope against the action cut half-plane and tolerance;
3. select an entity wholly on the closing side as `RigidMove`;
4. leave an entity wholly on the fixed side implicitly `Fixed`;
5. for each of the two target LINE/LWPOLYLINE entities, mark every closing-side vertex as `Stretch`; this makes the authorized crossing segment shorter while same-side adjacent segments translate without changing length;
6. reject a target polyline when any segment other than the authorized target span crosses the cut, because moving its shared vertices would change another span;
7. reject every non-target entity whose vertices or true curve envelope straddle/touch both sides of the cut, and reject any ambiguous/unsupported target.

The side test is exact and shared: `Right` means closing coordinates greater than the cut, `Left` means less than it, `Top` means greater than it, and `Bottom` means less than it, all with the action's source-unit tolerance. Coordinates inside the tolerance band are accepted only when they belong to an explicitly resolved target vertex; otherwise classification is ambiguous and fails closed.

For rigid shapes, classification uses their full geometric envelope, not only a center point. A circle, arc, ellipse, insert, text object, or dimension completely on one side may move/fix as a unit; one that intersects the cut is rejected in v2 unless a future explicit action type authorizes its deformation.

The resolved Floor role set is persisted. Runtime Preview and Floor export do not rediscover it from a half-plane threshold.

The implementation may use a direct scan over current operation entities. A reusable whole-plan graph is explicitly deferred until measured performance or unsupported topology requires it.

## 5. Recipe V2 Contract

Keep the existing positional constructor of `AdjustmentRecipeSummaryDto` for source and v1 JSON compatibility. Add an optional initialized v2 action collection; missing JSON maps to an empty collection.

Conceptual contract:

```csharp
AdjustmentRecipeSummaryDto
  Version                 // "v1" or "v2"
  FloorToSiteScale
  SiteOffsetX
  SiteOffsetY
  Operations              // retained legacy v1 operations
  StretchActions = []     // new v2 actions

AdjustmentRecipeStretchActionDto
  ActionId                // stable PinchGroupId representation
  AxisTag
  Edge
  CutCoordinate
  DeltaSourceUnits        // total logical delta, once
  MaxDeltaSourceUnits
  CoordinateTolerance
  CanonicalSourceBounds
  TargetSpans[2]
  CanonicalEntityRoles[]

AdjustmentRecipeTargetSpanDto
  SourceEntityRef
  GeometryPathId
  SegmentSortOrder
  StartX, StartY, EndX, EndY
  ClosingVertexIndex

AdjustmentRecipeEntityRoleDto
  EntityRef
  GeometryPathId?
  SegmentSortOrder?
  Role                     // Stretch or RigidMove; Fixed is implicit
  VertexIndices[]
```

The exact C# records may be colocated in the existing Contracts file unless file size becomes materially harmful. Do not add interfaces or polymorphic JSON for one v2 action type.

### Version behavior

- Existing stored `v1` JSON deserializes unchanged and continues through the legacy path.
- A new placement is `v2` only when all active compression groups compile successfully.
- New compressed adjustments must not silently fall back to v1. Compilation ambiguity produces a visible fail-closed result.
- Affine-only adjustments may remain v1 with zero operations or use v2 with zero actions; choose the smallest compatibility path during RED implementation.

## 6. Shared Pure Engine

Place the shared engine in Application so both Desktop and Infrastructure can reference it without reversing dependencies. It must not reference Avalonia or IxMilia.

The minimum API is a static/concrete service, not an interface:

```csharp
CadStretchResult Apply(
    CadStretchAction action,
    IReadOnlyList<CadStretchEntity> entities,
    IReadOnlyList<CadStretchEntityRole> roles);
```

The neutral entity model needs only stable entity ID and positional vertices. The result is an edit plan rather than DXF objects:

- per-entity rigid translation vector; or
- per-vertex translation vectors for `Stretch`; or
- no edit for `Fixed`.

Adapters remain responsible for applying an edit to their native representation. This preserves circles, arcs, inserts, dimensions, and text because rigid entities receive a translation instruction rather than a reconstructed polyline.

### Engine validation

Before returning edits, validate all actions against the original immutable input:

- positive delta and `delta <= max`;
- exactly two distinct target spans;
- exactly one role per entity per action;
- referenced entities and vertices exist;
- only supported straight spans use `Stretch`;
- both target spans shorten by the same measured delta;
- original and final paired-face separation match within tolerance;
- every `RigidMove` vertex receives exactly one identical vector;
- every implicit `Fixed` entity has no edit;
- no rejected crossing exists.

All actions are preflighted before any adapter writes output. Failure returns a structured rejection; it never returns a partial edit plan.

## 7. Adapter Flows

### 7.1 Desktop Preview — Loop 1 / Desktop

1. `SitePlanAdjustmentViewModel` groups current markers by `PinchGroupId` and compiles v2 actions using review geometry/wall candidates.
2. `FloorPlanPreviewGeometry` converts geometry paths to neutral entities and applies engine edit plans.
3. Existing viewport/rendering remains unchanged.
4. The preview may color Stretch/RigidMove/Fixed roles, but new visual polish is not required for correctness.

The old `TransformPoint` remains only for explicit v1 compatibility and is never called for v2.

### 7.2 Canonical FloorPlan DXF — Loop 1 / Infrastructure

1. `IxMiliaAdjustedSitePlanExporter` maps raw entity/vertex references to neutral entities.
2. It resolves persisted canonical roles exactly; it does not classify by coordinate threshold.
3. It preflights every action through the shared engine.
4. It applies returned vertex or rigid translations to raw DXF pairs, then continues existing dimension patching/site-plan injection and output writing.

The existing raw-pair preservation strategy stays because it previously avoided invalid/black AutoCAD outputs.

### 7.3 ElectricalPlan DXF — Loop 2 / Application + Infrastructure

1. `ExportProjectedPlanSheetHandler` keeps its current ownership/proof/hash checks and deserializes v1 or v2.
2. `ProjectedPlanSheetDxfExporter` transforms supported Electrical geometry through the confirmed registration into canonical coordinates.
3. The v2 resolver matches exactly one Electrical structural pair to the two canonical target span geometries using orientation, overlap/endpoints, registered residual tolerance, and structural-layer evidence already used by registration/export. It never copies Floor entity IDs.
4. It resolves its own explicit role set. Complete selected closing-side entities become `RigidMove`; the matched target spans become `Stretch`; any other crossing that was not selected/supported becomes `Rejected`.
5. The shared engine produces canonical edit instructions. The exporter applies registration + action + final placement while preserving native entity semantics.
6. Zero or multiple target-pair matches, stale proof/source hash, unsupported crossing, or invariant failure becomes `RequiresManualConfirmation` and produces no dependent output.

`ElectricalRecipeProjection.ProjectPoint` remains only for v1. V2 projection must not be expressible as an arbitrary standalone point function because role is an entity/vertex property.

## 8. Atomicity and Observability

Extend existing operation audits without breaking positional constructors where possible. Each v2 action records:

- action ID and source pinch group;
- axis, edge, cut coordinate, requested delta, measured delta;
- two target span refs and their before/after lengths;
- paired-face separation before/after;
- Stretch entity/vertex count;
- RigidMove entity/vertex count;
- Fixed entity/vertex count;
- rejected crossings and exact reason;
- invariant status;
- resolver evidence for Electrical target matching.

Use current package/audit artifact writing. No separate telemetry subsystem is needed.

Output rule:

- resolve and validate all actions in memory;
- write only after every action passes;
- use existing package staging/cleanup behavior;
- on failure, persist/report the manual-review reason but leave no partial dependent DXF.

## 9. Minimal TDD Matrix

### Pure engine — new Application test file

`CadStretchDeformationEngineTests`:

1. paired target spans receive one total delta, not two;
2. only listed closing vertices change;
3. rigid entity vertices receive one identical vector;
4. fixed entity edit set is empty;
5. paired separation is unchanged;
6. duplicate/missing role or unsupported stretch rejects without edits.

### Recipe compatibility

Extend `AdjustmentRecipeSummaryDtoTests`:

- v1 JSON without v2 fields deserializes;
- two same-group markers compile to one v2 action/delta;
- malformed one/three-marker groups fail closed;
- multiple groups allocate between groups, not faces.

### Preview and canonical export

- Extend `FloorPlanPreviewGeometryTests` for explicit role behavior.
- Extend `IxMiliaAdjustedSitePlanExporterTests` with the same synthetic geometry and assert Preview/Floor coordinates agree, untouched entities are byte/coordinate-equivalent, and no partial output is written on rejection.

### Electrical

- Keep `ElectricalRecipeProjectionTests` as v1 compatibility tests.
- Extend `ProjectedPlanSheetDxfExporterTests` for unique registered target resolution, own Electrical IDs, rigid block/symbol translation, unselected crossing rejection, paired thickness, exact delta, and Floor/Electrical output parity.
- Extend `ExportProjectedPlanSheetHandlerTests` only if v2 deserialization/manual-state propagation is not already covered by exporter tests.

No new fixture framework is needed. Reuse existing synthetic raw-DXF builders and registration proof helpers.

## 10. Expected File Ownership

Likely production changes:

- `src/FloorplanFit.Contracts/FloorPlans/AdjustedSitePlanPlacementDto.cs`
- one new pure Application engine file under `FloorPlans/SitePlanAdjustment/`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs`
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedSitePlanExporter.cs`
- `src/FloorplanFit.Application/PlanSets/Projection/ElectricalRecipeProjection.cs` (v1 boundary)
- `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs`
- existing audit DTO/writer only if RED requires additional fields.

Likely test changes are the files named in section 9 plus one pure engine test file.

## 11. Risks and Fail-Closed Responses

| Risk | Response |
|---|---|
| Marker ratio cannot resolve one segment | Reject action compilation. |
| Two markers are not parallel/aligned or do not represent distinct faces | Reject action compilation. |
| Floor raw entity ref cannot be replayed | Reject canonical export before writing. |
| Electrical has no/ambiguous equivalent target pair | Require manual confirmation; no Electrical output. |
| Arc/circle/ellipse/spline crosses cut | Reject v2 automatic export; rigid translation remains allowed when whole entity is selected. |
| Several actions reference the same entity incompatibly | Reject combined preflight; do not apply sequential partial mutations. |
| v1 historical recipe | Route unchanged through legacy behavior. |
| Existing binary-DXF preservation | Keep raw-pair patching and existing safety audit; do not round-trip through a new writer. |

## 12. Rejected Alternatives

1. **Keep coordinate thresholds but filter layers.** Rejected: layer does not express entity/vertex intent and still deforms unrelated geometry.
2. **Persist a whole-building topology graph.** Rejected: imported DXF connectivity is noisy and the current requirement needs only operation-local selection.
3. **Copy Floor entity IDs into Electrical.** Rejected: the drawings have different entity identities; registration maps geometry, not IDs.
4. **Use separate Preview/Floor/Electrical engines.** Rejected: this created the current drift and false confidence.
5. **Add a general constraint solver.** Rejected: disproportionate scope; explicit roles plus invariant checks are sufficient.

## 13. Self-Review Against Accepted Goal

- [x] One paired-wall group produces one delta.
- [x] Only exact authorized spans may shorten.
- [x] RigidMove and Fixed semantics are explicit.
- [x] Wall thickness/shape invariants are mandatory.
- [x] Unselected/unsupported crossings fail closed atomically.
- [x] No persisted whole-plan graph or new table.
- [x] Existing canonical/Electrical registration and package pipeline is reused.
- [x] V1 remains readable; v2 cannot call point-threshold logic.
- [x] Preview, Floor export, and Electrical export share one pure engine.
- [x] Electrical resolves its own IDs through registered canonical geometry.
- [x] Minimal RED locations and external-proof boundary are identified.
