using Avalonia;
using FloorplanFit.Desktop;
using FloorplanFit.Desktop.ViewModels;
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
        var sitePlanAdjustmentXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml");

        var reviewXaml = File.ReadAllText(reviewXamlPath);
        var mainXaml = File.ReadAllText(mainXamlPath);
        var sitePlanAdjustmentXaml = File.ReadAllText(sitePlanAdjustmentXamlPath);

        Assert.DoesNotContain("Background=\"{DynamicResource AppBackgroundBrush}\"", reviewXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background=\"{DynamicResource AppBackgroundBrush}\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background=\"{DynamicResource AppBackgroundBrush}\"", sitePlanAdjustmentXaml, StringComparison.Ordinal);
        Assert.Contains("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", reviewXaml, StringComparison.Ordinal);
        Assert.Contains("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", sitePlanAdjustmentXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_version_actions_expose_edit_and_published_only_adjust_to_site_plan()
    {
        var solutionRoot = FindSolutionRoot();
        var mainXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml");
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var xaml = File.ReadAllText(mainXamlPath);
        var codeBehind = File.ReadAllText(mainCodeBehindPath);

        Assert.Contains("Content=\"Edit Selected Review\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Edit\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Adjust to Site Plan\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanAdjustToSitePlan}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"AdjustVersionToSitePlanButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AdjustVersionToSitePlanButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.Contains("Title = \"Select site plan DXF\"", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Open\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Site_plan_adjustment_window_reuses_preview_ux_without_fit_tools()
    {
        var solutionRoot = FindSolutionRoot();
        var sitePlanAdjustmentXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml");
        var sitePlanAdjustmentCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml.cs");
        var xaml = File.ReadAllText(sitePlanAdjustmentXamlPath);
        var codeBehind = File.ReadAllText(sitePlanAdjustmentCodeBehindPath);

        Assert.Contains("x:DataType=\"viewModels:SitePlanAdjustmentViewModel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes=\"preview-switch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding ArePreviewDimensionsVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SitePlanGeometryPaths=\"{Binding SitePlanGeometryPaths}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SitePlanRenderPaths=\"{Binding SitePlanRenderPaths}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SitePlanTexts=\"{Binding SitePlanTexts}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("GeometryPaths=\"{Binding FloorPlanGeometryPaths}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Dimensions=\"{Binding Dimensions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ChangedNumberDimensionIds=\"{Binding ChangedNumberDimensionIds}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AreDimensionsVisible=\"{Binding ArePreviewDimensionsVisible}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Move Floor Plan\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.tool-active=\"{Binding IsFloorPlanMoveToolActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ToggleFloorPlanMoveToolCommand}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsFloorPlanMoveToolActive=\"{Binding IsFloorPlanMoveToolActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding AutoFitSuggestionOptions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"ApplyAutoFitOptionButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ApplyAutoFitOptionButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("DataContext.ApplyAutoFitPlanCommand", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("FitToolPalette", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Inspector", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Site_plan_adjustment_window_keeps_fit_options_in_a_bounded_horizontal_strip()
    {
        var solutionRoot = FindSolutionRoot();
        var sitePlanAdjustmentXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml");
        var xaml = File.ReadAllText(sitePlanAdjustmentXamlPath);

        Assert.Contains("x:Name=\"AutoFitOptionsScroller\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<StackPanel Orientation=\"Horizontal\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.fit-option-applied=\"{Binding IsApplied}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Site_plan_adjustment_window_reserves_suggestion_panel_height_so_options_do_not_push_the_preview()
    {
        var solutionRoot = FindSolutionRoot();
        var sitePlanAdjustmentXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml");
        var xaml = File.ReadAllText(sitePlanAdjustmentXamlPath).Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains(
            "Classes=\"section-card\"\n                Height=\"280\"\n                MaxHeight=\"280\"",
            xaml,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Site_plan_adjustment_window_initializes_xaml_without_throwing()
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .SetupWithoutStarting();

        var exception = Record.Exception(() =>
        {
            _ = new SitePlanAdjustmentWindow
            {
                DataContext = new SitePlanAdjustmentViewModel(
                    "Adjust",
                    "Subtitle",
                    "Preview",
                    "Status",
                    sitePlanGeometryPaths: [],
                    sitePlanRenderPaths: [],
                    sitePlanTexts: [],
                    floorPlanGeometryPaths: [],
                    roomLabels: [],
                    openingLabels: [],
                    dimensions: [])
            };
        });

        Assert.Null(exception);
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
