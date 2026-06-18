# 2026-06-16 - Synthetic previews hide fake title block text

> Superseded by: `Implementation/2026-06-16 - Synthetic preview preserves DXF text.md`

## Type
Implementation

## Current truth
- Real/non-synthetic site plans still show full site-plan text for visual fidelity.
- Synthetic site plans whose file name starts with `SYNTH ` now show only setback text in the Adjust-to-Site-Plan preview.
- Fake synthetic title-block/legal/survey-looking text is hidden from the product panel.

## Why
User clarified visible site-plan data must be precise and real. Synthetic values such as `57`, `RIO DRIVE`, legal lines, and curve table rows are generated fixtures, not real site-plan data.

## Product scope
- Loop 2: Adjust to Site Plan.
- Architecture layer: Desktop preview filtering.

## Files changed
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs`

## Verification
- RED: `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FilterSitePlanForAdjustment_hides_synthetic_title_block_text_but_keeps_setback_text" --artifacts-path .\.testartifacts\synthetic-title-red`
  - Failed because actual texts were `57`, `RIO DRIVE`, `SITE PLAN`, and `20' REAR SETBACK LINE`.
- GREEN: same focused test passed 1/1 after filtering synthetic non-setback text.
- Regression: `dotnet test .\tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~FilterSitePlanForAdjustment" --artifacts-path .\.testartifacts\synthetic-title-filter`
  - Passed 2/2.
- `git diff --check` exited 0 with CRLF warnings only.

## Tradeoff
This uses the existing synthetic file-name convention (`SYNTH `) instead of adding metadata plumbing. That is intentionally minimal. If synthetic provenance later becomes first-class product state, move the flag into `SitePlanPreviewDto`.
