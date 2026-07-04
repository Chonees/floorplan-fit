# 2026-06-16 - Synthetic preview preserves DXF text

## Type
Bugfix / correction

## Current truth
- Adjust-to-Site-Plan preview must preserve synthetic DXF text the same way it preserves real DXF text.
- The preview should not hide title-block/legal/survey-looking text just because the file is synthetic.
- If synthetic text is wrong or fake, the fix belongs in `generate_synthetic_siteplans.py` and regenerated DXF fixtures, not in Desktop preview filtering.

## Replaces
- `Implementation/2026-06-16 - Synthetic previews hide fake title block text.md`

## Why
The user clarified the DXF is the source of visual truth: if the DXF has that content, the preview must show it. The earlier filtering fix broke visual fidelity and solved the problem in the wrong layer.

## Files changed
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs`

## Verification
- RED: `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FilterSitePlanForAdjustment_keeps_synthetic_title_block_text_because_preview_matches_the_dxf" --artifacts-path .\.testartifacts\synthetic-preview-as-dxf-red`
  - Failed because actual text only included setback text.
- GREEN/regression: `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FilterSitePlanForAdjustment" --artifacts-path .\.testartifacts\synthetic-preview-as-dxf-green`
  - Passed 2/2.

## Next direction
Fix synthetic data at generation time: make visible title-block/survey-like values precise and derived from generated geometry/metadata, or remove them from the DXF itself if they cannot be true.
