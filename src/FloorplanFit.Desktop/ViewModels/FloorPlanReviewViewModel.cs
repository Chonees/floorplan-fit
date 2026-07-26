using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Desktop.Presentation;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.ViewModels;

public sealed partial class FloorPlanReviewViewModel : ObservableObject
{
    private const string InspectorToolOverview = "Overview";
    private const string InspectorToolPosition = "Position";
    private const string InspectorToolText = "Text";
    private const string InspectorToolClassification = "Classification";
    private const string InspectorToolActions = "Actions";
    private const string InspectorToolFit = "Fit";
    private const string PublishRequiresPinchMarkerMessage = "Agregá al menos un pinche antes de publicar.";
    private const string ExportRequiresDirtyNativeDimensionsMessage = "No hay cotas modificadas para exportar.";
    private const decimal MillimetersPerInch = 25.4m;

    private readonly FloorPlanReviewApplyCoordinator applyCoordinator;
    private readonly FloorPlanReviewMutationCoordinator mutationCoordinator;
    private readonly FloorPlanReviewSelectionCoordinator selectionCoordinator;
    private readonly FloorPlanReviewQueueCoordinator queueCoordinator;
    private readonly FloorPlanReviewInspectorCoordinator inspectorCoordinator;
    private readonly FloorPlanReviewNotificationCoordinator notificationCoordinator;
    private readonly FloorPlanReviewSessionCoordinator sessionCoordinator;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly Guid templateId;
    private readonly Guid? floorPlanVersionId;
    private IReadOnlyDictionary<Guid, DimensionAssociationDto> dimensionAssociationsById = new Dictionary<Guid, DimensionAssociationDto>();
    private IReadOnlyList<MeasurementNodeGroupOptionViewModel>? measurementNodeGroupOptionsCache;
    private IReadOnlyList<MeasurementNodeGroupNodeOptionViewModel>? selectedMeasurementGroupNodeOptionsCache;
    private Guid? selectedMeasurementGroupNodeOptionsCacheCorridorId;
    private bool isUpdatingCuratedArtifactEditors;
    private Guid? editingPinchMarkerId;
    private string pinchMaxTrimPrefill = string.Empty;

    public FloorPlanReviewViewModel(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory, Guid templateId)
        : this(scopeFactory, templateId, floorPlanVersionId: null)
    {
    }

    public FloorPlanReviewViewModel(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory, Guid templateId, Guid? floorPlanVersionId)
    {
        applyCoordinator = new FloorPlanReviewApplyCoordinator();
        mutationCoordinator = new FloorPlanReviewMutationCoordinator(scopeFactory);
        selectionCoordinator = new FloorPlanReviewSelectionCoordinator();
        queueCoordinator = new FloorPlanReviewQueueCoordinator();
        inspectorCoordinator = new FloorPlanReviewInspectorCoordinator();
        notificationCoordinator = new FloorPlanReviewNotificationCoordinator();
        sessionCoordinator = new FloorPlanReviewSessionCoordinator(scopeFactory);
        this.scopeFactory = scopeFactory;
        this.templateId = templateId;
        this.floorPlanVersionId = floorPlanVersionId;
    }

    public ObservableCollection<WallCandidateDto> WallCandidates { get; } = [];

    public ObservableCollection<GeometryPathDto> GeometryPaths { get; } = [];

    public ObservableCollection<RoomLabelDto> RoomLabels { get; } = [];

    public ObservableCollection<RoomLabelDto> VisibleRoomLabels { get; } = [];

    public ObservableCollection<OpeningCandidateDto> OpeningCandidates { get; } = [];

    public ObservableCollection<OpeningLabelDto> OpeningLabels { get; } = [];

    public ObservableCollection<OpeningLabelDto> VisibleOpeningLabels { get; } = [];

    public ObservableCollection<DimensionDto> Dimensions { get; } = [];

    public ObservableCollection<DimensionDto> VisibleDimensions { get; } = [];

    public ObservableCollection<DimensionBindingDto> DimensionBindings { get; } = [];

    public ObservableCollection<DimensionAssociationDto> DimensionAssociations { get; } = [];

    [ObservableProperty]
    private MeasurementContextDto? measurementContext;

    public ObservableCollection<FixedPlanComponentDto> FixedPlanComponents { get; } = [];

    public ObservableCollection<ProtectedDetailAssemblyDto> ProtectedDetailAssemblies { get; } = [];

    public ObservableCollection<WallCandidateDto> VisibleWallCandidates { get; } = [];

    public ObservableCollection<CuratedPlanArtifactDto> CuratedPlanArtifacts { get; } = [];

    public ObservableCollection<CuratedPlanArtifactDto> VisibleCuratedPlanArtifacts { get; } = [];

    public ObservableCollection<CuratedArtifactGroupViewModel> CuratedArtifactGroups { get; } = [];

    public ObservableCollection<string> EditableCuratedArtifactCategoryOptions { get; } = [];

    public ObservableCollection<string> EditableCuratedArtifactTypeOptions { get; } = [];

    public ObservableCollection<PinchGroupDto> PinchGroups { get; } = [];

    public ObservableCollection<PinchMarkerDto> PinchMarkers { get; } = [];

    public ObservableCollection<MeasurementCorridorDto> MeasurementCorridors { get; } = [];

    public ObservableCollection<MeasurementNodeDto> MeasurementNodes { get; } = [];

    public ObservableCollection<DimensionIntervalBindingDto> DimensionIntervalBindings { get; } = [];

    public ObservableCollection<ArticulationBandDto> ArticulationBands { get; } = [];

    public IReadOnlyList<string> PinchAxisOptions { get; } = Enum.GetNames<PinchAxisTag>();

    public IReadOnlyList<string> CuratedArtifactFamilyOptions { get; } = FloorPlanArtifactTaxonomy.Families;

    public IReadOnlyList<string> ReviewQueueFilterOptions { get; } =
    [
        "Everything",
        "Changed only",
        "Text & Notes",
        "Dimensions",
        "Curated Objects",
        "Structure"
    ];

    [ObservableProperty]
    private string code = string.Empty;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    [ObservableProperty]
    private int activeVersionNumber;

    [ObservableProperty]
    private Guid? activePublishedCurationId;

    [ObservableProperty]
    private Guid draftCurationId;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private WallCandidateDto? selectedCandidate;

    [ObservableProperty]
    private RoomLabelDto? selectedRoomLabel;

    [ObservableProperty]
    private PinchMarkerDto? selectedPinchMarker;

    [ObservableProperty]
    private OpeningCandidateDto? selectedOpeningCandidate;

    [ObservableProperty]
    private OpeningLabelDto? selectedOpeningLabel;

    [ObservableProperty]
    private DimensionDto? selectedDimension;

    [ObservableProperty]
    private FixedPlanComponentDto? selectedFixedPlanComponent;

    [ObservableProperty]
    private ProtectedDetailAssemblyDto? selectedProtectedDetailAssembly;

    [ObservableProperty]
    private CuratedPlanArtifactDto? selectedCuratedArtifact;

    [ObservableProperty]
    private string editableCuratedArtifactFamily = string.Empty;

    [ObservableProperty]
    private string editableCuratedArtifactCategory = string.Empty;

    [ObservableProperty]
    private string editableCuratedArtifactType = string.Empty;

    [ObservableProperty]
    private string editableSelectedLabelTextHeight = string.Empty;

    [ObservableProperty]
    private string reviewQueueSearchText = string.Empty;

    [ObservableProperty]
    private string selectedReviewQueueFilter = "Everything";

    [ObservableProperty]
    private PinchGroupDto? selectedPinchGroup;

    [ObservableProperty]
    private Guid? highlightGeometryPathId;

    [ObservableProperty]
    private string previewSelectionLabel = "Previewing floor plan";

    [ObservableProperty]
    private string selectedPinchAxis = nameof(PinchAxisTag.Width);

    [ObservableProperty]
    private string newPinchGroupName = string.Empty;

    [ObservableProperty]
    private MeasurementCorridorDto? selectedMeasurementCorridor;

    [ObservableProperty]
    private string selectedMeasurementCorridorAxis = nameof(PinchAxisTag.Width);

    [ObservableProperty]
    private MeasurementNodeDto? selectedMeasurementNode;

    [ObservableProperty]
    private MeasurementNodeDto? selectedMeasurementStartNode;

    [ObservableProperty]
    private MeasurementNodeDto? selectedMeasurementEndNode;

    [ObservableProperty]
    private string newPinchMaxTrimInches = "1";

    [ObservableProperty]
    private string editableSelectedPinchMaxTrim = string.Empty;

    [ObservableProperty]
    private bool isEditingSelectedPinchMaxTrim;

    [ObservableProperty]
    private bool isPinchPlacementArmed;

    [ObservableProperty]
    private bool isManualWallLinePlacementArmed;

    [ObservableProperty]
    private ManualWallLineDraft? manualWallLineDraft;

    [ObservableProperty]
    private bool isMeasurementNodePlacementArmed;

    [ObservableProperty]
    private bool arePreviewDimensionsVisible = true;

    [ObservableProperty]
    private bool isStructureQueueExpanded;

    [ObservableProperty]
    private bool isRoomNamesQueueExpanded;

    [ObservableProperty]
    private bool isOpeningCodesQueueExpanded;

    [ObservableProperty]
    private bool isDimensionsQueueExpanded;

    [ObservableProperty]
    private bool isCuratedObjectsQueueExpanded = true;

    [ObservableProperty]
    private string selectedInspectorTool = InspectorToolOverview;

    public string AddPinchButtonLabel => IsPinchPlacementArmed
        ? "Cancelar ajuste"
        : "Agregar ajuste";

    public string AddManualWallLineButtonLabel => IsManualWallLinePlacementArmed
        ? "Cancelar pared"
        : "Agregar pared";

    public string AddMeasurementNodeButtonLabel => IsMeasurementNodePlacementArmed
        ? "Cancelar punto"
        : "Elegir punto";

    public Guid? SelectedPinchGroupId => SelectedPinchGroup?.PinchGroupId;

    public Guid? SelectedPinchMarkerId => SelectedPinchMarker?.PinchMarkerId;

    public IReadOnlyList<PinchMarkerDto> SelectedPinchGroupMarkers =>
        SelectedPinchGroup is null
            ? []
            : PinchMarkers
                .Where(item => item.PinchGroupId == SelectedPinchGroup.PinchGroupId)
                .OrderBy(item => item.SortOrder)
                .ToArray();

    public bool CanRemoveSelectedPinchGroup =>
        DraftCurationId != Guid.Empty &&
        SelectedPinchGroup is not null;

    public bool CanRenameSelectedPinchGroup => CanRemoveSelectedPinchGroup;

    public string SuggestedPinchGroupName => CreateNextPinchGroupName();

    public bool CanRemoveSelectedPinch => SelectedPinchMarker is not null;

    public bool CanEditSelectedPinchMaxTrim =>
        DraftCurationId != Guid.Empty &&
        SelectedPinchMarker is not null;

    public string SelectedPinchMaxTrimDisplay => SelectedPinchMarker is null
        ? string.Empty
        : ArchitecturalLengthText.FormatInches(SelectedPinchMarker.MaxTrimMm / MillimetersPerInch);

    public bool CanDeleteSelectedItem =>
        CanRemoveSelectedPinch ||
        CanRemoveSelectedPinchGroup ||
        CanRemoveSelectedMeasurementNode ||
        CanRemoveSelectedMeasurementCorridor ||
        SelectedCuratedArtifact is not null ||
        SelectedCandidate is not null ||
        SelectedRoomLabel is not null ||
        SelectedOpeningCandidate is not null ||
        SelectedOpeningLabel is not null ||
        SelectedFixedPlanComponent is not null ||
        SelectedProtectedDetailAssembly is not null;

    public Guid? SelectedRoomLabelId => SelectedRoomLabel?.RoomLabelId;

    public Guid? SelectedOpeningLabelId => SelectedOpeningLabel?.OpeningLabelId;

    public Guid? SelectedDimensionId => SelectedDimension?.DimensionId;

    public Guid? SelectedMeasurementCorridorId => SelectedMeasurementCorridor?.CorridorId;

    public Guid? SelectedMeasurementNodeId => SelectedMeasurementNode?.NodeId;

    public Guid? SelectedMeasurementStartNodeId => SelectedMeasurementStartNode?.NodeId;

    public Guid? SelectedMeasurementEndNodeId => SelectedMeasurementEndNode?.NodeId;

    public bool CanEditPublishedCuration =>
        DraftCurationId == Guid.Empty &&
        ActivePublishedCurationId is not null;

    public bool CanPublishCuration =>
        DraftCurationId != Guid.Empty;

    public bool HasSelectedMeasurementCorridor => SelectedMeasurementCorridor is not null;

    public bool CanRemoveSelectedMeasurementCorridor =>
        DraftCurationId != Guid.Empty &&
        SelectedMeasurementCorridor is not null;

    public bool CanRemoveSelectedMeasurementNode =>
        DraftCurationId != Guid.Empty &&
        SelectedMeasurementNode is not null;

    public bool CanChangeSelectedMeasurementCorridorAxis =>
        DraftCurationId != Guid.Empty &&
        SelectedMeasurementCorridor is not null &&
        Enum.TryParse<PinchAxisTag>(SelectedMeasurementCorridorAxis, out var axisTag) &&
        !string.Equals(SelectedMeasurementCorridor.AxisTag, axisTag.ToString(), StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<MeasurementNodeDto> SelectedMeasurementCorridorNodes =>
        SelectedMeasurementCorridor is null
            ? []
            : MeasurementNodes
                .Where(item => item.CorridorId == SelectedMeasurementCorridor.CorridorId)
                .OrderBy(item => item.SortOrder)
                .ToArray();

    public IReadOnlyList<MeasurementNodeGroupOptionViewModel> MeasurementNodeGroupOptions =>
        measurementNodeGroupOptionsCache ??= MeasurementCorridors
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(CreateMeasurementNodeGroupOption)
            .ToArray();

    public MeasurementNodeGroupOptionViewModel? SelectedMeasurementNodeGroupOption
    {
        get => FindMeasurementNodeGroupOption();
        set
        {
            if (value is null)
            {
                RaiseMeasurementNodeGroupOptionNotifications();
                return;
            }

            SelectedMeasurementCorridor = value.Corridor;
            if (SelectedMeasurementNode?.CorridorId != value.CorridorId)
            {
                SelectedMeasurementNode = SelectedMeasurementCorridorNodes.FirstOrDefault();
            }

            RaiseMeasurementNodeGroupOptionNotifications();
        }
    }

    public IReadOnlyList<MeasurementNodeGroupNodeOptionViewModel> SelectedMeasurementGroupNodeOptions
    {
        get
        {
            if (SelectedMeasurementCorridor is null)
            {
                return [];
            }

            if (selectedMeasurementGroupNodeOptionsCache is not null &&
                selectedMeasurementGroupNodeOptionsCacheCorridorId == SelectedMeasurementCorridor.CorridorId)
            {
                return selectedMeasurementGroupNodeOptionsCache;
            }

            selectedMeasurementGroupNodeOptionsCacheCorridorId = SelectedMeasurementCorridor.CorridorId;
            selectedMeasurementGroupNodeOptionsCache = SelectedMeasurementCorridorNodes
                .Select(CreateMeasurementNodeGroupNodeOption)
                .ToArray();
            return selectedMeasurementGroupNodeOptionsCache;
        }
    }

    public MeasurementNodeGroupNodeOptionViewModel? SelectedMeasurementGroupNodeOption
    {
        get => FindMeasurementNodeGroupNodeOption(SelectedMeasurementNode);
        set
        {
            if (value is null)
            {
                OnPropertyChanged(nameof(SelectedMeasurementGroupNodeOption));
                return;
            }

            SelectedMeasurementNode = value.Node;
        }
    }

    public MeasurementNodeGroupNodeOptionViewModel? SelectedMeasurementStartNodeOption
    {
        get => FindMeasurementNodeGroupNodeOption(SelectedMeasurementStartNode);
        set
        {
            if (value is null)
            {
                OnPropertyChanged(nameof(SelectedMeasurementStartNodeOption));
                return;
            }

            SelectedMeasurementStartNode = value.Node;
        }
    }

    public MeasurementNodeGroupNodeOptionViewModel? SelectedMeasurementEndNodeOption
    {
        get => FindMeasurementNodeGroupNodeOption(SelectedMeasurementEndNode);
        set
        {
            if (value is null)
            {
                OnPropertyChanged(nameof(SelectedMeasurementEndNodeOption));
                return;
            }

            SelectedMeasurementEndNode = value.Node;
        }
    }

    public bool CanSaveSelectedDimensionIntervalBinding =>
        DraftCurationId != Guid.Empty &&
        SelectedDimension is not null &&
        SelectedMeasurementCorridor is not null &&
        SelectedMeasurementStartNode is not null &&
        SelectedMeasurementEndNode is not null &&
        SelectedMeasurementStartNode.NodeId != SelectedMeasurementEndNode.NodeId &&
        SelectedMeasurementStartNode.CorridorId == SelectedMeasurementCorridor.CorridorId &&
        SelectedMeasurementEndNode.CorridorId == SelectedMeasurementCorridor.CorridorId;

    public bool CanRestoreSelectedDimensionIntervalBinding =>
        DraftCurationId != Guid.Empty &&
        SelectedDimension is not null &&
        DimensionIntervalBindings.Any(item => item.DimensionId == SelectedDimension.DimensionId);

    public string SelectedDimensionIntervalBindingSummary
    {
        get
        {
            if (SelectedDimension is null)
            {
                return "Seleccion\u00E1 una cota para definir qu\u00E9 mide.";
            }

            var binding = DimensionIntervalBindings.FirstOrDefault(item => item.DimensionId == SelectedDimension.DimensionId);
            if (binding is null)
            {
                if (SelectedMeasurementCorridor is null)
                {
                    return "Estado: medida fija. Falta elegir una franja de medida.";
                }

                if (SelectedMeasurementStartNode is null || SelectedMeasurementEndNode is null)
                {
                    return "Estado: medida fija. Falta definir punto inicial y punto final.";
                }

                if (SelectedMeasurementStartNode.NodeId == SelectedMeasurementEndNode.NodeId)
                {
                    return "Estado: medida fija. El punto inicial y final no pueden ser el mismo.";
                }

                return "Estado: medida fija (lista para guardar relaci\u00F3n manual)";
            }

            var corridor = MeasurementCorridors.FirstOrDefault(item => item.CorridorId == binding.CorridorId);
            var corridorName = corridor?.Name ?? "franja desconocida";
            return $"Estado: relaci\u00F3n guardada en {corridorName} ({binding.IntervalStartCoordinate:0.###} a {binding.IntervalEndCoordinate:0.###})";
        }
    }

    public string SelectedPinchGroupImpactSummary
    {
        get
        {
            if (SelectedPinchGroup is null)
            {
                return "Seleccion\u00E1 un grupo de ajuste para ver qu\u00E9 franjas de medida toca.";
            }

            var band = ArticulationBands.FirstOrDefault(item => item.PinchGroupId == SelectedPinchGroup.PinchGroupId);
            if (band is null)
            {
                return "Franjas sugeridas: ninguna (el grupo elegido no tiene una banda reducible proyectada).";
            }

            var impactedCorridors = MeasurementCorridors
                .Where(item =>
                    string.Equals(item.AxisTag, band.AxisTag, StringComparison.OrdinalIgnoreCase) &&
                    IntervalsOverlap(item.BandMinCoordinate, item.BandMaxCoordinate, band.BandStartCoordinate, band.BandEndCoordinate))
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.Name)
                .ToArray();

            return impactedCorridors.Length == 0
                ? $"Franjas sugeridas para {band.PinchGroupName}: ninguna."
                : $"Franjas sugeridas para {band.PinchGroupName}: {string.Join(", ", impactedCorridors)}";
        }
    }

