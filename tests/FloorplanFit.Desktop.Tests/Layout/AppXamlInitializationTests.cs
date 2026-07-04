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
    public void Main_window_keeps_glass_shell_while_embedded_workbenches_stay_transparent()
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
        Assert.Contains("<UserControl", reviewXaml, StringComparison.Ordinal);
        Assert.Contains("<UserControl", sitePlanAdjustmentXaml, StringComparison.Ordinal);
        Assert.Contains("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", reviewXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", sitePlanAdjustmentXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestedThemeVariant=\"Dark\"", reviewXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestedThemeVariant=\"Dark\"", sitePlanAdjustmentXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_version_actions_expose_edit_and_published_only_adjust_to_site_plan()
    {
        var solutionRoot = FindSolutionRoot();
        var mainXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml");
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var xaml = File.ReadAllText(mainXamlPath);
        var codeBehind = File.ReadAllText(mainCodeBehindPath);

        Assert.Contains("Content=\"Edit\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Adjust to Site Plan\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Delete\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding CurationHistoryLabel}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanAdjustToSitePlan}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"AdjustVersionToSitePlanButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AdjustVersionToSitePlanButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.Contains("Title = \"Select site plan DXF\"", codeBehind, StringComparison.Ordinal);
        Assert.Contains("<views:ReviewFloorPlanWindow", xaml, StringComparison.Ordinal);
        Assert.Contains("<views:SitePlanAdjustmentWindow", xaml, StringComparison.Ordinal);
        Assert.Contains("BackToLibraryButton_OnClick", xaml, StringComparison.Ordinal);
        Assert.Contains("ShowVersionReviewAsync", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ShowVersionSitePlanAdjustmentAsync", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Extract Selected Version\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Edit Selected Review\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Delete Selected Version\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Select\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ExtractButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("ReviewButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteSelectedVersionButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectVersionButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowDialog(this)", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("new ReviewFloorPlanWindow", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("new SitePlanAdjustmentWindow", codeBehind, StringComparison.Ordinal);
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
        Assert.Contains("Content=\"Mover plano\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes=\"sidebar-action\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.sidebar-action-active=\"{Binding IsFloorPlanMoveToolActive}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"120\"", xaml, StringComparison.Ordinal);
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
    public void Dxf_export_actions_use_simple_exportar_dxf_copy()
    {
        var solutionRoot = FindSolutionRoot();
        var reviewXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var sitePlanAdjustmentXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml");
        var sitePlanAdjustmentCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml.cs");

        var reviewXaml = File.ReadAllText(reviewXamlPath);
        var sitePlanAdjustmentXaml = File.ReadAllText(sitePlanAdjustmentXamlPath);
        var sitePlanAdjustmentCodeBehind = File.ReadAllText(sitePlanAdjustmentCodeBehindPath);

        Assert.Contains("Content=\"Exportar DXF\"", reviewXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Exportar DXF\"", sitePlanAdjustmentXaml, StringComparison.Ordinal);
        Assert.Contains("Title = \"Exportar DXF\"", sitePlanAdjustmentCodeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("Exportar DXF ajustado", reviewXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Exportar DXF ajustado", sitePlanAdjustmentXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Exportar DXF ajustado", sitePlanAdjustmentCodeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Site_plan_adjustment_window_keeps_the_sidebar_scrollable()
    {
        var solutionRoot = FindSolutionRoot();
        var sitePlanAdjustmentXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml");
        var xaml = File.ReadAllText(sitePlanAdjustmentXamlPath).Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("ColumnDefinitions=\"*,400\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Column=\"1\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AdjustmentSidebarScroller\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("RowDefinitions=\"Auto,Auto,Auto,*\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<StackPanel Orientation=\"Vertical\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.fit-option-applied=\"{Binding IsApplied}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalAlignment=\"Stretch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MinHeight=\"144\"", xaml, StringComparison.Ordinal);
        Assert.Contains("RowDefinitions=\"Auto,Auto,Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Row=\"2\"\n                            HorizontalAlignment=\"Stretch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Sugerir ajuste (AI)\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Suggest Fit Plan", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ColumnDefinitions=\"*,Auto\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"420\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"112\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"132\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Site_plan_adjustment_window_reserves_suggestion_panel_height_so_options_do_not_push_the_preview()
    {
        var solutionRoot = FindSolutionRoot();
        var sitePlanAdjustmentXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml");
        var xaml = File.ReadAllText(sitePlanAdjustmentXamlPath).Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.Contains("x:Name=\"PreviewShell\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AdjustmentSidebar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Column=\"0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Column=\"1\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"30\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Subtitle}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding StatusMessage}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Preview\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"220\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"220\"", xaml, StringComparison.Ordinal);
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
