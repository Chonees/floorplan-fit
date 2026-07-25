using System.Xml.Linq;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Layout;

public sealed class SitePlanAdjustmentWindowLayoutTests
{
    [Fact]
    public void Commissioned_mode_exposes_single_confirm_export_action_and_gates_legacy_controls()
    {
        var xaml = ReadAdjustmentXaml();
        var document = XDocument.Parse(xaml);
        var defaultNs = document.Root!.GetDefaultNamespace();
        var buttons = document.Descendants(defaultNs + "Button").ToArray();

        var commissionedButtons = buttons
            .Where(button => (string?)button.Attribute("IsVisible") == "{Binding IsCommissionedAutoFit}")
            .ToArray();
        var confirmButton = Assert.Single(commissionedButtons);
        Assert.Equal("Confirmar y exportar", (string?)confirmButton.Attribute("Content"));
        Assert.Equal("{Binding CanExportAdjustedSitePlan}", (string?)confirmButton.Attribute("IsEnabled"));
        Assert.False(string.IsNullOrWhiteSpace((string?)confirmButton.Attribute("AutomationProperties.Name")));

        foreach (var legacyContent in new[]
                 {
                     "Sugerir ajuste (AI)",
                     "Exportar DXF",
                     "Confirmar manuales + re-exportar"
                 })
        {
            var legacyButton = Assert.Single(
                buttons,
                button => (string?)button.Attribute("Content") == legacyContent);
            Assert.Equal("{Binding !IsCommissionedAutoFit}", (string?)legacyButton.Attribute("IsVisible"));
        }

        var legacyOptions = Assert.Single(
            document.Descendants(defaultNs + "ItemsControl"),
            items => (string?)items.Attribute("ItemsSource") == "{Binding AutoFitSuggestionOptions}");
        Assert.Equal("{Binding !IsCommissionedAutoFit}", (string?)legacyOptions.Attribute("IsVisible"));
    }

    [Fact]
    public void Adjustment_xaml_has_no_pinch_vertex_entity_or_crossing_controls()
    {
        var xaml = ReadAdjustmentXaml();
        Assert.DoesNotContain("Pinch", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("crossing", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vertex", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vértice", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityRef", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SourceEntity", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("CompressionHandle", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("commissioning", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Commissioned_comparison_panel_labels_canonical_base_and_legend_honestly()
    {
        var xaml = ReadAdjustmentXaml();
        Assert.Contains(
            "Antes · FloorPlan + Electrical registrado (estructura WALL)",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "Después · FloorPlan ajustado = ArchitecturalBase de Electrical",
            xaml,
            StringComparison.Ordinal);
        Assert.Contains("FloorPlan · blanco", xaml, StringComparison.Ordinal);
        Assert.Contains("Electrical · cian", xaml, StringComparison.Ordinal);
        Assert.Contains(
            "IsVisible=\"{Binding IsCommissionedComparisonAvailable}\"",
            xaml,
            StringComparison.Ordinal);

        var document = XDocument.Parse(xaml);
        var defaultNs = document.Root!.GetDefaultNamespace();
        var comparisonShell = Assert.Single(
            document.Descendants(defaultNs + "Border"),
            border => (string?)border.Attribute("IsVisible") == "{Binding IsCommissionedAutoFit}");
        Assert.Contains(
            comparisonShell.Descendants(defaultNs + "TextBlock"),
            text => (string?)text.Attribute("Text") == "{Binding CommissionedComparisonStatus}");
    }

    [Fact]
    public void Library_adjust_button_is_gated_by_auto_fit_readiness_with_visible_reason()
    {
        var xaml = ReadLibraryXaml();
        Assert.Contains("Text=\"{Binding AutoFitReadinessLabel}\"", xaml, StringComparison.Ordinal);

        var document = XDocument.Parse(xaml);
        var defaultNs = document.Root!.GetDefaultNamespace();
        var adjustButton = Assert.Single(
            document.Descendants(defaultNs + "Button"),
            button => (string?)button.Attribute("Content") == "Adjust to Site Plan");
        Assert.Equal("{Binding CanAdjustToSitePlan}", (string?)adjustButton.Attribute("IsEnabled"));
        Assert.False(string.IsNullOrWhiteSpace((string?)adjustButton.Attribute("ToolTip.Tip")));
    }

    [Fact]
    public void Commissioned_preview_projector_source_never_references_pinch_or_legacy_suggester()
    {
        var source = File.ReadAllText(Path.Combine(
            FindSolutionRoot(),
            "src",
            "FloorplanFit.Desktop",
            "ViewModels",
            "CommissionedHouseFitPreviewProjector.cs"));
        Assert.DoesNotContain("Pinch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IAutoFitPlanSuggester", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoFitSuggestionOptionGenerator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CadStretchRecipeCompiler", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ToFloorSourceStretchAction", source, StringComparison.Ordinal);
    }

    private static string ReadAdjustmentXaml()
        => File.ReadAllText(Path.Combine(
            FindSolutionRoot(),
            "src",
            "FloorplanFit.Desktop",
            "SitePlanAdjustmentWindow.axaml"));

    private static string ReadLibraryXaml()
        => File.ReadAllText(Path.Combine(
            FindSolutionRoot(),
            "src",
            "FloorplanFit.Desktop",
            "MainWindow.axaml"));

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
