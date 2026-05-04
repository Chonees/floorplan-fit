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

        Assert.DoesNotContain("FloorPlanPreviewControl Height=\"700\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ListBox Height=\"180\"", xaml, StringComparison.Ordinal);
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
