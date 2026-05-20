using FloorplanFit.Desktop;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Layout;

public sealed class AppXamlInitializationTests
{
    [Fact]
    public void App_initialize_loads_application_xaml_without_throwing()
    {
        var app = new App();

        var exception = Record.Exception(app.Initialize);

        Assert.Null(exception);
    }

    [Fact]
    public void App_xaml_uses_dark_gray_glass_resources_with_white_text_and_controls()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "App.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("Color=\"#D813171E\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Color=\"#66343A46\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Color=\"#4C3C4452\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Color=\"#F5FFFFFF\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Color=\"#CCFFFFFF\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Background\" Value=\"Transparent\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("Button.tool", xaml, StringComparison.Ordinal);
        Assert.Contains("ToggleButton.folder-toggle", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Background\" Value=\"{DynamicResource SectionBrush}\" />", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Color=\"#EEF2F4F7\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Color=\"#111827\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Review_and_library_windows_do_not_apply_a_solid_background_layer_over_the_glass_shell()
    {
        var solutionRoot = FindSolutionRoot();
        var reviewXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var mainXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml");

        var reviewXaml = File.ReadAllText(reviewXamlPath);
        var mainXaml = File.ReadAllText(mainXamlPath);

        Assert.DoesNotContain("Background=\"{DynamicResource AppBackgroundBrush}\"", reviewXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background=\"{DynamicResource AppBackgroundBrush}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", reviewXaml, StringComparison.Ordinal);
        Assert.Contains("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", mainXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void AppAxaml_promotes_interactive_style_colors_to_named_resources()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "App.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("x:Key=\"MetricChipBrush\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ButtonBaseBrush\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ButtonPrimaryBorderBrush\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ToolActiveBrush\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"InputBrush\"", xaml, StringComparison.Ordinal);

        Assert.DoesNotContain("Background\" Value=\"#3D36404C\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background\" Value=\"#33374250\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background\" Value=\"#5A4B5665\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background\" Value=\"#26313C48\"", xaml, StringComparison.Ordinal);
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
