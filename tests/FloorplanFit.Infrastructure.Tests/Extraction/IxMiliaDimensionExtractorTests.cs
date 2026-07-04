using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Extraction;

public sealed class IxMiliaDimensionExtractorTests
{
    [Fact]
    public async Task ExtractAsync_reads_native_dimensions_from_seminole_and_excludes_electrical_wiring()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaDimensionExtractor();

        var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Equal(326, dimensions.Count);
        Assert.DoesNotContain(dimensions, item => item.SourceLayer == "ELECTRICAL WIRING");
    }

    [Fact]
    public async Task ExtractAsync_prefers_rendered_geometry_block_text_when_dimension_text_field_is_empty()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaDimensionExtractor();

        var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(dimensions, item =>
            item.RawTextOverride == string.Empty &&
            item.DisplayText == "10'-4\"" &&
            item.DisplayTextSource == "GeometryBlock");
    }

    [Fact]
    public async Task ExtractAsync_reconstructs_measurement_when_ixmilia_leaves_actual_measurement_at_zero()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaDimensionExtractor();

        var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(dimensions, item =>
            item.DisplayText == "10'-4\"" &&
            item.MeasurementSourceUnits > 123m &&
            item.MeasurementSourceUnits < 124m &&
            item.MeasurementMillimeters > 3140m &&
            item.MeasurementMillimeters < 3150m);
    }

    [Fact]
    public async Task ExtractAsync_reads_santa_barbara_native_dimensions()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var extractor = new IxMiliaDimensionExtractor();

        var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Equal(114, dimensions.Count);
    }

    [Fact]
    public async Task ExtractAsync_captures_geometry_block_segments_and_precise_text_position()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaDimensionExtractor();

        var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        var dimension = Assert.Single(dimensions, item => item.GeometryBlockName == "*D169");
        Assert.Equal(3, dimension.LineSegments.Count);
        Assert.Equal(440.574188825591m, dimension.LineSegments[0].StartX);
        Assert.Equal(519.778489196223m, dimension.LineSegments[0].StartY);
        Assert.Equal(440.574188825591m, dimension.LineSegments[0].EndX);
        Assert.Equal(512.956649466215m, dimension.LineSegments[0].EndY);
        Assert.Equal(408.83918996211m, dimension.RenderTextX);
        Assert.Equal(518.967780658254m, dimension.RenderTextY);
        Assert.Equal(3.5m, dimension.RenderTextHeight);
        Assert.Equal("ARCH", dimension.RenderTextStyleName);
        Assert.Equal("MiddleCenter", dimension.RenderTextAttachmentPoint);
    }

    [Fact]
    public async Task ExtractAsync_reads_dimension_block_terminal_insert_symbols_and_source_handle()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaDimensionExtractor();

        var dimensions = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        var dimension = Assert.Single(dimensions, item => item.GeometryBlockName == "*D169");
        Assert.False(string.IsNullOrWhiteSpace(dimension.SourceHandle));
        Assert.Equal($"DIMENSION:{dimension.SourceHandle}", dimension.SourceEntityRef);
        Assert.Equal(2, dimension.InsertPrimitives.Count);
        Assert.All(dimension.InsertPrimitives, primitive => Assert.Equal("_Dot", primitive.Name));
        Assert.All(dimension.InsertPrimitives, primitive => Assert.False(string.IsNullOrWhiteSpace(primitive.PrimitiveKey)));
    }
}