    public string SelectedDimensionImpactSummary
    {
        get
        {
            if (SelectedDimension is null)
            {
                return "Seleccion\u00E1 una cota para ver qu\u00E9 grupos de ajuste podr\u00EDan afectarla.";
            }

            var binding = DimensionIntervalBindings.FirstOrDefault(item => item.DimensionId == SelectedDimension.DimensionId);
            if (binding is null)
            {
                return "Grupos que la afectan: ninguno (la cota sigue fija)";
            }

            var corridor = MeasurementCorridors.FirstOrDefault(item => item.CorridorId == binding.CorridorId);
            if (corridor is null)
            {
                return "Grupos que la afectan: franja desconocida";
            }

            var impactedPinchGroups = ArticulationBands
                .Where(item =>
                    string.Equals(item.AxisTag, corridor.AxisTag, StringComparison.OrdinalIgnoreCase) &&
                    IntervalsOverlap(binding.IntervalStartCoordinate, binding.IntervalEndCoordinate, item.BandStartCoordinate, item.BandEndCoordinate))
                .OrderBy(item => item.PinchGroupName, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.PinchGroupName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return impactedPinchGroups.Length == 0
                ? "Grupos que la afectan: ninguno"
                : $"Grupos que la afectan: {string.Join(", ", impactedPinchGroups)}";
        }
    }

    public int DoorOpeningCount => OpeningCandidates.Count(item => string.Equals(item.Kind, "Door", StringComparison.OrdinalIgnoreCase));

    public int WindowOpeningCount => OpeningCandidates.Count(item => string.Equals(item.Kind, "Window", StringComparison.OrdinalIgnoreCase));

    public int FixedPlanComponentCount => FixedPlanComponents.Count;

    public int ProtectedDetailAssemblyCount => ProtectedDetailAssemblies.Count;

    public int RoomLabelCount => RoomLabels.Count;

    public int OpeningLabelCount => OpeningLabels.Count;

    public int DimensionCount => Dimensions.Count;

    public int CuratedObjectCount => CuratedArtifactGroups.Sum(item => item.Count);

    public int VisibleQueueItemCount =>
        VisibleWallCandidates.Count +
        VisibleRoomLabels.Count +
        VisibleOpeningLabels.Count +
        VisibleDimensions.Count +
        CuratedObjectCount;

    public int TotalQueueItemCount =>
        WallCandidates.Count +
        RoomLabels.Count +
        OpeningLabels.Count +
        Dimensions.Count +
        VisibleCuratedPlanArtifacts.Count;

    public int AdjustedQueueItemCount =>
        RoomLabels.Count(item => item.HasManualPosition || item.HasManualTextHeight) +
        OpeningLabels.Count(item => item.HasManualPosition || item.HasManualTextHeight) +
        VisibleCuratedPlanArtifacts.Count(item =>
            item.HasManualPosition ||
            !string.Equals(item.DecisionState, FloorPlanArtifactDecisionState.DetectedDefault.ToString(), StringComparison.Ordinal));

    public string QueueSummary => $"Showing {VisibleQueueItemCount} of {TotalQueueItemCount} review items";

    public string QueueInsightsSummary => $"{AdjustedQueueItemCount} adjusted • {PinchMarkers.Count} pinch markers";

    public string StructureSectionTitle => $"Structure ({VisibleWallCandidates.Count})";

    public string RoomNamesSectionTitle => $"Room Names ({VisibleRoomLabels.Count})";

    public string CuratedObjectsSectionTitle => $"Curated Objects ({CuratedObjectCount})";

    public string OpeningCodesSectionTitle => $"Door / Window Codes ({VisibleOpeningLabels.Count})";

    public string DimensionsSectionTitle => $"Dimensions ({VisibleDimensions.Count})";

    public string ExcludeSelectedArtifactLabel => "Exclude from Curation";

    public bool IsOverviewToolSelected => string.Equals(SelectedInspectorTool, InspectorToolOverview, StringComparison.Ordinal);

    public bool IsPositionToolSelected => string.Equals(SelectedInspectorTool, InspectorToolPosition, StringComparison.Ordinal);

    public bool IsTextToolSelected => string.Equals(SelectedInspectorTool, InspectorToolText, StringComparison.Ordinal);

    public bool IsClassificationToolSelected => string.Equals(SelectedInspectorTool, InspectorToolClassification, StringComparison.Ordinal);

    public bool IsActionsToolSelected => string.Equals(SelectedInspectorTool, InspectorToolActions, StringComparison.Ordinal);

    public bool IsFitToolSelected => string.Equals(SelectedInspectorTool, InspectorToolFit, StringComparison.Ordinal);

    public bool HasVisibleWallCandidates => VisibleWallCandidates.Count > 0;

    public bool HasVisibleRoomLabels => VisibleRoomLabels.Count > 0;

    public bool HasVisibleOpeningLabels => VisibleOpeningLabels.Count > 0;

    public bool HasVisibleDimensions => VisibleDimensions.Count > 0;

    public bool HasVisibleCuratedArtifactGroups => CuratedArtifactGroups.Count > 0;

    public bool CanUsePositionTool => HasSelectedArtifact;

    public bool CanUseTextTool => HasSelectedResizableLabel;

    public bool CanUseClassificationTool => HasSelectedCuratedArtifact;

    public bool CanUseActionsTool => HasSelectedArtifact;

    public bool HasSelectedCuratedArtifact => SelectedCuratedArtifact is not null;

    public bool CanSaveSelectedCuratedArtifactClassification =>
        SelectedCuratedArtifact is not null &&
        DraftCurationId != Guid.Empty &&
        !string.IsNullOrWhiteSpace(EditableCuratedArtifactFamily) &&
        !string.IsNullOrWhiteSpace(EditableCuratedArtifactCategory) &&
        !string.IsNullOrWhiteSpace(EditableCuratedArtifactType) &&
        FloorPlanArtifactTaxonomy.IsValidClassification(
            EditableCuratedArtifactFamily,
            EditableCuratedArtifactCategory,
            EditableCuratedArtifactType);

    public bool CanRestoreSelectedCuratedArtifactClassification =>
        SelectedCuratedArtifact is not null &&
        !string.Equals(
            SelectedCuratedArtifact.DecisionState,
            FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
            StringComparison.Ordinal);

    public bool CanRestoreSelectedArtifactPosition =>
        SelectedRoomLabel?.HasManualPosition == true ||
        SelectedOpeningLabel?.HasManualPosition == true ||
        SelectedCuratedArtifact?.HasManualPosition == true ||
        SelectedDimension?.IsEdited == true;

    public bool HasSelectedResizableLabel =>
        SelectedRoomLabel is not null || SelectedOpeningLabel is not null;

    public bool CanSaveSelectedLabelTextHeight =>
        DraftCurationId != Guid.Empty &&
        HasSelectedResizableLabel &&
        TryParseEditableSelectedLabelTextHeight(out _);

    public bool CanRestoreSelectedLabelTextHeight =>
        SelectedRoomLabel?.HasManualTextHeight == true ||
        SelectedOpeningLabel?.HasManualTextHeight == true;

    public bool HasSelectedArtifact =>
        SelectedCuratedArtifact is not null ||
        SelectedCandidate is not null ||
        SelectedRoomLabel is not null ||
        SelectedOpeningLabel is not null ||
        SelectedDimension is not null;

    public string SelectedArtifactTypeLabel => GetInspectorPresentation().ArtifactTypeLabel;

    public string SelectedArtifactTitle => GetInspectorPresentation().ArtifactTitle;

    public string SelectedArtifactSubtitle => GetInspectorPresentation().ArtifactSubtitle;

    public string SelectedArtifactDetails => GetInspectorPresentation().ArtifactDetails;

    public string SelectedCuratedArtifactDetectedSummary => GetInspectorPresentation().CuratedArtifactDetectedSummary;

    public string SelectedCuratedArtifactResolvedSummary => GetInspectorPresentation().CuratedArtifactResolvedSummary;

    public string SelectedCuratedArtifactDecisionSummary => GetInspectorPresentation().CuratedArtifactDecisionSummary;

    public string SelectedCuratedArtifactColorArgb => GetInspectorPresentation().CuratedArtifactColorArgb;

    public string SelectedArtifactPositionSummary => GetInspectorPresentation().ArtifactPositionSummary;

    public string SelectedLabelTextHeightSummary => GetInspectorPresentation().LabelTextHeightSummary;

    public string InteractionHint => GetInspectorPresentation().InteractionHint;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        StatusMessage = "Loading review session...";

        var loadResult = await sessionCoordinator.OpenAsync(templateId, floorPlanVersionId, cancellationToken);
        DraftCurationId = loadResult.DraftCurationId;
        ApplySessionState(loadResult.Projection, ReviewSelectionSnapshot.Empty);
        StatusMessage = $"Loaded review session for {Name}";
    }

    public async Task StartEditingPublishedCurationAsync(CancellationToken cancellationToken)
    {
        if (!CanEditPublishedCuration)
        {
            return;
        }

        StatusMessage = "Preparando edici\u00F3n de la versi\u00F3n publicada...";
        var loadResult = await sessionCoordinator.StartEditingPublishedAsync(templateId, floorPlanVersionId, cancellationToken);
        DraftCurationId = loadResult.DraftCurationId;
        ApplySessionState(loadResult.Projection, ReviewSelectionSnapshot.Empty);
        StatusMessage = $"Edici\u00F3n habilitada para {Name}.";
    }

