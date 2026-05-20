# 2026-05-11 - CAD-faithful native dimension editing and adjusted DXF export

## What
- Review preview now uses a world-space CAD grid instead of fixed screen dots.
- Native `DIMENSION` review data now supports canonical primitive families (`LINE`, `TEXT/MTEXT`, `INSERT`, `CIRCLE`, `ARC`, `SOLID`) plus source handles and measurement context.
- Draft curations can save native dimension override snapshots, restore them, and export a new managed `ExportedAdjustedDxf` document.
- Preview now supports native dimension hit-testing, handle dragging in world units, grid snapping, terminal marker rendering, and exact-ish native dimension editing for line/text/insert primitives.
- Review sessions now expose `MeasurableEdges` groundwork for wall/opening measurement indexing.

## Files
- `src/FloorplanFit.Application/FloorPlans/Curation/SaveFloorPlanDimensionOverrideHandler.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/RestoreFloorPlanDimensionOverrideHandler.cs`
- `src/FloorplanFit.Application/FloorPlans/Curation/ExportAdjustedDxfHandler.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanDimensionOverrideRepository.cs`
- `src/FloorplanFit.Infrastructure/Persistence/SqliteFloorPlanReviewSessionReader.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/DimensionPreviewLayerRenderer.cs`
- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`

## Verification
- `dotnet test tests/FloorplanFit.Application.Tests/FloorplanFit.Application.Tests.csproj /p:UseAppHost=false`
- `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj /p:UseAppHost=false`
- `dotnet test tests/FloorplanFit.Desktop.Tests/FloorplanFit.Desktop.Tests.csproj /p:UseAppHost=false`
