# HousePlanSet completion audit - 2026-07-01

## Verdict

The HousePlanSet modularization is **architecturally implemented enough for the first tool loop**, but the full goal is **not proven complete**.

The blocker for declaring completion is not another known module. The missing evidence is a real/manual end-to-end smoke with dependent `ElectricalPlan`, `RoofPlan`, and `FacadeElevation` DXFs. The repo currently contains only:

- `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
- `PLANS/originalFloorPlans/SEMINOLE2000.dxf`
- `PLANS/originalsSitePlans/158 DAWSON STREET.dxf`

No real electrical/roof/facade DXF samples were found in the worktree.

## Requirement audit

| Requirement | Current evidence | Status |
| --- | --- | --- |
| Treat a house as `HousePlanSet`, not isolated floor plans | `HousePlanSet`, `PlanSetVersion`, `PlanSheet`; repositories; Library selected plan-set sheets | Implemented |
| Phase 1: conceptual PlanSet backbone | `ResolveHousePlanSetHandler`, `ResolvePlanSetVersionHandler`, SQLite repos | Implemented |
| Phase 2: multiple sheets per house | `ImportPlanSheetHandler`, `SqlitePlanSheetRepository`, selected sheet list, unlink/correct/reject recovery | Implemented |
| SheetImport | managed file/imported document/measurement context/sheet persistence | Implemented |
| SheetClassification | `ClassifyPlanSheetHandler`, DXF layer hints, classification quality events, user correction | Implemented |
| Phase 3: register electrical | `RegisterElectricalSheetHandler`, manual transform dialog, confirm/reject, quality labels | Implemented |
| Phase 4: project electrical | `ProjectElectricalSheetAdjustmentHandler`, `ProjectRegisteredPlanSetSheetsHandler`, confirmation/export gates | Implemented |
| Phase 5: roof rules | `RegisterRoofSheetHandler`, `ProjectRoofSheetAdjustmentHandler`, overhang rule summary | Implemented, needs real roof validation |
| Phase 6: facade/elevation as separate case | `RegisterFacadeElevationSheetHandler`, `ProjectFacadeElevationSheetAdjustmentHandler`, horizontal-preserve-vertical rule | Implemented, needs real facade validation |
| CanonicalFloorPlanAdjustment | `RecordCanonicalFloorPlanAdjustmentHandler`, SQLite persistence, Desktop export flow integration | Implemented |
| SheetAdjustmentProjection | electrical/roof/facade projection handlers, projection repository, manual confirmation | Implemented |
| Phase 7: multi-sheet export audit | `ExportMultiSheetPlanSetPackageHandler`, `CreateMultiSheetExportAuditHandler`, manifest writer, package audit lines, post-review re-export | Implemented |
| DataCollection | `GetPlanSetQualityReportHandler`, audit events for classification/registration/projection/export | Implemented |
| No per-sheet fit engine | Projection handlers compose canonical adjustment + registration/rules; no separate electrical/roof/facade fit engine found | Satisfied |
| Know automatic/manual/confidence in export | `ExportedPlanSheetDto`, manifest serialization, quality report, manifest coverage test | Implemented |
| Runtime proof with real floor/electrical/roof/facade set | No real dependent DXFs found; no manual smoke evidence for full package with all sheet types | Missing evidence |

## What remains before calling the goal complete

1. Add or obtain real dependent sample files:
   - one electrical DXF
   - one roof DXF
   - one facade/elevation DXF
2. Manual Desktop smoke:
   - select canonical floor plan
   - import each dependent sheet explicitly
   - register with transform/confidence
   - confirm/reject as needed
   - adjust canonical floor plan against site plan
   - export HousePlanSet package
   - inspect manifest status/confidence/manual rows
3. If real sheets expose missing DXF entity transforms or calibration pain, add only those targeted slices.

## Do not add yet

- A separate fit engine per dependent sheet.
- A large visual anchor wizard before real sheets prove manual numeric transform is insufficient.
- New telemetry tables while `audit_events` covers the current DataCollection need.
