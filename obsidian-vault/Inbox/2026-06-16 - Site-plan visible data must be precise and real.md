# 2026-06-16 - Site-plan visible data must be precise and real

## Type
Requirement

## User requirement
Visible data in the Adjust-to-Site-Plan site-plan panel must be precise and real.

## Verified current gap
- The panel renders DXF text legitimately from the selected site plan.
- For synthetic site plans, the visible title-block/legal/survey-looking values are generated fixtures:
  - `57`
  - `RIO DRIVE`
  - `BEING LOT ..., BLOCK 25, RANCHO SANTA TERESA UNIT TWO`
  - `CITY OF SUNLAND PARK...`
  - curve-table values such as `C186`, `325.00`, `49.16`
- Those values are not real surveyed/legal data and should not be presented as such.

## Product implication
Need choose one direction before implementation:
1. Hide/remove non-essential fake title-block/legal text from synthetic previews.
2. Generate only precise synthetic metadata derived from actual generated geometry and label it as synthetic/test data.
3. Use only real fixture/site-plan title data when the source is a real site plan.

## Evidence
- `generate_synthetic_siteplans.py`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaSitePlanPreviewReader.cs`
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/SitePlanPreviewLayerRenderer.cs`
