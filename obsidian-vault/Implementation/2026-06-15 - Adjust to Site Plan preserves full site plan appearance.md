---
type: Implementation
date: 2026-06-15
project: floorplan-fit
status: current
related:
  - ../Inbox/2026-06-15 - Adjust to Site Plan must preserve full site plan colors and content.md
  - ../Current State.md
tags:
  - floorplan-fit
  - loop2
  - adjust-to-site-plan
  - dxf
  - preview
---

# Adjust to Site Plan preserves full site plan appearance

## Change

Loop 2 `Adjust to Site Plan` now treats the site-plan DXF as CAD appearance truth. The preview and export no longer intentionally collapse it to a simplified terrain/setback-only, gray/orange overlay.

## Preview behavior

`SitePlanAdjustmentPreviewProjector.FilterSitePlanForAdjustment(...)` now keeps the full `SitePlanPreviewDto.RenderPaths` and `Texts` for display. The buildable area is still read from setback geometry for fit calculations, but visual display keeps the full site plan.

`SitePlanPreviewLayerRenderer.ResolveColor(...)` now honors source `ColorArgb` for both setback and non-setback site-plan paths/texts. Fallback colors remain only for entities where the DXF reader could not resolve a source color.

## Export behavior

`IxMiliaAdjustedSitePlanExporter` now injects site-plan model-space entities from raw source DXF group-code records instead of rebuilding them from IxMilia entity objects. It regenerates handles/owner references and transforms coordinates/lengths into the floor-plan coordinate system, while preserving visual group codes such as layer, entity color, lineweight, text style, width factor, and oblique angle.

Missing site-plan layers are copied from the site-plan source `LAYER` records when available, preserving layer visual metadata such as color and lineweight. Compatibility tail metadata (`370`, `390`, `347`, `348`) is still added only when absent to keep AutoCAD happy.

## Verification

RED/GREEN preview tests:

```txt
dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "SitePlanPreviewLayerRenderer_preserves_site_plan_source_colors|FilterSitePlanForAdjustment_keeps_full_site_plan_content_for_visual_fidelity" --artifacts-path .testartifacts\dotnet-test-artifacts
```

GREEN broader preview slice:

```txt
dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~SitePlanAdjustmentPreviewProjectorTests|FullyQualifiedName~FloorPlanPreviewControlTests" --artifacts-path .testartifacts\dotnet-test-artifacts
# Passed: 84
```

RED/GREEN export test:

```txt
dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "ExportAsync_preserves_site_plan_layer_and_entity_visual_metadata" --artifacts-path .testartifacts\dotnet-test-artifacts
```

GREEN broader exporter slice:

```txt
dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedSitePlanExporterTests" --artifacts-path .testartifacts\dotnet-test-artifacts
# Passed: 5
```

## Relevant files

- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs` ? Loop 2 display now passes through all site-plan render paths/texts.
- `src/FloorplanFit.Desktop/Controls/Preview/SitePlanPreviewLayerRenderer.cs` ? renderer honors source `ColorArgb` before fallback colors.
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedSitePlanExporter.cs` ? export injects source-preserved site-plan entity/layer records with transformed geometry.
- `tests/FloorplanFit.Desktop.Tests/Controls/FloorPlanPreviewControlTests.cs` ? color-preservation regression.
- `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs` ? full-content preview regression.
- `tests/FloorplanFit.Infrastructure.Tests/Dxf/IxMiliaAdjustedSitePlanExporterTests.cs` ? source visual metadata export regression.
