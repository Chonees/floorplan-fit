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

    [Fact]
    public async Task ExtractAsync_infers_four_and_six_inch_wall_thickness_from_parallel_wall_faces()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaWallExtractor();

        var candidates = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        var fourInchCandidates = candidates.Where(item => item.ThicknessMm == 101.6m).ToArray();
        var sixInchCandidates = candidates.Where(item => item.ThicknessMm == 152.4m).ToArray();

        Assert.NotEmpty(fourInchCandidates);
        Assert.NotEmpty(sixInchCandidates);
        Assert.All(
            fourInchCandidates.Concat(sixInchCandidates),
            item => Assert.Contains("parallel wall faces", item.DetectionNotes, StringComparison.OrdinalIgnoreCase));
    }
}
