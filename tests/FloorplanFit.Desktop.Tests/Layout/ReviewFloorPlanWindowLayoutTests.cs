using Xunit;

namespace FloorplanFit.Desktop.Tests.Layout;

public sealed class ReviewFloorPlanWindowLayoutTests
{
    [Fact]
    public void Review_xaml_uses_minimal_review_queue_and_contextual_inspector_layout()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("RequestedThemeVariant=\"Dark\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TransparencyLevelHint=\"AcrylicBlur, Mica, Blur\"", xaml, StringComparison.Ordinal);
        Assert.Contains("WindowState=\"Maximized\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Width=\"1450\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Height=\"920\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("FloorPlanPreviewControl Height=\"700\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ListBox Height=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<controls:FloorPlanPreviewControl", xaml, StringComparison.Ordinal);
        Assert.Contains("Review Queue", xaml, StringComparison.Ordinal);
        Assert.Contains("ReviewQueueSearchText", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedReviewQueueFilter", xaml, StringComparison.Ordinal);
        Assert.Contains("VisibleWallCandidates", xaml, StringComparison.Ordinal);
        Assert.Contains("VisibleRoomLabels", xaml, StringComparison.Ordinal);
        Assert.Contains("VisibleOpeningLabels", xaml, StringComparison.Ordinal);
        Assert.Contains("VisibleDimensions", xaml, StringComparison.Ordinal);
        Assert.Contains("Preview", xaml, StringComparison.Ordinal);
        Assert.Contains("Inspector", xaml, StringComparison.Ordinal);
        Assert.Contains("InspectorToolBar", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.tool-active", xaml, StringComparison.Ordinal);
        Assert.Contains("Eje", xaml, StringComparison.Ordinal);
        Assert.Contains("Agregar ajuste", xaml, StringComparison.Ordinal);
        Assert.Contains("Quitar ajuste", xaml, StringComparison.Ordinal);
        Assert.Contains("Excluir del curado", xaml, StringComparison.Ordinal);
        Assert.Contains("CuratedObjectsSectionTitle", xaml, StringComparison.Ordinal);
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
        Assert.Contains("RoomLabels=\"{Binding RoomLabels}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OpeningCandidates=\"{Binding OpeningCandidates}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OpeningLabels=\"{Binding OpeningLabels}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Dimensions=\"{Binding Dimensions}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FixedPlanComponents=\"{Binding FixedPlanComponents}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedRoomLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedOpeningLabel", xaml, StringComparison.Ordinal);
        Assert.Contains("Quick Filters", xaml, StringComparison.Ordinal);
        Assert.Contains("QueueSummary", xaml, StringComparison.Ordinal);
        Assert.Contains("Acciones de selección", xaml, StringComparison.Ordinal);
        Assert.Contains("IsStructureQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.Contains("IsRoomNamesQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.Contains("IsOpeningCodesQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.Contains("IsDimensionsQueueExpanded", xaml, StringComparison.Ordinal);
        Assert.Contains("IsCuratedObjectsQueueExpanded", xaml, StringComparison.Ordinal);
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
    public void Review_queue_uses_custom_folder_buttons_and_a_bounded_shared_content_region()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("ReviewQueueContentHost", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes=\"folder-toggle\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Row=\"8\"", xaml, StringComparison.Ordinal);
        Assert.Contains("RowDefinitions=\"Auto,Auto,Auto,Auto,Auto,Auto,Auto,Auto,*\"", xaml, StringComparison.Ordinal);
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
    public void Published_review_header_exposes_explicit_edit_action_before_publish_action()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var codeBehindPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml.cs");
        var xaml = File.ReadAllText(xamlPath);
        var codeBehind = File.ReadAllText(codeBehindPath);

        Assert.Contains("Content=\"Editar\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanEditPublishedCuration}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"EditPublishedButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanPublishCuration}\"", xaml, StringComparison.Ordinal);
        Assert.True(
            xaml.IndexOf("Content=\"Editar\"", StringComparison.Ordinal) <
            xaml.IndexOf("Content=\"Publish Curation\"", StringComparison.Ordinal));
        Assert.Contains("StartEditingPublishedCurationAsync", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Fit_tool_uses_a_canvas_first_icon_palette_instead_of_a_workbench_column()
    {
        var solutionRoot = FindSolutionRoot();
        var xamlPath = Path.Combine(solutionRoot, "src", "FloorplanFit.Desktop", "ReviewFloorPlanWindow.axaml");
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("x:Name=\"FitToolPalette\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FitToolbarCommands\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FitExistingPanel\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding IsFitToolSelected}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Marcar pinch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Crear franja\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"AddMeasurementCorridorButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Elegir nodo\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ToolTip.Tip=\"Quitar pinch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.tool-active=\"{Binding IsPinchPlacementArmed}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Classes.tool-active=\"{Binding IsMeasurementNodePlacementArmed}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedMeasurementCorridorId=\"{Binding SelectedMeasurementCorridorId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedMeasurementNodeId=\"{Binding SelectedMeasurementNodeId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedMeasurementStartNodeId=\"{Binding SelectedMeasurementStartNodeId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedMeasurementEndNodeId=\"{Binding SelectedMeasurementEndNodeId}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"in\"", xaml, StringComparison.Ordinal);
        Assert.Contains("NewPinchMaxTrimInches", xaml, StringComparison.Ordinal);
        Assert.Contains("MaxTrimInches", xaml, StringComparison.Ordinal);
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
        Assert.Contains("Content=\"Eliminar grupo de pinches\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanRemoveSelectedPinchGroup}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"RemovePinchGroupButton_OnClick\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Pinches de este grupo\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding SelectedPinchGroupMarkers}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectedItem=\"{Binding SelectedPinchMarker}\"", xaml, StringComparison.Ordinal);
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
