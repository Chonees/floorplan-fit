using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Export;

public sealed class IxMiliaAdjustedDxfExporterTests
{
    [Fact]
    public async Task ExportAsync_writes_adjusted_native_dimension_and_round_trips_through_extractor()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-adjusted-dxf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var outputPath = Path.Combine(tempRoot, "SEMINOLE2000-adjusted.dxf");

        try
        {
            var extractor = new IxMiliaDimensionExtractor();
            var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);
            var sourceDimension = Assert.Single(dimensions, item => item.GeometryBlockName == "*D169");

            var adjustedDimension = new DimensionDto(
                Guid.NewGuid(),
                sourceDimension.SourceEntityRef,
                sourceDimension.SourceLayer,
                sourceDimension.SourceEntityKind,
                sourceDimension.GeometryBlockName,
                "6'-0\"",
                sourceDimension.DisplayTextSource,
                "6'-0\"",
                sourceDimension.MeasurementSourceUnits,
                sourceDimension.MeasurementMillimeters,
                sourceDimension.SourceUnit,
                sourceDimension.DimType,
                sourceDimension.Angle,
                sourceDimension.ObliqueAngle,
                sourceDimension.DefPointX,
                sourceDimension.DefPointY,
                sourceDimension.DefPointZ,
                sourceDimension.DefPoint2X,
                sourceDimension.DefPoint2Y,
                sourceDimension.DefPoint2Z,
                sourceDimension.DefPoint3X,
                sourceDimension.DefPoint3Y,
                sourceDimension.DefPoint3Z,
                sourceDimension.Confidence,
                sourceDimension.DetectionNotes,
                1)
            {
                SourceHandle = sourceDimension.SourceHandle,
                RenderTextX = (sourceDimension.RenderTextX ?? 0m) + 10m,
                RenderTextY = (sourceDimension.RenderTextY ?? 0m) + 5m,
                RenderTextHeight = sourceDimension.RenderTextHeight,
                RenderTextRotationDegrees = sourceDimension.RenderTextRotationDegrees,
                RenderTextStyleName = sourceDimension.RenderTextStyleName,
                RenderTextHorizontalAlignment = sourceDimension.RenderTextHorizontalAlignment,
                RenderTextVerticalAlignment = sourceDimension.RenderTextVerticalAlignment,
                RenderTextAttachmentPoint = sourceDimension.RenderTextAttachmentPoint,
                LineSegments = sourceDimension.LineSegments
                    .Select((segment, index) => new DimensionLineSegmentDto(
                        index == 0 ? segment.StartX + 12m : segment.StartX,
                        segment.StartY,
                        index == 0 ? segment.EndX + 12m : segment.EndX,
                        segment.EndY))
                    .ToArray(),
                LinePrimitives = sourceDimension.LinePrimitives
                    .Select((primitive, index) => new DimensionLinePrimitiveDto(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        index == 0 ? primitive.StartX + 12m : primitive.StartX,
                        primitive.StartY,
                        index == 0 ? primitive.EndX + 12m : primitive.EndX,
                        primitive.EndY)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer
                    })
                    .ToArray(),
                TextPrimitives = sourceDimension.TextPrimitives
                    .Select(primitive => new DimensionTextPrimitiveDto(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        "6'-0\"",
                        primitive.X + 10m,
                        primitive.Y + 5m,
                        primitive.Height,
                        primitive.RotationDegrees)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer,
                        StyleName = primitive.StyleName,
                        HorizontalAlignment = primitive.HorizontalAlignment,
                        VerticalAlignment = primitive.VerticalAlignment,
                        AttachmentPoint = primitive.AttachmentPoint
                    })
                    .ToArray(),
                InsertPrimitives = sourceDimension.InsertPrimitives
                    .Select(primitive => new DimensionInsertPrimitiveDto(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.Name,
                        primitive.X,
                        primitive.Y,
                        primitive.Z)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer,
                        RotationDegrees = primitive.RotationDegrees,
                        ScaleX = primitive.ScaleX,
                        ScaleY = primitive.ScaleY,
                        ScaleZ = primitive.ScaleZ
                    })
                    .ToArray(),
                CirclePrimitives = sourceDimension.CirclePrimitives
                    .Select(primitive => new DimensionCirclePrimitiveDto(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.CenterX,
                        primitive.CenterY,
                        primitive.Radius)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer
                    })
                    .ToArray(),
                ArcPrimitives = sourceDimension.ArcPrimitives
                    .Select(primitive => new DimensionArcPrimitiveDto(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.CenterX,
                        primitive.CenterY,
                        primitive.Radius,
                        primitive.StartAngleDegrees,
                        primitive.EndAngleDegrees)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer
                    })
                    .ToArray(),
                SolidPrimitives = sourceDimension.SolidPrimitives
                    .Select(primitive => new DimensionSolidPrimitiveDto(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.Point1X,
                        primitive.Point1Y,
                        primitive.Point2X,
                        primitive.Point2Y,
                        primitive.Point3X,
                        primitive.Point3Y,
                        primitive.Point4X,
                        primitive.Point4Y)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer
                    })
                    .ToArray()
            };

            var exporter = new IxMiliaAdjustedDxfExporter();

            await exporter.ExportAsync(sourcePath, outputPath, [adjustedDimension], CancellationToken.None);

            Assert.True(File.Exists(outputPath));

            var adjustedDimensions = await extractor.ExtractAsync(outputPath, CancellationToken.None);
            var reloaded = Assert.Single(adjustedDimensions, item => item.GeometryBlockName == sourceDimension.GeometryBlockName);

            Assert.Equal("6'-0\"", reloaded.DisplayText);
            Assert.Equal((sourceDimension.RenderTextX ?? 0m) + 10m, reloaded.RenderTextX);
            Assert.Equal((sourceDimension.RenderTextY ?? 0m) + 5m, reloaded.RenderTextY);
            Assert.Equal(sourceDimension.LineSegments[0].StartX + 12m, reloaded.LineSegments[0].StartX);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
