---
type: implementation
date: 2026-06-09
topic: loop2-autofit-dimension-change-highlight
---
# Auto-fit changed-number dimensions highlighted red

## Context
The user wanted Loop 2 Adjust to Site Plan to visually mark only the dimensions whose visible measurement number changed after applying an auto-fit option.

## Decision
Use a preview-only set of changed-number dimension IDs, computed by comparing baseline `DimensionDto.DisplayText` against the applied preview `DisplayText`.

This intentionally excludes dimensions that merely moved with the geometry while keeping the same visible number.

## Implementation
- `SitePlanAdjustmentViewModel` now exposes `ChangedNumberDimensionIds`.
- `ApplyAutoFitPlan(...)` computes those IDs after applying the selected option from the baseline.
- `SitePlanAdjustmentWindow.axaml` binds `ChangedNumberDimensionIds` into `FloorPlanPreviewControl`.
- `FloorPlanPreviewControl` carries the IDs into `PreviewRenderScene`.
- `DimensionPreviewLayerRenderer` and `CadTextPreviewLayerRenderer` paint changed-number dimensions red for lines, terminals, solids, and text.

## Visual priority
1. Selected dimension: green.
2. Changed visible number: red.
3. Node-bound unchanged dimension: cyan.
4. Normal dimension: black.

## Verification
- RED/GREEN tests covered ViewModel changed-number ID selection, preview control binding surface, renderer red color, and selection priority.
- Focused Desktop tests passed 91/91.
- Focused Application tests passed 23/23.
- `git diff --check` passed with LF-to-CRLF warnings only.
