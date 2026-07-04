---
type: decision
date: 2026-06-30
topic: architecture/plan-set-adjustment
---
# 2026-06-30 - House plan set adjustment model

## Decision
A house must be modeled as a plan set, not as a single floor-plan-only asset. Each house can contain multiple sheets: floor plan, electrical plan, roof plan, and facade/elevation sheets.

## Why
The product direction is that adjusting the floor plan for a site should propagate the approved adjustment to all related sheets. Most non-floor sheets have few dimensions, so they should usually be registered/rescaled/projected from the canonical floor-plan adjustment instead of being fully independently curated.

## Verified current state
Current code does not have first-class electrical, roof-plan, or facade/elevation modules. Search found only electrical exclusions in extraction, facade domain roles/constraints/tests, and `roof` as an internal variable name for dimension-shape geometry.

## Architectural implication
The canonical adjustable model should come from the curated floor plan. Electrical, roof, and facade sheets are dependent sheets with their own registration, sparse measurements, artifact preservation rules, and projection/export behavior.

## Candidate modules
- PlanSet / HousePackage: owns the grouping of related sheets for one house/version.
- SheetImport: imports each sheet and classifies sheet type.
- SheetRegistration: maps dependent sheets to the canonical floor-plan coordinate system using anchors, footprint, sparse dimensions, or manual confirmation.
- CanonicalFloorPlanAdjustment: computes the fit adjustment once from curated floor-plan truth.
- SheetAdjustmentProjection: applies the approved adjustment transform to electrical/roof/facade sheets with sheet-specific rules.
- MultiSheetExportAudit: exports all adjusted sheets and records confidence, transforms, manual overrides, and warnings.

## Boundary rule
Do not build one independent fit engine per sheet. Build one canonical fit/adjustment model, then project that adjustment to dependent sheets through explicit registration/transforms.
