# 2026-06-16 - Synthetic title block provenance

## Type
Implementation / discovery

## Current truth
- The Adjust-to-Site-Plan panel does **not** hardcode visible site-plan title-block text.
- `IxMiliaSitePlanPreviewReader` reads DXF `TEXT`/`MTEXT` entities into `SitePlanTextDto`.
- `SitePlanAdjustmentViewModel.FilterSitePlanForAdjustment(...)` currently passes all site-plan render paths/texts through for full visual fidelity.
- `SitePlanPreviewLayerRenderer` renders the `SitePlanTextDto.Text` values from the selected site plan.

## Synthetic generator truth
- `generate_synthetic_siteplans.py` hardcodes fake Pointe-style title-block content while generating synthetic DXFs.
- The RIO case is defined as `("RIO", "57", "RIO DRIVE")`.
- `add_pointe_title_block(...)` writes:
  - house number and street name
  - `SITE PLAN`
  - `SCALE 1'=20'`
  - `BEING LOT ..., BLOCK 25, RANCHO SANTA TERESA UNIT TWO`
  - `CITY OF SUNLAND PARK, DO\U+00D1A ANA COUNTY, NEW MEXICO`
  - the curve table values like `C186`, `325.00`, `49.16`

## Product interpretation
- Legitimate as content of the synthetic DXF: AutoCAD/app are showing what the file contains.
- Not legitimate as real surveyed/legal data: it is decorative/fake test content copied from the Dawson/Pointe visual language.

## Evidence
- `generate_synthetic_siteplans.py`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaSitePlanPreviewReader.cs`
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/SitePlanPreviewLayerRenderer.cs`
- `D:\PointAIData\PLANS\originalsSitePlans\SYNTH FALTA 2 ALTO - RECTANGULAR - RIO.dxf`
