---
type: Bug
date: 2026-06-15
project: floorplan-fit
status: fixed
related:
  - 2026-06-15 - Adjust to Site Plan export opens black blank DXF.md
  - 2026-06-09 - Auto-fit apply did not rebuild related dimensions.md
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - export
  - dimensions
  - dxf-semantics
---

# Adjust to Site Plan export omits reactive dimension values

## Reported symptom/question

After fixing the AutoCAD black/blank open failure, the user asked whether the **actual re-adjustment values** are exported too: the Loop 2 preview shows adjusted width/affected cotas in red after applying auto-fit, so exporting should carry those same changed dimension numbers into the DXF.

## Verified code evidence

Current preview flow:

- `SitePlanAdjustmentViewModel.ApplyAutoFitPlan(...)` rebuilds preview dimensions per applied compression step.
- When measurement context exists, it calls `DimensionIntervalReactiveProjector.Project(...)`.
- It then replaces `Dimensions` and computes `ChangedNumberDimensionIds` by comparing baseline versus applied `DisplayText`.
- Existing Desktop regression proves a bound width cota changes from `10'-4"` / `124"` to `10'-2"` / `122"` and is marked changed/red.

Current export flow:

- `ExportAdjustedSitePlanAsync(...)` calls `BuildAdjustedSitePlanPlacement()`.
- `AdjustedSitePlanPlacementDto` carries only `FloorToSiteScale`, `SiteOffsetX`, `SiteOffsetY`, and `CompressionSteps`.
- `IAdjustedSitePlanExporter.ExportAsync(...)` receives that placement only; it does not receive `Dimensions` or `ChangedNumberDimensionIds`.
- `IxMiliaAdjustedSitePlanExporter` applies coordinate compression to generic entity records and anonymous `*D` blocks, then injects site-plan entities. It does not patch dimension geometry-block text from the preview-projected `DimensionDto.TextPrimitives`.

## Current conclusion

The DXF validity fixes made the exported file open, but there is a separate semantic gap: the export pipeline does not currently have the preview's reactive dimension values, so it cannot guarantee that red preview cota numbers are written to the exported DXF.

## Correct direction

Export should use the same preview-projected dimension state that drives the red numbers, not recompute dimension text inside the exporter.

Candidate fix direction:

1. Extend the Adjust-to-Site-Plan export DTO/contract to carry adjusted `DimensionDto` patches (or at least changed adjusted dimensions).
2. Build those patches from `SitePlanAdjustmentViewModel.Dimensions`, ideally filtered to dimensions whose text/geometry changed.
3. Reuse the source-preserving geometry-block patching approach from `IxMiliaAdjustedDxfExporter` before/in addition to compression and site-plan injection.
4. Add a RED regression proving exported DXF text for an affected cota matches the red preview value.

## Status

Investigation confirmed the gap. Implementation must be TDD: add failing exporter/desktop contract test first, then patch the contract/exporter.

## Fix implemented

The export pipeline now carries the same changed dimension state that drives the red preview numbers:

- `AdjustedSitePlanPlacementDto` includes `AdjustedDimensions`.
- `SitePlanAdjustmentViewModel.BuildAdjustedSitePlanPlacement()` filters dimensions by `ChangedNumberDimensionIds` and maps those preview/site coordinates back into floor-plan source coordinates.
- `IxMiliaAdjustedSitePlanExporter` applies source-coordinate compression first, then patches affected native dimension geometry blocks/text using `DxfDimensionBlockPatcher`, and only then injects the site-plan entities/layers.

This keeps the source-preserving DXF architecture: no whole-file IxMilia reserialization was reintroduced.

## Verification

- RED Desktop regression failed first because `AdjustedSitePlanPlacementDto` had no `AdjustedDimensions`.
- RED Infrastructure regression failed first because `AdjustedSitePlanPlacementDto` had no `AdjustedDimensions` parameter and the exporter could not receive preview cota patches.
- GREEN Desktop: `SitePlanAdjustmentPreviewProjectorTests.ApplyAutoFitPlan_rebuilds_related_dimensions_with_interval_reactive_projector` passed.
- GREEN Infrastructure: `IxMiliaAdjustedSitePlanExporterTests.ExportAsync_patches_reactive_dimension_text_from_adjustment_preview` passed.
- Final relevant Infrastructure suite: `IxMiliaAdjustedSitePlanExporterTests|IxMiliaAdjustedDxfExporterTests` passed 8/8.
- Final relevant Desktop suite: `SitePlanAdjustmentPreviewProjectorTests` passed 25/25.
- `git diff --check` exited 0 with CRLF warnings only.
