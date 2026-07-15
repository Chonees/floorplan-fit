using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Desktop.ViewModels;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Layout;

public sealed class RegistrationReviewLayoutTests
{
    [Fact]
    public void Pending_registration_row_keeps_compact_quality_fields_and_opens_review_instead_of_confirming()
    {
        var solutionRoot = FindSolutionRoot();
        var xaml = File.ReadAllText(Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml"));
        var codeBehind = File.ReadAllText(Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs"));

        Assert.Contains("RegistrationMethod", xaml, StringComparison.Ordinal);
        Assert.Contains("RegistrationConfidence", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding RegistrationQualityLabel}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Revisar y confirmar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"ReviewSheetRegistrationButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("new RegistrationReviewDialog", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ShowDialog<bool>", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ConfirmDependentSheetRegistrationAsync", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Pending_registration_actions_route_only_electrical_to_visual_review()
    {
        var solutionRoot = FindSolutionRoot();
        var xaml = File.ReadAllText(Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml"));
        var codeBehind = File.ReadAllText(Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs"));

        Assert.Contains("ConverterParameter=Electrical", xaml, StringComparison.Ordinal);
        Assert.Contains("ConverterParameter=NonElectrical", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Confirm\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"ConfirmSheetRegistrationButton_OnClick\"", xaml, StringComparison.Ordinal);

        var reviewStart = codeBehind.IndexOf("private async void ReviewSheetRegistrationButton_OnClick", StringComparison.Ordinal);
        var confirmStart = codeBehind.IndexOf("private async void ConfirmSheetRegistrationButton_OnClick", StringComparison.Ordinal);
        var rejectStart = codeBehind.IndexOf("private async void RejectSheetRegistrationButton_OnClick", StringComparison.Ordinal);
        Assert.True(reviewStart >= 0 && confirmStart > reviewStart && rejectStart > confirmStart);

        var reviewHandler = codeBehind[reviewStart..confirmStart];
        var directConfirmHandler = codeBehind[confirmStart..rejectStart];
        Assert.Contains("sheet.SheetType != \"ElectricalPlan\"", reviewHandler, StringComparison.Ordinal);
        Assert.Contains("new RegistrationReviewDialog", reviewHandler, StringComparison.Ordinal);
        Assert.Contains("sheet.SheetType == \"ElectricalPlan\"", directConfirmHandler, StringComparison.Ordinal);
        Assert.DoesNotContain("RegistrationReviewDialog", directConfirmHandler, StringComparison.Ordinal);
        Assert.Contains("ConfirmDependentSheetRegistrationAsync", directConfirmHandler, StringComparison.Ordinal);
    }

    [Fact]
    public void Registration_review_keeps_diagnostics_scrollable_and_actions_in_a_fixed_bottom_row()
    {
        var solutionRoot = FindSolutionRoot();
        var xaml = File.ReadAllText(Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "RegistrationReviewDialog.axaml"));

        Assert.Contains("RowDefinitions=\"Auto,*,Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<controls:RegistrationOverlayPreviewControl", xaml, StringComparison.Ordinal);
        Assert.Contains("CanonicalGeometry=\"{Binding CanonicalGeometry}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ElectricalGeometry=\"{Binding ElectricalGeometry}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"Wrap\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Row=\"2\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Cancelar / Volver\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Confirmar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanConfirm}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Overlay_transform_matches_registration_scale_rotation_and_translation()
    {
        var pathId = Guid.NewGuid();
        GeometryPathDto[] source =
        [
            new(pathId, false, [new GeometrySegmentDto(pathId, 1, 1m, 2m, 3m, 4m)])
        ];

        var transformed = RegistrationReviewData.TransformElectricalGeometry(
            source,
            new SheetRegistrationTransformDto(2m, 90m, 10m, -5m));

        var segment = Assert.Single(Assert.Single(transformed).Segments);
        Assert.Equal(6m, segment.StartX);
        Assert.Equal(-3m, segment.StartY);
        Assert.Equal(2m, segment.EndX);
        Assert.Equal(1m, segment.EndY);
    }

    [Fact]
    public void Failed_preview_disables_confirmation_and_keeps_the_failure_visible()
    {
        var sheet = new PlanSetSheetDto(
            Guid.NewGuid(),
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            Guid.NewGuid(),
            IsCanonical: false,
            RegistrationStatus: "PendingConfirmation",
            ProjectionStatus: "NotProjected",
            SheetRegistrationId: Guid.NewGuid());

        var review = RegistrationReviewData.Failure(sheet, "DXF could not load.");

        Assert.True(review.HasError);
        Assert.False(review.CanConfirm);
        Assert.Equal("DXF could not load.", review.ErrorMessage);
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
