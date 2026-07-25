# CAD-Style Pinch Deformation Implementation Plan

**Design:** `docs/superpowers/specs/2026-07-19-cad-style-pinch-deformation-design.md`  
**Constraint:** Do not run `.NET` commands; leave one external proof sequence.

## Task 1 — RED: pure semantics and recipe compatibility

**Tests**

- Create `tests/FloorplanFit.Application.Tests/FloorPlans/SitePlanAdjustment/CadStretchDeformationEngineTests.cs`.
- Extend `tests/FloorplanFit.Application.Tests/PlanSets/Projection/AdjustmentRecipeSummaryDtoTests.cs`.

Cover one delta for two faces, explicit vertex stretch, rigid translation, implicit fixed, pair spacing, zero edits on rejection, v1 JSON, and one-action v2 JSON.

**Static gate:** tests name observable behavior and reference no Avalonia/IxMilia types.

## Task 2 — GREEN: minimal v2 Contracts

**Files**

- Modify `src/FloorplanFit.Contracts/FloorPlans/AdjustedSitePlanPlacementDto.cs`.

Keep every existing positional constructor. Add initialized optional v2 collections and the minimum target-span/entity-role records. Missing v2 JSON must deserialize to empty collections. V1 `Operations` remains unchanged.

**Static gate:** all existing `new AdjustmentRecipeSummaryDto(...)` call shapes remain valid; no persistence schema changes.

## Task 3 — GREEN: shared pure engine

**Files**

- Create `src/FloorplanFit.Application/FloorPlans/SitePlanAdjustment/CadStretchDeformationEngine.cs`.

Implement explicit-role preflight and edit-plan generation. The engine receives resolved IDs/vertices; it never selects from a coordinate threshold. It returns no edits on any failure.

**Static gate:** no Desktop/Infrastructure references; every branch maps directly to a RED contract.

## Task 4 — RED/GREEN: compile paired markers once

**Tests**

- Add focused compiler tests beside the pure engine tests or the smallest existing site-adjustment test file.
- Update ViewModel tests only if orchestration cannot be exercised below Application.

**Production**

- Add the smallest compiler beside the engine or in the existing Application site-adjustment folder.
- Modify `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs` only to call it and place v2 actions on `AdjustedSitePlanPlacementDto`.

Group by `PinchGroupId`; require exactly two distinct aligned wall paths; allocate delta between logical groups, never between faces; resolve path ratio to exact segment/source ref; fail closed instead of falling back to v1.

## Task 5 — RED/GREEN: Desktop Preview adapter

**Tests**

- Extend `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewGeometryTests.cs`.

**Production**

- Modify `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs`.

For v2, map geometry paths to neutral entities, call the shared engine, and apply its edit plan. Keep `TransformPoint` only behind an explicit v1 path.

**Parity gate:** the synthetic v2 preview coordinates become the canonical expected coordinates for both exporters.

## Task 6 — RED/GREEN: canonical Floor DXF adapter

**Tests**

- Extend `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaAdjustedSitePlanExporterTests.cs`.

**Production**

- Modify `src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedSitePlanExporter.cs`.

Resolve persisted raw entity/vertex refs, preflight all actions, call the shared engine, and patch raw DXF pairs. Preserve dimension patching, site-plan injection, handles/owners, and raw-pair output. V2 must never call `ApplyCompressionPoint`.

**Atomic gate:** rejected action creates no final output file.

## Task 7 — RED/GREEN: registered Electrical adapter

**Tests**

- Extend `tests/FloorplanFit.Infrastructure.Tests/Dxf/ProjectedPlanSheetDxfExporterTests.cs`.
- Extend `tests/FloorplanFit.Application.Tests/PlanSets/Export/ExportProjectedPlanSheetHandlerTests.cs` only if handler status propagation lacks coverage.

**Production**

- Keep `src/FloorplanFit.Application/PlanSets/Export/ExportProjectedPlanSheetHandler.cs` proof/hash/ownership flow.
- Keep `src/FloorplanFit.Application/PlanSets/Projection/ElectricalRecipeProjection.cs` as v1-only projection.
- Modify `src/FloorplanFit.Infrastructure/Dxf/ProjectedPlanSheetDxfExporter.cs` to resolve v2 roles in registered canonical coordinates and call the shared engine.

Match exactly one Electrical structural pair to canonical target spans. Use Electrical's own IDs. Fully closing-side entities move rigidly; fixed-side entities remain fixed; every other cut crossing rejects. Preflight before write.

## Task 8 — Observability and manifest integration

**Tests**

- Extend existing audit assertions rather than creating a new audit framework.

**Production**

- Extend current Floor/Electrical operation audit DTOs with backward-compatible initialized properties only when required.
- Reuse current manifest/audit writers.

Record role counts, target refs, before/after lengths and pair spacing, requested/measured delta, invariant status, Electrical match evidence, and rejection reason.

## Task 9 — Static completion audit and external handoff

Allowed checks:

1. scoped `git diff --check`;
2. source searches proving no v2 branch calls coordinate-threshold functions;
3. constructor/call-site shape inspection;
4. XAML/XML parse only if XAML changes (none expected);
5. requirement-to-test/source evidence matrix.

Update Obsidian Current State, implementation record, and any discovered bug note. Do not commit/push.

Deliver exactly one external sequence:

1. stop/restart source watch launcher;
2. run focused tests/build externally;
3. author a fresh paired pinch adjustment;
4. export HousePlanSet;
5. run the existing latest-manifest verifier;
6. overlay Floor/Electrical in AutoCAD and inspect the new action audit.

