using Avalonia.Media;
using FloorplanFit.Desktop.Controls.Preview;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class PreviewSemanticPaletteTests
{
    [Fact]
    public void PreviewSemanticPalette_exposes_workspace_handle_and_default_argb_tokens()
    {
        Assert.Equal("#00000000", PreviewSemanticPalette.TransparentArgb);
        Assert.Equal(Color.Parse("#FF0F172A"), PreviewSemanticPalette.WorkspaceBackground);
        Assert.Equal(Color.FromArgb(92, 190, 190, 190), PreviewSemanticPalette.WorkspaceDot);
        Assert.Equal(Color.Parse("#FF1E293B"), PreviewSemanticPalette.MinorGrid);
        Assert.Equal(Color.Parse("#FF334155"), PreviewSemanticPalette.MajorGrid);
        Assert.Equal(Color.FromArgb(32, 0, 0, 0), PreviewSemanticPalette.HandleFill);
        Assert.Equal(Color.FromArgb(220, 0, 0, 0), PreviewSemanticPalette.HandleStroke);
    }

    [Fact]
    public void Preview_renderers_and_review_view_model_consume_palette_tokens_instead_of_local_literals()
    {
        var solutionRoot = FindSolutionRoot();
        var workspaceRendererPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "Controls", "Preview", "PreviewWorkspaceRenderer.cs");
        var compressionRendererPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "Controls", "Preview", "CompressionHandlePreviewLayerRenderer.cs");
        var reviewViewModelPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ViewModels", "FloorPlanReviewViewModel.cs");
        var inspectorCoordinatorPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ViewModels", "Review", "FloorPlanReviewInspectorCoordinator.cs");

        var workspaceRenderer = File.ReadAllText(workspaceRendererPath);
        var compressionRenderer = File.ReadAllText(compressionRendererPath);
        var reviewViewModel = File.ReadAllText(reviewViewModelPath);
        var inspectorCoordinator = File.ReadAllText(inspectorCoordinatorPath);

        Assert.Contains("PreviewSemanticPalette.WorkspaceBackground", workspaceRenderer, StringComparison.Ordinal);
        Assert.Contains("PreviewSemanticPalette.WorkspaceDot", workspaceRenderer, StringComparison.Ordinal);
        Assert.Contains("PreviewSemanticPalette.MinorGrid", workspaceRenderer, StringComparison.Ordinal);
        Assert.Contains("PreviewSemanticPalette.MajorGrid", workspaceRenderer, StringComparison.Ordinal);
        Assert.DoesNotContain("Color.Parse(\"#FF0F172A\")", workspaceRenderer, StringComparison.Ordinal);
        Assert.DoesNotContain("Color.FromArgb(92, 190, 190, 190)", workspaceRenderer, StringComparison.Ordinal);
        Assert.DoesNotContain("Color.Parse(\"#FF1E293B\")", workspaceRenderer, StringComparison.Ordinal);
        Assert.DoesNotContain("Color.Parse(\"#FF334155\")", workspaceRenderer, StringComparison.Ordinal);

        Assert.Contains("PreviewSemanticPalette.HandleFill", compressionRenderer, StringComparison.Ordinal);
        Assert.Contains("PreviewSemanticPalette.HandleStroke", compressionRenderer, StringComparison.Ordinal);
        Assert.DoesNotContain("Color.FromArgb(32, 0, 0, 0)", compressionRenderer, StringComparison.Ordinal);
        Assert.DoesNotContain("Color.FromArgb(220, 0, 0, 0)", compressionRenderer, StringComparison.Ordinal);

        Assert.DoesNotContain("?? \"#00000000\"", reviewViewModel, StringComparison.Ordinal);
        Assert.Contains("PreviewSemanticPalette.TransparentArgb", inspectorCoordinator, StringComparison.Ordinal);
        Assert.DoesNotContain("?? \"#00000000\"", inspectorCoordinator, StringComparison.Ordinal);
    }

    private static string FindSolutionRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "FloorplanFit.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate FloorplanFit.sln from test base directory.");
    }
}
