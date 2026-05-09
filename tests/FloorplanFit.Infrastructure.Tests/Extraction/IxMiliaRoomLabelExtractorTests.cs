using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Extraction;

public sealed class IxMiliaRoomLabelExtractorTests
{
    [Fact]
    public async Task ExtractAsync_reads_room_labels_from_seminole_fixture()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaRoomLabelExtractor();

        var labels = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(labels, item => item.Text == "KITCHEN" && item.SourceLayer == "ROOM LBLS");
        Assert.Contains(labels, item => item.Text == "LIVING ROOM" && item.SourceLayer == "ROOM LBLS");
        Assert.Contains(labels, item => item.Text == "BEDROOM 2" && item.SourceLayer == "ROOM LBLS");
        Assert.Contains(labels, item => item.Text == "BATH 3" && item.SourceLayer == "ROOM LBLS");
        Assert.DoesNotContain(labels, item => item.Text == "FLOOR PLAN");
        Assert.All(labels, item =>
        {
            Assert.NotEqual(0m, item.X);
            Assert.NotEqual(0m, item.Confidence);
        });
    }

    [Fact]
    public async Task ExtractAsync_preserves_room_label_dxf_visual_metadata()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SEMINOLE2000.dxf");
        var extractor = new IxMiliaRoomLabelExtractor();

        var labels = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        var kitchen = labels.Single(item => item.Text == "KITCHEN");
        Assert.Equal("TEXT", kitchen.SourceEntityKind);
        Assert.True(kitchen.TextHeight > 0m);
        Assert.NotNull(kitchen.TextStyleName);
        Assert.NotNull(kitchen.HorizontalAlignment);
        Assert.NotNull(kitchen.VerticalAlignment);
    }

    [Fact]
    public async Task ExtractAsync_reads_room_labels_from_santa_barbara_fixture_without_wall_legend()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var extractor = new IxMiliaRoomLabelExtractor();

        var labels = await extractor.ExtractAsync(sourcePath, CancellationToken.None);

        Assert.Contains(labels, item => item.Text == "KITCHEN");
        Assert.Contains(labels, item => item.Text == "LIVING ROOM");
        Assert.Contains(labels, item => item.Text == "2 CAR GARAGE");
        Assert.Contains(labels, item => item.Text == "COV'D. PATIO");
        Assert.DoesNotContain(labels, item => item.Text == "WALL LEGEND");
    }
}
