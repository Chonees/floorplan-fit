---
type: bug
status: fixed-static
date: 2026-07-21
project: FloorplanFit
area: Loop 1 commissioned daily Auto-fit
replaces: "[[Implementation/2026-07-21 - Commissioned Floor Electrical before-after comparison]]"
implements: "[[Implementation/2026-07-20 - Finite goal for automatic site fitting UX]]"
---

# Commissioned comparison used global compression

## Defect

The commissioned after-comparison converted CAD-stretch v2 `StretchActions` into legacy `AdjustmentRecipeOperationDto` values and passed every point through `ElectricalRecipeProjection.ProjectPoint`. That produced coordinate-global compression, not the local-role result already produced by `CommissionedHouseFitPreviewProjector` and `CadStretchDeformationEngine`.

The UI could therefore claim an Electrical after-state that differed from the actual Floor preview and exported Electrical architecture.

## Fix

- Removed all commissioned comparison use of `ElectricalRecipeProjection`, `AdjustmentRecipeOperationDto`, and synthesized local operations.
- **Before Floor:** the ViewModel's real recentered `autoFitBaselineGeometryPaths`.
- **Before Electrical:** confirmed registered Electrical WALL evidence transformed only by the current floor-to-site affine scale/offset.
- **After Floor:** the actual current `FloorPlanGeometryPaths` produced by the commissioned CAD-stretch preview.
- **After Electrical architecture:** the exact same adjusted FloorPlan geometry, because export uses canonical adjusted FloorPlan as Electrical `ArchitecturalBase`.
- Original Electrical WALL data is not presented as projected output architecture. Devices and wiring remain explicitly excluded.
- Missing or unsafe evidence remains fail-closed and `Confirmar y exportar` remains the sole commissioned action.

## Static proof

- Focused contracts reject any commissioned comparison reference to `ElectricalRecipeProjection`, `AdjustmentRecipeOperationDto`, or the removed operation builders.
- The integration contract proves after Electrical architecture is coordinate-equal to after Floor.
- The protected-label `AuxiliaryEntityBindings` fixture and Fixed roles remain intact.
- XAML parses, one confirm/export action and two overlay controls remain, `git diff --check` passes, and no `.NET` command was run.
