---
type: bug
project: floorplan-fit
date: 2026-06-11
topic_key: bugs/width-setback-examples-used-wall-bbox
status: superseded-invalid
replaced_by: Bugs/2026-06-11 - Width deficit still uses non-footprint geometry.md
---

# 2026-06-11 - Width setback examples used wall bbox instead of preview bbox

## Superseded correction

This note is superseded. The "total preview bbox" correction was also wrong because it included non-footprint geometry and could invent width deficits. The current truth is:

```txt
Width setback detection uses the structural fit footprint / accepted wall-candidate bbox.
```

See: `Bugs/2026-06-11 - Width deficit still uses non-footprint geometry.md`.

## Symptom

In the total-deficit width examples, the orange setback appeared close on the left but wildly short on the right. For an example named “ancho menos 1 inch total”, the expected visual is symmetric: `0.5"` outside on each side.

## Root cause

The exported examples were generated from the SEMINOLE2000 `WALLS` bbox:

```txt
483.786" x 930"
```

But Loop 2 preview displays and overlays a broader total preview geometry basis. In the current active SEMINOLE2000 data, that bbox is:

```txt
561.244544" x 930.000286"
```

The missing width comes from non-wall preview geometry extending to the right. So the old width examples were internally centered against the wall structure, but visually judged against the full preview floor plan.

## Fix

- Regenerated the four DXFs in `C:\Users\lucas\OneDrive\Escritorio\exports` from the total preview geometry bbox.
- Updated `setback ejemplos - README.txt` and `setback ejemplos - overview.svg`.
- Updated Loop 2 Adjust build so initial projection and auto-fit facts use the total preview geometry passed to the preview instead of reducing the placement/facts basis to wall-candidate ids.

## Verified dimensions

```txt
Example 01: footprint 561.244544 x 930.000286; setback 560.244544 x 930.000286; deficit W/H = 1/0; centers equal
Example 02: footprint 561.244544 x 930.000286; setback 559.244544 x 930.000286; deficit W/H = 2/0; centers equal
Example 03: footprint 561.244544 x 930.000286; setback 561.244544 x 929.000286; deficit W/H = 0/1; centers equal
Example 04: footprint 561.244544 x 930.000286; setback 561.244544 x 928.000286; deficit W/H = 0/2; centers equal
```

## Verification commands

- RED: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .testartifacts\setback-total-preview-red --filter "FullyQualifiedName~Build_uses_total_preview_geometry_for_width_setback_examples"` failed with projected min X `129.5` instead of expected `99.5`.
- GREEN: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .testartifacts\setback-total-preview-green --filter "FullyQualifiedName~Build_uses_total_preview_geometry_for_width_setback_examples"` passed 1/1.
- Desktop slice: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .testartifacts\setback-total-preview-desktop-final --filter "FullyQualifiedName~SitePlanAdjustmentPreviewProjectorTests|FullyQualifiedName~AppXamlInitializationTests"` passed 30/30.
- Application slice: `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .testartifacts\setback-total-preview-app --filter "FullyQualifiedName~AutoFitSuggestion"` passed 10/10.
- DXF numeric parse verified footprint/setback widths, heights, and equal centers for all four exports.