    public async Task RejectSelectedCandidateAsync(CancellationToken cancellationToken)
    {
        var rejected = SelectedCandidate;
        if (rejected is null || DraftCurationId == Guid.Empty || string.Equals(rejected.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        StatusMessage = $"Rejecting {rejected.SourceEntityRef}...";
        await mutationCoordinator.RejectWallCandidateAsync(DraftCurationId, rejected.CandidateId, cancellationToken);

        await RefreshSessionAsync(null, null, SelectedPinchGroup?.PinchGroupId, preferredCuratedArtifact: null, cancellationToken);
        StatusMessage = $"Rejected {rejected.SourceEntityRef}";
    }

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        var publishedCurationId = DraftCurationId;
        StatusMessage = "Publishing curation...";
        try
        {
            await mutationCoordinator.PublishCurationAsync(templateId, publishedCurationId, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = string.Equals(
                exception.Message,
                "A curation must contain at least one pinch marker before publish.",
                StringComparison.Ordinal)
                    ? PublishRequiresPinchMarkerMessage
                    : $"No se pudo publicar: {exception.Message}";
            OnPropertyChanged(nameof(CanPublishCuration));
            return;
        }

        var commissioningStatus = floorPlanVersionId.HasValue
            ? await CommissionPublishedCurationAsync(
                floorPlanVersionId.Value,
                publishedCurationId,
                cancellationToken)
            : "Publicado, pero no qued\u00F3 Auto-fit ready: la revisi\u00F3n no est\u00E1 vinculada a una versi\u00F3n can\u00F3nica exacta.";
        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, GetSelectedCuratedArtifactSelection(), cancellationToken);
        StatusMessage = commissioningStatus;
    }

    private async Task<string> CommissionPublishedCurationAsync(
        Guid publishedFloorPlanVersionId,
        Guid publishedCurationId,
        CancellationToken cancellationToken)
    {
        const string failurePrefix = "Publicado, pero no qued\u00F3 Auto-fit ready: ";
        if (MeasurementContext is null || MeasurementContext.ToMillimetersFactor <= 0m)
        {
            return failurePrefix + "falta un contexto de unidades v\u00E1lido en la curaci\u00F3n publicada.";
        }

        decimal coordinateTolerance;
        try
        {
            coordinateTolerance = MeasurementContext.LinearToleranceMm / MeasurementContext.ToMillimetersFactor;
        }
        catch (OverflowException)
        {
            return failurePrefix + "la tolerancia lineal no se puede representar en unidades del plano.";
        }

        if (coordinateTolerance < 0m)
        {
            return failurePrefix + "la tolerancia lineal no puede ser negativa.";
        }

        if (!TryBuildCommissioningAuxiliaryBindings(
                coordinateTolerance,
                out var auxiliaryBindings,
                out var auxiliaryReason))
        {
            return failurePrefix + auxiliaryReason;
        }

        var compilation = CompileUniqueCommissionedProfile(
            publishedFloorPlanVersionId,
            publishedCurationId,
            MeasurementContext.ToMillimetersFactor,
            coordinateTolerance,
            auxiliaryBindings);
        if (!compilation.Succeeded || compilation.Profile is null)
        {
            return failurePrefix + (compilation.RejectionReason ?? "la receta comisionada no pudo compilarse de forma segura.");
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<SaveCommissionedHouseAdaptationProfileHandler>();
            var readiness = await handler.HandleAsync(
                compilation.Profile,
                cancellationToken);
            if (!readiness.IsReady)
            {
                return failurePrefix + string.Join("; ", readiness.Reasons);
            }

            return $"Publicado {Name}. Auto-fit ready: ancho {readiness.WidthCapacityInches:0.###}\"; profundidad {readiness.DepthCapacityInches:0.###}\".";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return failurePrefix + exception.Message;
        }
    }

    private CommissionExistingCurationProfileCompilationResult CompileUniqueCommissionedProfile(
        Guid publishedFloorPlanVersionId,
        Guid publishedCurationId,
        decimal sourceToMillimetersFactor,
        decimal coordinateTolerance,
        IReadOnlyList<CommissionExistingCurationAuxiliaryEntityBinding> auxiliaryBindings)
    {
        var widthGroups = PinchGroups
            .Where(group => string.Equals(group.AxisTag, nameof(PinchAxisTag.Width), StringComparison.OrdinalIgnoreCase))
            .OrderBy(group => group.SortOrder)
            .ThenBy(group => group.PinchGroupId)
            .ToArray();
        var depthGroups = PinchGroups
            .Where(group => string.Equals(group.AxisTag, nameof(PinchAxisTag.Height), StringComparison.OrdinalIgnoreCase))
            .OrderBy(group => group.SortOrder)
            .ThenBy(group => group.PinchGroupId)
            .ToArray();
        if (widthGroups.Length == 0 || depthGroups.Length == 0)
        {
            var missing = widthGroups.Length == 0 ? "Width" : "Height";
            return new(false, null, $"falta al menos un grupo {missing} publicado para comisionar ancho y profundidad.");
        }

        // Carry the edge pair alongside each success so an ambiguous outcome can name the
        // combinations that survived. Without it the operator is told the geometry is
        // ambiguous but not which axis failed to pin a direction.
        var successes = new List<(string WidthEdge, string DepthEdge, CommissionExistingCurationProfileCompilationResult Result)>(1);
        var rejections = new List<string>();
        foreach (var widthEdge in new[] { "Right", "Left" })
        {
            foreach (var depthEdge in new[] { "Top", "Bottom" })
            {
                CommissionExistingCurationVariableSelection[] variables =
                [
                    new(
                        "width",
                        "Commissioned width",
                        HouseAdaptationAxis.Width,
                        Priority: 1,
                        widthGroups.Select(group =>
                            new CommissionExistingCurationPinchGroupSelection(group.PinchGroupId, widthEdge)).ToArray()),
                    new(
                        "depth",
                        "Commissioned depth",
                        HouseAdaptationAxis.Depth,
                        Priority: 1,
                        depthGroups.Select(group =>
                            new CommissionExistingCurationPinchGroupSelection(group.PinchGroupId, depthEdge)).ToArray())
                ];
                var result = CommissionExistingCurationProfileCompiler.Compile(
                    new CommissionExistingCurationProfileCompilationRequest(
                        publishedFloorPlanVersionId,
                        publishedCurationId,
                        sourceToMillimetersFactor,
                        coordinateTolerance,
                        variables,
                        PinchGroups.ToArray(),
                        PinchMarkers.ToArray(),
                        WallCandidates.ToArray(),
                        GeometryPaths.ToArray(),
                        auxiliaryBindings));
                if (result.Succeeded)
                {
                    successes.Add((widthEdge, depthEdge, result));
                }
                else if (!string.IsNullOrWhiteSpace(result.RejectionReason))
                {
                    rejections.Add(result.RejectionReason);
                }
            }
        }

        return successes.Count switch
        {
            1 => successes[0].Result,
            > 1 => new(
                false,
                null,
                "la geometr\u00EDa admite m\u00E1s de un borde de cierre (" +
                string.Join(", ", successes.Select(pair => $"{pair.WidthEdge}/{pair.DepthEdge}")) +
                "); dej\u00E1 una sola combinaci\u00F3n estructural completa en la curaci\u00F3n."),
            _ => new(
                false,
                null,
                "ninguna combinaci\u00F3n Left/Right y Top/Bottom cerr\u00F3 de forma segura. " +
                (rejections.FirstOrDefault() ?? "Revis\u00E1 los grupos, pares de paredes y capacidades."))
        };
    }

    private bool TryBuildCommissioningAuxiliaryBindings(
        decimal coordinateTolerance,
        out IReadOnlyList<CommissionExistingCurationAuxiliaryEntityBinding> bindings,
        out string reason)
    {
        bindings = [];
        reason = string.Empty;
        var duplicatePath = GeometryPaths
            .GroupBy(path => path.Id)
            .FirstOrDefault(group => group.Count() != 1);
        if (duplicatePath is not null)
        {
            reason = $"la geometr\u00EDa auxiliar contiene el path duplicado {duplicatePath.Key:D}.";
            return false;
        }

        var pathsById = GeometryPaths.ToDictionary(path => path.Id);
        var acceptedWallCandidates = WallCandidates
            .Where(candidate => string.Equals(candidate.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => candidate.SortOrder)
            .ThenBy(candidate => candidate.CandidateId)
            .ToArray();
        var result = new List<CommissionExistingCurationAuxiliaryEntityBinding>();
        foreach (var artifact in VisibleCuratedPlanArtifacts)
        {
            var isOpening = string.Equals(
                artifact.SourceArtifactKind,
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                StringComparison.Ordinal);
            var isProtected = string.Equals(
                                  artifact.SourceArtifactKind,
                                  FloorPlanArtifactSourceKinds.FixedPlanComponent,
                                  StringComparison.Ordinal) ||
                              string.Equals(
                                  artifact.SourceArtifactKind,
                                  FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
                                  StringComparison.Ordinal);
            if (!isOpening && !isProtected)
            {
                continue;
            }

            if (!TryResolveGeometryBounds(
                    artifact.GeometryPathIds,
                    pathsById,
                    artifact.TranslationDx,
                    artifact.TranslationDy,
                    out var sourceBounds))
            {
                reason = $"el objeto {artifact.SourceEntityRef} no tiene geometr\u00EDa auxiliar completa.";
                return false;
            }

            Guid? geometryPathId = null;
            int? segmentSortOrder = null;
            Guid? hostGeometryPathId = null;
            int? hostSegmentSortOrder = null;
            var artifactPathIds = artifact.GeometryPathIds.Distinct().ToArray();
            if (isOpening)
            {
                if (artifactPathIds.Length != 1 ||
                    !pathsById.TryGetValue(artifactPathIds[0], out var openingPath) ||
                    openingPath.Segments.Count != 1)
                {
                    reason = $"la abertura {artifact.SourceEntityRef} necesita un \u00FAnico path y segmento de geometr\u00EDa propia antes de publicar Auto-fit.";
                    return false;
                }

                geometryPathId = openingPath.Id;
                segmentSortOrder = openingPath.Segments[0].SortOrder;
                if (!TryResolveOpeningWallHost(
                        artifact,
                        openingPath,
                        acceptedWallCandidates,
                        pathsById,
                        coordinateTolerance,
                        out hostGeometryPathId,
                        out hostSegmentSortOrder,
                        out reason))
                {
                    return false;
                }
            }
            else
            {
                if (artifactPathIds.Length != 1)
                {
                    reason = $"el objeto protegido {artifact.SourceEntityRef} necesita un \u00FAnico path geom\u00E9trico antes de publicar Auto-fit.";
                    return false;
                }

                geometryPathId = artifactPathIds[0];
            }

            result.Add(new CommissionExistingCurationAuxiliaryEntityBinding(
                artifact.SourceEntityRef,
                isOpening
                    ? CommissionExistingCurationAuxiliaryEntityKind.Opening
                    : CommissionExistingCurationAuxiliaryEntityKind.ProtectedEntity,
                sourceBounds,
                geometryPathId,
                segmentSortOrder,
                IsImmutableSize: true,
                IsProtected: isProtected)
            {
                HostGeometryPathId = hostGeometryPathId,
                HostSegmentSortOrder = hostSegmentSortOrder
            });
        }

        foreach (var label in RoomLabels)
        {
            if (!TryCreateLabelBounds(label.X, label.Y, label.TextHeight, coordinateTolerance, out var sourceBounds))
            {
                reason = $"el label {label.SourceEntityRef} no tiene coordenadas representables.";
                return false;
            }

            result.Add(new CommissionExistingCurationAuxiliaryEntityBinding(
                label.SourceEntityRef,
                CommissionExistingCurationAuxiliaryEntityKind.Label,
                sourceBounds,
                GeometryPathId: null,
                SegmentSortOrder: null,
                IsImmutableSize: true,
                IsProtected: false));
        }

        foreach (var label in OpeningLabels)
        {
            if (!TryCreateLabelBounds(label.X, label.Y, label.TextHeight, coordinateTolerance, out var sourceBounds))
            {
                reason = $"el label {label.SourceEntityRef} no tiene coordenadas representables.";
                return false;
            }

            result.Add(new CommissionExistingCurationAuxiliaryEntityBinding(
                label.SourceEntityRef,
                CommissionExistingCurationAuxiliaryEntityKind.Label,
                sourceBounds,
                GeometryPathId: null,
                SegmentSortOrder: null,
                IsImmutableSize: true,
                IsProtected: false));
        }

        foreach (var dimension in Dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimension.SourceEntityRef))
            {
                reason = "una cota DIMENSION no tiene una referencia fuente exacta.";
                return false;
            }

            if (!string.Equals(dimension.SourceEntityKind, "DIMENSION", StringComparison.OrdinalIgnoreCase))
            {
                reason = $"la cota {dimension.SourceEntityRef} usa el tipo no soportado '{dimension.SourceEntityKind}'.";
                return false;
            }

            if (!TryCreateDimensionBounds(dimension, coordinateTolerance, out var sourceBounds))
            {
                reason = $"la cota {dimension.SourceEntityRef} no tiene bounds completos y representables.";
                return false;
            }

            result.Add(new CommissionExistingCurationAuxiliaryEntityBinding(
                dimension.SourceEntityRef,
                CommissionExistingCurationAuxiliaryEntityKind.Label,
                sourceBounds,
                GeometryPathId: null,
                SegmentSortOrder: null,
                IsImmutableSize: true,
                IsProtected: false));
        }

        var duplicateRef = result
            .GroupBy(binding => binding.SourceEntityRef, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() != 1);
        if (duplicateRef is not null)
        {
            reason = $"la referencia auxiliar {duplicateRef.Key} aparece más de una vez en el commissioning.";
            return false;
        }

        if (result.Count == 0)
        {
            reason = "faltan bindings expl\u00EDcitos para aberturas, labels, cotas o elementos protegidos.";
            return false;
        }

        bindings = result;
        return true;
    }

    private static bool TryResolveOpeningWallHost(
        CuratedPlanArtifactDto opening,
        GeometryPathDto openingPath,
        IReadOnlyList<WallCandidateDto> acceptedWallCandidates,
        IReadOnlyDictionary<Guid, GeometryPathDto> pathsById,
        decimal coordinateTolerance,
        out Guid? hostGeometryPathId,
        out int? hostSegmentSortOrder,
        out string reason)
    {
        hostGeometryPathId = null;
        hostSegmentSortOrder = null;
        reason = string.Empty;

        var sourceSegment = openingPath.Segments[0];
        (decimal StartX, decimal StartY, decimal EndX, decimal EndY) openingSegment;
        try
        {
            openingSegment = (
                checked(sourceSegment.StartX + opening.TranslationDx),
                checked(sourceSegment.StartY + opening.TranslationDy),
                checked(sourceSegment.EndX + opening.TranslationDx),
                checked(sourceSegment.EndY + opening.TranslationDy));
        }
        catch (OverflowException)
        {
            reason = $"la abertura {opening.SourceEntityRef} tiene coordenadas host no representables.";
            return false;
        }

        if (openingSegment.StartX == openingSegment.EndX && openingSegment.StartY == openingSegment.EndY)
        {
            reason = $"la abertura {opening.SourceEntityRef} no tiene un segmento propio representable para resolver su pared host.";
            return false;
        }

        var tolerance = Math.Max(0m, coordinateTolerance);
        var matches = new List<(Guid PathId, int SegmentSortOrder)>();
        var crossingCount = 0;
        try
        {
            foreach (var candidate in acceptedWallCandidates)
            {
                if (!candidate.GeometryPathId.HasValue ||
                    candidate.GeometryPathId.Value == openingPath.Id ||
                    !pathsById.TryGetValue(candidate.GeometryPathId.Value, out var candidatePath))
                {
                    continue;
                }

                foreach (var candidateSegment in candidatePath.Segments)
                {
                    if (IsOpeningSupportedByWallSegment(openingSegment, candidateSegment, tolerance))
                    {
                        matches.Add((candidatePath.Id, candidateSegment.SortOrder));
                        continue;
                    }

                    if (SegmentsCrossWithoutCollinearSupport(openingSegment, candidateSegment, tolerance))
                    {
                        crossingCount++;
                    }
                }
            }
        }
        catch (OverflowException)
        {
            reason = $"la abertura {opening.SourceEntityRef} no permite resolver su pared host sin desbordar las coordenadas.";
            return false;
        }

        if (matches.Count == 1 && crossingCount == 0)
        {
            hostGeometryPathId = matches[0].PathId;
            hostSegmentSortOrder = matches[0].SegmentSortOrder;
            return true;
        }

        if (matches.Count == 1)
        {
            reason = $"la abertura {opening.SourceEntityRef} tiene un candidato de pared host con soporte colineal completo, pero además cruza {crossingCount} segmento(s) estructural(es).";
            return false;
        }

        if (matches.Count > 1)
        {
            reason = $"la abertura {opening.SourceEntityRef} tiene {matches.Count} candidatos de pared host; debe quedar exactamente uno.";
            return false;
        }

        reason = crossingCount > 0
            ? $"la abertura {opening.SourceEntityRef} cruza {crossingCount} segmento(s) estructural(es), pero ninguno la soporta como pared host."
            : $"la abertura {opening.SourceEntityRef} tiene 0 candidatos de pared host con soporte colineal completo.";
        return false;
    }

    private static bool IsOpeningSupportedByWallSegment(
        (decimal StartX, decimal StartY, decimal EndX, decimal EndY) opening,
        GeometrySegmentDto wall,
        decimal tolerance)
    {
        if (wall.StartX == wall.EndX && wall.StartY == wall.EndY)
        {
            return false;
        }

        return IsPointOnInfiniteLine(
                   wall.StartX,
                   wall.StartY,
                   wall.EndX,
                   wall.EndY,
                   opening.StartX,
                   opening.StartY,
                   tolerance) &&
               IsPointOnInfiniteLine(
                   wall.StartX,
                   wall.StartY,
                   wall.EndX,
                   wall.EndY,
                   opening.EndX,
                   opening.EndY,
                   tolerance) &&
               IsPointWithinSegmentBounds(opening.StartX, opening.StartY, wall, tolerance) &&
               IsPointWithinSegmentBounds(opening.EndX, opening.EndY, wall, tolerance);
    }

    private static bool SegmentsCrossWithoutCollinearSupport(
        (decimal StartX, decimal StartY, decimal EndX, decimal EndY) opening,
        GeometrySegmentDto wall,
        decimal tolerance)
    {
        var openingIsCollinear = IsPointOnInfiniteLine(
                                     wall.StartX,
                                     wall.StartY,
                                     wall.EndX,
                                     wall.EndY,
                                     opening.StartX,
                                     opening.StartY,
                                     tolerance) &&
                                 IsPointOnInfiniteLine(
                                     wall.StartX,
                                     wall.StartY,
                                     wall.EndX,
                                     wall.EndY,
                                     opening.EndX,
                                     opening.EndY,
                                     tolerance);
        if (openingIsCollinear)
        {
            return false;
        }

        var wallStartSide = OrientationSign(
            opening.StartX,
            opening.StartY,
            opening.EndX,
            opening.EndY,
            wall.StartX,
            wall.StartY,
            tolerance);
        var wallEndSide = OrientationSign(
            opening.StartX,
            opening.StartY,
            opening.EndX,
            opening.EndY,
            wall.EndX,
            wall.EndY,
            tolerance);
        var openingStartSide = OrientationSign(
            wall.StartX,
            wall.StartY,
            wall.EndX,
            wall.EndY,
            opening.StartX,
            opening.StartY,
            tolerance);
        var openingEndSide = OrientationSign(
            wall.StartX,
            wall.StartY,
            wall.EndX,
            wall.EndY,
            opening.EndX,
            opening.EndY,
            tolerance);

        if (wallStartSide * wallEndSide < 0 && openingStartSide * openingEndSide < 0)
        {
            return true;
        }

        return wallStartSide == 0 && IsPointWithinSegmentBounds(wall.StartX, wall.StartY, opening, tolerance) ||
               wallEndSide == 0 && IsPointWithinSegmentBounds(wall.EndX, wall.EndY, opening, tolerance) ||
               openingStartSide == 0 && IsPointWithinSegmentBounds(opening.StartX, opening.StartY, wall, tolerance) ||
               openingEndSide == 0 && IsPointWithinSegmentBounds(opening.EndX, opening.EndY, wall, tolerance);
    }

    private static int OrientationSign(
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY,
        decimal pointX,
        decimal pointY,
        decimal tolerance)
    {
        var cross = Cross(startX, startY, endX, endY, pointX, pointY);
        var scaledTolerance = tolerance * Math.Max(Math.Abs(endX - startX), Math.Abs(endY - startY));
        if (Math.Abs(cross) <= scaledTolerance)
        {
            return 0;
        }

        return cross < 0m ? -1 : 1;
    }

    private static bool IsPointOnInfiniteLine(
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY,
        decimal pointX,
        decimal pointY,
        decimal tolerance)
    {
        var lineScale = Math.Max(Math.Abs(endX - startX), Math.Abs(endY - startY));
        return lineScale > 0m &&
               Math.Abs(Cross(startX, startY, endX, endY, pointX, pointY)) <= tolerance * lineScale;
    }

    private static decimal Cross(
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY,
        decimal pointX,
        decimal pointY)
        => (endX - startX) * (pointY - startY) - (endY - startY) * (pointX - startX);

