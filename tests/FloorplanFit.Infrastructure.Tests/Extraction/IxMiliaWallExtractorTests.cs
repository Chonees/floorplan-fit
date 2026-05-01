using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Extraction;

public sealed class IxMiliaWallExtractorTests
{
    [Fact]
    public async Task ExtractAsync_reads_wall_layer_candidates_from_santa_barbara_fixture()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var extractor = new IxMiliaWallExtractor();

        var candidates = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.NotEmpty(candidates);
        Assert.InRange(candidates.Count, 200, 300);
        Assert.All(candidates, item =>
        {
            Assert.True(item.Points.Count >= 2);
            Assert.Contains("WALL", item.SourceLayer, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Contains(candidates, item => item.SourceEntityRef.StartsWith("LINE:", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(candidates, item => item.SourceEntityRef.StartsWith("LWPOLYLINE:", StringComparison.OrdinalIgnoreCase));
    }
}
