using Xunit;

namespace FloorplanFit.Desktop.Tests.Layout;

public sealed class ReviewFloorPlanWindowLayoutTests
{
    [Fact]
    public void Review_xaml_does_not_use_rigid_preview_or_curated_wall_heights()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("WindowState=\"Maximized\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"1450\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"920\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("FloorPlanPreviewControl Height=\"700\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ListBox Height=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<controls:FloorPlanPreviewControl", xaml, StringComparison.Ordinal);
        Assert.Contains("Lines", xaml, StringComparison.Ordinal);
        Assert.Contains("Preview", xaml, StringComparison.Ordinal);
        Assert.Contains("Pinch Tools", xaml, StringComparison.Ordinal);
        Assert.Contains("Pinch Groups", xaml, StringComparison.Ordinal);
        Assert.Contains("Create Group", xaml, StringComparison.Ordinal);
        Assert.Contains("Axis", xaml, StringComparison.Ordinal);
        Assert.Contains("Add Pinch", xaml, StringComparison.Ordinal);
        Assert.Contains("Remove Pinch", xaml, StringComparison.Ordinal);
        Assert.Contains("PreviewPinchGroupId=\"{Binding SelectedPinchGroupId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("RoomLabels=\"{Binding RoomLabels}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OpeningCandidates=\"{Binding OpeningCandidates}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OpeningLabels=\"{Binding OpeningLabels}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FixedPlanComponents=\"{Binding FixedPlanComponents}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Openings", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedOpeningCandidate", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedOpeningLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("Remove Selected Opening", xaml, StringComparison.Ordinal);
        Assert.Contains("Remove Selected Label", xaml, StringComparison.Ordinal);
        Assert.Contains("DoorOpeningCount", xaml, StringComparison.Ordinal);
        Assert.Contains("WindowOpeningCount", xaml, StringComparison.Ordinal);
        Assert.Contains("Fixed Elements", xaml, StringComparison.Ordinal);
        Assert.Contains("FixedPlanComponentCount", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedFixedPlanComponent", xaml, StringComparison.Ordinal);
        Assert.Contains("Remove Selected Component", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Accept Candidate", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Save Metadata", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Inspector", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Curated Walls", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Stable Wall Id", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_control_uses_conservative_fit_padding_to_keep_tall_floorplans_visible()
    {
        var solutionRoot = FindSolutionRoot();
        var controlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "Controls", "FloorPlanPreviewControl.cs");
        var source = File.ReadAllText(controlPath);

        Assert.Contains("private const double PreviewPadding = 48d;", source, StringComparison.Ordinal);
        Assert.DoesNotContain("private const double PreviewPadding = 16d;", source, StringComparison.Ordinal);
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
