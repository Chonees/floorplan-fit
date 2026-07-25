using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Layout;

public sealed class ReviewFloorPlanWindowLayoutTests
{
    [Fact]
    public void Review_xaml_uses_preview_first_layout_without_review_queue_column()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);
        Assert.Contains("<UserControl", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestedThemeVariant=\"Dark\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("WindowState=\"Maximized\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"1450\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"920\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("FloorPlanPreviewControl Height=\"700\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ListBox Height=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ColumnDefinitions=\"*,440,64\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<controls:FloorPlanPreviewControl", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Review Queue\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ReviewQueueSearchText", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedReviewQueueFilter", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("VisibleWallCandidates", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("VisibleRoomLabels", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("VisibleOpeningLabels", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("VisibleDimensions", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Preview\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Inspector\"", xaml, StringComparison.Ordinal);
        Assert.Contains("InspectorToolBar", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.tool-active", xaml, StringComparison.Ordinal);
        Assert.Contains("Eje", xaml, StringComparison.Ordinal);
        Assert.Contains("Agregar ajuste", xaml, StringComparison.Ordinal);
        Assert.Contains("Quitar ajuste", xaml, StringComparison.Ordinal);
        Assert.Contains("Excluir del curado", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("CuratedObjectsSectionTitle", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedCuratedArtifact", xaml, StringComparison.Ordinal);
        Assert.Contains("CuratedPlanArtifacts=\"{Binding VisibleCuratedPlanArtifacts}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Save Classification", xaml, StringComparison.Ordinal);
        Assert.Contains("Restore Detected Classification", xaml, StringComparison.Ordinal);
        Assert.Contains("Restore Detected Position", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedArtifactPositionSummary", xaml, StringComparison.Ordinal);
        Assert.Contains("EditableSelectedLabelTextHeight", xaml, StringComparison.Ordinal);
        Assert.Contains("Save Label Size", xaml, StringComparison.Ordinal);
        Assert.Contains("Restore Detected Size", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedLabelTextHeightSummary", xaml, StringComparison.Ordinal);
        Assert.Contains("MovableArtifactMoved", xaml, StringComparison.Ordinal);
        Assert.Contains("RoomLabelClicked", xaml, StringComparison.Ordinal);
        Assert.Contains("OpeningLabelClicked", xaml, StringComparison.Ordinal);
        Assert.Contains("PreviewPinchGroupId=\"{Binding SelectedPinchGroupId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedPinchMarkerId=\"{Binding SelectedPinchMarkerId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("PinchMarkerClicked=\"PreviewControl_OnPinchMarkerClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("RoomLabels=\"{Binding RoomLabels}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OpeningCandidates=\"{Binding OpeningCandidates}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OpeningLabels=\"{Binding OpeningLabels}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Dimensions=\"{Binding Dimensions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FixedPlanComponents=\"{Binding FixedPlanComponents}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedRoomLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedOpeningLabel", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Quick Filters", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("QueueSummary", xaml, StringComparison.Ordinal);
        Assert.Contains("Acciones de selección", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsStructureQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsRoomNamesQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsOpeningCodesQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsDimensionsQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsCuratedObjectsQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Accept Candidate", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Save Metadata", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Reject Selected Line", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove Selected Opening", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove Selected Label", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove Selected Component", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Remove Selected Detail", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain(">Openings<", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedOpeningCandidate", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("DoorOpeningCount", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("WindowOpeningCount", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Fixed Elements", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedFixedPlanComponent", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Protected Details", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedProtectedDetailAssembly", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Curated Walls", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Stable Wall Id", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Plan Elements", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Selected Item", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Pinch Tools", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Review_queue_column_is_removed_from_edit_layout()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.DoesNotContain("ReviewQueueContentHost", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Classes=\"folder-toggle\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("RowDefinitions=\"Auto,Auto,Auto,Auto,Auto,Auto,*\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ColumnDefinitions=\"*,440,64\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds=\"True\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Expander", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxHeight=\"420\"", xaml, StringComparison.Ordinal);
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

    [Fact]
    public void Preview_panel_exposes_a_toggle_switch_to_hide_dimensions_visually()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("<ToggleSwitch", xaml, StringComparison.Ordinal);
        Assert.Contains("ArePreviewDimensionsVisible", xaml, StringComparison.Ordinal);
        Assert.Contains("OnContent=\"Visibles\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OffContent=\"Ocultas\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Main_review_header_exposes_edit_and_publish_on_library_back_row()
    {
        var solutionRoot = FindSolutionRoot();
        var mainXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml");
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var mainXaml = File.ReadAllText(mainXamlPath);
        var mainCodeBehind = File.ReadAllText(mainCodeBehindPath);
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("ColumnDefinitions=\"Auto,*,Auto,Auto\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Editar\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding ActiveReviewViewModel.CanEditPublishedCuration}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"EditPublishedButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Publish Curation\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding ActiveReviewViewModel.CanPublishCuration}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"PublishButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.True(
            mainXaml.IndexOf("BackToLibraryButton_OnClick", StringComparison.Ordinal) <
            mainXaml.IndexOf("Content=\"Editar\"", StringComparison.Ordinal));
        Assert.True(
            mainXaml.IndexOf("Content=\"Editar\"", StringComparison.Ordinal) <
            mainXaml.IndexOf("Content=\"Publish Curation\"", StringComparison.Ordinal));
        Assert.DoesNotContain("Content=\"Editar\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"Publish Curation\"", xaml, StringComparison.Ordinal);
        Assert.Contains("StartEditingPublishedCurationAsync", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("PublishAsync", mainCodeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Adjust_to_site_plan_entry_opens_the_site_plan_picker_directly()
    {
        var solutionRoot = FindSolutionRoot();
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var mainCodeBehind = File.ReadAllText(mainCodeBehindPath);

        var handlerStart = mainCodeBehind.IndexOf(
            "private async void AdjustVersionToSitePlanButton_OnClick",
            StringComparison.Ordinal);
        var pickerStart = mainCodeBehind.IndexOf(
            "private async Task<string?> OpenSitePlanPickerAsync",
            StringComparison.Ordinal);
        Assert.True(handlerStart >= 0 && pickerStart > handlerStart);

        var handler = mainCodeBehind[handlerStart..pickerStart];
        Assert.Contains("var sitePlanFilePath = await OpenSitePlanPickerAsync();", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("AdjustSitePlanSetupDialog", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("SyntheticSitePlanDxfWriter", handler, StringComparison.Ordinal);
        Assert.Contains("if (sitePlanFilePath is null)", handler, StringComparison.Ordinal);
        Assert.Contains("ShowVersionSitePlanAdjustmentAsync", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_header_removes_ambiguous_dependent_sheet_auto_import_entry()
    {
        var solutionRoot = FindSolutionRoot();
        var mainXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml");
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var mainXaml = File.ReadAllText(mainXamlPath);
        var mainCodeBehind = File.ReadAllText(mainCodeBehindPath);

        Assert.DoesNotContain("Content=\"Auto Import Sheet\"", mainXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ImportDependentSheetButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("Auto sheet import needs type", mainCodeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_keeps_site_plan_adjustment_scope_alive_until_leaving_adjustment_screen()
    {
        var solutionRoot = FindSolutionRoot();
        var viewModelPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ViewModels", "LibraryViewModel.cs");
        var viewModel = File.ReadAllText(viewModelPath);

        Assert.Contains("private IServiceScope? activeSitePlanAdjustmentScope;", viewModel, StringComparison.Ordinal);
        Assert.Contains("activeSitePlanAdjustmentScope = scope;", viewModel, StringComparison.Ordinal);
        Assert.Contains("DisposeActiveSitePlanAdjustmentScope();", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("using var scope = scopeFactory.CreateScope();\r\n        var sitePlanReader", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("using var scope = scopeFactory.CreateScope();\n        var sitePlanReader", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_header_exposes_explicit_dependent_sheet_import_entries()
    {
        var solutionRoot = FindSolutionRoot();
        var mainXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml");
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var mainXaml = File.ReadAllText(mainXamlPath);
        var mainCodeBehind = File.ReadAllText(mainCodeBehindPath);

        Assert.Contains("Content=\"Import Electrical\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Import Roof\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Import Facade\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("ImportElectricalSheetButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("\"ElectricalPlan\"", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("\"RoofPlan\"", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("\"FacadeElevation\"", mainCodeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_version_row_exposes_select_button_for_dependent_sheet_import_target()
    {
        var solutionRoot = FindSolutionRoot();
        var mainXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml");
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var mainXaml = File.ReadAllText(mainXamlPath);
        var mainCodeBehind = File.ReadAllText(mainCodeBehindPath);

        Assert.Contains("Content=\"Select\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"SelectVersionButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("SelectVersionButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("viewModel.SelectVersion(item, version);", mainCodeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_screen_shows_selected_plan_set_sheets()
    {
        var solutionRoot = FindSolutionRoot();
        var mainXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml");
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var mainXaml = File.ReadAllText(mainXamlPath);
        var mainCodeBehind = File.ReadAllText(mainCodeBehindPath);

        Assert.Contains("SelectedPlanSetSheetsLabel", mainXaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding SelectedPlanSetSheets}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"planSets:PlanSetSheetDto\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("RegistrationStatus", mainXaml, StringComparison.Ordinal);
        Assert.Contains("ProjectionStatus", mainXaml, StringComparison.Ordinal);
        Assert.Contains("RegistrationQualityLabel", mainXaml, StringComparison.Ordinal);
        Assert.Contains("ProjectionQualityLabel", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"×\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanUnlink}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"UnlinkDependentSheetButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Register\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanRegisterDependent}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"RegisterDependentSheetButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Revisar y confirmar\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"ReviewSheetRegistrationButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Reject Reg\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanRejectRegistration}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"RejectSheetRegistrationButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Confirm Projection\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanConfirmProjection}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"ConfirmSheetProjectionButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"To Electrical\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanCorrectToElectrical}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"CorrectSheetToElectricalButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"To Roof\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanCorrectToRoof}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"CorrectSheetToRoofButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"To Facade\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanCorrectToFacade}\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"CorrectSheetToFacadeButton_OnClick\"", mainXaml, StringComparison.Ordinal);
        Assert.Contains("UnlinkDependentSheetButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("RegisterDependentSheetButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("ReviewSheetRegistrationButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("RejectSheetRegistrationButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("ConfirmSheetProjectionButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("CorrectSheetToElectricalButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("CorrectSheetToRoofButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("CorrectSheetToFacadeButton_OnClick", mainCodeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Library_register_action_bypasses_the_manual_dialog_for_electrical_but_keeps_it_for_roof_and_facade()
    {
        var solutionRoot = FindSolutionRoot();
        var mainCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "MainWindow.axaml.cs");
        var dialogXamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "RegistrationTransformDialog.axaml");
        var dialogCodeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "RegistrationTransformDialog.axaml.cs");
        var mainCodeBehind = File.ReadAllText(mainCodeBehindPath);
        var dialogXaml = File.ReadAllText(dialogXamlPath);
        var dialogCodeBehind = File.ReadAllText(dialogCodeBehindPath);

        var registerHandlerStart = mainCodeBehind.IndexOf(
            "private async void RegisterDependentSheetButton_OnClick",
            StringComparison.Ordinal);
        var confirmHandlerStart = mainCodeBehind.IndexOf(
            "private async void ReviewSheetRegistrationButton_OnClick",
            StringComparison.Ordinal);
        Assert.True(registerHandlerStart >= 0, "The dependent-sheet registration handler must exist.");
        Assert.True(
            confirmHandlerStart > registerHandlerStart,
            "The confirmation handler must follow the dependent-sheet registration handler.");

        var registerHandler = mainCodeBehind[registerHandlerStart..confirmHandlerStart];
        var electricalBranch = Regex.Match(
            registerHandler,
            @"if\s*\(\s*sheet\.SheetType\s*(?:==|is)\s*""ElectricalPlan""\s*\)",
            RegexOptions.CultureInvariant);
        Assert.True(
            electricalBranch.Success,
            "Electrical registration must have an explicit direct branch before the manual dialog.");

        var directRegistrationIndex = registerHandler.IndexOf(
            "await viewModel.RegisterDependentSheetAsync(",
            electricalBranch.Index,
            StringComparison.Ordinal);
        Assert.True(
            directRegistrationIndex > electricalBranch.Index,
            "The Electrical branch must call registration directly.");

        var directReturnIndex = registerHandler.IndexOf(
            "return;",
            directRegistrationIndex,
            StringComparison.Ordinal);
        Assert.True(
            directReturnIndex > directRegistrationIndex,
            "The Electrical branch must return before the manual dialog path.");

        var dialogIndex = registerHandler.IndexOf(
            "new RegistrationTransformDialog(sheet)",
            StringComparison.Ordinal);
        Assert.True(
            dialogIndex > directReturnIndex,
            "RegistrationTransformDialog must only be reached after Electrical registration returns.");

        var roofAndFacadeDialogPath = registerHandler[dialogIndex..];
        Assert.Contains("RegistrationTransformDialog", mainCodeBehind, StringComparison.Ordinal);
        Assert.Contains("ShowDialog<RegistrationTransformDialogResult?>", roofAndFacadeDialogPath, StringComparison.Ordinal);
        Assert.Contains("registrationResult.Transform", roofAndFacadeDialogPath, StringComparison.Ordinal);
        Assert.Contains("registrationResult.Confidence", roofAndFacadeDialogPath, StringComparison.Ordinal);
        Assert.Contains("registrationResult.OverhangInches", roofAndFacadeDialogPath, StringComparison.Ordinal);
        Assert.Contains("registrationResult.HorizontalReferenceName", roofAndFacadeDialogPath, StringComparison.Ordinal);

        Assert.Contains("x:Name=\"ScaleTextBox\"", dialogXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"RotationDegreesTextBox\"", dialogXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"TranslateXTextBox\"", dialogXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"TranslateYTextBox\"", dialogXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ConfidenceTextBox\"", dialogXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"OverhangInchesTextBox\"", dialogXaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"HorizontalReferenceTextBox\"", dialogXaml, StringComparison.Ordinal);
        Assert.Contains("RegistrationTransformDialogResult", dialogCodeBehind, StringComparison.Ordinal);
        Assert.Contains("TryParseDecimal", dialogCodeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Site_plan_adjustment_screen_exposes_manual_projection_reexport_action()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "SitePlanAdjustmentWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("Confirmar manuales + re-exportar", xaml, StringComparison.Ordinal);
        Assert.Contains("ConfirmManualPlanSetProjectionsAndReExportCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("CanConfirmManualPlanSetProjections", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Adjust_to_site_plan_setup_dialog_is_retired_from_the_desktop_source_tree()
    {
        var solutionRoot = FindSolutionRoot();
        var desktopRoot = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop");

        Assert.False(File.Exists(Path.Combine(desktopRoot, "AdjustSitePlanSetupDialog.axaml")));
        Assert.False(File.Exists(Path.Combine(desktopRoot, "AdjustSitePlanSetupDialog.axaml.cs")));
        Assert.Empty(EnumerateAuthoredDesktopSources(desktopRoot)
            .Where(path => File.ReadAllText(path).Contains("AdjustSitePlanSetup", StringComparison.Ordinal)));
    }

    private static IEnumerable<string> EnumerateAuthoredDesktopSources(string desktopRoot)
    {
        var binDirectory = Path.Combine(desktopRoot, "bin") + Path.DirectorySeparatorChar;
        var objDirectory = Path.Combine(desktopRoot, "obj") + Path.DirectorySeparatorChar;

        return Directory
            .EnumerateFiles(desktopRoot, "*.*", SearchOption.AllDirectories)
            .Where(path =>
                (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) &&
                !path.StartsWith(binDirectory, StringComparison.OrdinalIgnoreCase) &&
                !path.StartsWith(objDirectory, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Fit_tool_uses_a_canvas_first_icon_palette_instead_of_a_workbench_column()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);
        var codeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml.cs");
        var codeBehind = File.ReadAllText(codeBehindPath);
        var xamlDocument = XDocument.Parse(xaml);

        Assert.Contains("x:Name=\"FitToolPalette\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FitToolbarCommands\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanPublishCuration}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FitExistingPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsFitToolSelected}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Marcar pinch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Tag=\"Agregar pared\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Agregar pared\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"AddManualWallLineButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Crear franja\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"AddMeasurementCorridorButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Elegir nodo\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Quitar pinch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.tool-active=\"{Binding IsPinchPlacementArmed}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.tool-active=\"{Binding IsManualWallLinePlacementArmed}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.tool-active=\"{Binding IsMeasurementNodePlacementArmed}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedMeasurementCorridorId=\"{Binding SelectedMeasurementCorridorId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedMeasurementNodeId=\"{Binding SelectedMeasurementNodeId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedMeasurementStartNodeId=\"{Binding SelectedMeasurementStartNodeId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedMeasurementEndNodeId=\"{Binding SelectedMeasurementEndNodeId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ManualWallLineDraft=\"{Binding ManualWallLineDraft}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsManualWallLinePlacementArmed=\"{Binding IsManualWallLinePlacementArmed}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ManualWallLinePointClicked=\"PreviewControl_OnManualWallLinePointClicked\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ManualWallLinePreviewPointChanged=\"PreviewControl_OnManualWallLinePreviewPointChanged\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"in / ft-in\"", xaml, StringComparison.Ordinal);
        Assert.Contains("NewPinchMaxTrimInches", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding MaxTrimInches", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("StringFormat='Max: {0} in'", xaml, StringComparison.Ordinal);
        Assert.Contains("Watermark=\"6 1/2&quot;\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("NewPinchMaxTrimMm", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxTrimMm, StringFormat='Max: {0} mm'", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Herramientas Fit\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Existente\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Grupos de pinches\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding PinchGroups}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedPinchGroup}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"contracts:PinchGroupDto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Crear grupo de pinches\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"AddPinchGroupButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Renombrar grupo\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanRenameSelectedPinchGroup}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"RenamePinchGroupButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Eliminar grupo de pinches\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanRemoveSelectedPinchGroup}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"RemovePinchGroupButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Pinches de este grupo\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding SelectedPinchGroupMarkers}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedPinchMarker}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SelectedPinchMaxTrimDisplay", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Editar capacidad\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"EditPinchMaxTrimButton_OnClick\"", xaml, StringComparison.Ordinal);
        var editableCapacityTextBox = xamlDocument
            .Descendants()
            .Single(element =>
                element.Name.LocalName == "TextBox" &&
                element.Attribute("Text")?.Value == "{Binding EditableSelectedPinchMaxTrim}");
        Assert.Equal("Capacidad editable del pinch en pulgadas", editableCapacityTextBox.Attribute("AutomationProperties.Name")?.Value);

        var halfInchButtons = xamlDocument
            .Descendants()
            .Where(element =>
                element.Name.LocalName == "Button" &&
                element.Attribute("Content")?.Value is "- 1/2\"" or "+ 1/2\"")
            .ToArray();
        Assert.Equal(2, halfInchButtons.Length);
        var decreaseButton = halfInchButtons.Single(element => element.Attribute("Content")?.Value == "- 1/2\"");
        Assert.Equal("DecreasePinchMaxTrimButton_OnClick", decreaseButton.Attribute("Click")?.Value);
        Assert.Equal("Disminuir capacidad del pinch en media pulgada", decreaseButton.Attribute("AutomationProperties.Name")?.Value);
        var increaseButton = halfInchButtons.Single(element => element.Attribute("Content")?.Value == "+ 1/2\"");
        Assert.Equal("IncreasePinchMaxTrimButton_OnClick", increaseButton.Attribute("Click")?.Value);
        Assert.Equal("Aumentar capacidad del pinch en media pulgada", increaseButton.Attribute("AutomationProperties.Name")?.Value);
        Assert.Contains("Click=\"SavePinchMaxTrimButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"CancelPinchMaxTrimButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AdjustEditableSelectedPinchMaxTrim(-0.5m)", codeBehind, StringComparison.Ordinal);
        Assert.Contains("AdjustEditableSelectedPinchMaxTrim(0.5m)", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SelectPinchMarker(e.PinchMarkerId)", codeBehind, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanRemoveSelectedPinch}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Grupos de A y B\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding MeasurementNodeGroupOptions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedMeasurementNodeGroupOption}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"viewModels:MeasurementNodeGroupOptionViewModel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Name}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Details}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Eliminar franja\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanRemoveSelectedMeasurementCorridor}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"RemoveMeasurementCorridorButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Tipo de franja\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedMeasurementCorridorAxis}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Cambiar tipo\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanChangeSelectedMeasurementCorridorAxis}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"ChangeMeasurementCorridorAxisButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Nodos de esta franja\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ListBox ItemsSource=\"{Binding SelectedMeasurementGroupNodeOptions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding SelectedMeasurementGroupNodeOptions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedMeasurementGroupNodeOption}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Eliminar nodo\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanRemoveSelectedMeasurementNode}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"RemoveMeasurementNodeButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:DataType=\"viewModels:MeasurementNodeGroupNodeOptionViewModel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Elegí qué mide la cota\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"A\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedMeasurementStartNodeOption}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"B\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedMeasurementEndNodeOption}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Cota vinculada\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SelectedDimensionIntervalBindingSummary}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Guardar qué mide\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanSaveSelectedDimensionIntervalBinding}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"SaveDimensionIntervalBindingButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Volver a medida fija\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"RestoreDimensionIntervalBindingButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Las acciones principales viven arriba del plano: eleg?s contexto, toc?s ?cono, clic en canvas.\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ToolTip.Tip=\"Vincular cota\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Pinche existente\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ComboBox ItemsSource=\"{Binding PinchMarkers}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Grupos de ajuste\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Crear grupo\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Franjas existentes\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Nodos existentes\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Nodo existente\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Grupo A/B\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Nodo del grupo\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Nombre de franja\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("NewMeasurementCorridorName", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ItemsSource=\"{Binding SelectedMeasurementCorridorNodeOptions}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"FitWorkbench\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<StackPanel IsVisible=\"{Binding IsFitToolSelected}\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Pinch_group_actions_open_a_naming_dialog_before_mutating_groups()
    {
        var solutionRoot = FindSolutionRoot();
        var codeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml.cs");
        var codeBehind = File.ReadAllText(codeBehindPath);

        Assert.Contains("PinchGroupNameDialog", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ShowDialog<string?>(owner)", codeBehind, StringComparison.Ordinal);
        Assert.Contains("AddPinchGroupAsync(groupName", codeBehind, StringComparison.Ordinal);
        Assert.Contains("RenamePinchGroupButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.Contains("RenameSelectedPinchGroupAsync(groupName", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Review_screen_wires_a_global_delete_selected_action()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var codeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml.cs");
        var viewModelPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ViewModels", "FloorPlanReviewViewModel.cs");
        var xaml = File.ReadAllText(xamlPath);
        var codeBehind = File.ReadAllText(codeBehindPath);
        var viewModel = File.ReadAllText(viewModelPath);

        Assert.DoesNotContain("Content=\"Eliminar seleccionado\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes=\"tool danger\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Eliminar seleccionado (Supr)\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanDeleteSelectedItem}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"DeleteSelectedItemButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("KeyDown=\"ReviewFloorPlanWindow_OnKeyDown\"", xaml, StringComparison.Ordinal);

        Assert.Contains("ReviewFloorPlanWindow_OnKeyDown", codeBehind, StringComparison.Ordinal);
        Assert.Contains("Key.Delete", codeBehind, StringComparison.Ordinal);
        Assert.Contains("DeleteSelectedItemButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.Contains("await viewModel.DeleteSelectedItemAsync(CancellationToken.None);", codeBehind, StringComparison.Ordinal);

        Assert.Contains("public bool CanDeleteSelectedItem =>", viewModel, StringComparison.Ordinal);
        Assert.Contains("public async Task DeleteSelectedItemAsync(CancellationToken cancellationToken)", viewModel, StringComparison.Ordinal);
        Assert.Contains("await RemoveSelectedPinchAsync(cancellationToken);", viewModel, StringComparison.Ordinal);
        Assert.Contains("await RemoveSelectedPinchGroupAsync(cancellationToken);", viewModel, StringComparison.Ordinal);
        Assert.Contains("await RemoveSelectedMeasurementNodeAsync(cancellationToken);", viewModel, StringComparison.Ordinal);
        Assert.Contains("await RemoveSelectedMeasurementCorridorAsync(cancellationToken);", viewModel, StringComparison.Ordinal);
        Assert.Contains("await ExcludeSelectedArtifactAsync(cancellationToken);", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Manual_wall_line_tool_wires_preview_events_to_review_view_model()
    {
        var solutionRoot = FindSolutionRoot();
        var codeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml.cs");
        var codeBehind = File.ReadAllText(codeBehindPath);

        Assert.Contains("AddManualWallLineButton_OnClick", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ToggleManualWallLinePlacement", codeBehind, StringComparison.Ordinal);
        Assert.Contains("PreviewControl_OnManualWallLinePointClicked", codeBehind, StringComparison.Ordinal);
        Assert.Contains("HandleManualWallLinePointAsync", codeBehind, StringComparison.Ordinal);
        Assert.Contains("PreviewControl_OnManualWallLinePreviewPointChanged", codeBehind, StringComparison.Ordinal);
        Assert.Contains("UpdateManualWallLinePreviewPoint", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Pinch_group_name_dialog_contains_name_input_and_save_cancel_actions()
    {
        var solutionRoot = FindSolutionRoot();
        var dialogPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "PinchGroupNameDialog.axaml");
        var dialog = File.ReadAllText(dialogPath);

        Assert.Contains("x:Name=\"NameTextBox\"", dialog, StringComparison.Ordinal);
        Assert.Contains("Watermark=\"Nombre del grupo\"", dialog, StringComparison.Ordinal);
        Assert.Contains("Content=\"Cancelar\"", dialog, StringComparison.Ordinal);
        Assert.Contains("Content=\"Guardar\"", dialog, StringComparison.Ordinal);
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
