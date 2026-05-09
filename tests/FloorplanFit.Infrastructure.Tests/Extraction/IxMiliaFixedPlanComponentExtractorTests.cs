using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Extraction;

public sealed class IxMiliaFixedPlanComponentExtractorTests
{
    [Fact]
    public async Task ExtractAsync_reads_fixture_cabinet_and_insert_block_components_from_santa_barbara()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(components, item => item.Kind == "Cabinet" && item.SourceLayer == "CABS" && item.SourceEntityKind == "LINE");
        Assert.Contains(components, item => item.Kind == "Toilet" && item.SourceBlockName == "TOILET1" && item.GeometryPaths.Count > 1);
        Assert.Contains(components, item => item.Kind == "Appliance" && item.SourceBlockName == "STOVE");
        Assert.Contains(components, item => item.Kind == "Fixture" && item.SourceBlockName == "TUB");
        Assert.Contains(components, item => item.ColorArgb is { Length: 9 } color && color.StartsWith("#FF", StringComparison.Ordinal));
        Assert.All(components, item =>
        {
            Assert.NotEmpty(item.SourceEntityRef);
            Assert.NotEmpty(item.SourceLayer);
            Assert.InRange(item.Confidence, 0.80m, 1m);
            Assert.NotEmpty(item.GeometryPaths);
            Assert.All(item.GeometryPaths, path => Assert.True(path.Count >= 2));
        });
    }

    [Fact]
    public async Task ExtractAsync_reads_seminole_toilet_blocks_and_cabinet_floorplan_geometry()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaFixedPlanComponentExtractor();

        var components = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(components, item => item.Kind == "Toilet" && item.SourceBlockName == "TOILET1");
        Assert.Contains(components, item => item.Kind == "Cabinet" && item.SourceLayer == "CABS-FLOORPLAN");
        Assert.Contains(components, item => item.Kind == "Fixture" && item.SourceLayer == "FIXTURES");
    }
}
