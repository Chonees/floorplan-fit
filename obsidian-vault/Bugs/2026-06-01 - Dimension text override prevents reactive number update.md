---
type: Bug
date: 2026-06-01
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - measurement-bindings
  - reactive-preview
---

# Dimension text override prevents reactive number update

## Verified root cause

Some native CAD dimensions include text override content such as a measured value plus suffix/purpose text (for example, a cota that visually reads like `16'-4" TO CL. OF EXH. VENT`). The DXF extractor persists that as dimension display text and keeps the raw override in `RawTextOverride` when the DXF text is non-empty and not exactly `<>`.

During reactive pinch preview, node-bound dimensions with `ManualVerified` interval bindings are routed through `DimensionIntervalReactiveProjector` and then `DimensionGeometryProjector.RebuildAssociatedDimensionFromAnchorDeltas`. That path does recompute geometry and `MeasurementSourceUnits`, but `DimensionGeometryProjector.ResolveDisplayText` intentionally returns the existing `DisplayText` whenever `RawTextOverride` is present and not exactly `<>`.

So the dimension is related to the two nodes and can be geometrically/measurement-wise recomputed, but the visible number remains static because the current text policy treats the whole display string as a literal manual override.

## Why this exists

The conservative behavior protects true manual labels like `VERIFY`, `EQ`, `TYP.`, or other non-measure text from being overwritten by generated numbers. The missing case is a mixed override where `<>` means "use measured value here" and the rest is prefix/suffix context.

## Correct next direction

Add a tokenized/override-aware display-text path:

1. Detect raw overrides that contain the AutoCAD `<>` placeholder plus optional prefix/suffix.
2. Recalculate the numeric measurement from the reactive span.
3. Substitute only the numeric token while preserving the prefix/suffix, e.g. `<> TO CL. OF EXH. VENT` becomes `15'-10" TO CL. OF EXH. VENT` after trimming.
4. Continue preserving full literal overrides that do not contain `<>`.

## Evidence

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDimensionExtractor.cs` stores `RawTextOverride` for non-empty non-`<>` dimension text.
- `src/FloorplanFit.Application/FloorPlans/Review/DimensionGeometryProjector.cs` preserves `DisplayText` whenever `RawTextOverride` exists and is not exactly `<>`.
- `tests/FloorplanFit.Application.Tests/FloorPlans/Review/ReactiveDimensionProjectorTests.cs` explicitly covers preserving a custom text override while recomputing measurement.

## Fixed

Implemented a shared `DimensionDisplayTextFormatter` in the Application review layer. The formatter treats `RawTextOverride` as a CAD text template:

- empty / `<>` override regenerates the whole measured value;
- override containing `<>` replaces only that placeholder with the recalculated measured value and preserves suffix/prefix text;
- override without `<>` remains literal, preserving notes like `VERIFY`, `EQ`, or `TYP.`.

`DimensionGeometryProjector` and `NativeDimensionEditor` now both use the shared formatter, so reactive pinch preview and direct dimension endpoint editing follow the same display-text contract.

Verification:

- `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --nologo --filter "FullyQualifiedName~DimensionIntervalReactiveProjectorTests|FullyQualifiedName~ReactiveDimensionProjectorTests" --artifacts-path .testartifacts\dotnet-test-artifacts-dim-placeholder-app-final` passed 21/21.
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --nologo --filter "FullyQualifiedName~NativeDimensionPreviewControlTests" --artifacts-path .testartifacts\dotnet-test-artifacts-dim-placeholder-desktop-final` passed 16/16.
