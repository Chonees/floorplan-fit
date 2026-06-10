using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Tests.TestSupport;

namespace FloorplanFit.Infrastructure.Tests.Dxf;

public sealed class IxMiliaSitePlanPreviewReaderTests
{
    [Fact]
    public async Task ReadAsync_preserves_site_plan_text_layers_colors_and_setback_semantics()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var fixturePath = Path.Combine(solutionRoot, "PLANS", "originalsSitePlans", "158 DAWSON STREET.dxf");
        var reader = new IxMiliaSitePlanPreviewReader();

        var preview = await reader.ReadAsync(fixturePath, CancellationToken.None);

        Assert.Contains(preview.Texts, text => text.Text.Contains("SITE PLAN", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.RenderPaths, path =>
            path.SourceLayer.Contains("SETBACK", StringComparison.OrdinalIgnoreCase) &&
            path.IsSetback);
        Assert.True(preview.RenderPaths.Count > 0);
        Assert.Contains(preview.RenderPaths, path => !string.IsNullOrWhiteSpace(path.ColorArgb));
        Assert.True(preview.RenderPaths.Select(path => path.ColorArgb).Where(color => color is not null).Distinct().Count() > 1);
    }

    [Fact]
    public async Task ReadAsync_extracts_more_than_line_and_lwpolyline_geometry_for_full_site_plan_preview()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var fixturePath = Path.Combine(solutionRoot, "PLANS", "originalsSitePlans", "158 DAWSON STREET.dxf");
        var reader = new IxMiliaSitePlanPreviewReader();

        var preview = await reader.ReadAsync(fixturePath, CancellationToken.None);

        Assert.Contains(preview.RenderPaths, path =>
            string.Equals(path.SourceEntityKind, "ARC", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path.SourceEntityKind, "CIRCLE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path.SourceEntityKind, "ELLIPSE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path.SourceEntityKind, "INSERT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReadAsync_uses_setback_geometry_bounds_as_buildable_area()
    {
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var fixturePath = Path.Combine(solutionRoot, "PLANS", "originalsSitePlans", "158 DAWSON STREET.dxf");
        var reader = new IxMiliaSitePlanPreviewReader();

        var preview = await reader.ReadAsync(fixturePath, CancellationToken.None);

        var expected = BoundsOf(preview.RenderPaths.Where(path => path.IsSetback));

        Assert.Equal(expected.MinX, preview.BuildableArea.MinX);
        Assert.Equal(expected.MinY, preview.BuildableArea.MinY);
        Assert.Equal(expected.MaxX, preview.BuildableArea.MaxX);
        Assert.Equal(expected.MaxY, preview.BuildableArea.MaxY);
    }

    private static GeometryBounds BoundsOf(IEnumerable<FloorplanFit.Contracts.FloorPlans.SitePlanRenderPathDto> paths)
    {
        var points = paths
            .SelectMany(path => path.Segments)
            .SelectMany(segment => new[]
            {
                (X: segment.StartX, Y: segment.StartY),
                (X: segment.EndX, Y: segment.EndY)
            })
            .ToArray();

        return new GeometryBounds(
            points.Min(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.X),
            points.Max(point => point.Y));
    }

    private readonly record struct GeometryBounds(decimal MinX, decimal MinY, decimal MaxX, decimal MaxY)
    {
        public decimal Area => (MaxX - MinX) * (MaxY - MinY);
    }
}
