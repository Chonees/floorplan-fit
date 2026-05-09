using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.ViewModels;

public sealed partial class FloorPlanReviewViewModel : ObservableObject
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly Guid templateId;

    public FloorPlanReviewViewModel(IServiceScopeFactory scopeFactory, Guid templateId)
    {
        this.scopeFactory = scopeFactory;
        this.templateId = templateId;
    }

    public ObservableCollection<WallCandidateDto> WallCandidates { get; } = [];

    public ObservableCollection<GeometryPathDto> GeometryPaths { get; } = [];

    public ObservableCollection<RoomLabelDto> RoomLabels { get; } = [];

    public ObservableCollection<OpeningCandidateDto> OpeningCandidates { get; } = [];

    public ObservableCollection<OpeningLabelDto> OpeningLabels { get; } = [];

    public ObservableCollection<FixedPlanComponentDto> FixedPlanComponents { get; } = [];

    public ObservableCollection<ProtectedDetailAssemblyDto> ProtectedDetailAssemblies { get; } = [];

    public ObservableCollection<PinchGroupDto> PinchGroups { get; } = [];

    public ObservableCollection<PinchMarkerDto> PinchMarkers { get; } = [];

    public IReadOnlyList<string> PinchAxisOptions { get; } = Enum.GetNames<PinchAxisTag>();

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
    private PinchMarkerDto? selectedPinchMarker;

    [ObservableProperty]
    private OpeningCandidateDto? selectedOpeningCandidate;

    [ObservableProperty]
    private OpeningLabelDto? selectedOpeningLabel;

    [ObservableProperty]
    private FixedPlanComponentDto? selectedFixedPlanComponent;

    [ObservableProperty]
    private ProtectedDetailAssemblyDto? selectedProtectedDetailAssembly;

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

    public string AddPinchButtonLabel => IsPinchPlacementArmed
        ? "Cancel Add Pinch"
        : "Add Pinch";

    public Guid? SelectedPinchGroupId => SelectedPinchGroup?.PinchGroupId;

    public int DoorOpeningCount => OpeningCandidates.Count(item => string.Equals(item.Kind, "Door", StringComparison.OrdinalIgnoreCase));

    public int WindowOpeningCount => OpeningCandidates.Count(item => string.Equals(item.Kind, "Window", StringComparison.OrdinalIgnoreCase));

    public int FixedPlanComponentCount => FixedPlanComponents.Count;

    public int ProtectedDetailAssemblyCount => ProtectedDetailAssemblies.Count;

    public string InteractionHint
    {
        get
        {
            if (SelectedOpeningCandidate is not null)
            {
                return $"Selected opening {SelectedOpeningCandidate.SourceEntityRef}. Press 'Remove Selected Opening' if this is a false positive.";
            }

            if (SelectedOpeningLabel is not null)
            {
                return $"Selected opening label {SelectedOpeningLabel.Text}. Press 'Remove Selected Label' if this label should not persist.";
            }

            if (SelectedFixedPlanComponent is not null)
            {
                return $"Selected {SelectedFixedPlanComponent.Kind} component {SelectedFixedPlanComponent.SourceEntityRef}. Press 'Remove Selected Component' if this is a false positive.";
            }

            if (SelectedProtectedDetailAssembly is not null)
            {
                return $"Selected protected detail {SelectedProtectedDetailAssembly.SourceEntityRef}. Press 'Remove Selected Detail' if this should not persist.";
            }

            if (SelectedPinchGroup is null)
            {
                return "Create or select a pinch group before placing pinches. Groups tell the fit engine which area may shrink together.";
            }

            if (SelectedCandidate is null)
            {
                return $"Select a line to place a {SelectedPinchGroup.Name} pinch. To preview only this group, drag the green {GetHandleHint()} handle.";
            }

            if (IsPinchPlacementArmed)
            {
                return $"Click on the preview to place a {SelectedPinchGroup.Name} pinch on {SelectedCandidate.SourceEntityRef}.";
            }

            return $"Selected {SelectedCandidate.SourceEntityRef}. Group: {SelectedPinchGroup.Name}. Press '{AddPinchButtonLabel}' or preview this group with the green {GetHandleHint()} handle.";
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        StatusMessage = "Loading review session...";

        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OpenFloorPlanReviewSessionHandler>();
        var response = await handler.HandleAsync(templateId, cancellationToken);

        DraftCurationId = response.DraftCurationId;
        ApplySession(response.Session, preferredCandidateId: null, preferredPinchMarkerId: null);
        StatusMessage = $"Loaded review session for {Name}";
    }

    public async Task RejectSelectedCandidateAsync(CancellationToken cancellationToken)
    {
        if (SelectedCandidate is null || DraftCurationId == Guid.Empty || !string.Equals(SelectedCandidate.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        StatusMessage = $"Rejecting {SelectedCandidate.SourceEntityRef}...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<RejectWallCandidateHandler>();
            await handler.HandleAsync(DraftCurationId, SelectedCandidate.CandidateId, cancellationToken);
        }

        await RefreshSessionAsync(null, null, SelectedPinchGroup?.PinchGroupId, cancellationToken);
        StatusMessage = $"Rejected {SelectedCandidate.SourceEntityRef}";
    }

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        if (DraftCurationId == Guid.Empty)
        {
            return;
        }

        StatusMessage = "Publishing curation...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<PublishFloorPlanCurationHandler>();
            await handler.HandleAsync(templateId, DraftCurationId, cancellationToken);
        }

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, cancellationToken);
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
        Guid groupId;

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<AddPinchGroupHandler>();
            groupId = await handler.HandleAsync(
                DraftCurationId,
                groupName,
                Enum.Parse<PinchAxisTag>(SelectedPinchAxis),
                cancellationToken);
        }

        NewPinchGroupName = string.Empty;
        await RefreshSessionAsync(SelectedCandidate?.CandidateId, null, groupId, cancellationToken);
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

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<AddPinchMarkerHandler>();
            await handler.HandleAsync(
                DraftCurationId,
                SelectedCandidate.CandidateId,
                SelectedPinchGroup.PinchGroupId,
                positionRatio,
                maxTrimMm,
                cancellationToken);
        }

        IsPinchPlacementArmed = false;
        await RefreshSessionAsync(SelectedCandidate.CandidateId, null, SelectedPinchGroup.PinchGroupId, cancellationToken);
        StatusMessage = $"Added {SelectedPinchGroup.Name} pinch to {SelectedCandidate.SourceEntityRef}";
    }

    public async Task RemoveSelectedPinchAsync(CancellationToken cancellationToken)
    {
        if (SelectedPinchMarker is null)
        {
            return;
        }

        StatusMessage = "Removing pinch...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<RemovePinchMarkerHandler>();
            await handler.HandleAsync(SelectedPinchMarker.PinchMarkerId, cancellationToken);
        }

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, null, SelectedPinchGroup?.PinchGroupId, cancellationToken);
        StatusMessage = "Removed pinch";
    }

    public async Task RemoveSelectedOpeningCandidateAsync(CancellationToken cancellationToken)
    {
        if (SelectedOpeningCandidate is null)
        {
            return;
        }

        var removed = SelectedOpeningCandidate;
        StatusMessage = $"Removing opening {removed.SourceEntityRef}...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<RemoveOpeningCandidateHandler>();
            await handler.HandleAsync(removed.OpeningCandidateId, cancellationToken);
        }

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, cancellationToken);
        SelectedOpeningCandidate = null;
        StatusMessage = $"Removed opening {removed.SourceEntityRef}";
    }

    public async Task RemoveSelectedOpeningLabelAsync(CancellationToken cancellationToken)
    {
        if (SelectedOpeningLabel is null)
        {
            return;
        }

        var removed = SelectedOpeningLabel;
        StatusMessage = $"Removing opening label {removed.Text}...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<RemoveOpeningLabelHandler>();
            await handler.HandleAsync(removed.OpeningLabelId, cancellationToken);
        }

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, cancellationToken);
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
        StatusMessage = $"Removing component {removed.SourceEntityRef}...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<RemoveFixedPlanComponentHandler>();
            await handler.HandleAsync(removed.FixedPlanComponentId, cancellationToken);
        }

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, cancellationToken);
        SelectedFixedPlanComponent = null;
        StatusMessage = $"Removed component {removed.SourceEntityRef}";
    }

    public async Task RemoveSelectedProtectedDetailAssemblyAsync(CancellationToken cancellationToken)
    {
        if (SelectedProtectedDetailAssembly is null)
        {
            return;
        }

        var removed = SelectedProtectedDetailAssembly;
        StatusMessage = $"Removing protected detail {removed.SourceEntityRef}...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<RemoveProtectedDetailAssemblyHandler>();
            await handler.HandleAsync(removed.ProtectedDetailAssemblyId, cancellationToken);
        }

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedPinchMarker?.PinchMarkerId, SelectedPinchGroup?.PinchGroupId, cancellationToken);
        SelectedProtectedDetailAssembly = null;
        StatusMessage = $"Removed protected detail {removed.SourceEntityRef}";
    }

    public bool SelectPreviewPath(Guid geometryPathId)
    {
        var protectedDetail = ProtectedDetailAssemblies.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
        if (protectedDetail is not null)
        {
            SelectedProtectedDetailAssembly = protectedDetail;
            HighlightGeometryPathId = geometryPathId;
            PreviewSelectionLabel = $"Previewing protected detail: {protectedDetail.SourceEntityRef}";
            return true;
        }

        var fixedComponent = FixedPlanComponents.FirstOrDefault(item => item.GeometryPathIds.Contains(geometryPathId));
        if (fixedComponent is not null)
        {
            SelectedFixedPlanComponent = fixedComponent;
            HighlightGeometryPathId = geometryPathId;
            PreviewSelectionLabel = $"Previewing fixed component: {fixedComponent.SourceEntityRef}";
            return true;
        }

        var opening = OpeningCandidates.FirstOrDefault(item => item.GeometryPathId == geometryPathId);
        if (opening is not null)
        {
            SelectedOpeningCandidate = opening;
            return true;
        }

        var candidate = WallCandidates.FirstOrDefault(item => item.GeometryPathId == geometryPathId);
        if (candidate is null)
        {
            return false;
        }

        SelectedPinchMarker = null;
        SelectedCandidate = candidate;
        return true;
    }

    partial void OnSelectedCandidateChanged(WallCandidateDto? value)
    {
        NotifyUxStateChanged();

        if (value is null)
        {
            HighlightGeometryPathId = SelectedPinchMarker?.GeometryPathId;
            PreviewSelectionLabel = SelectedPinchMarker is null
                ? "Previewing floor plan"
                : $"Previewing pinch marker on {SelectedPinchAxis}";
            return;
        }

        HighlightGeometryPathId = value.GeometryPathId;
        PreviewSelectionLabel = $"Previewing candidate: {value.SourceEntityRef}";
        SelectedOpeningCandidate = null;
        SelectedOpeningLabel = null;
        SelectedFixedPlanComponent = null;
        SelectedProtectedDetailAssembly = null;
    }

    partial void OnSelectedPinchMarkerChanged(PinchMarkerDto? value)
    {
        if (value is null)
        {
            NotifyUxStateChanged();
            return;
        }

        if (!string.Equals(SelectedPinchAxis, value.AxisTag, StringComparison.OrdinalIgnoreCase))
        {
            SelectedPinchAxis = value.AxisTag;
        }

        SelectedPinchGroup = PinchGroups.FirstOrDefault(item => item.PinchGroupId == value.PinchGroupId) ?? SelectedPinchGroup;

        var sourceCandidate = WallCandidates.FirstOrDefault(item => item.CandidateId == value.SourceCandidateId);
        if (sourceCandidate is not null)
        {
            SelectedCandidate = sourceCandidate;
        }

        HighlightGeometryPathId = value.GeometryPathId;
        PreviewSelectionLabel = $"Previewing {value.AxisTag} pinch";
        NotifyUxStateChanged();
    }

    partial void OnSelectedOpeningCandidateChanged(OpeningCandidateDto? value)
    {
        if (value is null)
        {
            NotifyUxStateChanged();
            return;
        }

        SelectedCandidate = null;
        SelectedPinchMarker = null;
        SelectedOpeningLabel = null;
        SelectedFixedPlanComponent = null;
        SelectedProtectedDetailAssembly = null;
        HighlightGeometryPathId = value.GeometryPathId;
        PreviewSelectionLabel = $"Previewing opening: {value.SourceEntityRef}";
        NotifyUxStateChanged();
    }

    partial void OnSelectedOpeningLabelChanged(OpeningLabelDto? value)
    {
        if (value is null)
        {
            NotifyUxStateChanged();
            return;
        }

        SelectedCandidate = null;
        SelectedPinchMarker = null;
        SelectedOpeningCandidate = null;
        SelectedFixedPlanComponent = null;
        SelectedProtectedDetailAssembly = null;
        HighlightGeometryPathId = null;
        PreviewSelectionLabel = $"Previewing opening label: {value.Text}";
        NotifyUxStateChanged();
    }

    partial void OnSelectedFixedPlanComponentChanged(FixedPlanComponentDto? value)
    {
        if (value is null)
        {
            NotifyUxStateChanged();
            return;
        }

        SelectedCandidate = null;
        SelectedPinchMarker = null;
        SelectedOpeningCandidate = null;
        SelectedOpeningLabel = null;
        SelectedProtectedDetailAssembly = null;
        HighlightGeometryPathId = value.GeometryPathIds.FirstOrDefault();
        PreviewSelectionLabel = $"Previewing fixed component: {value.SourceEntityRef}";
        NotifyUxStateChanged();
    }

    partial void OnSelectedProtectedDetailAssemblyChanged(ProtectedDetailAssemblyDto? value)
    {
        if (value is null)
        {
            NotifyUxStateChanged();
            return;
        }

        SelectedCandidate = null;
        SelectedPinchMarker = null;
        SelectedOpeningCandidate = null;
        SelectedOpeningLabel = null;
        SelectedFixedPlanComponent = null;
        HighlightGeometryPathId = value.GeometryPathIds.FirstOrDefault();
        PreviewSelectionLabel = $"Previewing protected detail: {value.SourceEntityRef}";
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

    partial void OnIsPinchPlacementArmedChanged(bool value)
    {
        NotifyUxStateChanged();
    }

    private async Task RefreshSessionAsync(Guid? preferredCandidateId, Guid? preferredPinchMarkerId, Guid? preferredPinchGroupId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFloorPlanReviewSessionHandler>();
        var session = await handler.HandleAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan review session was not found.");

        DraftCurationId = string.Equals(session.Status, "Published", StringComparison.OrdinalIgnoreCase)
            ? Guid.Empty
            : DraftCurationId;
        ApplySession(session, preferredCandidateId, preferredPinchMarkerId, preferredPinchGroupId);
    }

    private void ApplySession(FloorPlanReviewSessionDto session, Guid? preferredCandidateId, Guid? preferredPinchMarkerId)
    {
        ApplySession(session, preferredCandidateId, preferredPinchMarkerId, preferredPinchGroupId: null);
    }

    private void ApplySession(FloorPlanReviewSessionDto session, Guid? preferredCandidateId, Guid? preferredPinchMarkerId, Guid? preferredPinchGroupId)
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
        ReplaceItems(FixedPlanComponents, session.FixedPlanComponents);
        ReplaceItems(ProtectedDetailAssemblies, session.ProtectedDetailAssemblies);
        ReplaceItems(WallCandidates, session.WallCandidates);
        ReplaceItems(PinchGroups, session.PinchGroups);
        ReplaceItems(PinchMarkers, session.PinchMarkers);

        OnPropertyChanged(nameof(DoorOpeningCount));
        OnPropertyChanged(nameof(WindowOpeningCount));
        OnPropertyChanged(nameof(FixedPlanComponentCount));
        OnPropertyChanged(nameof(ProtectedDetailAssemblyCount));

        SelectedCandidate = preferredCandidateId is null
            ? WallCandidates.FirstOrDefault()
            : WallCandidates.FirstOrDefault(item => item.CandidateId == preferredCandidateId) ?? WallCandidates.FirstOrDefault();

        SelectedPinchMarker = preferredPinchMarkerId is null
            ? null
            : PinchMarkers.FirstOrDefault(item => item.PinchMarkerId == preferredPinchMarkerId);

        SelectedPinchGroup = preferredPinchGroupId is null
            ? SelectedPinchMarker is null
                ? PinchGroups.FirstOrDefault()
                : PinchGroups.FirstOrDefault(item => item.PinchGroupId == SelectedPinchMarker.PinchGroupId) ?? PinchGroups.FirstOrDefault()
            : PinchGroups.FirstOrDefault(item => item.PinchGroupId == preferredPinchGroupId) ?? PinchGroups.FirstOrDefault();
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void NotifyUxStateChanged()
    {
        OnPropertyChanged(nameof(AddPinchButtonLabel));
        OnPropertyChanged(nameof(SelectedPinchGroupId));
        OnPropertyChanged(nameof(InteractionHint));
    }

    private string GetHandleHint()
    {
        return string.Equals(SelectedPinchAxis, nameof(PinchAxisTag.Height), StringComparison.OrdinalIgnoreCase)
            ? "top or bottom"
            : "left or right";
    }
}
