using System.Collections.ObjectModel;
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

    public ObservableCollection<CuratedWallDto> CuratedWalls { get; } = [];

    public ObservableCollection<GeometryPathDto> GeometryPaths { get; } = [];

    public IReadOnlyList<string> WallRoleOptions { get; } = Enum.GetNames<WallRole>();

    public IReadOnlyList<string> MobilityOptions { get; } = Enum.GetNames<WallMobilityLevel>();

    public IReadOnlyList<string> ProtectionOptions { get; } = Enum.GetNames<WallProtectionLevel>();

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
    private CuratedWallDto? selectedCuratedWall;

    [ObservableProperty]
    private Guid? highlightGeometryPathId;

    [ObservableProperty]
    private string pendingStableWallId = string.Empty;

    [ObservableProperty]
    private string editableWallRole = nameof(WallRole.Partition);

    [ObservableProperty]
    private string editableMobilityLevel = nameof(WallMobilityLevel.Flexible);

    [ObservableProperty]
    private string editableProtectionLevel = nameof(WallProtectionLevel.None);

    [ObservableProperty]
    private string editableThicknessMm = "101.6";

    [ObservableProperty]
    private string editableAssemblyCode = "2x4";

    [ObservableProperty]
    private string editableHeightMm = string.Empty;

    [ObservableProperty]
    private bool editableIsExterior;

    [ObservableProperty]
    private bool editableIsStructuralHint;

    [ObservableProperty]
    private string editableNotes = string.Empty;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        StatusMessage = "Loading review session...";

        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<OpenFloorPlanReviewSessionHandler>();
        var response = await handler.HandleAsync(templateId, cancellationToken);

        DraftCurationId = response.DraftCurationId;
        ApplySession(response.Session, preferredCandidateId: null, preferredCuratedWallId: null);
        StatusMessage = $"Loaded review session for {Name}";
    }

    public async Task AcceptSelectedCandidateAsync(CancellationToken cancellationToken)
    {
        if (SelectedCandidate is null || DraftCurationId == Guid.Empty || !string.Equals(SelectedCandidate.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var stableWallId = string.IsNullOrWhiteSpace(PendingStableWallId)
            ? GenerateStableWallId()
            : PendingStableWallId.Trim();

        StatusMessage = $"Accepting {SelectedCandidate.SourceEntityRef}...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<AcceptWallCandidateHandler>();
            await handler.HandleAsync(DraftCurationId, SelectedCandidate.CandidateId, stableWallId, cancellationToken);
        }

        await RefreshSessionAsync(SelectedCandidate.CandidateId, null, cancellationToken);
        PendingStableWallId = stableWallId;
        StatusMessage = $"Accepted {stableWallId}";
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

        await RefreshSessionAsync(SelectedCandidate.CandidateId, null, cancellationToken);
        StatusMessage = $"Rejected {SelectedCandidate.SourceEntityRef}";
    }

    public async Task SaveSelectedWallMetadataAsync(CancellationToken cancellationToken)
    {
        if (SelectedCuratedWall is null)
        {
            return;
        }

        var thicknessMm = decimal.Parse(EditableThicknessMm.Trim(), System.Globalization.CultureInfo.InvariantCulture);
        decimal? heightMm = string.IsNullOrWhiteSpace(EditableHeightMm)
            ? null
            : decimal.Parse(EditableHeightMm.Trim(), System.Globalization.CultureInfo.InvariantCulture);

        StatusMessage = $"Saving metadata for {SelectedCuratedWall.StableWallId}...";

        using (var scope = scopeFactory.CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<UpdateCuratedWallMetadataHandler>();
            await handler.HandleAsync(
                SelectedCuratedWall.CuratedWallId,
                Enum.Parse<WallRole>(EditableWallRole),
                Enum.Parse<WallMobilityLevel>(EditableMobilityLevel),
                Enum.Parse<WallProtectionLevel>(EditableProtectionLevel),
                thicknessMm,
                EditableAssemblyCode.Trim(),
                heightMm,
                EditableIsExterior,
                EditableIsStructuralHint,
                string.IsNullOrWhiteSpace(EditableNotes) ? null : EditableNotes.Trim(),
                cancellationToken);
        }

        await RefreshSessionAsync(null, SelectedCuratedWall.CuratedWallId, cancellationToken);
        StatusMessage = $"Saved metadata for {SelectedCuratedWall.StableWallId}";
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

        await RefreshSessionAsync(SelectedCandidate?.CandidateId, SelectedCuratedWall?.CuratedWallId, cancellationToken);
        StatusMessage = $"Published curation for {Name}";
    }

    partial void OnSelectedCandidateChanged(WallCandidateDto? value)
    {
        if (value is null)
        {
            HighlightGeometryPathId = SelectedCuratedWall?.GeometryPathId;
            PendingStableWallId = string.Empty;
            return;
        }

        HighlightGeometryPathId = value.GeometryPathId;
        PendingStableWallId = CuratedWalls.FirstOrDefault(item => item.SourceCandidateId == value.CandidateId)?.StableWallId
            ?? GenerateStableWallId();
    }

    partial void OnSelectedCuratedWallChanged(CuratedWallDto? value)
    {
        if (value is null)
        {
            return;
        }

        HighlightGeometryPathId = value.GeometryPathId ?? HighlightGeometryPathId;
        EditableWallRole = value.WallRole;
        EditableMobilityLevel = value.MobilityLevel;
        EditableProtectionLevel = value.ProtectionLevel;
        EditableThicknessMm = (value.ThicknessMm ?? 101.6m).ToString(System.Globalization.CultureInfo.InvariantCulture);
        EditableAssemblyCode = value.AssemblyCode ?? "2x4";
        EditableHeightMm = value.HeightMm?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        EditableIsExterior = value.IsExterior;
        EditableIsStructuralHint = value.IsStructuralHint;
        EditableNotes = value.Notes ?? string.Empty;
        PendingStableWallId = value.StableWallId;
    }

    private async Task RefreshSessionAsync(Guid? preferredCandidateId, Guid? preferredCuratedWallId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFloorPlanReviewSessionHandler>();
        var session = await handler.HandleAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan review session was not found.");

        DraftCurationId = string.Equals(session.Status, "Published", StringComparison.OrdinalIgnoreCase)
            ? Guid.Empty
            : DraftCurationId;
        ApplySession(session, preferredCandidateId, preferredCuratedWallId);
    }

    private void ApplySession(FloorPlanReviewSessionDto session, Guid? preferredCandidateId, Guid? preferredCuratedWallId)
    {
        Code = session.Code;
        Name = session.Name;
        Status = session.Status;
        ActiveVersionNumber = session.ActiveVersionNumber;
        ActivePublishedCurationId = session.ActivePublishedCurationId;

        ReplaceItems(GeometryPaths, session.GeometryPaths);
        ReplaceItems(WallCandidates, session.WallCandidates);
        ReplaceItems(CuratedWalls, session.CuratedWalls);

        SelectedCandidate = preferredCandidateId is null
            ? WallCandidates.FirstOrDefault()
            : WallCandidates.FirstOrDefault(item => item.CandidateId == preferredCandidateId) ?? WallCandidates.FirstOrDefault();

        SelectedCuratedWall = preferredCuratedWallId is null
            ? CuratedWalls.FirstOrDefault(item => SelectedCandidate is not null && item.SourceCandidateId == SelectedCandidate.CandidateId)
                ?? CuratedWalls.FirstOrDefault()
            : CuratedWalls.FirstOrDefault(item => item.CuratedWallId == preferredCuratedWallId) ?? CuratedWalls.FirstOrDefault();
    }

    private static void ReplaceItems<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private string GenerateStableWallId()
    {
        return $"W-{CuratedWalls.Count + 1:000}";
    }
}
