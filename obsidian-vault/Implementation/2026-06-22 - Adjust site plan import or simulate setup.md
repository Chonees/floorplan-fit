# 2026-06-22 - Adjust site plan import or simulate setup

## Type
Implementation

## Implements
- [[2026-06-22 - Adjust entry should import or simulate site plan]]

## What changed
- Clicking **Adjust to Site Plan** now opens a setup dialog instead of immediately opening the DXF picker.
- The dialog offers two paths:
  - **Importar DXF**: keeps the existing file-picker + `ISitePlanPreviewReader` path.
  - **Simular site plan**: asks for buildable width/height in feet, writes a temporary synthetic DXF, then sends that DXF through the same reader/preview path.
- The synthetic DXF writer uses inches (`$INSUNITS=1`) and the synth-compatible layer vocabulary: `SETBACKS`, `2312-001-BM$0$C-PROP-SUBD`, `E`, `TEXT`, and `0`.
- The generated buildable envelope is the `SETBACKS` rectangle, so Adjust computes fit/deficit against the user-entered size.

## Why
The old flow only supported imported site plans and made SEMINOLE-calibrated synthetic fixtures feel like universal validators. The new flow lets any selected floor plan be tested against an explicit buildable size.

## Verification
- RED: `dotnet test tests\\FloorplanFit.Infrastructure.Tests\\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Synthetic_site_plan" --no-restore` failed because `SyntheticSitePlanDxfWriter` did not exist.
- RED: source check failed because `AdjustSitePlanSetupDialog.axaml` did not exist.
- GREEN: `dotnet test tests\\FloorplanFit.Infrastructure.Tests\\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~Synthetic_site_plan" --no-restore -v minimal` passed `5/5`.
- GREEN: `dotnet test tests\\FloorplanFit.Desktop.Tests\\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~Adjust_to_site_plan_entry_offers_import_or_simulate_paths" --no-restore --output .testartifacts\\desktop-tests-out -v minimal` passed `1/1`.
- `git diff --check` exited `0` with CRLF warnings only.
- No `dotnet build` command was run.

## Files
- `src/FloorplanFit.Desktop/AdjustSitePlanSetupDialog.axaml`
- `src/FloorplanFit.Desktop/AdjustSitePlanSetupDialog.axaml.cs`
- `src/FloorplanFit.Desktop/MainWindow.axaml.cs`
- `src/FloorplanFit.Infrastructure/Dxf/SyntheticSitePlanDxfWriter.cs`
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaSitePlanPreviewReaderTests.cs`
- `tests/FloorplanFit.Desktop.Tests/Layout/ReviewFloorPlanWindowLayoutTests.cs`