    private static bool IsPointWithinSegmentBounds(
        decimal pointX,
        decimal pointY,
        GeometrySegmentDto segment,
        decimal tolerance)
        => pointX >= Math.Min(segment.StartX, segment.EndX) - tolerance &&
           pointX <= Math.Max(segment.StartX, segment.EndX) + tolerance &&
           pointY >= Math.Min(segment.StartY, segment.EndY) - tolerance &&
           pointY <= Math.Max(segment.StartY, segment.EndY) + tolerance;

    private static bool IsPointWithinSegmentBounds(
        decimal pointX,
        decimal pointY,
        (decimal StartX, decimal StartY, decimal EndX, decimal EndY) segment,
        decimal tolerance)
        => pointX >= Math.Min(segment.StartX, segment.EndX) - tolerance &&
           pointX <= Math.Max(segment.StartX, segment.EndX) + tolerance &&
           pointY >= Math.Min(segment.StartY, segment.EndY) - tolerance &&
           pointY <= Math.Max(segment.StartY, segment.EndY) + tolerance;

    private static bool TryResolveGeometryBounds(
        IReadOnlyList<Guid> pathIds,
        IReadOnlyDictionary<Guid, GeometryPathDto> pathsById,
        decimal translationDx,
        decimal translationDy,
        out AdjustmentRecipeBoundsDto? bounds)
    {
        bounds = null;
        var segments = new List<GeometrySegmentDto>();
        foreach (var pathId in pathIds.Distinct())
        {
            if (!pathsById.TryGetValue(pathId, out var path) || path.Segments.Count == 0)
            {
                return false;
            }

            segments.AddRange(path.Segments);
        }

        if (segments.Count == 0)
        {
            return false;
        }

        try
        {
            var minX = checked(segments.Min(segment => Math.Min(segment.StartX, segment.EndX)) + translationDx);
            var minY = checked(segments.Min(segment => Math.Min(segment.StartY, segment.EndY)) + translationDy);
            var maxX = checked(segments.Max(segment => Math.Max(segment.StartX, segment.EndX)) + translationDx);
            var maxY = checked(segments.Max(segment => Math.Max(segment.StartY, segment.EndY)) + translationDy);
            if (minX == maxX && minY == maxY)
            {
                return false;
            }

            bounds = new AdjustmentRecipeBoundsDto(minX, minY, maxX, maxY);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static bool TryCreateLabelBounds(
        decimal x,
        decimal y,
        decimal? textHeight,
        decimal coordinateTolerance,
        out AdjustmentRecipeBoundsDto? bounds)
    {
        bounds = null;
        var halfExtent = Math.Max(
            0.001m,
            Math.Max(coordinateTolerance, textHeight is > 0m ? textHeight.Value / 2m : 0m));
        try
        {
            bounds = new AdjustmentRecipeBoundsDto(
                checked(x - halfExtent),
                checked(y - halfExtent),
                checked(x + halfExtent),
                checked(y + halfExtent));
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static bool TryCreateDimensionBounds(
        DimensionDto dimension,
        decimal coordinateTolerance,
        out AdjustmentRecipeBoundsDto? bounds)
    {
        bounds = null;
        if (dimension.LineSegments is null ||
            dimension.LinePrimitives is null ||
            dimension.TextPrimitives is null ||
            dimension.InsertPrimitives is null ||
            dimension.CirclePrimitives is null ||
            dimension.ArcPrimitives is null ||
            dimension.SolidPrimitives is null ||
            dimension.RenderTextX.HasValue != dimension.RenderTextY.HasValue ||
            dimension.RenderTextHeight is < 0m ||
            dimension.TextPrimitives.Any(text => text.Height < 0m) ||
            dimension.CirclePrimitives.Any(circle => circle.Radius < 0m) ||
            dimension.ArcPrimitives.Any(arc => arc.Radius < 0m))
        {
            return false;
        }

        var points = new List<(decimal X, decimal Y)>
        {
            (dimension.DefPointX, dimension.DefPointY),
            (dimension.DefPoint2X, dimension.DefPoint2Y),
            (dimension.DefPoint3X, dimension.DefPoint3Y)
        };

        try
        {
            foreach (var segment in dimension.LineSegments)
            {
                points.Add((segment.StartX, segment.StartY));
                points.Add((segment.EndX, segment.EndY));
            }

            foreach (var line in dimension.LinePrimitives)
            {
                points.Add((line.StartX, line.StartY));
                points.Add((line.EndX, line.EndY));
            }

            if (dimension.RenderTextX.HasValue)
            {
                AddExtent(
                    dimension.RenderTextX.Value,
                    dimension.RenderTextY!.Value,
                    dimension.RenderTextHeight);
            }

            foreach (var text in dimension.TextPrimitives)
            {
                AddExtent(text.X, text.Y, text.Height);
            }

            points.AddRange(dimension.InsertPrimitives.Select(insert => (insert.X, insert.Y)));
            foreach (var circle in dimension.CirclePrimitives)
            {
                AddExtent(circle.CenterX, circle.CenterY, checked(circle.Radius * 2m));
            }

            foreach (var arc in dimension.ArcPrimitives)
            {
                AddExtent(arc.CenterX, arc.CenterY, checked(arc.Radius * 2m));
            }

            foreach (var solid in dimension.SolidPrimitives)
            {
                points.Add((solid.Point1X, solid.Point1Y));
                points.Add((solid.Point2X, solid.Point2Y));
                points.Add((solid.Point3X, solid.Point3Y));
                points.Add((solid.Point4X, solid.Point4Y));
            }

            var minX = points.Min(point => point.X);
            var minY = points.Min(point => point.Y);
            var maxX = points.Max(point => point.X);
            var maxY = points.Max(point => point.Y);
            if (minX == maxX && minY == maxY)
            {
                var extent = Math.Max(0.001m, coordinateTolerance);
                minX = checked(minX - extent);
                minY = checked(minY - extent);
                maxX = checked(maxX + extent);
                maxY = checked(maxY + extent);
            }

            bounds = new AdjustmentRecipeBoundsDto(minX, minY, maxX, maxY);
            return true;

            void AddExtent(decimal x, decimal y, decimal? fullExtent)
            {
                var halfExtent = Math.Max(
                    0.001m,
                    Math.Max(coordinateTolerance, fullExtent.GetValueOrDefault() / 2m));
                points.Add((checked(x - halfExtent), checked(y - halfExtent)));
                points.Add((checked(x + halfExtent), checked(y + halfExtent)));
            }
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    public Task AddPinchGroupAsync(CancellationToken cancellationToken)
        => AddPinchGroupAsync(CreateNextPinchGroupName(), cancellationToken);

    public async Task AddPinchGroupAsync(string groupName, CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        var normalizedGroupName = NormalizePinchGroupName(groupName);
        if (normalizedGroupName.Length == 0)
        {
            StatusMessage = "El nombre del grupo de pinches es obligatorio.";
            return;
        }

        StatusMessage = $"Creando grupo de pinches {normalizedGroupName}...";
        var groupId = await mutationCoordinator.AddPinchGroupAsync(
            DraftCurationId,
            normalizedGroupName,
            Enum.Parse<PinchAxisTag>(SelectedPinchAxis),
            cancellationToken);

        NewPinchGroupName = string.Empty;
        await RefreshSessionAsync(SelectedCandidate?.CandidateId, null, groupId, GetSelectedCuratedArtifactSelection(), cancellationToken);
        StatusMessage = $"Grupo de pinches {normalizedGroupName} creado.";
    }

    public async Task RenameSelectedPinchGroupAsync(string groupName, CancellationToken cancellationToken)
    {
        if (!CanRenameSelectedPinchGroup || SelectedPinchGroup is null)
        {
            return;
        }

        var normalizedGroupName = NormalizePinchGroupName(groupName);
        if (normalizedGroupName.Length == 0)
        {
            StatusMessage = "El nombre del grupo de pinches es obligatorio.";
            return;
        }

        var groupId = SelectedPinchGroup.PinchGroupId;
        var previousName = SelectedPinchGroup.Name;
        StatusMessage = $"Renombrando grupo de pinches {previousName}...";
        await mutationCoordinator.RenamePinchGroupAsync(
            DraftCurationId,
            groupId,
            normalizedGroupName,
            cancellationToken);

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, groupId, GetSelectedCuratedArtifactSelection(), cancellationToken);
        StatusMessage = $"Grupo de pinches {previousName} renombrado a {normalizedGroupName}.";
    }

    public async Task AddMeasurementCorridorAsync(CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        if (HighlightGeometryPathId is not Guid guideGeometryPathId)
        {
            StatusMessage = "Primero seleccion\u00E1 una l\u00EDnea del preview para crear la franja de medida.";
            return;
        }

        var path = GeometryPaths.FirstOrDefault(item => item.Id == guideGeometryPathId);
        if (path is null || path.Segments.Count == 0)
        {
            StatusMessage = "La geometr\u00EDa elegida no sirve para crear una franja de medida.";
            return;
        }

        var axisTag = Enum.Parse<PinchAxisTag>(SelectedPinchAxis);
        var bandCoordinates = ResolveCorridorBandCoordinates(path, axisTag);
        var corridorName = CreateNextMeasurementCorridorName();
        StatusMessage = "Creando la franja de medida...";
        var corridorId = await mutationCoordinator.AddMeasurementCorridorAsync(
            DraftCurationId,
            corridorName,
            axisTag,
            guideGeometryPathId,
            bandCoordinates.Min,
            bandCoordinates.Max,
            cancellationToken);

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, null, SelectedPinchGroup?.PinchGroupId, GetSelectedCuratedArtifactSelection(), cancellationToken);
        RestoreMeasurementBindingSelection(corridorId, null, null, null);
        StatusMessage = "Franja de medida creada.";
    }

    public void ToggleMeasurementNodePlacement()
    {
        if (IsMeasurementNodePlacementArmed)
        {
            IsMeasurementNodePlacementArmed = false;
            StatusMessage = "Selecci\u00F3n de punto cancelada.";
            return;
        }

        if (SelectedMeasurementCorridor is null)
        {
            StatusMessage = "Seleccion\u00E1 una franja de medida antes de marcar puntos.";
            return;
        }

        if (SelectedMeasurementCorridorNodes.Count >= 2)
        {
            StatusMessage = "La franja ya tiene dos nodos. Elimin\u00E1 uno antes de marcar otro.";
            return;
        }

        IsPinchPlacementArmed = false;
        IsManualWallLinePlacementArmed = false;
        ManualWallLineDraft = null;
        IsMeasurementNodePlacementArmed = true;
        StatusMessage = "Ahora hac\u00E9 click en una l\u00EDnea o punto v\u00E1lido del preview para marcar un punto de medida en la franja seleccionada.";
    }

    public void TogglePinchPlacement()
    {
        if (SelectedPinchGroup is null)
        {
            StatusMessage = "Cre\u00E1 o seleccion\u00E1 un grupo de ajuste antes de marcar un ajuste.";
            return;
        }

        if (SelectedCandidate is null)
        {
            StatusMessage = "Primero seleccion\u00E1 una l\u00EDnea antes de marcar un ajuste.";
            return;
        }

        IsMeasurementNodePlacementArmed = false;
        IsManualWallLinePlacementArmed = false;
        ManualWallLineDraft = null;
        IsPinchPlacementArmed = !IsPinchPlacementArmed;
        StatusMessage = IsPinchPlacementArmed
            ? $"Ahora hac\u00E9 click en el preview para marcar un ajuste de {SelectedPinchGroup.Name}."
            : "Selecci\u00F3n de ajuste cancelada.";
    }

    public void ToggleManualWallLinePlacement()
    {
        IsPinchPlacementArmed = false;
        IsMeasurementNodePlacementArmed = false;
        ManualWallLineDraft = null;
        IsManualWallLinePlacementArmed = !IsManualWallLinePlacementArmed;
        StatusMessage = IsManualWallLinePlacementArmed
            ? "Ahora hac\u00E9 click en el primer punto de la pared manual."
            : "Creaci\u00F3n de pared cancelada.";
    }

    public void UpdateManualWallLinePreviewPoint(decimal x, decimal y)
    {
        if (!IsManualWallLinePlacementArmed || ManualWallLineDraft is not { } draft)
        {
            return;
        }

        ManualWallLineDraft = draft with
        {
            CurrentX = x,
            CurrentY = y
        };
    }

    public async Task HandleManualWallLinePointAsync(decimal x, decimal y, CancellationToken cancellationToken)
    {
        if (!IsManualWallLinePlacementArmed)
        {
            return;
        }

        if (ManualWallLineDraft is null)
        {
            ManualWallLineDraft = new ManualWallLineDraft(x, y, x, y);
            StatusMessage = "Primer punto listo. Mov\u00E9 el cursor y hac\u00E9 click en el segundo punto de la pared.";
            return;
        }

        var draft = ManualWallLineDraft;
        if (draft.StartX == x && draft.StartY == y)
        {
            StatusMessage = "La pared manual necesita dos puntos distintos.";
            return;
        }

        StatusMessage = "Guardando pared manual...";
        var candidateId = await mutationCoordinator.AddManualWallCandidateAsync(
            templateId,
            floorPlanVersionId,
            draft.StartX,
            draft.StartY,
            x,
            y,
            cancellationToken);

        ManualWallLineDraft = null;
        IsManualWallLinePlacementArmed = false;
        await RefreshSessionAsync(candidateId, null, SelectedPinchGroup?.PinchGroupId, preferredCuratedArtifact: null, cancellationToken);
        StatusMessage = "Pared manual agregada al preview.";
    }

    public async Task HandlePreviewInteractionAsync(Guid geometryPathId, decimal positionRatio, CancellationToken cancellationToken)
    {
        if (!SelectPreviewPath(geometryPathId))
        {
            return;
        }

        if (IsMeasurementNodePlacementArmed)
        {
            await AddMeasurementNodeFromPreviewAsync(geometryPathId, positionRatio, cancellationToken);
            return;
        }

        if (!IsPinchPlacementArmed || SelectedCandidate is null || SelectedPinchGroup is null)
        {
            return;
        }

        if (!ArchitecturalLengthText.TryParsePositiveInches(
                NewPinchMaxTrimInches,
                ArchitecturalLengthDefaultUnit.Inches,
                out var maxTrimInches) ||
            maxTrimInches > decimal.MaxValue / MillimetersPerInch)
        {
            StatusMessage = "Escrib\u00ED un ajuste m\u00E1ximo positivo. Ej: 1, 6 1/2\" o 1'-2\". No uses restas como 5' -9\".";
            return;
        }

        var maxTrimMm = maxTrimInches * MillimetersPerInch;
        StatusMessage = $"Guardando un ajuste de {SelectedPinchGroup.Name} sobre {SelectedCandidate.SourceEntityRef}...";
        await mutationCoordinator.AddPinchMarkerAsync(
            DraftCurationId,
            SelectedCandidate.CandidateId,
            SelectedPinchGroup.PinchGroupId,
            positionRatio,
            maxTrimMm,
            cancellationToken);

        IsPinchPlacementArmed = false;
        await RefreshSessionAsync(SelectedCandidate.CandidateId, null, SelectedPinchGroup.PinchGroupId, preferredCuratedArtifact: null, cancellationToken);
        StatusMessage = $"Ajuste de {SelectedPinchGroup.Name} agregado sobre {SelectedCandidate.SourceEntityRef}.";
    }

    public void BeginEditSelectedPinchMaxTrim()
    {
        if (!CanEditSelectedPinchMaxTrim || SelectedPinchMarker is null)
        {
            return;
        }

        pinchMaxTrimPrefill = ArchitecturalLengthText.FormatInches(
            SelectedPinchMarker.MaxTrimMm / MillimetersPerInch);
        editingPinchMarkerId = SelectedPinchMarker.PinchMarkerId;
        EditableSelectedPinchMaxTrim = pinchMaxTrimPrefill;
        IsEditingSelectedPinchMaxTrim = true;
    }

    public void AdjustEditableSelectedPinchMaxTrim(decimal deltaInches)
    {
        if (!IsEditingSelectedPinchMaxTrim)
        {
            return;
        }

        if (!ArchitecturalLengthText.TryAdjustInches(
                EditableSelectedPinchMaxTrim,
                ArchitecturalLengthDefaultUnit.Inches,
                deltaInches,
                out var adjusted))
        {
            StatusMessage = "Escribí una capacidad positiva válida para ajustar.";
            return;
        }

        EditableSelectedPinchMaxTrim = adjusted;
        StatusMessage = "Valor preparado; todavía falta Guardar.";
    }

    public void CancelEditSelectedPinchMaxTrim()
    {
        IsEditingSelectedPinchMaxTrim = false;
        EditableSelectedPinchMaxTrim = string.Empty;
        editingPinchMarkerId = null;
        pinchMaxTrimPrefill = string.Empty;
    }

    public async Task SaveSelectedPinchMaxTrimAsync(CancellationToken cancellationToken)
    {
        var marker = SelectedPinchMarker;
        if (!IsEditingSelectedPinchMaxTrim)
        {
            return;
        }

        if (editingPinchMarkerId is not Guid markerId ||
            marker is null ||
            marker.PinchMarkerId != markerId)
        {
            CancelEditSelectedPinchMaxTrim();
            StatusMessage = "La selección cambió; no se guardó la capacidad anterior.";
            return;
        }

        if (!CanEditSelectedPinchMaxTrim)
        {
            return;
        }

        if (string.Equals(EditableSelectedPinchMaxTrim.Trim(), pinchMaxTrimPrefill.Trim(), StringComparison.Ordinal))
        {
            CancelEditSelectedPinchMaxTrim();
            StatusMessage = "La capacidad del pinch no cambió.";
            return;
        }

        if (!ArchitecturalLengthText.TryParsePositiveInches(
                EditableSelectedPinchMaxTrim,
                ArchitecturalLengthDefaultUnit.Inches,
                out var maxTrimInches) ||
            maxTrimInches > decimal.MaxValue / MillimetersPerInch)
        {
            StatusMessage = "Escrib\u00ED una capacidad positiva. Ej: 1, 6 1/2\" o 1'-2\". No uses restas como 5' -9\".";
            return;
        }

        var maxTrimMm = maxTrimInches * MillimetersPerInch;
        if (maxTrimMm == marker.MaxTrimMm)
        {
            CancelEditSelectedPinchMaxTrim();
            StatusMessage = "La capacidad del pinch no cambió.";
            return;
        }

        StatusMessage = "Guardando capacidad del pinch...";
        await mutationCoordinator.UpdatePinchMarkerMaxTrimAsync(
            DraftCurationId,
            markerId,
            maxTrimMm,
            cancellationToken);

        CancelEditSelectedPinchMaxTrim();
        await RefreshSessionAsync(
            marker.SourceCandidateId,
            markerId,
            marker.PinchGroupId,
            GetSelectedCuratedArtifactSelection(),
            cancellationToken);
        StatusMessage = "Capacidad del pinch actualizada.";
    }

    public async Task SaveSelectedDimensionIntervalBindingAsync(CancellationToken cancellationToken)
    {
        var selectedDimension = SelectedDimension;
        var selectedMeasurementCorridor = SelectedMeasurementCorridor;
        var selectedMeasurementStartNode = SelectedMeasurementStartNode;
        var selectedMeasurementEndNode = SelectedMeasurementEndNode;

        if (!CanSaveSelectedDimensionIntervalBinding ||
            selectedDimension is null ||
            selectedMeasurementCorridor is null ||
            selectedMeasurementStartNode is null ||
            selectedMeasurementEndNode is null)
        {
            return;
        }

        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            selectedDimension,
            SelectedCuratedArtifact);
        var selectedDimensionId = selectedDimension.DimensionId;
        var selectedDimensionDisplayText = selectedDimension.DisplayText;
        var selectedCorridorId = selectedMeasurementCorridor.CorridorId;
        var selectedStartNodeId = selectedMeasurementStartNode.NodeId;
        var selectedEndNodeId = selectedMeasurementEndNode.NodeId;
        StatusMessage = $"Guardando qu\u00E9 mide {selectedDimensionDisplayText}...";
        await mutationCoordinator.SaveDimensionIntervalBindingAsync(
            DraftCurationId,
            selectedDimensionId,
            selectedCorridorId,
            selectedStartNodeId,
            selectedEndNodeId,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        RestoreMeasurementBindingSelection(
            selectedCorridorId,
            null,
            selectedStartNodeId,
            selectedEndNodeId);
        StatusMessage = $"La cota {SelectedDimension?.DisplayText ?? selectedDimensionDisplayText} ya qued\u00F3 relacionada.";
    }

    public async Task RestoreSelectedDimensionIntervalBindingAsync(CancellationToken cancellationToken)
    {
        if (!CanRestoreSelectedDimensionIntervalBinding || SelectedDimension is null)
        {
            return;
        }

        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);
        StatusMessage = $"Quitando la relaci\u00F3n manual de {SelectedDimension.DisplayText}...";
        await mutationCoordinator.RestoreDimensionIntervalBindingAsync(
            DraftCurationId,
            SelectedDimension.DimensionId,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        SelectedMeasurementStartNode = null;
        SelectedMeasurementEndNode = null;
        StatusMessage = $"La cota {SelectedDimension.DisplayText} volvi\u00F3 a medida fija.";
    }

    public async Task RemoveSelectedMeasurementCorridorAsync(CancellationToken cancellationToken)
    {
        if (!CanRemoveSelectedMeasurementCorridor || SelectedMeasurementCorridor is null)
        {
            return;
        }

        var removedCorridor = SelectedMeasurementCorridor;
        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);
        IsMeasurementNodePlacementArmed = false;
        StatusMessage = "Eliminando la franja de medida...";
        await mutationCoordinator.RemoveMeasurementCorridorAsync(
            DraftCurationId,
            removedCorridor.CorridorId,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        SelectedMeasurementCorridor = null;
        SelectedMeasurementNode = null;
        SelectedMeasurementStartNode = null;
        SelectedMeasurementEndNode = null;
        StatusMessage = "Franja de medida eliminada.";
    }

    public async Task RemoveSelectedMeasurementNodeAsync(CancellationToken cancellationToken)
    {
        if (!CanRemoveSelectedMeasurementNode || SelectedMeasurementNode is null)
        {
            return;
        }

        var removedNode = SelectedMeasurementNode;
        var corridorId = removedNode.CorridorId;
        var startNodeId = SelectedMeasurementStartNode?.NodeId;
        var endNodeId = SelectedMeasurementEndNode?.NodeId;
        var removedStartNode = startNodeId == removedNode.NodeId;
        var removedEndNode = endNodeId == removedNode.NodeId;
        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);

        IsMeasurementNodePlacementArmed = false;
        StatusMessage = "Eliminando el nodo de la franja...";
        await mutationCoordinator.RemoveMeasurementNodeAsync(
            DraftCurationId,
            removedNode.NodeId,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        SelectedMeasurementCorridor = MeasurementCorridors.FirstOrDefault(item => item.CorridorId == corridorId);
        SelectedMeasurementNode = null;
        SelectedMeasurementStartNode = removedStartNode || startNodeId is null
            ? null
            : MeasurementNodes.FirstOrDefault(item => item.NodeId == startNodeId.Value);
        SelectedMeasurementEndNode = removedEndNode || endNodeId is null
            ? null
            : MeasurementNodes.FirstOrDefault(item => item.NodeId == endNodeId.Value);
        StatusMessage = "Nodo de la franja eliminado.";
    }

    public async Task ChangeSelectedMeasurementCorridorAxisAsync(CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty || SelectedMeasurementCorridor is null)
        {
            return;
        }

        if (!Enum.TryParse<PinchAxisTag>(SelectedMeasurementCorridorAxis, out var axisTag))
        {
            StatusMessage = "Elegí Width o Height para cambiar el tipo de franja.";
            return;
        }

        var selectedCorridor = SelectedMeasurementCorridor;
        if (string.Equals(selectedCorridor.AxisTag, axisTag.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "La franja ya tiene ese tipo.";
            return;
        }

        var guidePath = GeometryPaths.FirstOrDefault(item => item.Id == selectedCorridor.GuideGeometryPathId);
        if (guidePath is null || guidePath.Segments.Count == 0)
        {
            StatusMessage = "No pude leer la línea guía de esa franja para recalcular el tipo.";
            return;
        }

        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);
        var selectedCorridorId = selectedCorridor.CorridorId;
        var selectedNodeId = SelectedMeasurementNode?.NodeId;
        var selectedStartNodeId = SelectedMeasurementStartNode?.NodeId;
        var selectedEndNodeId = SelectedMeasurementEndNode?.NodeId;
        var bandCoordinates = ResolveCorridorBandCoordinates(guidePath, axisTag);

        StatusMessage = $"Cambiando franja a {axisTag}...";
        await mutationCoordinator.ChangeMeasurementCorridorAxisAsync(
            DraftCurationId,
            selectedCorridorId,
            axisTag,
            bandCoordinates.Min,
            bandCoordinates.Max,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        RestoreMeasurementBindingSelection(
            selectedCorridorId,
            selectedNodeId,
            selectedStartNodeId,
            selectedEndNodeId);
        StatusMessage = $"Franja cambiada a {axisTag}.";
    }

    public void SelectRoomLabel(Guid roomLabelId)
    {
        SelectedRoomLabel = RoomLabels.FirstOrDefault(item => item.RoomLabelId == roomLabelId);
    }

    public void SelectOpeningLabel(Guid openingLabelId)
    {
        SelectedOpeningLabel = OpeningLabels.FirstOrDefault(item => item.OpeningLabelId == openingLabelId);
    }

    public void SelectDimension(Guid dimensionId, bool selectSavedBinding = false)
    {
        SelectedDimension = Dimensions.FirstOrDefault(item => item.DimensionId == dimensionId);
        if (selectSavedBinding)
        {
            SelectSavedMeasurementBindingForDimension(SelectedDimension);
        }
    }

    public async Task SaveEditedDimensionAsync(
        FloorPlanPreviewControl.DimensionEditedEventArgs e,
        CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact) with { DimensionId = e.DimensionId };
        StatusMessage = $"Saving native dimension {e.SourceDimensionKey}...";
        await mutationCoordinator.SaveDimensionOverrideAsync(
            DraftCurationId,
            e.Dimension,
            null,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        StatusMessage = "Saved native dimension override";
    }

    public async Task ExportAdjustedDxfAsync(CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        StatusMessage = "Exporting adjusted DXF...";
        ExportAdjustedDxfResponse response;
        try
        {
            response = await mutationCoordinator.ExportAdjustedDxfAsync(
                templateId,
                floorPlanVersionId,
                DraftCurationId,
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            StatusMessage = string.Equals(
                exception.Message,
                "No dirty native dimensions are available to export.",
                StringComparison.Ordinal) ||
                string.Equals(
                    exception.Message,
                    "No edited native dimensions are available to export.",
                    StringComparison.Ordinal)
                    ? ExportRequiresDirtyNativeDimensionsMessage
                    : $"No se pudo exportar el DXF ajustado: {exception.Message}";
            return;
        }

        await RefreshSessionAsync(
            selectionCoordinator.CaptureSelection(
                SelectedCandidate,
                SelectedPinchMarker,
                SelectedPinchGroup,
                SelectedRoomLabel,
                SelectedOpeningLabel,
                SelectedDimension,
                SelectedCuratedArtifact),
            cancellationToken);
        StatusMessage = $"Adjusted DXF exported: {response.ManagedFilePath}";
    }

    public async Task SaveMovedArtifactPositionAsync(
        FloorPlanPreviewControl.MovableArtifactMovedEventArgs e,
        CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        var selection = selectionCoordinator.BuildSelectionSnapshotForMovedArtifact(SelectedPinchGroup?.PinchGroupId, e);
        StatusMessage = $"Saving moved artifact {e.SourceArtifactKind}:{e.SourceArtifactId}...";
        await mutationCoordinator.SaveArtifactPositionAsync(DraftCurationId, e, cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        StatusMessage = "Saved canonical artifact position";
    }

    public async Task RestoreSelectedArtifactPositionAsync(CancellationToken cancellationToken)
    {
        if (!CanRestoreSelectedArtifactPosition || DraftCurationId == Guid.Empty)
        {
            return;
        }

        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);
        FloorPlanReviewMutationCoordinator.RestoreArtifactPositionRequest? request = null;
        if (SelectedRoomLabel is not null)
        {
            request = new FloorPlanReviewMutationCoordinator.RestoreArtifactPositionRequest(
                FloorPlanArtifactPositionSourceKinds.RoomLabel,
                SelectedRoomLabel.RoomLabelId,
                FloorPlanArtifactPositionMode.AbsolutePoint,
                SelectedRoomLabel.DetectedX ?? SelectedRoomLabel.X,
                SelectedRoomLabel.DetectedY ?? SelectedRoomLabel.Y,
                null,
                null);
        }
        else if (SelectedOpeningLabel is not null)
        {
            request = new FloorPlanReviewMutationCoordinator.RestoreArtifactPositionRequest(
                FloorPlanArtifactPositionSourceKinds.OpeningLabel,
                SelectedOpeningLabel.OpeningLabelId,
                FloorPlanArtifactPositionMode.AbsolutePoint,
                SelectedOpeningLabel.DetectedX ?? SelectedOpeningLabel.X,
                SelectedOpeningLabel.DetectedY ?? SelectedOpeningLabel.Y,
                null,
                null);
        }
        else if (SelectedCuratedArtifact is not null)
        {
            request = new FloorPlanReviewMutationCoordinator.RestoreArtifactPositionRequest(
                SelectedCuratedArtifact.SourceArtifactKind,
                SelectedCuratedArtifact.SourceArtifactId,
                FloorPlanArtifactPositionMode.Translation,
                null,
                null,
                0m,
                0m);
        }
        else if (SelectedDimension is not null)
        {
            await mutationCoordinator.RestoreDimensionOverrideAsync(
                DraftCurationId,
                ResolveSelectedDimensionSourceKey(SelectedDimension),
                cancellationToken);
        }

        if (request is { } restoreRequest)
        {
            await mutationCoordinator.RestoreArtifactPositionAsync(
                DraftCurationId,
                restoreRequest,
                cancellationToken);
        }

        await RefreshSessionAsync(selection, cancellationToken);
        StatusMessage = "Restored detected artifact position";
    }

    public async Task SaveSelectedLabelTextHeightAsync(CancellationToken cancellationToken)
    {
        if (!CanSaveSelectedLabelTextHeight || !TryParseEditableSelectedLabelTextHeight(out var resolvedTextHeight))
        {
            return;
        }

        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);
        var sourceArtifactKind = SelectedRoomLabel is not null
            ? FloorPlanLabelOverrideSourceKinds.RoomLabel
            : FloorPlanLabelOverrideSourceKinds.OpeningLabel;
        var sourceArtifactId = SelectedRoomLabel?.RoomLabelId ?? SelectedOpeningLabel?.OpeningLabelId ?? Guid.Empty;
        if (sourceArtifactId == Guid.Empty)
        {
            return;
        }

        StatusMessage = $"Saving label text height for {sourceArtifactKind}:{sourceArtifactId}...";
        await mutationCoordinator.SaveLabelTextHeightAsync(
            DraftCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            resolvedTextHeight,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        StatusMessage = "Saved canonical label size";
    }

    public async Task RestoreSelectedLabelTextHeightAsync(CancellationToken cancellationToken)
    {
        if (!CanRestoreSelectedLabelTextHeight || DraftCurationId == Guid.Empty)
        {
            return;
        }

        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);
        var sourceArtifactKind = SelectedRoomLabel is not null
            ? FloorPlanLabelOverrideSourceKinds.RoomLabel
            : FloorPlanLabelOverrideSourceKinds.OpeningLabel;
        var sourceArtifactId = SelectedRoomLabel?.RoomLabelId ?? SelectedOpeningLabel?.OpeningLabelId ?? Guid.Empty;
        if (sourceArtifactId == Guid.Empty)
        {
            return;
        }
        await mutationCoordinator.RestoreLabelTextHeightAsync(
            DraftCurationId,
            sourceArtifactKind,
            sourceArtifactId,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        StatusMessage = "Restored detected label size";
    }

    public async Task RemoveSelectedPinchGroupAsync(CancellationToken cancellationToken)
    {
        if (!CanRemoveSelectedPinchGroup || SelectedPinchGroup is null)
        {
            return;
        }

        var removedGroup = SelectedPinchGroup;
        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            null,
            null,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);

        IsPinchPlacementArmed = false;
        StatusMessage = $"Eliminando grupo de pinches {removedGroup.Name}...";
        await mutationCoordinator.RemovePinchGroupAsync(
            DraftCurationId,
            removedGroup.PinchGroupId,
            cancellationToken);

        await RefreshSessionAsync(selection, cancellationToken);
        SelectedPinchGroup = null;
        SelectedPinchMarker = null;
        StatusMessage = $"Grupo de pinches {removedGroup.Name} eliminado.";
    }

    public async Task RemoveSelectedPinchAsync(CancellationToken cancellationToken)
    {
        if (SelectedPinchMarker is null)
        {
            return;
        }

        StatusMessage = "Removing pinch...";
        await mutationCoordinator.RemovePinchMarkerAsync(SelectedPinchMarker.PinchMarkerId, cancellationToken);

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, null, SelectedPinchGroup?.PinchGroupId, GetSelectedCuratedArtifactSelection(), cancellationToken);
        StatusMessage = "Removed pinch";
    }

    public async Task RemoveSelectedRoomLabelAsync(CancellationToken cancellationToken)
    {
        if (SelectedRoomLabel is null)
        {
            return;
        }

        var removed = SelectedRoomLabel;
        StatusMessage = $"Excluding room label {removed.Text}...";
        await mutationCoordinator.RemoveRoomLabelAsync(removed.RoomLabelId, cancellationToken);

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, preferredCuratedArtifact: null, cancellationToken);
        SelectedRoomLabel = null;
        StatusMessage = $"Excluded room label {removed.Text}";
    }

    public async Task RemoveSelectedOpeningCandidateAsync(CancellationToken cancellationToken)
    {
        if (SelectedOpeningCandidate is null)
        {
            return;
        }

        var removed = SelectedOpeningCandidate;
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedOpeningClassification(removed.Kind);
        await ExcludeArtifactByOverlayAsync(
            new CuratedArtifactSelection(FloorPlanArtifactSourceKinds.OpeningCandidate, removed.OpeningCandidateId),
            detected.Family,
            detected.Category,
            detected.Type,
            removed.SourceEntityRef,
            cancellationToken);
    }

    public async Task RemoveSelectedOpeningLabelAsync(CancellationToken cancellationToken)
    {
        if (SelectedOpeningLabel is null)
        {
            return;
        }

        var removed = SelectedOpeningLabel;
        StatusMessage = $"Removing opening label {removed.Text}...";
        await mutationCoordinator.RemoveOpeningLabelAsync(removed.OpeningLabelId, cancellationToken);

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, preferredCuratedArtifact: null, cancellationToken);
        SelectedOpeningLabel = null;
        StatusMessage = $"Removed opening label {removed.Text}";
    }

    public async Task RemoveSelectedFixedPlanComponentAsync(CancellationToken cancellationToken)
    {
        if (SelectedFixedPlanComponent is null)
        {
            return;
        }

        var removed = SelectedFixedPlanComponent;
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedFixedClassification(removed.Kind);
        await ExcludeArtifactByOverlayAsync(
            new CuratedArtifactSelection(FloorPlanArtifactSourceKinds.FixedPlanComponent, removed.FixedPlanComponentId),
            detected.Family,
            detected.Category,
            detected.Type,
            removed.SourceEntityRef,
            cancellationToken);
    }

    public async Task RemoveSelectedProtectedDetailAssemblyAsync(CancellationToken cancellationToken)
    {
        if (SelectedProtectedDetailAssembly is null)
        {
            return;
        }

        var removed = SelectedProtectedDetailAssembly;
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedProtectedClassification(removed.Kind);
        await ExcludeArtifactByOverlayAsync(
            new CuratedArtifactSelection(FloorPlanArtifactSourceKinds.ProtectedDetailAssembly, removed.ProtectedDetailAssemblyId),
            detected.Family,
            detected.Category,
            detected.Type,
            removed.SourceEntityRef,
            cancellationToken);
    }

    public async Task SaveSelectedCuratedArtifactClassificationAsync(CancellationToken cancellationToken)
    {
        if (!CanSaveSelectedCuratedArtifactClassification || SelectedCuratedArtifact is null)
        {
            return;
        }

        StatusMessage = $"Saving classification for {SelectedCuratedArtifact.SourceEntityRef}...";
        await mutationCoordinator.SaveCuratedArtifactClassificationAsync(
            DraftCurationId,
            SelectedCuratedArtifact.SourceArtifactKind,
            SelectedCuratedArtifact.SourceArtifactId,
            EditableCuratedArtifactFamily,
            EditableCuratedArtifactCategory,
            EditableCuratedArtifactType,
            cancellationToken);

        var selection = new CuratedArtifactSelection(SelectedCuratedArtifact.SourceArtifactKind, SelectedCuratedArtifact.SourceArtifactId);
        await RefreshSessionAsync(null, null, SelectedPinchGroup?.PinchGroupId, selection, cancellationToken);
        StatusMessage = $"Saved classification for {SelectedCuratedArtifact.SourceEntityRef}";
    }

    public async Task RestoreSelectedCuratedArtifactClassificationAsync(CancellationToken cancellationToken)
    {
        if (SelectedCuratedArtifact is null || DraftCurationId == Guid.Empty)
        {
            return;
        }

        StatusMessage = $"Restoring detected classification for {SelectedCuratedArtifact.SourceEntityRef}...";
        await mutationCoordinator.RestoreCuratedArtifactClassificationAsync(
            DraftCurationId,
            SelectedCuratedArtifact.SourceArtifactKind,
            SelectedCuratedArtifact.SourceArtifactId,
            SelectedCuratedArtifact.DetectedFamily,
            SelectedCuratedArtifact.DetectedCategory,
            SelectedCuratedArtifact.DetectedType,
            cancellationToken);

        var selection = new CuratedArtifactSelection(SelectedCuratedArtifact.SourceArtifactKind, SelectedCuratedArtifact.SourceArtifactId);
        await RefreshSessionAsync(null, null, SelectedPinchGroup?.PinchGroupId, selection, cancellationToken);
        StatusMessage = $"Restored detected classification for {SelectedCuratedArtifact.SourceEntityRef}";
    }

    public async Task ExcludeSelectedCuratedArtifactAsync(CancellationToken cancellationToken)
    {
        if (SelectedCuratedArtifact is null)
        {
            return;
        }

        await ExcludeArtifactByOverlayAsync(
            new CuratedArtifactSelection(SelectedCuratedArtifact.SourceArtifactKind, SelectedCuratedArtifact.SourceArtifactId),
            SelectedCuratedArtifact.ResolvedFamily,
            SelectedCuratedArtifact.ResolvedCategory,
            SelectedCuratedArtifact.ResolvedType,
            SelectedCuratedArtifact.SourceEntityRef,
            cancellationToken);
    }

    public async Task DeleteSelectedItemAsync(CancellationToken cancellationToken)
    {
        if (!CanDeleteSelectedItem)
        {
            return;
        }

        if (SelectedPinchMarker is not null)
        {
            await RemoveSelectedPinchAsync(cancellationToken);
            return;
        }

        if (SelectedPinchGroup is not null)
        {
            await RemoveSelectedPinchGroupAsync(cancellationToken);
            return;
        }

        if (SelectedMeasurementNode is not null)
        {
            await RemoveSelectedMeasurementNodeAsync(cancellationToken);
            return;
        }

        if (SelectedMeasurementCorridor is not null)
        {
            await RemoveSelectedMeasurementCorridorAsync(cancellationToken);
            return;
        }

        await ExcludeSelectedArtifactAsync(cancellationToken);
    }

    public async Task ExcludeSelectedArtifactAsync(CancellationToken cancellationToken)
    {
        if (SelectedCuratedArtifact is not null)
        {
            await ExcludeSelectedCuratedArtifactAsync(cancellationToken);
            return;
        }

        if (SelectedCandidate is not null)
        {
            await RejectSelectedCandidateAsync(cancellationToken);
            return;
        }

        if (SelectedRoomLabel is not null)
        {
            await RemoveSelectedRoomLabelAsync(cancellationToken);
            return;
        }

        if (SelectedOpeningLabel is not null)
        {
            await RemoveSelectedOpeningLabelAsync(cancellationToken);
            return;
        }

        if (SelectedOpeningCandidate is not null)
        {
            await RemoveSelectedOpeningCandidateAsync(cancellationToken);
            return;
        }

        if (SelectedFixedPlanComponent is not null)
        {
            await RemoveSelectedFixedPlanComponentAsync(cancellationToken);
            return;
        }

        if (SelectedProtectedDetailAssembly is not null)
        {
            await RemoveSelectedProtectedDetailAssemblyAsync(cancellationToken);
        }
    }

    public bool SelectPinchMarker(Guid pinchMarkerId)
    {
        var marker = PinchMarkers.FirstOrDefault(item => item.PinchMarkerId == pinchMarkerId);
        if (marker is null)
        {
            return false;
        }

        SelectedPinchMarker = marker;
        return true;
    }

    public bool SelectPreviewPath(Guid geometryPathId)
    {
        var selection = selectionCoordinator.ResolvePreviewHit(
            geometryPathId,
            VisibleCuratedPlanArtifacts,
            ProtectedDetailAssemblies,
            FixedPlanComponents,
            OpeningCandidates,
            WallCandidates);
        if (!selection.IsMatched)
        {
            return false;
        }

        if (selection.ClearSelectedPinchMarker)
        {
            SelectedPinchMarker = null;
        }

        if (selection.CuratedArtifact is not null)
        {
            SelectedCuratedArtifact = selection.CuratedArtifact;
            return true;
        }

        if (selection.ProtectedDetailAssembly is not null)
        {
            SelectedProtectedDetailAssembly = selection.ProtectedDetailAssembly;
            return true;
        }

        if (selection.FixedPlanComponent is not null)
        {
            SelectedFixedPlanComponent = selection.FixedPlanComponent;
            return true;
        }

        if (selection.OpeningCandidate is not null)
        {
            SelectedOpeningCandidate = selection.OpeningCandidate;
            return true;
        }

        SelectedCandidate = selection.Candidate;
        return true;
    }

    partial void OnActivePublishedCurationIdChanged(Guid? value)
    {
        OnPropertyChanged(nameof(CanEditPublishedCuration));
    }

    partial void OnDraftCurationIdChanged(Guid value)
    {
        if (value == Guid.Empty)
        {
            CancelEditSelectedPinchMaxTrim();
        }

        OnPropertyChanged(nameof(CanEditPublishedCuration));
        OnPropertyChanged(nameof(CanPublishCuration));
        OnPropertyChanged(nameof(CanDeleteSelectedItem));
        OnPropertyChanged(nameof(CanRemoveSelectedPinchGroup));
        OnPropertyChanged(nameof(CanRemoveSelectedMeasurementCorridor));
        OnPropertyChanged(nameof(CanRemoveSelectedMeasurementNode));
        OnPropertyChanged(nameof(CanChangeSelectedMeasurementCorridorAxis));
        OnPropertyChanged(nameof(CanSaveSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(CanRestoreSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(CanSaveSelectedCuratedArtifactClassification));
        OnPropertyChanged(nameof(CanSaveSelectedLabelTextHeight));
        OnPropertyChanged(nameof(CanEditSelectedPinchMaxTrim));
    }

    partial void OnSelectedCandidateChanged(WallCandidateDto? value)
    {
        ApplySelectionPresentation(
            selectionCoordinator.ResolveCandidatePresentation(value, SelectedPinchMarker, SelectedPinchAxis));
        RaiseUxNotifications();
    }

    partial void OnSelectedCuratedArtifactChanged(CuratedPlanArtifactDto? value)
    {
        ApplySelectionPresentation(selectionCoordinator.ResolveCuratedArtifactPresentation(value));
        RaiseUxNotifications();
    }

    partial void OnSelectedRoomLabelChanged(RoomLabelDto? value)
    {
        ApplySelectionPresentation(selectionCoordinator.ResolveRoomLabelPresentation(value));
        RaiseUxNotifications();
    }

    partial void OnSelectedPinchMarkerChanged(PinchMarkerDto? value)
    {
        if (IsEditingSelectedPinchMaxTrim && editingPinchMarkerId != value?.PinchMarkerId)
        {
            CancelEditSelectedPinchMaxTrim();
        }

        ApplySelectionPresentation(selectionCoordinator.ResolvePinchMarkerPresentation(value));
        OnPropertyChanged(nameof(SelectedPinchMarkerId));
        OnPropertyChanged(nameof(CanRemoveSelectedPinch));
        OnPropertyChanged(nameof(CanEditSelectedPinchMaxTrim));
        OnPropertyChanged(nameof(SelectedPinchMaxTrimDisplay));
        OnPropertyChanged(nameof(CanDeleteSelectedItem));
        RaiseUxNotifications();
    }

    partial void OnSelectedOpeningCandidateChanged(OpeningCandidateDto? value)
    {
        ApplySelectionPresentation(selectionCoordinator.ResolveOpeningCandidatePresentation(value));
        RaiseUxNotifications();
    }

    partial void OnSelectedOpeningLabelChanged(OpeningLabelDto? value)
    {
        ApplySelectionPresentation(selectionCoordinator.ResolveOpeningLabelPresentation(value));
        RaiseUxNotifications();
    }

    partial void OnSelectedFixedPlanComponentChanged(FixedPlanComponentDto? value)
    {
        ApplySelectionPresentation(selectionCoordinator.ResolveFixedPlanComponentPresentation(value));
        RaiseUxNotifications();
    }

    partial void OnSelectedProtectedDetailAssemblyChanged(ProtectedDetailAssemblyDto? value)
    {
        ApplySelectionPresentation(selectionCoordinator.ResolveProtectedDetailAssemblyPresentation(value));
        RaiseUxNotifications();
    }

    partial void OnSelectedDimensionChanged(DimensionDto? value)
    {
        TryAutoAssignMeasurementEndpoints();
        ApplySelectionPresentation(selectionCoordinator.ResolveDimensionPresentation(value));
        RaiseUxNotifications();
        OnPropertyChanged(nameof(CanSaveSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(CanRestoreSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(SelectedDimensionIntervalBindingSummary));
        OnPropertyChanged(nameof(SelectedDimensionImpactSummary));
    }

    partial void OnSelectedPinchGroupChanged(PinchGroupDto? value)
    {
        if (value is not null && !string.Equals(SelectedPinchAxis, value.AxisTag, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPinchAxis = value.AxisTag;
        }

        if (value is null || SelectedPinchMarker?.PinchGroupId != value.PinchGroupId)
        {
            SelectedPinchMarker = null;
        }

        OnPropertyChanged(nameof(SelectedPinchGroupId));
        OnPropertyChanged(nameof(SelectedPinchGroupMarkers));
        OnPropertyChanged(nameof(CanRemoveSelectedPinchGroup));
        OnPropertyChanged(nameof(CanRenameSelectedPinchGroup));
        OnPropertyChanged(nameof(CanDeleteSelectedItem));
        OnPropertyChanged(nameof(SelectedPinchGroupImpactSummary));
        RaiseUxNotifications();
    }

    partial void OnSelectedPinchAxisChanged(string value)
    {
        if (SelectedPinchGroup is null ||
            !string.Equals(SelectedPinchGroup.AxisTag, value, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPinchGroup = ResolvePreviewPinchGroupForAxis(value);
        }

        RaiseUxNotifications();
    }

    private PinchGroupDto? ResolvePreviewPinchGroupForAxis(string axisTag)
    {
        return PinchGroups
            .Where(group => string.Equals(group.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
            .OrderBy(group => PinchMarkers.Any(marker =>
                marker.PinchGroupId == group.PinchGroupId &&
                string.Equals(marker.AxisTag, axisTag, StringComparison.OrdinalIgnoreCase))
                    ? 0
                    : 1)
            .ThenBy(group => group.SortOrder)
            .FirstOrDefault();
    }

    partial void OnSelectedMeasurementCorridorAxisChanged(string value)
    {
        if (Enum.TryParse<PinchAxisTag>(value, out _) &&
            !string.Equals(SelectedPinchAxis, value, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPinchAxis = value;
        }

        OnPropertyChanged(nameof(CanChangeSelectedMeasurementCorridorAxis));
    }

    partial void OnSelectedMeasurementCorridorChanged(MeasurementCorridorDto? value)
    {
        InvalidateSelectedMeasurementGroupNodeOptionCache();

        if (value is null)
        {
            SelectedMeasurementNode = null;
            SelectedMeasurementStartNode = null;
            SelectedMeasurementEndNode = null;
        }
        else
        {
            if (!string.Equals(SelectedMeasurementCorridorAxis, value.AxisTag, StringComparison.OrdinalIgnoreCase))
            {
                SelectedMeasurementCorridorAxis = value.AxisTag;
            }

            if (SelectedMeasurementNode?.CorridorId != value.CorridorId)
            {
                SelectedMeasurementNode = null;
            }

            if (SelectedMeasurementStartNode?.CorridorId != value.CorridorId)
            {
                SelectedMeasurementStartNode = null;
            }

            if (SelectedMeasurementEndNode?.CorridorId != value.CorridorId)
            {
                SelectedMeasurementEndNode = null;
            }
        }

        TryAutoAssignMeasurementEndpoints();

        OnPropertyChanged(nameof(SelectedMeasurementCorridorId));
        OnPropertyChanged(nameof(HasSelectedMeasurementCorridor));
        OnPropertyChanged(nameof(CanRemoveSelectedMeasurementCorridor));
        OnPropertyChanged(nameof(CanDeleteSelectedItem));
        OnPropertyChanged(nameof(CanChangeSelectedMeasurementCorridorAxis));
        OnPropertyChanged(nameof(SelectedMeasurementCorridorNodes));
        OnPropertyChanged(nameof(SelectedMeasurementNodeGroupOption));
        OnPropertyChanged(nameof(SelectedMeasurementGroupNodeOptions));
        OnPropertyChanged(nameof(SelectedMeasurementGroupNodeOption));
        OnPropertyChanged(nameof(SelectedMeasurementStartNodeOption));
        OnPropertyChanged(nameof(SelectedMeasurementEndNodeOption));
        OnPropertyChanged(nameof(CanSaveSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(CanRestoreSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(SelectedDimensionIntervalBindingSummary));
        OnPropertyChanged(nameof(SelectedPinchGroupImpactSummary));
        OnPropertyChanged(nameof(SelectedDimensionImpactSummary));
    }

    partial void OnSelectedMeasurementNodeChanged(MeasurementNodeDto? value)
    {
        SelectMeasurementCorridorForNode(value);
        OnPropertyChanged(nameof(SelectedMeasurementNodeId));
        OnPropertyChanged(nameof(SelectedMeasurementGroupNodeOption));
        OnPropertyChanged(nameof(CanRemoveSelectedMeasurementNode));
        OnPropertyChanged(nameof(CanDeleteSelectedItem));
    }

    partial void OnSelectedMeasurementStartNodeChanged(MeasurementNodeDto? value)
    {
        SelectMeasurementCorridorForNode(value);
        OnPropertyChanged(nameof(SelectedMeasurementStartNodeId));
        OnPropertyChanged(nameof(CanSaveSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(CanRestoreSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(SelectedDimensionIntervalBindingSummary));
        OnPropertyChanged(nameof(SelectedDimensionImpactSummary));
        OnPropertyChanged(nameof(SelectedMeasurementStartNodeOption));
    }

    partial void OnSelectedMeasurementEndNodeChanged(MeasurementNodeDto? value)
    {
        SelectMeasurementCorridorForNode(value);
        OnPropertyChanged(nameof(SelectedMeasurementEndNodeId));
        OnPropertyChanged(nameof(CanSaveSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(CanRestoreSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(SelectedDimensionIntervalBindingSummary));
        OnPropertyChanged(nameof(SelectedDimensionImpactSummary));
        OnPropertyChanged(nameof(SelectedMeasurementEndNodeOption));
    }

    partial void OnEditableCuratedArtifactFamilyChanged(string value)
    {
        if (isUpdatingCuratedArtifactEditors)
        {
            return;
        }

        ReplaceItems(EditableCuratedArtifactCategoryOptions, FloorPlanArtifactTaxonomy.GetCategories(value));
        if (!EditableCuratedArtifactCategoryOptions.Contains(EditableCuratedArtifactCategory, StringComparer.Ordinal))
        {
            EditableCuratedArtifactCategory = EditableCuratedArtifactCategoryOptions.FirstOrDefault() ?? string.Empty;
        }
        else
        {
            ReplaceItems(EditableCuratedArtifactTypeOptions, FloorPlanArtifactTaxonomy.GetTypes(value, EditableCuratedArtifactCategory));
            if (!EditableCuratedArtifactTypeOptions.Contains(EditableCuratedArtifactType, StringComparer.Ordinal))
            {
                EditableCuratedArtifactType = EditableCuratedArtifactTypeOptions.FirstOrDefault() ?? string.Empty;
            }
        }

        RaiseUxNotifications();
    }

    partial void OnEditableCuratedArtifactCategoryChanged(string value)
    {
        if (isUpdatingCuratedArtifactEditors)
        {
            return;
        }

        ReplaceItems(EditableCuratedArtifactTypeOptions, FloorPlanArtifactTaxonomy.GetTypes(EditableCuratedArtifactFamily, value));
        if (!EditableCuratedArtifactTypeOptions.Contains(EditableCuratedArtifactType, StringComparer.Ordinal))
        {
            EditableCuratedArtifactType = EditableCuratedArtifactTypeOptions.FirstOrDefault() ?? string.Empty;
        }

        RaiseUxNotifications();
    }

    partial void OnEditableCuratedArtifactTypeChanged(string value)
    {
        RaiseUxNotifications();
    }

    partial void OnEditableSelectedLabelTextHeightChanged(string value)
    {
        RaiseUxNotifications();
    }

    partial void OnReviewQueueSearchTextChanged(string value)
    {
        RefreshReviewQueue();
    }

    partial void OnSelectedReviewQueueFilterChanged(string value)
    {
        RefreshReviewQueue();
    }

    partial void OnIsStructureQueueExpandedChanged(bool value)
    {
        if (value)
        {
            ApplyQueueExpansionState(queueCoordinator.CollapseSectionsExcept(
                CaptureQueueExpansionState(),
                ReviewQueueSection.Structure));
        }
    }

    partial void OnIsRoomNamesQueueExpandedChanged(bool value)
    {
        if (value)
        {
            ApplyQueueExpansionState(queueCoordinator.CollapseSectionsExcept(
                CaptureQueueExpansionState(),
                ReviewQueueSection.RoomNames));
        }
    }

    partial void OnIsOpeningCodesQueueExpandedChanged(bool value)
    {
        if (value)
        {
            ApplyQueueExpansionState(queueCoordinator.CollapseSectionsExcept(
                CaptureQueueExpansionState(),
                ReviewQueueSection.OpeningCodes));
        }
    }

    partial void OnIsDimensionsQueueExpandedChanged(bool value)
    {
        if (value)
        {
            ApplyQueueExpansionState(queueCoordinator.CollapseSectionsExcept(
                CaptureQueueExpansionState(),
                ReviewQueueSection.Dimensions));
        }
    }

    partial void OnIsCuratedObjectsQueueExpandedChanged(bool value)
    {
        if (value)
        {
            ApplyQueueExpansionState(queueCoordinator.CollapseSectionsExcept(
                CaptureQueueExpansionState(),
                ReviewQueueSection.CuratedObjects));
        }
    }

    partial void OnSelectedInspectorToolChanged(string value)
    {
        RaiseUxNotifications();
    }

    partial void OnIsPinchPlacementArmedChanged(bool value)
    {
        RaiseUxNotifications();
    }

    partial void OnIsManualWallLinePlacementArmedChanged(bool value)
    {
        RaiseUxNotifications();
        OnPropertyChanged(nameof(AddManualWallLineButtonLabel));
    }

    partial void OnIsMeasurementNodePlacementArmedChanged(bool value)
    {
        RaiseUxNotifications();
        OnPropertyChanged(nameof(AddMeasurementNodeButtonLabel));
    }

    public void SelectInspectorTool(string tool)
    {
        if (string.IsNullOrWhiteSpace(tool))
        {
            return;
        }

        SelectedInspectorTool = tool;
    }

    private async Task RefreshSessionAsync(
        Guid? preferredCandidateId,
        Guid? preferredPinchMarkerId,
        Guid? preferredPinchGroupId,
        CuratedArtifactSelection? preferredCuratedArtifact,
        CancellationToken cancellationToken)
    {
        await RefreshSessionAsync(
            new ReviewSelectionSnapshot(
                preferredCandidateId,
                preferredPinchMarkerId,
                preferredPinchGroupId,
                null,
                null,
                null,
                preferredCuratedArtifact?.SourceArtifactKind,
                preferredCuratedArtifact?.SourceArtifactId),
            cancellationToken);
    }

    private async Task RefreshSessionAsync(ReviewSelectionSnapshot selection, CancellationToken cancellationToken)
    {
        var loadResult = await sessionCoordinator.RefreshAsync(templateId, floorPlanVersionId, DraftCurationId, cancellationToken);
        DraftCurationId = loadResult.DraftCurationId;
        ApplySessionState(loadResult.Projection, selection);
    }

    private void ApplySessionState(ReviewSessionProjection session, ReviewSelectionSnapshot selection)
    {
        var applyPlan = applyCoordinator.BuildSessionApplyPlan(session, selection, selectionCoordinator);

        Code = applyPlan.Session.Code;
        Name = applyPlan.Session.Name;
        Status = applyPlan.Session.Status;
        ActiveVersionNumber = applyPlan.Session.ActiveVersionNumber;
        ActivePublishedCurationId = applyPlan.Session.ActivePublishedCurationId;

        ReplaceItems(GeometryPaths, applyPlan.Session.GeometryPaths);
        ReplaceItems(RoomLabels, applyPlan.Session.RoomLabels);
        ReplaceItems(OpeningCandidates, applyPlan.Session.OpeningCandidates);
        ReplaceItems(OpeningLabels, applyPlan.Session.OpeningLabels);
        ReplaceItems(Dimensions, applyPlan.Session.Dimensions);
        MeasurementContext = applyPlan.Session.MeasurementContext;
        ReplaceItems(DimensionBindings, applyPlan.Session.DimensionBindings);
        ReplaceItems(DimensionAssociations, applyPlan.Session.DimensionAssociations);
        ReplaceItems(FixedPlanComponents, applyPlan.Session.FixedPlanComponents);
        ReplaceItems(ProtectedDetailAssemblies, applyPlan.Session.ProtectedDetailAssemblies);
        ReplaceItems(WallCandidates, applyPlan.Session.WallCandidates);
        ReplaceItems(PinchGroups, applyPlan.Session.PinchGroups);
        ReplaceItems(PinchMarkers, applyPlan.Session.PinchMarkers);
        OnPropertyChanged(nameof(SelectedPinchGroupMarkers));
        ReplaceItems(MeasurementCorridors, applyPlan.Session.MeasurementCorridors);
        ReplaceItems(MeasurementNodes, applyPlan.Session.MeasurementNodes);
        ReplaceItems(DimensionIntervalBindings, applyPlan.Session.DimensionIntervalBindings);
        ReplaceItems(ArticulationBands, applyPlan.Session.ArticulationBands);
        ReplaceItems(CuratedPlanArtifacts, applyPlan.Session.CuratedPlanArtifacts);
        dimensionAssociationsById = applyPlan.Session.DimensionAssociationsById;
        ReplaceItems(VisibleCuratedPlanArtifacts, applyPlan.Session.VisibleCuratedPlanArtifacts);
        RefreshReviewQueue();

        NotifySessionProjectionCountsChanged();
        InvalidateMeasurementNodeGroupOptionCaches();
        OnPropertyChanged(nameof(SelectedMeasurementCorridorNodes));
        RaiseMeasurementNodeGroupOptionNotifications();
        OnPropertyChanged(nameof(CanPublishCuration));
        OnPropertyChanged(nameof(CanRemoveSelectedPinchGroup));
        OnPropertyChanged(nameof(CanRenameSelectedPinchGroup));
        OnPropertyChanged(nameof(SuggestedPinchGroupName));
        OnPropertyChanged(nameof(CanRemoveSelectedMeasurementCorridor));
        OnPropertyChanged(nameof(SelectedDimensionIntervalBindingSummary));
        OnPropertyChanged(nameof(SelectedPinchGroupImpactSummary));
        OnPropertyChanged(nameof(SelectedDimensionImpactSummary));
        OnPropertyChanged(nameof(CanSaveSelectedDimensionIntervalBinding));
        OnPropertyChanged(nameof(CanRestoreSelectedDimensionIntervalBinding));
        ApplySelectionReplay(applyPlan.SelectionReplay);
    }

    private static bool IntervalsOverlap(decimal startA, decimal endA, decimal startB, decimal endB, decimal tolerance = 0.001m)
    {
        var minA = Math.Min(startA, endA);
        var maxA = Math.Max(startA, endA);
        var minB = Math.Min(startB, endB);
        var maxB = Math.Max(startB, endB);
        return maxA > minB + tolerance && minA < maxB - tolerance;
    }

    private void NotifySessionProjectionCountsChanged()
    {
        OnPropertyChanged(nameof(DoorOpeningCount));
        OnPropertyChanged(nameof(WindowOpeningCount));
        OnPropertyChanged(nameof(FixedPlanComponentCount));
        OnPropertyChanged(nameof(ProtectedDetailAssemblyCount));
        OnPropertyChanged(nameof(RoomLabelCount));
        OnPropertyChanged(nameof(OpeningLabelCount));
        OnPropertyChanged(nameof(DimensionCount));
    }

    private void ApplySelectionReplay(ReviewSelectionReplayResult selectionReplay)
    {
        SelectedCandidate = selectionReplay.Candidate;
        SelectedPinchMarker = selectionReplay.PinchMarker;
        SelectedPinchGroup = selectionReplay.PinchGroup;

        if (selectionReplay.CuratedArtifact is not null)
        {
            SelectedCuratedArtifact = selectionReplay.CuratedArtifact;
            return;
        }

        if (selectionReplay.OpeningLabel is not null)
        {
            SelectedOpeningLabel = selectionReplay.OpeningLabel;
            return;
        }

        if (selectionReplay.Dimension is not null)
        {
            SelectedDimension = selectionReplay.Dimension;
            return;
        }

        if (selectionReplay.RoomLabel is not null)
        {
            SelectedRoomLabel = selectionReplay.RoomLabel;
        }
    }

    private async Task ExcludeArtifactByOverlayAsync(
        CuratedArtifactSelection selection,
        string resolvedFamily,
        string resolvedCategory,
        string resolvedType,
        string displayName,
        CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        StatusMessage = $"Excluding curated object {displayName}...";
        await mutationCoordinator.ExcludeCuratedArtifactAsync(
            DraftCurationId,
            selection.SourceArtifactKind,
            selection.SourceArtifactId,
            resolvedFamily,
            resolvedCategory,
            resolvedType,
            cancellationToken);

        await RefreshSessionAsync(null, null, SelectedPinchGroup?.PinchGroupId, selection, cancellationToken);
        StatusMessage = $"Excluded curated object {displayName}";
    }

    private void RefreshReviewQueue()
    {
        var projection = queueCoordinator.BuildProjection(
            SelectedReviewQueueFilter,
            ReviewQueueSearchText,
            WallCandidates,
            RoomLabels,
            OpeningLabels,
            Dimensions,
            VisibleCuratedPlanArtifacts);

        ApplyQueueProjection(projection);
        ApplyQueueExpansionState(queueCoordinator.NormalizeExpansion(CaptureQueueExpansionState(), projection));
        RaiseReviewQueueNotifications();
    }

    private ReviewQueueExpansionState CaptureQueueExpansionState()
    {
        return new ReviewQueueExpansionState(
            IsStructureQueueExpanded,
            IsRoomNamesQueueExpanded,
            IsOpeningCodesQueueExpanded,
            IsDimensionsQueueExpanded,
            IsCuratedObjectsQueueExpanded);
    }

    private void ApplyQueueProjection(ReviewQueueProjection projection)
    {
        ReplaceItems(VisibleWallCandidates, projection.VisibleWallCandidates);
        ReplaceItems(VisibleRoomLabels, projection.VisibleRoomLabels);
        ReplaceItems(VisibleOpeningLabels, projection.VisibleOpeningLabels);
        ReplaceItems(VisibleDimensions, projection.VisibleDimensions);
        ReplaceItems(CuratedArtifactGroups, projection.CuratedArtifactGroups);
    }

    private void ApplyQueueExpansionState(ReviewQueueExpansionState state)
    {
        IsStructureQueueExpanded = state.IsStructureQueueExpanded;
        IsRoomNamesQueueExpanded = state.IsRoomNamesQueueExpanded;
        IsOpeningCodesQueueExpanded = state.IsOpeningCodesQueueExpanded;
        IsDimensionsQueueExpanded = state.IsDimensionsQueueExpanded;
        IsCuratedObjectsQueueExpanded = state.IsCuratedObjectsQueueExpanded;
    }

    private void RaiseReviewQueueNotifications()
    {
        RaisePropertyNotifications(notificationCoordinator.GetQueuePropertyNames());
    }

    private static (decimal Min, decimal Max) ResolveCorridorBandCoordinates(GeometryPathDto path, PinchAxisTag axisTag)
    {
        var values = path.Segments
            .SelectMany(segment => axisTag == PinchAxisTag.Height
                ? new[] { segment.StartX, segment.EndX }
                : new[] { segment.StartY, segment.EndY })
            .ToArray();

        return (values.Min(), values.Max());
    }

    private async Task AddMeasurementNodeFromPreviewAsync(Guid geometryPathId, decimal positionRatio, CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty || SelectedMeasurementCorridor is null)
        {
            return;
        }

        if (SelectedMeasurementCorridorNodes.Count >= 2)
        {
            IsMeasurementNodePlacementArmed = false;
            StatusMessage = "La franja ya tiene dos nodos. Elimin\u00E1 uno antes de marcar otro.";
            return;
        }

        var path = GeometryPaths.FirstOrDefault(item => item.Id == geometryPathId);
        var worldPoint = path is null ? null : FloorPlanPreviewGeometry.GetPointAtRatio(path, positionRatio);
        if (path is null || worldPoint is null)
        {
            StatusMessage = "No pude leer ese punto de la geometr\u00EDa seleccionada.";
            return;
        }

        var sourceArtifact = ResolveSelectedMeasurementSourceArtifact();
        if (sourceArtifact is null)
        {
            StatusMessage = "Esa geometr\u00EDa no sirve para marcar un punto de medida.";
            return;
        }

        var axisCoordinate = string.Equals(SelectedMeasurementCorridor.AxisTag, nameof(PinchAxisTag.Height), StringComparison.OrdinalIgnoreCase)
            ? decimal.Round((decimal)worldPoint.Value.Y, 3, MidpointRounding.AwayFromZero)
            : decimal.Round((decimal)worldPoint.Value.X, 3, MidpointRounding.AwayFromZero);

        var selection = selectionCoordinator.CaptureSelection(
            SelectedCandidate,
            SelectedPinchMarker,
            SelectedPinchGroup,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedCuratedArtifact);
        var selectedCorridorId = SelectedMeasurementCorridor.CorridorId;
        var selectedCorridorName = SelectedMeasurementCorridor.Name;
        StatusMessage = $"Guardando un punto de medida en {selectedCorridorName}...";
        var nodeId = await mutationCoordinator.AddMeasurementNodeAsync(
            DraftCurationId,
            selectedCorridorId,
            referenceKind: "ProjectedGeometry",
            sourceArtifact.Value.SourceArtifactKind,
            sourceArtifact.Value.SourceArtifactId,
            geometryPathId,
            snapKind: "Projected",
            decimal.Round((decimal)worldPoint.Value.X, 3, MidpointRounding.AwayFromZero),
            decimal.Round((decimal)worldPoint.Value.Y, 3, MidpointRounding.AwayFromZero),
            axisCoordinate,
            offsetAlongAxis: 0m,
            offsetNormal: 0m,
            positionRatio,
            cancellationToken);

        IsMeasurementNodePlacementArmed = false;
        await RefreshSessionAsync(selection, cancellationToken);
        RestoreMeasurementBindingSelection(selectedCorridorId, nodeId, null, null);
        StatusMessage = $"Punto de medida agregado a {SelectedMeasurementCorridor?.Name ?? selectedCorridorName}.";
    }

    private (string SourceArtifactKind, Guid SourceArtifactId)? ResolveSelectedMeasurementSourceArtifact()
    {
        if (SelectedCuratedArtifact is not null)
        {
            return (SelectedCuratedArtifact.SourceArtifactKind, SelectedCuratedArtifact.SourceArtifactId);
        }

        if (SelectedProtectedDetailAssembly is not null)
        {
            return (FloorPlanArtifactSourceKinds.ProtectedDetailAssembly, SelectedProtectedDetailAssembly.ProtectedDetailAssemblyId);
        }

        if (SelectedFixedPlanComponent is not null)
        {
            return (FloorPlanArtifactSourceKinds.FixedPlanComponent, SelectedFixedPlanComponent.FixedPlanComponentId);
        }

        if (SelectedOpeningCandidate is not null)
        {
            return (FloorPlanArtifactSourceKinds.OpeningCandidate, SelectedOpeningCandidate.OpeningCandidateId);
        }

        if (SelectedCandidate is not null)
        {
            return (FloorPlanArtifactSourceKinds.WallCandidate, SelectedCandidate.CandidateId);
        }

        return null;
    }

    private MeasurementNodeGroupOptionViewModel CreateMeasurementNodeGroupOption(MeasurementCorridorDto corridor)
    {
        var nodeCount = MeasurementNodes.Count(item => item.CorridorId == corridor.CorridorId);
        var nodeLabel = nodeCount == 1 ? "nodo" : "nodos";
        return new MeasurementNodeGroupOptionViewModel(
            corridor,
            $"Franja {ResolveMeasurementCorridorDisplayNumber(corridor).ToString(CultureInfo.InvariantCulture)}",
            $"{corridor.AxisTag} - {nodeCount.ToString(CultureInfo.InvariantCulture)} {nodeLabel}");
    }

    private string CreateNextMeasurementCorridorName()
    {
        var nextDisplayNumber = MeasurementCorridors.Count == 0
            ? 1
            : MeasurementCorridors.Max(ResolveMeasurementCorridorDisplayNumber) + 1;
        return $"Franja {nextDisplayNumber.ToString(CultureInfo.InvariantCulture)}";
    }

    private string CreateNextPinchGroupName()
    {
        var nextDisplayNumber = PinchGroups.Count == 0
            ? 1
            : PinchGroups.Max(item => item.SortOrder) + 1;
        return $"Ajuste {nextDisplayNumber.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string NormalizePinchGroupName(string groupName)
    {
        return string.IsNullOrWhiteSpace(groupName)
            ? string.Empty
            : groupName.Trim();
    }

    private MeasurementNodeGroupOptionViewModel? FindMeasurementNodeGroupOption()
    {
        if (SelectedMeasurementCorridor is null)
        {
            return null;
        }

        return MeasurementNodeGroupOptions.FirstOrDefault(item => item.CorridorId == SelectedMeasurementCorridor.CorridorId);
    }

    private MeasurementNodeGroupNodeOptionViewModel CreateMeasurementNodeGroupNodeOption(
        MeasurementNodeDto node)
    {
        var name = node.SortOrder > 0
            ? $"Nodo {node.SortOrder.ToString(CultureInfo.InvariantCulture)}"
            : "Nodo";
        var corridor = MeasurementCorridors.FirstOrDefault(item => item.CorridorId == node.CorridorId);
        var axisTag = corridor?.AxisTag ?? "Eje";

        return new MeasurementNodeGroupNodeOptionViewModel(
            name,
            node,
            CreateMeasurementNodeDetails(axisTag, node));
    }

    private MeasurementNodeGroupNodeOptionViewModel? FindMeasurementNodeGroupNodeOption(MeasurementNodeDto? node)
    {
        if (node is null)
        {
            return null;
        }

        return SelectedMeasurementGroupNodeOptions.FirstOrDefault(item => item.NodeId == node.NodeId);
    }

    private static string CreateMeasurementNodeDetails(
        string axisTag,
        MeasurementNodeDto node) =>
        $"{node.SourceArtifactKind} - {axisTag} - eje {FormatMeasurementNodeDecimal(node.AxisCoordinate)} - linea {FormatMeasurementNodeDecimal(node.PositionRatio)}";

    private int ResolveMeasurementCorridorDisplayNumber(MeasurementCorridorDto corridor)
    {
        if (corridor.SortOrder > 0)
        {
            return corridor.SortOrder;
        }

        var orderedCorridors = MeasurementCorridors
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.CorridorId)
            .ToArray();
        var index = Array.FindIndex(orderedCorridors, item => item.CorridorId == corridor.CorridorId);
        return index >= 0 ? index + 1 : 1;
    }

    private void SelectMeasurementCorridorForNode(MeasurementNodeDto? node)
    {
        if (node is null || SelectedMeasurementCorridor?.CorridorId == node.CorridorId)
        {
            return;
        }

        var corridor = MeasurementCorridors.FirstOrDefault(item => item.CorridorId == node.CorridorId);
        if (corridor is not null)
        {
            SelectedMeasurementCorridor = corridor;
        }
    }

    private void RaiseMeasurementNodeGroupOptionNotifications()
    {
        OnPropertyChanged(nameof(MeasurementNodeGroupOptions));
        OnPropertyChanged(nameof(SelectedMeasurementNodeGroupOption));
        OnPropertyChanged(nameof(SelectedMeasurementGroupNodeOptions));
        OnPropertyChanged(nameof(SelectedMeasurementGroupNodeOption));
        OnPropertyChanged(nameof(SelectedMeasurementStartNodeOption));
        OnPropertyChanged(nameof(SelectedMeasurementEndNodeOption));
    }

    private void SelectSavedMeasurementBindingForDimension(DimensionDto? dimension)
    {
        if (dimension is null)
        {
            return;
        }

        var binding = DimensionIntervalBindings.FirstOrDefault(item => item.DimensionId == dimension.DimensionId);
        if (binding is null)
        {
            return;
        }

        RestoreMeasurementBindingSelection(
            binding.CorridorId,
            binding.StartNodeId,
            binding.StartNodeId,
            binding.EndNodeId);
    }

    private void InvalidateMeasurementNodeGroupOptionCaches()
    {
        measurementNodeGroupOptionsCache = null;
        InvalidateSelectedMeasurementGroupNodeOptionCache();
    }

    private void InvalidateSelectedMeasurementGroupNodeOptionCache()
    {
        selectedMeasurementGroupNodeOptionsCache = null;
        selectedMeasurementGroupNodeOptionsCacheCorridorId = null;
    }

    private static string FormatMeasurementNodeDecimal(decimal value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private void RestoreMeasurementBindingSelection(
        Guid? corridorId,
        Guid? nodeId,
        Guid? startNodeId,
        Guid? endNodeId)
    {
        if (corridorId is Guid resolvedCorridorId)
        {
            SelectedMeasurementCorridor = MeasurementCorridors.FirstOrDefault(item => item.CorridorId == resolvedCorridorId);
        }

        if (nodeId is Guid resolvedNodeId)
        {
            SelectedMeasurementNode = MeasurementNodes.FirstOrDefault(item => item.NodeId == resolvedNodeId);
        }

        if (startNodeId is Guid resolvedStartNodeId)
        {
            SelectedMeasurementStartNode = MeasurementNodes.FirstOrDefault(item => item.NodeId == resolvedStartNodeId);
        }

        if (endNodeId is Guid resolvedEndNodeId)
        {
            SelectedMeasurementEndNode = MeasurementNodes.FirstOrDefault(item => item.NodeId == resolvedEndNodeId);
        }
    }

    private void TryAutoAssignMeasurementEndpoints()
    {
        if (SelectedMeasurementCorridor is null)
        {
            return;
        }

        var corridorNodes = SelectedMeasurementCorridorNodes;
        if (corridorNodes.Count != 2)
        {
            return;
        }

        var orderedNodes = corridorNodes
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.AxisCoordinate)
            .ToArray();
        var startNode = orderedNodes[0];
        var endNode = orderedNodes[1];

        if (SelectedMeasurementStartNode is null)
        {
            SelectedMeasurementStartNode = startNode;
        }

        if (SelectedMeasurementEndNode is null)
        {
            SelectedMeasurementEndNode = endNode;
        }
    }

    private void ApplySelectionPresentation(SelectionPresentationOutcome outcome)
    {
        ApplySelectionPresentationPlan(
            applyCoordinator.BuildSelectionPresentationApplyPlan(outcome, PinchGroups, WallCandidates));
    }

    private void ApplySelectionPresentationPlan(SelectionPresentationApplyPlan plan)
    {
        ApplySelectionClears(plan.ClearSelections);

        if (plan.SelectedPinchGroup is not null)
        {
            SelectedPinchGroup = plan.SelectedPinchGroup;
        }

        if (!string.IsNullOrWhiteSpace(plan.SelectedPinchAxis) &&
            !string.Equals(SelectedPinchAxis, plan.SelectedPinchAxis, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPinchAxis = plan.SelectedPinchAxis;
        }

        if (plan.LinkedCandidate is not null)
        {
            SelectedCandidate = plan.LinkedCandidate;
        }

        if (plan.HasHighlightGeometryPathId)
        {
            HighlightGeometryPathId = plan.HighlightGeometryPathId;
        }

        if (plan.HasPreviewSelectionLabel && plan.PreviewSelectionLabel is not null)
        {
            PreviewSelectionLabel = plan.PreviewSelectionLabel;
        }

        switch (plan.CuratedArtifactEditorAction)
        {
            case CuratedArtifactEditorAction.Sync when plan.CuratedArtifactEditorArtifact is not null:
                PopulateCuratedArtifactEditors(plan.CuratedArtifactEditorArtifact);
                break;
            case CuratedArtifactEditorAction.Clear:
                ResetCuratedArtifactEditors();
                break;
        }

        switch (plan.LabelTextHeightEditorAction)
        {
            case LabelTextHeightEditorAction.Sync:
                SetSelectedLabelTextHeightEditorValue(plan.LabelTextHeight);
                break;
            case LabelTextHeightEditorAction.Clear:
                ResetSelectedLabelTextHeightEditor();
                break;
        }
    }

    private void ApplySelectionClears(ReviewSelectionSlots clearSelections)
    {
        if (clearSelections.HasFlag(ReviewSelectionSlots.CuratedArtifact))
        {
            SelectedCuratedArtifact = null;
        }

        if (clearSelections.HasFlag(ReviewSelectionSlots.Candidate))
        {
            SelectedCandidate = null;
        }

        if (clearSelections.HasFlag(ReviewSelectionSlots.RoomLabel))
        {
            SelectedRoomLabel = null;
        }

        if (clearSelections.HasFlag(ReviewSelectionSlots.PinchMarker))
        {
            SelectedPinchMarker = null;
        }

        if (clearSelections.HasFlag(ReviewSelectionSlots.OpeningCandidate))
        {
            SelectedOpeningCandidate = null;
        }

        if (clearSelections.HasFlag(ReviewSelectionSlots.OpeningLabel))
        {
            SelectedOpeningLabel = null;
        }

        if (clearSelections.HasFlag(ReviewSelectionSlots.Dimension))
        {
            SelectedDimension = null;
        }

        if (clearSelections.HasFlag(ReviewSelectionSlots.FixedPlanComponent))
        {
            SelectedFixedPlanComponent = null;
        }

        if (clearSelections.HasFlag(ReviewSelectionSlots.ProtectedDetailAssembly))
        {
            SelectedProtectedDetailAssembly = null;
        }
    }

    private void PopulateCuratedArtifactEditors(CuratedPlanArtifactDto artifact)
    {
        isUpdatingCuratedArtifactEditors = true;
        EditableCuratedArtifactFamily = artifact.ResolvedFamily;
        ReplaceItems(EditableCuratedArtifactCategoryOptions, FloorPlanArtifactTaxonomy.GetCategories(artifact.ResolvedFamily));
        EditableCuratedArtifactCategory = artifact.ResolvedCategory;
        ReplaceItems(EditableCuratedArtifactTypeOptions, FloorPlanArtifactTaxonomy.GetTypes(artifact.ResolvedFamily, artifact.ResolvedCategory));
        EditableCuratedArtifactType = artifact.ResolvedType;
        isUpdatingCuratedArtifactEditors = false;
    }

    private void ResetCuratedArtifactEditors()
    {
        isUpdatingCuratedArtifactEditors = true;
        EditableCuratedArtifactFamily = string.Empty;
        EditableCuratedArtifactCategory = string.Empty;
        EditableCuratedArtifactType = string.Empty;
        EditableCuratedArtifactCategoryOptions.Clear();
        EditableCuratedArtifactTypeOptions.Clear();
        isUpdatingCuratedArtifactEditors = false;
    }

    private CuratedArtifactSelection? GetSelectedCuratedArtifactSelection()
    {
        return SelectedCuratedArtifact is null
            ? null
            : new CuratedArtifactSelection(SelectedCuratedArtifact.SourceArtifactKind, SelectedCuratedArtifact.SourceArtifactId);
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void SetSelectedLabelTextHeightEditorValue(decimal? textHeight)
    {
        EditableSelectedLabelTextHeight = textHeight?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void ResetSelectedLabelTextHeightEditor()
    {
        EditableSelectedLabelTextHeight = string.Empty;
    }

    private bool TryParseEditableSelectedLabelTextHeight(out decimal textHeight)
    {
        return decimal.TryParse(EditableSelectedLabelTextHeight.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out textHeight) &&
               textHeight > 0m;
    }

    private ReviewInspectorPresentation GetInspectorPresentation()
    {
        return inspectorCoordinator.BuildPresentation(
            SelectedCuratedArtifact,
            SelectedCandidate,
            SelectedRoomLabel,
            SelectedOpeningLabel,
            SelectedDimension,
            SelectedPinchGroup,
            SelectedPinchAxis,
            SelectedPinchGroupMarkers.Any(marker => string.Equals(marker.AxisTag, SelectedPinchAxis, StringComparison.OrdinalIgnoreCase)),
            IsPinchPlacementArmed,
            AddPinchButtonLabel,
            ExcludeSelectedArtifactLabel,
            GetSelectedDimensionAssociation());
    }

    private DimensionAssociationDto? GetSelectedDimensionAssociation()
    {
        return SelectedDimension is not null &&
               dimensionAssociationsById.TryGetValue(SelectedDimension.DimensionId, out var association)
            ? association
            : null;
    }

    private static string ResolveSelectedDimensionSourceKey(DimensionDto dimension)
    {
        return !string.IsNullOrWhiteSpace(dimension.SourceHandle)
            ? dimension.SourceHandle
            : dimension.SourceEntityRef;
    }

    private void RaiseUxNotifications()
    {
        var normalizedInspectorTool = notificationCoordinator.NormalizeSelectedInspectorTool(
            SelectedInspectorTool,
            CanUsePositionTool,
            CanUseTextTool,
            CanUseClassificationTool,
            CanUseActionsTool,
            InspectorToolOverview,
            InspectorToolPosition,
            InspectorToolText,
            InspectorToolClassification,
            InspectorToolActions);
        if (!string.Equals(SelectedInspectorTool, normalizedInspectorTool, StringComparison.Ordinal))
        {
            SelectedInspectorTool = normalizedInspectorTool;
        }
        RaisePropertyNotifications(notificationCoordinator.GetUxPropertyNames());
    }

    private void RaisePropertyNotifications(IEnumerable<string> propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    private readonly record struct CuratedArtifactSelection(string SourceArtifactKind, Guid SourceArtifactId);

}
