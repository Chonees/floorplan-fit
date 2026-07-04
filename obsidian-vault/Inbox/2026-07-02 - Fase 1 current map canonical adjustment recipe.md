# 2026-07-02 - Fase 1 current map canonical adjustment recipe

type: Inbox
status: Research/current-system map

## Objective
Map real CAD setback adjustment practice to the current Floorplan Fit HousePlanSet implementation before implementing AdjustmentRecipe changes.

## Macro research
Real CAD workflow separates:

1. **Global placement/alignment**: place one drawing against another using move/rotate/reference-scale style operations. Autodesk's ALIGN explanation describes ALIGN as combining Move, Rotate, and Scale, and its example aligns an electrical plan to a floor plan by matching source/destination points.
2. **Local revision/stretch**: when geometry itself changes, STRETCH-like behavior moves only selected endpoints/vertices while leaving outside geometry unchanged; fully enclosed objects move, and circles/ellipses/blocks are not directly stretchable.
3. **Reference/overlay thinking**: XREF workflows attach/overlay external drawings so disciplines can compare/reference each other instead of each sheet becoming its own independent truth.
4. **Setback/site-plan truth**: municipal site-plan requirements ask for existing conditions, setback lines, proposed work dimensions, building envelope, lot coverage, roof line/eaves, and impervious surfaces. This makes the site/buildable envelope the legal container, not a random drawing scale.

## Current system evidence

### Exists today
- `AdjustedSitePlanPlacementDto` already carries affine placement plus `CompressionSteps` and `AdjustedDimensions`; its XML docs explicitly say compression is not affine and is expressed in floor-plan source coordinates.
- `SitePlanAdjustmentViewModel` records `appliedCompressionSteps` after auto-fit and includes them in `BuildAdjustedSitePlanPlacement()`.
- `IxMiliaAdjustedSitePlanExporter` applies `placement.CompressionSteps` to the FloorPlan before site-plan injection.
- `CanonicalFloorPlanAdjustment` persists the full placement as `placement_json` through `RecordCanonicalFloorPlanAdjustmentHandler`.
- `ProjectRegisteredPlanSetSheetsHandler` projects latest dependent registrations after canonical adjustment.
- Electrical/Roof/Facade projection handlers compose only affine transform today and count compression steps.
- `ProjectedPlanSheetDxfExporter` transforms dependent DXF modelspace `ENTITIES` only and quarantines dangerous electrical route curves.
- Package export exports only latest `ReadyForExport` projections and records the audit/manifest.

### Missing today
- No first-class Domain `AdjustmentRecipe` concept yet.
- No typed local operation model beyond the existing contract DTOs serialized inside `placement_json`.
- Dependent projections do **not** replay local compression; they mark compression as manual review through `CanonicalCompressionStepCount` and warning strings.
- No anchor/control-line model for dependent sheets.
- No entity-level dependent local compression policy beyond electrical curve quarantine.
- No persisted ?applied vs skipped local recipe operation? report per dependent sheet.

## File map
- Contracts: `src/FloorplanFit.Contracts/FloorPlans/AdjustedSitePlanPlacementDto.cs`
- Desktop: `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- Domain: `src/FloorplanFit.Domain/PlanSets/CanonicalFloorPlanAdjustment.cs`, `SheetAdjustmentProjection.cs`, `SheetAdjustmentProjectionTransform.cs`
- Application: `RecordCanonicalFloorPlanAdjustmentHandler.cs`, `ProjectElectricalSheetAdjustmentHandler.cs`, `ProjectRoofSheetAdjustmentHandler.cs`, `ProjectFacadeElevationSheetAdjustmentHandler.cs`, `ProjectRegisteredPlanSetSheetsHandler.cs`, `ExportMultiSheetPlanSetPackageHandler.cs`
- Infrastructure: `IxMiliaAdjustedSitePlanExporter.cs`, `ProjectedPlanSheetDxfExporter.cs`, `SqliteCanonicalFloorPlanAdjustmentRepository.cs`, `SqliteSheetAdjustmentProjectionRepository.cs`, `SqliteSchemaInitializer.cs`
- Tests: `IxMiliaAdjustedSitePlanExporterTests.cs`, `ProjectedPlanSheetDxfExporterTests.cs`, `ProjectElectricalSheetAdjustmentHandlerTests.cs`

## Gap list
| Current capability | Target capability | Gap | Risk | Minimal next step |
| --- | --- | --- | --- | --- |
| FloorPlan stores `CompressionSteps` in placement JSON | AdjustmentRecipe as domain/read model | Recipe is implicit, not named | Hard to reason/report per dependent sheet | Add recipe/read-model naming without changing storage first |
| FloorPlan exporter applies compression | Shared recipe operation application | Compression logic private inside exporter | Duplication or divergent behavior | Extract tiny compression point mapper with tests |
| Electrical projection counts compression | Electrical consumes recipe info | No per-operation result | User cannot know what was applied/skipped | Store/report `Applied/RequiresReview` summary |
| Dependent DXF exporter applies affine transform | Dependent local operation policy | No local compression replay | Blind curve deformation corrupts/garabatea | First support only safe point entities or report review |
| Roof/Facade have separate warnings | Sheet-specific operation policy | No anchor semantics | Wrong deformation across roofs/elevations | Keep review-gated until real anchors exist |

## Recommended first slice
Do **not** rewrite everything. Smallest useful slice:

1. Treat existing `AdjustedSitePlanPlacementDto.CompressionSteps` as the first recipe payload.
2. Extract the existing `ApplyCompressionPoint` behavior into a small reusable mapper protected by one test.
3. Make dependent projection explicitly report recipe operation handling:
   - no compression: `AppliedAffineOnly` / ready when confidence allows.
   - compression present: `RequiresReview` with count and sheet-specific warning.
4. Only after that, add safe dependent entity replay for a tiny whitelist, if approved.

## Out of scope for first slice
- Full electrical routing.
- Stretching unknown curves.
- Automatic roof overhang recalculation.
- Facade vertical changes.
- New csproj modularization.
- Rewriting FloorPlan export.

## Verification performed
- Static code inspection only.
- Web research used Autodesk and municipal site-plan sources.
- No build/test run because AGENTS.md says never build after changes.

## Key Learnings:
1. Floorplan Fit already has an implicit recipe payload: `AdjustedSitePlanPlacementDto` carries global placement plus local compression steps.
2. The current blocker is not absence of compression data; it is absence of a named recipe/read model and dependent per-operation projection result.
3. Dependent sheets currently consume the canonical placement only as affine transform and use compression count as a review gate.
4. The safest first implementation slice is to name/report the recipe and extract the point-compression mapper before attempting dependent DXF local deformation.
