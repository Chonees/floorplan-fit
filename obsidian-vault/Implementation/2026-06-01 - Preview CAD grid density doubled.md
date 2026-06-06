---
type: Implementation
date: 2026-06-01
project: floorplan-fit
status: active
tags:
  - floorplan-fit
  - loop1
  - preview
  - cad-grid
  - precision
---

# Preview CAD grid density doubled

## Change

The Floorplan Review preview CAD grid now renders at double visual density by lowering the target minor grid spacing from 32 px to 16 px.

## Why

The user liked the existing precision-square CAD background and requested twice as many squares for more precise visual alignment.

## Design

The change is implemented in `CadViewportContext` instead of adding another renderer layer or changing palette colors. This preserves the existing CAD visual language while making the world-grid spacing choose a denser 1/2/5 interval.

At the tested viewport scale:

- minor grid spacing changed from 20 world units to 10;
- major grid spacing changed from 100 world units to 50;
- snap tolerance remains unchanged at 8 px / 4 world units in the tested context.

## Verification

- RED: `CadViewportContext_create_reports_world_units_per_pixel_and_denser_1_2_5_grid_spacing` failed while the old spacing still returned 20/100.
- GREEN: the same test passed after lowering the target minor grid spacing to 16 px.
- Focused preview/snap slice passed 64/64:
  `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~FloorPlanPreviewControlTests|FullyQualifiedName~NativeDimensionPreviewControlTests" --artifacts-path .testartifacts\dotnet-test-artifacts-denser-grid-preview`
