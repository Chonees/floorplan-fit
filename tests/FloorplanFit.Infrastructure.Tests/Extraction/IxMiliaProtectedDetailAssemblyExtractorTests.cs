using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Extraction;

public sealed class IxMiliaProtectedDetailAssemblyExtractorTests
{
    [Fact]
    public async Task ExtractAsync_groups_seminole_wet_area_misc_detail_geometry_without_treating_L1_as_protected()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaProtectedDetailAssemblyExtractor();

        var assemblies = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        var wetAreaAssembly = Assert.Single(assemblies, item =>
            item.Kind == "WetAreaDetail" &&
            item.SourceLayer == "MISC" &&
            item.GeometryPaths.Count > 1);
        Assert.Equal("DETAIL-GROUP", wetAreaAssembly.SourceEntityKind);
        Assert.Contains("protected detail", wetAreaAssembly.DetectionNotes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(assemblies, item => item.SourceLayer == "L1");
        Assert.All(assemblies, item =>
        {
            Assert.NotEmpty(item.SourceEntityRef);
            Assert.NotEmpty(item.SourceLayer);
            Assert.InRange(item.Confidence, 0.80m, 1m);
            Assert.NotEmpty(item.GeometryPaths);
            Assert.All(item.GeometryPaths, path => Assert.True(path.Count >= 2));
        });
    }
}
