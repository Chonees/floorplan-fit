using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Extraction;

public sealed class IxMiliaOpeningExtractorTests
{
    [Fact]
    public async Task ExtractAsync_reads_door_and_window_geometry_from_seminole_fixture()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaOpeningExtractor();

        var extraction = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(extraction.Candidates, item => item.Kind == "Door" && item.SourceLayer == "DOORS" && item.SourceEntityKind == "ARC" && item.Points.Count > 2);
        Assert.Contains(extraction.Candidates, item => item.Kind == "Window" && item.SourceLayer == "WINS" && item.SourceEntityKind == "LINE");
        Assert.All(extraction.Candidates, item =>
        {
            Assert.NotEmpty(item.SourceEntityRef);
            Assert.InRange(item.Confidence, 0.90m, 1m);
            Assert.True(item.Points.Count >= 2);
        });
    }

    [Fact]
    public async Task ExtractAsync_reads_exact_door_and_window_labels_from_dxf_label_layers()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaOpeningExtractor();

        var extraction = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(extraction.Labels, item => item.Kind == "Door" && item.SourceLayer == "DOORTEXT" && item.Text == "2668");
        Assert.Contains(extraction.Labels, item => item.Kind == "Door" && item.SourceLayer == "DOORTEXT" && item.Text == "24\"DR.");
        Assert.Contains(extraction.Labels, item => item.Kind == "Door" && item.SourceLayer == "DOORTEXT" && item.Text == "27\" R.O.");
        Assert.Contains(extraction.Labels, item => item.Kind == "Window" && item.SourceLayer == "WINDWS LBLS" && item.Text == "3050 S.H.");
        Assert.Contains(extraction.Labels, item => item.Kind == "Window" && item.Text == "(3) 3050 FXD. HDR. @ 6'-8\"");
        Assert.All(extraction.Labels, item =>
        {
            Assert.NotEmpty(item.Text);
            Assert.NotEqual(0m, item.X);
            Assert.True(item.TextHeight > 0m);
            Assert.NotNull(item.TextStyleName);
        });
    }

    [Fact]
    public async Task ExtractAsync_ignores_non_opening_notes_that_share_door_text_layer()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaOpeningExtractor();

        var extraction = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.DoesNotContain(extraction.Labels, item => item.Text.Contains("STAND", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(extraction.Labels, item => item.Text.Contains("TUB", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(extraction.Labels, item => item.Text.Contains("WTR", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(extraction.Labels, item => item.Text.Contains("SAFETY GLASS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExtractAsync_reads_santa_barbara_garage_door_label_without_keyword_filtering()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var extractor = new IxMiliaOpeningExtractor();

        var extraction = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(extraction.Labels, item => item.Kind == "Door" && item.Text == "16' W X 8' H GAR. DR.");
        Assert.Contains(extraction.Labels, item => item.Kind == "Window" && item.Text == "4060 FRT FXD.");
        Assert.Contains(extraction.Candidates, item => item.Kind == "Window" && item.SourceLayer == "WIN");
    }
}
