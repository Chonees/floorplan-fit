# 2026-07-02 - CAD setback alignment informs canonical adjustment recipe

type: Decision
status: Research-backed direction
replaces:
replaced_by:

## What
Real CAD/site-plan practice separates two operations:

1. Place/orient the building footprint inside the legal site envelope using move/rotate/reference scale/alignment.
2. If the footprint does not fit, revise the architectural plan geometry locally with stretch-like edits, not by independently scaling every dependent sheet.

## Why
Municipal site-plan requirements focus on property lines, setbacks, easements, building envelope, existing/proposed work, and measured distances from structures to property lines. AutoCAD workflows support placing drawings by reference points (`ALIGN`, xrefs) and editing geometry locally (`STRETCH`) when the design itself changes.

## Implication for Floorplan Fit
For HousePlanSet, the canonical FloorPlan should emit an adjustment recipe:

- global placement transform for the entire footprint/sheet
- local stretch/compression operations for footprint changes
- anchors/control lines that define what moved, what stayed fixed, and what was compressed

Dependent sheets (Electrical/Roof/Facade) should not decide their own fit. They should register to canonical floor coordinates and replay the approved recipe with per-sheet entity policies.

## Sources
- Autodesk AutoCAD ALIGN/reference scale docs: move, rotate, and optionally scale objects by source/destination point pairs.
- Autodesk AutoCAD STRETCH docs: crossing selection stretches only vertices/endpoints inside the crossing window; fully enclosed objects move; circles/ellipses/blocks cannot be stretched directly.
- Chapel Hill site-plan requirements: existing conditions, setback lines, easements, proposed work dimensions, building envelope, land disturbance, impervious area.
- Maricopa County site-plan checklist: setbacks regulate distance from structures to property lines and setback measurements are required from structures to property lines.

## Design consequence
Do not model dependent sheet adjustment as simple whole-sheet rescale. Model it as:

`SiteEnvelopeFit -> CanonicalFloorPlacement -> FloorPlanAdjustmentRecipe -> DependentSheetRecipeProjection`.