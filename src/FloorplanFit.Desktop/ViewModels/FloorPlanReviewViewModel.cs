using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
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

    private readonly IServiceScopeFactory scopeFactory;
    private readonly FloorPlanReviewMutationCoordinator mutationCoordinator;
    private readonly FloorPlanReviewSelectionCoordinator selectionCoordinator;
    private readonly Guid templateId;
    private readonly Guid? floorPlanVersionId;
    private IReadOnlyDictionary<Guid, DimensionAssociationDto> dimensionAssociationsById = new Dictionary<Guid, DimensionAssociationDto>();
    private bool isUpdatingCuratedArtifactEditors;

    public FloorPlanReviewViewModel(IServiceScopeFactory scopeFactory, Guid templateId)
        : this(scopeFactory, templateId, floorPlanVersionId: null)
    {
    }

    public FloorPlanReviewViewModel(IServiceScopeFactory scopeFactory, Guid templateId, Guid? floorPlanVersionId)
    {
        this.scopeFactory = scopeFactory;
        mutationCoordinator = new FloorPlanReviewMutationCoordinator(scopeFactory);
        selectionCoordinator = new FloorPlanReviewSelectionCoordinator();
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

    public ObservableCollection<DimensionAssociationDto> DimensionAssociations { get; } = [];

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
    private string newPinchMaxTrimMm = "120";

    [ObservableProperty]
    private bool isPinchPlacementArmed;

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
        ? "Cancel Add Pinch"
        : "Add Pinch";

    public Guid? SelectedPinchGroupId => SelectedPinchGroup?.PinchGroupId;

    public Guid? SelectedRoomLabelId => SelectedRoomLabel?.RoomLabelId;

    public Guid? SelectedOpeningLabelId => SelectedOpeningLabel?.OpeningLabelId;

    public Guid? SelectedDimensionId => SelectedDimension?.DimensionId;

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

    public bool CanUseTextTool => HasSelectedResizableLabel || SelectedDimension is not null;

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

    public string SelectedArtifactTypeLabel =>
        SelectedCuratedArtifact is not null ? "Curated Object" :
        SelectedCandidate is not null ? "Wall Candidate" :
        SelectedRoomLabel is not null ? "Room Name" :
        SelectedOpeningLabel is not null ? "Door / Window Code" :
        SelectedDimension is not null ? "Dimension" :
        "Nothing selected";

    public string SelectedArtifactTitle =>
        SelectedCuratedArtifact?.SourceEntityRef ??
        SelectedCandidate?.SourceEntityRef ??
        SelectedRoomLabel?.Text ??
        SelectedOpeningLabel?.Text ??
        SelectedDimension?.DisplayText ??
        "Select something from the review queue or preview.";

    public string SelectedArtifactSubtitle
    {
        get
        {
            if (SelectedCuratedArtifact is not null)
            {
                return $"Layer: {SelectedCuratedArtifact.SourceLayer} • {SelectedCuratedArtifact.ResolvedFamily} > {SelectedCuratedArtifact.ResolvedCategory} > {SelectedCuratedArtifact.ResolvedType}";
            }

            if (SelectedCandidate is not null)
            {
                return $"Layer: {SelectedCandidate.SourceLayer} • {SelectedCandidate.AssemblyHint}";
            }

            if (SelectedRoomLabel is not null)
            {
                return $"Layer: {SelectedRoomLabel.SourceLayer} • Ref: {SelectedRoomLabel.SourceEntityRef}";
            }

            if (SelectedOpeningLabel is not null)
            {
                return $"Layer: {SelectedOpeningLabel.SourceLayer} • {SelectedOpeningLabel.Kind}";
            }

            if (SelectedDimension is not null)
            {
                return $"Layer: {SelectedDimension.SourceLayer} • Ref: {SelectedDimension.SourceEntityRef}";
            }

            return "Everything is included by default. Exclude only false positives before publishing.";
        }
    }

    public string SelectedArtifactDetails
    {
        get
        {
            if (SelectedCuratedArtifact is not null)
            {
                return $"Detected: {SelectedCuratedArtifact.DetectedFamily} > {SelectedCuratedArtifact.DetectedCategory} > {SelectedCuratedArtifact.DetectedType} • Decision: {SelectedCuratedArtifact.DecisionState} • Confidence: {SelectedCuratedArtifact.Confidence:P0}";
            }

            if (SelectedCandidate is not null)
            {
                return $"Confidence: {SelectedCandidate.Confidence:P0}";
            }

            if (SelectedRoomLabel is not null)
            {
                return $"Confidence: {SelectedRoomLabel.Confidence:P0}";
            }

            if (SelectedOpeningLabel is not null)
            {
                return $"Confidence: {SelectedOpeningLabel.Confidence:P0}";
            }

            if (SelectedDimension is not null)
            {
                return $"Confidence: {SelectedDimension.Confidence:P0} • {SelectedDimension.SourceUnit} • {ResolveSelectedDimensionAssociationSummary()}";
            }

            return string.Empty;
        }
    }

    public string SelectedCuratedArtifactDetectedSummary => SelectedCuratedArtifact is null
        ? string.Empty
        : $"Detected: {SelectedCuratedArtifact.DetectedFamily} > {SelectedCuratedArtifact.DetectedCategory} > {SelectedCuratedArtifact.DetectedType}";

    public string SelectedCuratedArtifactResolvedSummary => SelectedCuratedArtifact is null
        ? string.Empty
        : $"Resolved: {SelectedCuratedArtifact.ResolvedFamily} > {SelectedCuratedArtifact.ResolvedCategory} > {SelectedCuratedArtifact.ResolvedType}";

    public string SelectedCuratedArtifactDecisionSummary => SelectedCuratedArtifact is null
        ? string.Empty
        : $"Decision: {SelectedCuratedArtifact.DecisionState}";

    public string SelectedCuratedArtifactColorArgb => SelectedCuratedArtifact?.ResolvedColorArgb ?? PreviewSemanticPalette.TransparentArgb;

    public string SelectedArtifactPositionSummary =>
        SelectedRoomLabel is not null
            ? $"Room label at ({SelectedRoomLabel.X}, {SelectedRoomLabel.Y}) mm. Detected: ({SelectedRoomLabel.DetectedX ?? SelectedRoomLabel.X}, {SelectedRoomLabel.DetectedY ?? SelectedRoomLabel.Y}) mm."
            : SelectedOpeningLabel is not null
                ? $"Opening label at ({SelectedOpeningLabel.X}, {SelectedOpeningLabel.Y}) mm. Detected: ({SelectedOpeningLabel.DetectedX ?? SelectedOpeningLabel.X}, {SelectedOpeningLabel.DetectedY ?? SelectedOpeningLabel.Y}) mm."
                : SelectedDimension is not null
                    ? $"Dimension defpoints: ({SelectedDimension.DefPointX}, {SelectedDimension.DefPointY}) -> ({SelectedDimension.DefPoint2X}, {SelectedDimension.DefPoint2Y}) • text anchor: ({SelectedDimension.RenderTextX ?? 0m}, {SelectedDimension.RenderTextY ?? 0m}) • {ResolveSelectedDimensionAssociationDebugSummary()} • dirty: {SelectedDimension.IsDirty}."
                : SelectedCuratedArtifact is not null
                    ? $"Curated translation dx/dy: ({SelectedCuratedArtifact.TranslationDx}, {SelectedCuratedArtifact.TranslationDy}) mm."
                    : string.Empty;

    public string SelectedLabelTextHeightSummary =>
        SelectedRoomLabel is not null
            ? $"Room label text height: {FormatTextHeight(SelectedRoomLabel.TextHeight)} mm. Detected: {FormatTextHeight(SelectedRoomLabel.DetectedTextHeight)} mm."
            : SelectedOpeningLabel is not null
                ? $"Opening label text height: {FormatTextHeight(SelectedOpeningLabel.TextHeight)} mm. Detected: {FormatTextHeight(SelectedOpeningLabel.DetectedTextHeight)} mm."
                : string.Empty;

    public string InteractionHint
    {
        get
        {
            if (SelectedCuratedArtifact is not null)
            {
                return string.Equals(
                    SelectedCuratedArtifact.DecisionState,
                    FloorPlanArtifactDecisionState.Excluded.ToString(),
                    StringComparison.Ordinal)
                    ? "This curated object is excluded from the active preview. Restore detected classification or save a new classification to bring it back into the curation set."
                    : $"Selected curated object {SelectedCuratedArtifact.SourceEntityRef}. Adjust Family, Category, and Type, then use 'Save Classification' to persist the correction or '{ExcludeSelectedArtifactLabel}' if this is a false positive.";
            }

            if (SelectedRoomLabel is not null)
            {
                return $"Selected room label {SelectedRoomLabel.Text}. Press '{ExcludeSelectedArtifactLabel}' if this label should not persist.";
            }

            if (SelectedOpeningLabel is not null)
            {
                return $"Selected opening label {SelectedOpeningLabel.Text}. Press '{ExcludeSelectedArtifactLabel}' if this label should not persist.";
            }

            if (SelectedPinchGroup is null)
            {
                return "Select an artifact to inspect it, or create/select a pinch group before placing pinches. Groups tell the fit engine which area may shrink together.";
            }

            if (SelectedCandidate is null)
            {
                return $"Select a line to place a {SelectedPinchGroup.Name} pinch. To preview only this group, drag the green {GetHandleHint()} handle.";
            }

            if (IsPinchPlacementArmed)
            {
                return $"Click on the preview to place a {SelectedPinchGroup.Name} pinch on {SelectedCandidate.SourceEntityRef}.";
            }

            return $"Selected {SelectedCandidate.SourceEntityRef}. Group: {SelectedPinchGroup.Name}. Press '{AddPinchButtonLabel}' to place a pinch, drag the green {GetHandleHint()} handle to preview, or use '{ExcludeSelectedArtifactLabel}' if this line is a false positive.";
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        StatusMessage = "Loading review session...";

        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OpenFloorPlanReviewSessionHandler>();
        var response = floorPlanVersionId is null
            ? await handler.HandleAsync(templateId, cancellationToken)
            : await handler.HandleAsync(templateId, floorPlanVersionId.Value, cancellationToken);

        DraftCurationId = response.DraftCurationId;
        ApplySession(response.Session, ReviewSelectionSnapshot.Empty);
        StatusMessage = $"Loaded review session for {Name}";
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

        StatusMessage = "Publishing curation...";
        await mutationCoordinator.PublishCurationAsync(templateId, DraftCurationId, cancellationToken);

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, GetSelectedCuratedArtifactSelection(), cancellationToken);
        StatusMessage = $"Published curation for {Name}";
    }

    public async Task AddPinchGroupAsync(CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        var groupName = NewPinchGroupName.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            StatusMessage = "Enter a group name before creating a pinch group.";
            return;
        }

        StatusMessage = $"Creating {groupName} pinch group...";
        var groupId = await mutationCoordinator.AddPinchGroupAsync(
            DraftCurationId,
            groupName,
            Enum.Parse<PinchAxisTag>(SelectedPinchAxis),
            cancellationToken);

        NewPinchGroupName = string.Empty;
        await RefreshSessionAsync(SelectedCandidate?.CandidateId, null, groupId, GetSelectedCuratedArtifactSelection(), cancellationToken);
        StatusMessage = $"Created {groupName} pinch group";
    }

    public void TogglePinchPlacement()
    {
        if (SelectedPinchGroup is null)
        {
            StatusMessage = "Create or select a pinch group before adding a pinch.";
            return;
        }

        if (SelectedCandidate is null)
        {
            StatusMessage = "Select a line before adding a pinch.";
            return;
        }

        IsPinchPlacementArmed = !IsPinchPlacementArmed;
        StatusMessage = IsPinchPlacementArmed
            ? $"Click the preview to place a {SelectedPinchGroup.Name} pinch."
            : "Add pinch cancelled.";
    }

    public async Task HandlePreviewInteractionAsync(Guid geometryPathId, decimal positionRatio, CancellationToken cancellationToken)
    {
        if (!SelectPreviewPath(geometryPathId))
        {
            return;
        }

        if (!IsPinchPlacementArmed || SelectedCandidate is null || SelectedPinchGroup is null)
        {
            return;
        }

        if (!decimal.TryParse(NewPinchMaxTrimMm.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var maxTrimMm) || maxTrimMm <= 0m)
        {
            StatusMessage = "Enter a valid positive max trim before placing a pinch.";
            return;
        }

        StatusMessage = $"Adding {SelectedPinchGroup.Name} pinch to {SelectedCandidate.SourceEntityRef}...";
        await mutationCoordinator.AddPinchMarkerAsync(
            DraftCurationId,
            SelectedCandidate.CandidateId,
            SelectedPinchGroup.PinchGroupId,
            positionRatio,
            maxTrimMm,
            cancellationToken);

        IsPinchPlacementArmed = false;
        await RefreshSessionAsync(SelectedCandidate.CandidateId, null, SelectedPinchGroup.PinchGroupId, preferredCuratedArtifact: null, cancellationToken);
        StatusMessage = $"Added {SelectedPinchGroup.Name} pinch to {SelectedCandidate.SourceEntityRef}";
    }

    public void SelectRoomLabel(Guid roomLabelId)
    {
        SelectedRoomLabel = RoomLabels.FirstOrDefault(item => item.RoomLabelId == roomLabelId);
    }

    public void SelectOpeningLabel(Guid openingLabelId)
    {
        SelectedOpeningLabel = OpeningLabels.FirstOrDefault(item => item.OpeningLabelId == openingLabelId);
    }

    public void SelectDimension(Guid dimensionId)
    {
        SelectedDimension = Dimensions.FirstOrDefault(item => item.DimensionId == dimensionId);
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
        await mutationCoordinator.SaveDimensionOverrideAsync(DraftCurationId, e.Dimension, cancellationToken);

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
        var response = await mutationCoordinator.ExportAdjustedDxfAsync(
            templateId,
            floorPlanVersionId,
            DraftCurationId,
            cancellationToken);
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
        StatusMessage = $"Adjusted DXF exported: {Path.GetFileName(response.ManagedFilePath)}";
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

    partial void OnSelectedCandidateChanged(WallCandidateDto? value)
    {
        ApplySelectionPresentationOutcome(
            selectionCoordinator.ResolveCandidatePresentation(value, SelectedPinchMarker, SelectedPinchAxis));
        NotifyUxStateChanged();
    }

    partial void OnSelectedCuratedArtifactChanged(CuratedPlanArtifactDto? value)
    {
        ApplySelectionPresentationOutcome(selectionCoordinator.ResolveCuratedArtifactPresentation(value));
        NotifyUxStateChanged();
    }

    partial void OnSelectedRoomLabelChanged(RoomLabelDto? value)
    {
        ApplySelectionPresentationOutcome(selectionCoordinator.ResolveRoomLabelPresentation(value));
        NotifyUxStateChanged();
    }

    partial void OnSelectedPinchMarkerChanged(PinchMarkerDto? value)
    {
        ApplySelectionPresentationOutcome(selectionCoordinator.ResolvePinchMarkerPresentation(value));
        NotifyUxStateChanged();
    }

    partial void OnSelectedOpeningCandidateChanged(OpeningCandidateDto? value)
    {
        ApplySelectionPresentationOutcome(selectionCoordinator.ResolveOpeningCandidatePresentation(value));
        NotifyUxStateChanged();
    }

    partial void OnSelectedOpeningLabelChanged(OpeningLabelDto? value)
    {
        ApplySelectionPresentationOutcome(selectionCoordinator.ResolveOpeningLabelPresentation(value));
        NotifyUxStateChanged();
    }

    partial void OnSelectedFixedPlanComponentChanged(FixedPlanComponentDto? value)
    {
        ApplySelectionPresentationOutcome(selectionCoordinator.ResolveFixedPlanComponentPresentation(value));
        NotifyUxStateChanged();
    }

    partial void OnSelectedProtectedDetailAssemblyChanged(ProtectedDetailAssemblyDto? value)
    {
        ApplySelectionPresentationOutcome(selectionCoordinator.ResolveProtectedDetailAssemblyPresentation(value));
        NotifyUxStateChanged();
    }

    partial void OnSelectedDimensionChanged(DimensionDto? value)
    {
        ApplySelectionPresentationOutcome(selectionCoordinator.ResolveDimensionPresentation(value));
        NotifyUxStateChanged();
    }

    partial void OnSelectedPinchGroupChanged(PinchGroupDto? value)
    {
        if (value is not null && !string.Equals(SelectedPinchAxis, value.AxisTag, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPinchAxis = value.AxisTag;
        }

        OnPropertyChanged(nameof(SelectedPinchGroupId));
        NotifyUxStateChanged();
    }

    partial void OnSelectedPinchAxisChanged(string value)
    {
        NotifyUxStateChanged();
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

        NotifyUxStateChanged();
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

        NotifyUxStateChanged();
    }

    partial void OnEditableCuratedArtifactTypeChanged(string value)
    {
        NotifyUxStateChanged();
    }

    partial void OnEditableSelectedLabelTextHeightChanged(string value)
    {
        NotifyUxStateChanged();
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
            CollapseQueueSectionsExcept(nameof(IsStructureQueueExpanded));
        }
    }

    partial void OnIsRoomNamesQueueExpandedChanged(bool value)
    {
        if (value)
        {
            CollapseQueueSectionsExcept(nameof(IsRoomNamesQueueExpanded));
        }
    }

    partial void OnIsOpeningCodesQueueExpandedChanged(bool value)
    {
        if (value)
        {
            CollapseQueueSectionsExcept(nameof(IsOpeningCodesQueueExpanded));
        }
    }

    partial void OnIsDimensionsQueueExpandedChanged(bool value)
    {
        if (value)
        {
            CollapseQueueSectionsExcept(nameof(IsDimensionsQueueExpanded));
        }
    }

    partial void OnIsCuratedObjectsQueueExpandedChanged(bool value)
    {
        if (value)
        {
            CollapseQueueSectionsExcept(nameof(IsCuratedObjectsQueueExpanded));
        }
    }

    partial void OnSelectedInspectorToolChanged(string value)
    {
        NotifyUxStateChanged();
    }

    partial void OnIsPinchPlacementArmedChanged(bool value)
    {
        NotifyUxStateChanged();
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
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFloorPlanReviewSessionHandler>();
        FloorPlanReviewSessionDto? session = floorPlanVersionId is null
            ? await handler.HandleAsync(templateId, cancellationToken)
            : await handler.HandleAsync(templateId, floorPlanVersionId.Value, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException("Floor plan review session was not found.");
        }

        DraftCurationId = string.Equals(session.Status, "Published", StringComparison.OrdinalIgnoreCase)
            ? Guid.Empty
            : DraftCurationId;
        ApplySession(session, selection);
    }

    private void ApplySession(FloorPlanReviewSessionDto session, ReviewSelectionSnapshot selection)
    {
        Code = session.Code;
        Name = session.Name;
        Status = session.Status;
        ActiveVersionNumber = session.ActiveVersionNumber;
        ActivePublishedCurationId = session.ActivePublishedCurationId;

        ReplaceItems(GeometryPaths, session.GeometryPaths);
        ReplaceItems(RoomLabels, session.RoomLabels);
        ReplaceItems(OpeningCandidates, session.OpeningCandidates);
        ReplaceItems(OpeningLabels, session.OpeningLabels);
        ReplaceItems(Dimensions, session.Dimensions);
        ReplaceItems(DimensionAssociations, session.DimensionAssociations);
        ReplaceItems(FixedPlanComponents, session.FixedPlanComponents);
        ReplaceItems(ProtectedDetailAssemblies, session.ProtectedDetailAssemblies);
        ReplaceItems(WallCandidates, session.WallCandidates);
        ReplaceItems(PinchGroups, session.PinchGroups);
        ReplaceItems(PinchMarkers, session.PinchMarkers);
        ReplaceItems(CuratedPlanArtifacts, ResolveCuratedArtifacts(session));
        dimensionAssociationsById = session.DimensionAssociations.ToDictionary(item => item.DimensionId);
        RefreshVisibleCuratedArtifacts();
        RefreshReviewQueue();

        OnPropertyChanged(nameof(DoorOpeningCount));
        OnPropertyChanged(nameof(WindowOpeningCount));
        OnPropertyChanged(nameof(FixedPlanComponentCount));
        OnPropertyChanged(nameof(ProtectedDetailAssemblyCount));
        OnPropertyChanged(nameof(RoomLabelCount));
        OnPropertyChanged(nameof(OpeningLabelCount));
        OnPropertyChanged(nameof(DimensionCount));

        var resolvedSelection = selectionCoordinator.ResolveSelectionSnapshot(
            selection,
            WallCandidates,
            PinchMarkers,
            PinchGroups,
            RoomLabels,
            OpeningLabels,
            Dimensions,
            CuratedPlanArtifacts);

        SelectedCandidate = resolvedSelection.Candidate;
        SelectedPinchMarker = resolvedSelection.PinchMarker;
        SelectedPinchGroup = resolvedSelection.PinchGroup;

        if (resolvedSelection.CuratedArtifact is not null)
        {
            SelectedCuratedArtifact = resolvedSelection.CuratedArtifact;
            return;
        }

        if (resolvedSelection.OpeningLabel is not null)
        {
            SelectedOpeningLabel = resolvedSelection.OpeningLabel;
            return;
        }

        if (resolvedSelection.Dimension is not null)
        {
            SelectedDimension = resolvedSelection.Dimension;
            return;
        }

        if (resolvedSelection.RoomLabel is not null)
        {
            SelectedRoomLabel = resolvedSelection.RoomLabel;
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

    private IReadOnlyList<CuratedPlanArtifactDto> ResolveCuratedArtifacts(FloorPlanReviewSessionDto session)
    {
        if (session.CuratedPlanArtifacts.Count > 0)
        {
            return SortCuratedArtifacts(session.CuratedPlanArtifacts);
        }

        var items = new List<CuratedPlanArtifactDto>();
        items.AddRange(session.OpeningCandidates.Select(CreateDetectedCuratedArtifact));
        items.AddRange(session.FixedPlanComponents.Select(CreateDetectedCuratedArtifact));
        items.AddRange(session.ProtectedDetailAssemblies.Select(CreateDetectedCuratedArtifact));
        return SortCuratedArtifacts(items);
    }

    private void RefreshVisibleCuratedArtifacts()
    {
        var visible = CuratedPlanArtifacts
            .Where(item => !string.Equals(item.DecisionState, FloorPlanArtifactDecisionState.Excluded.ToString(), StringComparison.Ordinal))
            .ToArray();
        ReplaceItems(VisibleCuratedPlanArtifacts, visible);
    }

    private void RefreshReviewQueue()
    {
        ReplaceItems(
            VisibleWallCandidates,
            WallCandidates
                .Where(ShouldIncludeWallCandidateInQueue)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase));

        ReplaceItems(
            VisibleRoomLabels,
            RoomLabels
                .Where(ShouldIncludeRoomLabelInQueue)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Text, StringComparer.OrdinalIgnoreCase));

        ReplaceItems(
            VisibleOpeningLabels,
            OpeningLabels
                .Where(ShouldIncludeOpeningLabelInQueue)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Text, StringComparer.OrdinalIgnoreCase));

        ReplaceItems(
            VisibleDimensions,
            Dimensions
                .Where(ShouldIncludeDimensionInQueue)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.DisplayText, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase));

        CuratedArtifactGroups.Clear();
        foreach (var group in VisibleCuratedPlanArtifacts
                     .Where(ShouldIncludeCuratedArtifactInQueue)
                     .GroupBy(item => (item.ResolvedFamily, item.ResolvedCategory))
                     .OrderBy(group => FloorPlanArtifactTaxonomy.ResolveFamilySortOrder(group.Key.ResolvedFamily))
                     .ThenBy(group => FloorPlanArtifactTaxonomy.ResolveCategorySortOrder(group.Key.ResolvedFamily, group.Key.ResolvedCategory)))
        {
            CuratedArtifactGroups.Add(new CuratedArtifactGroupViewModel(
                group.Key.ResolvedFamily,
                group.Key.ResolvedCategory,
                FloorPlanReviewDisplayText.GetCuratedGroupTitle(group.Key.ResolvedFamily, group.Key.ResolvedCategory),
                FloorPlanReviewDisplayText.GetCuratedGroupSubtitle(group.Key.ResolvedFamily, group.Key.ResolvedCategory),
                group.First().ResolvedColorArgb,
                group.OrderBy(item => item.SortOrder).ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        NormalizeQueueExpansion();

        OnPropertyChanged(nameof(CuratedObjectCount));
        OnPropertyChanged(nameof(VisibleQueueItemCount));
        OnPropertyChanged(nameof(TotalQueueItemCount));
        OnPropertyChanged(nameof(AdjustedQueueItemCount));
        OnPropertyChanged(nameof(QueueSummary));
        OnPropertyChanged(nameof(QueueInsightsSummary));
        OnPropertyChanged(nameof(StructureSectionTitle));
        OnPropertyChanged(nameof(RoomNamesSectionTitle));
        OnPropertyChanged(nameof(OpeningCodesSectionTitle));
        OnPropertyChanged(nameof(DimensionsSectionTitle));
        OnPropertyChanged(nameof(CuratedObjectsSectionTitle));
        OnPropertyChanged(nameof(HasVisibleWallCandidates));
        OnPropertyChanged(nameof(HasVisibleRoomLabels));
        OnPropertyChanged(nameof(HasVisibleOpeningLabels));
        OnPropertyChanged(nameof(HasVisibleDimensions));
        OnPropertyChanged(nameof(HasVisibleCuratedArtifactGroups));
    }

    private void NormalizeQueueExpansion()
    {
        if (IsStructureQueueExpanded && !HasVisibleWallCandidates)
        {
            IsStructureQueueExpanded = false;
        }

        if (IsRoomNamesQueueExpanded && !HasVisibleRoomLabels)
        {
            IsRoomNamesQueueExpanded = false;
        }

        if (IsOpeningCodesQueueExpanded && !HasVisibleOpeningLabels)
        {
            IsOpeningCodesQueueExpanded = false;
        }

        if (IsDimensionsQueueExpanded && !HasVisibleDimensions)
        {
            IsDimensionsQueueExpanded = false;
        }

        if (IsCuratedObjectsQueueExpanded && !HasVisibleCuratedArtifactGroups)
        {
            IsCuratedObjectsQueueExpanded = false;
        }

        if (IsStructureQueueExpanded || IsRoomNamesQueueExpanded || IsOpeningCodesQueueExpanded || IsDimensionsQueueExpanded || IsCuratedObjectsQueueExpanded)
        {
            return;
        }

        if (HasVisibleCuratedArtifactGroups)
        {
            IsCuratedObjectsQueueExpanded = true;
            return;
        }

        if (HasVisibleRoomLabels)
        {
            IsRoomNamesQueueExpanded = true;
            return;
        }

        if (HasVisibleOpeningLabels)
        {
            IsOpeningCodesQueueExpanded = true;
            return;
        }

        if (HasVisibleDimensions)
        {
            IsDimensionsQueueExpanded = true;
            return;
        }

        if (HasVisibleWallCandidates)
        {
            IsStructureQueueExpanded = true;
        }
    }

    private void CollapseQueueSectionsExcept(string expandedPropertyName)
    {
        if (!string.Equals(expandedPropertyName, nameof(IsStructureQueueExpanded), StringComparison.Ordinal))
        {
            IsStructureQueueExpanded = false;
        }

        if (!string.Equals(expandedPropertyName, nameof(IsRoomNamesQueueExpanded), StringComparison.Ordinal))
        {
            IsRoomNamesQueueExpanded = false;
        }

        if (!string.Equals(expandedPropertyName, nameof(IsOpeningCodesQueueExpanded), StringComparison.Ordinal))
        {
            IsOpeningCodesQueueExpanded = false;
        }

        if (!string.Equals(expandedPropertyName, nameof(IsDimensionsQueueExpanded), StringComparison.Ordinal))
        {
            IsDimensionsQueueExpanded = false;
        }

        if (!string.Equals(expandedPropertyName, nameof(IsCuratedObjectsQueueExpanded), StringComparison.Ordinal))
        {
            IsCuratedObjectsQueueExpanded = false;
        }
    }

    private bool ShouldIncludeWallCandidateInQueue(WallCandidateDto candidate)
    {
        if (!MatchesReviewQueueFilter("Structure", changed: false))
        {
            return false;
        }

        return MatchesReviewQueueSearch(candidate.SourceEntityRef, candidate.SourceLayer, candidate.AssemblyHint);
    }

    private bool ShouldIncludeRoomLabelInQueue(RoomLabelDto label)
    {
        var changed = label.HasManualPosition || label.HasManualTextHeight;
        if (!MatchesReviewQueueFilter("Text & Notes", changed))
        {
            return false;
        }

        return MatchesReviewQueueSearch(label.Text, label.SourceEntityRef, label.SourceLayer);
    }

    private bool ShouldIncludeOpeningLabelInQueue(OpeningLabelDto label)
    {
        var changed = label.HasManualPosition || label.HasManualTextHeight;
        if (!MatchesReviewQueueFilter("Text & Notes", changed))
        {
            return false;
        }

        return MatchesReviewQueueSearch(label.Text, label.Kind, label.SourceEntityRef, label.SourceLayer);
    }

    private bool ShouldIncludeDimensionInQueue(DimensionDto dimension)
    {
        if (!MatchesReviewQueueFilter("Dimensions", changed: false))
        {
            return false;
        }

        return MatchesReviewQueueSearch(
            dimension.DisplayText,
            dimension.SourceEntityRef,
            dimension.SourceLayer,
            dimension.GeometryBlockName,
            dimension.RawTextOverride,
            dimension.SourceEntityKind);
    }

    private bool ShouldIncludeCuratedArtifactInQueue(CuratedPlanArtifactDto artifact)
    {
        var changed =
            artifact.HasManualPosition ||
            !string.Equals(artifact.DecisionState, FloorPlanArtifactDecisionState.DetectedDefault.ToString(), StringComparison.Ordinal);

        if (!MatchesReviewQueueFilter("Curated Objects", changed))
        {
            return false;
        }

        return MatchesReviewQueueSearch(
            artifact.SourceEntityRef,
            artifact.SourceLayer,
            artifact.ResolvedFamily,
            artifact.ResolvedCategory,
            artifact.ResolvedType,
            artifact.SourceBlockName,
            artifact.SourceEntityKind);
    }

    private bool MatchesReviewQueueFilter(string bucket, bool changed)
    {
        return SelectedReviewQueueFilter switch
        {
            "Everything" => true,
            "Changed only" => changed,
            "Text & Notes" => string.Equals(bucket, "Text & Notes", StringComparison.Ordinal),
            "Dimensions" => string.Equals(bucket, "Dimensions", StringComparison.Ordinal),
            "Curated Objects" => string.Equals(bucket, "Curated Objects", StringComparison.Ordinal),
            "Structure" => string.Equals(bucket, "Structure", StringComparison.Ordinal),
            _ => true
        };
    }

    private bool MatchesReviewQueueSearch(params string?[] values)
    {
        var search = ReviewQueueSearchText.Trim();
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains(search, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplySelectionPresentationOutcome(SelectionPresentationOutcome outcome)
    {
        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.CuratedArtifact))
        {
            SelectedCuratedArtifact = null;
        }

        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.Candidate))
        {
            SelectedCandidate = null;
        }

        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.RoomLabel))
        {
            SelectedRoomLabel = null;
        }

        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.PinchMarker))
        {
            SelectedPinchMarker = null;
        }

        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.OpeningCandidate))
        {
            SelectedOpeningCandidate = null;
        }

        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.OpeningLabel))
        {
            SelectedOpeningLabel = null;
        }

        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.Dimension))
        {
            SelectedDimension = null;
        }

        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.FixedPlanComponent))
        {
            SelectedFixedPlanComponent = null;
        }

        if (outcome.ClearSelections.HasFlag(ReviewSelectionSlots.ProtectedDetailAssembly))
        {
            SelectedProtectedDetailAssembly = null;
        }

        if (!string.IsNullOrWhiteSpace(outcome.SelectedPinchAxis) &&
            !string.Equals(SelectedPinchAxis, outcome.SelectedPinchAxis, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPinchAxis = outcome.SelectedPinchAxis;
        }

        if (outcome.SelectedPinchGroupId is Guid pinchGroupId)
        {
            var pinchGroup = PinchGroups.FirstOrDefault(item => item.PinchGroupId == pinchGroupId);
            if (pinchGroup is not null)
            {
                SelectedPinchGroup = pinchGroup;
            }
        }

        if (outcome.LinkedCandidateId is Guid candidateId)
        {
            var sourceCandidate = WallCandidates.FirstOrDefault(item => item.CandidateId == candidateId);
            if (sourceCandidate is not null)
            {
                SelectedCandidate = sourceCandidate;
            }
        }

        if (outcome.HasHighlightGeometryPathId)
        {
            HighlightGeometryPathId = outcome.HighlightGeometryPathId;
        }

        if (outcome.HasPreviewSelectionLabel && outcome.PreviewSelectionLabel is not null)
        {
            PreviewSelectionLabel = outcome.PreviewSelectionLabel;
        }

        switch (outcome.CuratedArtifactEditorAction)
        {
            case CuratedArtifactEditorAction.Sync when outcome.CuratedArtifactEditorArtifact is not null:
                PopulateCuratedArtifactEditors(outcome.CuratedArtifactEditorArtifact);
                break;
            case CuratedArtifactEditorAction.Clear:
                ResetCuratedArtifactEditors();
                break;
        }

        switch (outcome.LabelTextHeightEditorAction)
        {
            case LabelTextHeightEditorAction.Sync:
                SetSelectedLabelTextHeightEditorValue(outcome.LabelTextHeight);
                break;
            case LabelTextHeightEditorAction.Clear:
                ResetSelectedLabelTextHeightEditor();
                break;
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

    private static CuratedPlanArtifactDto CreateDetectedCuratedArtifact(OpeningCandidateDto artifact)
    {
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedOpeningClassification(artifact.Kind);
        return new CuratedPlanArtifactDto(
            artifact.OpeningCandidateId,
            FloorPlanArtifactSourceKinds.OpeningCandidate,
            artifact.SourceEntityRef,
            artifact.SourceLayer,
            artifact.SourceEntityKind,
            null,
            artifact.GeometryPathId is null ? [] : [artifact.GeometryPathId.Value],
            artifact.Confidence,
            artifact.DetectionNotes,
            artifact.SortOrder,
            detected.Family,
            detected.Category,
            detected.Type,
            detected.Family,
            detected.Category,
            detected.Type,
            FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
            FloorPlanArtifactTaxonomy.ResolveColorArgb(detected.Family, detected.Category, detected.Type));
    }

    private static CuratedPlanArtifactDto CreateDetectedCuratedArtifact(FixedPlanComponentDto artifact)
    {
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedFixedClassification(artifact.Kind);
        return new CuratedPlanArtifactDto(
            artifact.FixedPlanComponentId,
            FloorPlanArtifactSourceKinds.FixedPlanComponent,
            artifact.SourceEntityRef,
            artifact.SourceLayer,
            artifact.SourceEntityKind,
            artifact.SourceBlockName,
            artifact.GeometryPathIds,
            artifact.Confidence,
            artifact.DetectionNotes,
            artifact.SortOrder,
            detected.Family,
            detected.Category,
            detected.Type,
            detected.Family,
            detected.Category,
            detected.Type,
            FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
            FloorPlanArtifactTaxonomy.ResolveColorArgb(detected.Family, detected.Category, detected.Type));
    }

    private static CuratedPlanArtifactDto CreateDetectedCuratedArtifact(ProtectedDetailAssemblyDto artifact)
    {
        var detected = FloorPlanArtifactTaxonomy.ResolveDetectedProtectedClassification(artifact.Kind);
        return new CuratedPlanArtifactDto(
            artifact.ProtectedDetailAssemblyId,
            FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
            artifact.SourceEntityRef,
            artifact.SourceLayer,
            artifact.SourceEntityKind,
            null,
            artifact.GeometryPathIds,
            artifact.Confidence,
            artifact.DetectionNotes,
            artifact.SortOrder,
            detected.Family,
            detected.Category,
            detected.Type,
            detected.Family,
            detected.Category,
            detected.Type,
            FloorPlanArtifactDecisionState.DetectedDefault.ToString(),
            FloorPlanArtifactTaxonomy.ResolveColorArgb(detected.Family, detected.Category, detected.Type));
    }

    private static IReadOnlyList<CuratedPlanArtifactDto> SortCuratedArtifacts(IEnumerable<CuratedPlanArtifactDto> artifacts)
    {
        return artifacts
            .OrderBy(item => FloorPlanArtifactTaxonomy.ResolveFamilySortOrder(item.ResolvedFamily))
            .ThenBy(item => FloorPlanArtifactTaxonomy.ResolveCategorySortOrder(item.ResolvedFamily, item.ResolvedCategory))
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

    private static string ResolveSelectedDimensionSourceKey(DimensionDto dimension)
    {
        return !string.IsNullOrWhiteSpace(dimension.SourceHandle)
            ? dimension.SourceHandle
            : dimension.SourceEntityRef;
    }

    private string ResolveSelectedDimensionAssociationSummary()
    {
        if (SelectedDimension is null ||
            !dimensionAssociationsById.TryGetValue(SelectedDimension.DimensionId, out var association))
        {
            return "Association: unresolved";
        }

        var start = association.StartAnchor?.SourceArtifactKind ?? "?";
        var end = association.EndAnchor?.SourceArtifactKind ?? "?";
        return association.IsFullyResolved
            ? $"Association: {start} -> {end} ({association.Confidence:P0})"
            : $"Association: partial ({association.Confidence:P0})";
    }

    private string ResolveSelectedDimensionAssociationDebugSummary()
    {
        if (SelectedDimension is null ||
            !dimensionAssociationsById.TryGetValue(SelectedDimension.DimensionId, out var association))
        {
            return "assoc: unresolved";
        }

        var start = association.StartAnchor is null
            ? "start=?"
            : $"start={association.StartAnchor.EdgeKey}/{association.StartAnchor.EdgeAnchorKind}";
        var end = association.EndAnchor is null
            ? "end=?"
            : $"end={association.EndAnchor.EdgeKey}/{association.EndAnchor.EdgeAnchorKind}";
        return $"assoc: {start} • {end}";
    }

    private static string FormatTextHeight(decimal? textHeight)
    {
        return textHeight?.ToString(CultureInfo.InvariantCulture) ?? "Auto";
    }

    private void NotifyUxStateChanged()
    {
        NormalizeSelectedInspectorTool();
        OnPropertyChanged(nameof(AddPinchButtonLabel));
        OnPropertyChanged(nameof(SelectedPinchGroupId));
        OnPropertyChanged(nameof(InteractionHint));
        OnPropertyChanged(nameof(HasSelectedArtifact));
        OnPropertyChanged(nameof(HasSelectedCuratedArtifact));
        OnPropertyChanged(nameof(CanUsePositionTool));
        OnPropertyChanged(nameof(CanUseTextTool));
        OnPropertyChanged(nameof(CanUseClassificationTool));
        OnPropertyChanged(nameof(CanUseActionsTool));
        OnPropertyChanged(nameof(IsOverviewToolSelected));
        OnPropertyChanged(nameof(IsPositionToolSelected));
        OnPropertyChanged(nameof(IsTextToolSelected));
        OnPropertyChanged(nameof(IsClassificationToolSelected));
        OnPropertyChanged(nameof(IsActionsToolSelected));
        OnPropertyChanged(nameof(IsFitToolSelected));
        OnPropertyChanged(nameof(CanSaveSelectedCuratedArtifactClassification));
        OnPropertyChanged(nameof(CanRestoreSelectedCuratedArtifactClassification));
        OnPropertyChanged(nameof(SelectedArtifactTypeLabel));
        OnPropertyChanged(nameof(SelectedArtifactTitle));
        OnPropertyChanged(nameof(SelectedArtifactSubtitle));
        OnPropertyChanged(nameof(SelectedArtifactDetails));
        OnPropertyChanged(nameof(SelectedCuratedArtifactDetectedSummary));
        OnPropertyChanged(nameof(SelectedCuratedArtifactResolvedSummary));
        OnPropertyChanged(nameof(SelectedCuratedArtifactDecisionSummary));
        OnPropertyChanged(nameof(SelectedCuratedArtifactColorArgb));
        OnPropertyChanged(nameof(CanRestoreSelectedArtifactPosition));
        OnPropertyChanged(nameof(SelectedArtifactPositionSummary));
        OnPropertyChanged(nameof(HasSelectedResizableLabel));
        OnPropertyChanged(nameof(CanSaveSelectedLabelTextHeight));
        OnPropertyChanged(nameof(CanRestoreSelectedLabelTextHeight));
        OnPropertyChanged(nameof(SelectedLabelTextHeightSummary));
        OnPropertyChanged(nameof(SelectedRoomLabelId));
        OnPropertyChanged(nameof(SelectedOpeningLabelId));
        OnPropertyChanged(nameof(SelectedDimensionId));
        OnPropertyChanged(nameof(ExcludeSelectedArtifactLabel));
    }

    private void NormalizeSelectedInspectorTool()
    {
        if (IsPositionToolSelected && !CanUsePositionTool)
        {
            SelectedInspectorTool = InspectorToolOverview;
            return;
        }

        if (IsTextToolSelected && !CanUseTextTool)
        {
            SelectedInspectorTool = InspectorToolOverview;
            return;
        }

        if (IsClassificationToolSelected && !CanUseClassificationTool)
        {
            SelectedInspectorTool = InspectorToolOverview;
            return;
        }

        if (IsActionsToolSelected && !CanUseActionsTool)
        {
            SelectedInspectorTool = InspectorToolOverview;
        }
    }

    private string GetHandleHint()
    {
        return string.Equals(SelectedPinchAxis, nameof(PinchAxisTag.Height), StringComparison.OrdinalIgnoreCase)
            ? "top or bottom"
            : "left or right";
    }

    private readonly record struct CuratedArtifactSelection(string SourceArtifactKind, Guid SourceArtifactId);

}
