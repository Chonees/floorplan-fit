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

    [Fact]
    public async Task ExtractAsync_discards_collapsed_zero_extent_detail_geometry()
    {
        var sourcePath = CreateCollapsedDetailFixture();
        var extractor = new IxMiliaProtectedDetailAssemblyExtractor();

        try
        {
            var assemblies = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

            var assembly = Assert.Single(assemblies);
            Assert.Equal("MISC", assembly.SourceLayer);
            Assert.Equal(2, assembly.GeometryPaths.Count);
            Assert.All(assembly.GeometryPaths, path =>
                Assert.Contains(path, point => point != path[0]));
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    private static string CreateCollapsedDetailFixture()
    {
        var dxf = new IxMilia.Dxf.DxfFile();
        dxf.Layers.Add(new IxMilia.Dxf.DxfLayer("MISC", IxMilia.Dxf.DxfColor.FromIndex(2)));

        dxf.Entities.Add(new IxMilia.Dxf.Entities.DxfLine(
            new IxMilia.Dxf.DxfPoint(0d, 0d, 0d),
            new IxMilia.Dxf.DxfPoint(10d, 0d, 0d))
        {
            Layer = "MISC"
        });
        dxf.Entities.Add(new IxMilia.Dxf.Entities.DxfLine(
            new IxMilia.Dxf.DxfPoint(10d, 0d, 0d),
            new IxMilia.Dxf.DxfPoint(10d, 5d, 0d))
        {
            Layer = "MISC"
        });
        dxf.Entities.Add(new IxMilia.Dxf.Entities.DxfLine(
            new IxMilia.Dxf.DxfPoint(500d, 500d, 0d),
            new IxMilia.Dxf.DxfPoint(500d, 500d, 0d))
        {
            Layer = "MISC"
        });

        var sourcePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
        dxf.Save(sourcePath, false);
        return sourcePath;
    }
}
