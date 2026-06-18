using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Contracts.FloorPlans;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly IServiceScopeFactory scopeFactory;

    public LibraryViewModel(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public ObservableCollection<FloorPlanLibraryItemDto> Items { get; } = [];

    [ObservableProperty]
    private FloorPlanLibraryItemDto? selectedItem;

    [ObservableProperty]
    private FloorPlanLibraryVersionDto? selectedVersion;

    [ObservableProperty]
    private string statusMessage = "Ready";

    [ObservableProperty]
    private FloorPlanReviewViewModel? activeReviewViewModel;

    [ObservableProperty]
    private SitePlanAdjustmentViewModel? activeSitePlanAdjustmentViewModel;

    public string SelectedVersionLabel => SelectedItem is null || SelectedVersion is null
        ? "No version selected"
        : $"Selected: {SelectedItem.Code} v{SelectedVersion.VersionNumber}";

    public string ShellTitle => ActiveReviewViewModel is not null
        ? "Floorplan Fit - Edit"
        : ActiveSitePlanAdjustmentViewModel is not null
            ? "Floorplan Fit - Adjust to Site Plan"
            : "Floorplan Fit - Library";

    public bool IsLibraryScreenVisible => ActiveReviewViewModel is null && ActiveSitePlanAdjustmentViewModel is null;

    public bool IsReviewScreenVisible => ActiveReviewViewModel is not null;

    public bool IsSitePlanAdjustmentScreenVisible => ActiveSitePlanAdjustmentViewModel is not null;

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        StatusMessage = "Loading...";
        await RefreshItemsAsync(cancellationToken);
        StatusMessage = Items.Count == 0
            ? "Ready"
            : $"Loaded {Items.Count} floor plan(s)";
    }

    public async Task ImportAsync(string filePath, CancellationToken cancellationToken)
    {
        StatusMessage = "Importing...";

        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportFloorPlanHandler>();
        var response = await handler.HandleAsync(new ImportFloorPlanRequest(filePath), cancellationToken);

        await ExtractByTemplateAsync(response.Item.TemplateId, cancellationToken);
        await RefreshItemsAsync(cancellationToken);
        SelectedItem = Items.FirstOrDefault(item => item.TemplateId == response.Item.TemplateId);
        SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.IsCurrent);
        StatusMessage = $"Imported and extracted {response.Item.Name} v{response.Item.ActiveVersionNumber}";
    }

    public async Task ExtractSelectedAsync(CancellationToken cancellationToken)
    {
        if (SelectedItem is null || SelectedVersion is null)
        {
            return;
        }

        var templateId = SelectedItem.TemplateId;
        var versionId = SelectedVersion.VersionId;
        StatusMessage = $"Extracting walls for {SelectedItem.Name} v{SelectedVersion.VersionNumber}...";
        await ExtractByVersionAsync(versionId, cancellationToken);
        await RefreshItemsAsync(cancellationToken);
        SelectedItem = Items.FirstOrDefault(item => item.TemplateId == templateId);
        SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == versionId)
            ?? SelectedItem?.Versions.FirstOrDefault(item => item.IsCurrent);
        StatusMessage = $"Extracted walls for {SelectedItem?.Name ?? "floor plan"} v{SelectedVersion?.VersionNumber}";
    }

    public async Task<FloorPlanReviewViewModel?> OpenSelectedReviewAsync(CancellationToken cancellationToken)
    {
        if (SelectedItem is null || SelectedVersion is null)
        {
            return null;
        }

        var templateId = SelectedItem.TemplateId;
        var versionId = SelectedVersion.VersionId;
        if (string.Equals(SelectedVersion.Status, "Imported", StringComparison.OrdinalIgnoreCase))
        {
            await ExtractSelectedAsync(cancellationToken);
        }

        var reviewViewModel = new FloorPlanReviewViewModel(scopeFactory, templateId, versionId);
        await reviewViewModel.LoadAsync(cancellationToken);

        if (RequiresDimensionPrecisionRefresh(reviewViewModel))
        {
            StatusMessage = $"Refreshing native dimensions for {SelectedItem.Name} v{SelectedVersion.VersionNumber}...";
            await ExtractByVersionAsync(versionId, cancellationToken);
            await reviewViewModel.LoadAsync(cancellationToken);
            StatusMessage = $"Refreshed native dimensions for {SelectedItem.Name} v{SelectedVersion.VersionNumber}";
        }

        return reviewViewModel;
    }

    public async Task<FloorPlanReviewViewModel?> OpenVersionReviewAsync(
        FloorPlanLibraryItemDto item,
        FloorPlanLibraryVersionDto version,
        CancellationToken cancellationToken)
    {
        SelectVersion(item, version);
        return await OpenSelectedReviewAsync(cancellationToken);
    }

    public async Task ShowSelectedReviewAsync(CancellationToken cancellationToken)
    {
        var reviewViewModel = await OpenSelectedReviewAsync(cancellationToken);
        if (reviewViewModel is null)
        {
            return;
        }

        ActiveSitePlanAdjustmentViewModel = null;
        ActiveReviewViewModel = reviewViewModel;
    }

    public async Task ShowVersionReviewAsync(
        FloorPlanLibraryItemDto item,
        FloorPlanLibraryVersionDto version,
        CancellationToken cancellationToken)
    {
        SelectVersion(item, version);
        await ShowSelectedReviewAsync(cancellationToken);
    }

    public async Task<SitePlanAdjustmentViewModel?> OpenVersionSitePlanAdjustmentAsync(
        FloorPlanLibraryItemDto item,
        FloorPlanLibraryVersionDto version,
        string sitePlanFilePath,
        CancellationToken cancellationToken)
    {
        SelectVersion(item, version);
        if (!version.CanAdjustToSitePlan)
        {
            StatusMessage = "Publish this floor plan before adjusting it to a site plan.";
            return null;
        }

        StatusMessage = $"Loading site plan for {item.Code} v{version.VersionNumber}...";
        using var scope = scopeFactory.CreateScope();
        var sitePlanReader = scope.ServiceProvider.GetRequiredService<ISitePlanPreviewReader>();
        var sitePlan = await sitePlanReader.ReadAsync(sitePlanFilePath, cancellationToken);
        var reviewViewModel = await OpenSelectedReviewAsync(cancellationToken);
        if (reviewViewModel is null)
        {
            return null;
        }

        var autoFitPlanSuggester = scope.ServiceProvider.GetRequiredService<IAutoFitPlanSuggester>();
        var adjustedSitePlanExporter = scope.ServiceProvider.GetRequiredService<IAdjustedSitePlanExporter>();
        var extractionSourceReader = scope.ServiceProvider.GetRequiredService<IFloorPlanExtractionSourceReader>();
        var extractionSource = await extractionSourceReader.GetByVersionAsync(version.VersionId, cancellationToken);
        StatusMessage = $"Previewing {item.Code} v{version.VersionNumber} over {sitePlan.FileName}";
        return SitePlanAdjustmentPreviewProjector.Build(
            item,
            version,
            reviewViewModel,
            sitePlan,
            autoFitPlanSuggester,
            extractionSource?.ManagedFilePath,
            sitePlanFilePath,
            adjustedSitePlanExporter);
    }

    public async Task ShowVersionSitePlanAdjustmentAsync(
        FloorPlanLibraryItemDto item,
        FloorPlanLibraryVersionDto version,
        string sitePlanFilePath,
        CancellationToken cancellationToken)
    {
        var adjustmentViewModel = await OpenVersionSitePlanAdjustmentAsync(
            item,
            version,
            sitePlanFilePath,
            cancellationToken);
        if (adjustmentViewModel is null)
        {
            return;
        }

        ActiveReviewViewModel = null;
        ActiveSitePlanAdjustmentViewModel = adjustmentViewModel;
    }

    public async Task ShowLibraryAsync(CancellationToken cancellationToken)
    {
        ActiveReviewViewModel = null;
        ActiveSitePlanAdjustmentViewModel = null;
        await LoadAsync(cancellationToken);
    }

    public void SelectVersion(FloorPlanLibraryItemDto item, FloorPlanLibraryVersionDto version)
    {
        SelectedItem = item;
        SelectedVersion = version;
        StatusMessage = $"Selected {item.Code} v{version.VersionNumber}";
    }

    public async Task DeleteSelectedVersionAsync(CancellationToken cancellationToken)
    {
        if (SelectedVersion is null)
        {
            StatusMessage = "Select a version before deleting.";
            return;
        }

        await DeleteVersionAsync(SelectedVersion, cancellationToken);
    }

    public async Task DeleteVersionAsync(FloorPlanLibraryVersionDto version, CancellationToken cancellationToken)
    {
        var optimistic = RemoveVersionFromUi(version);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<RemoveFloorPlanVersionHandler>();
            await handler.HandleAsync(version.VersionId, cancellationToken);
        }
        catch
        {
            RestoreVersionInUi(optimistic);
            StatusMessage = $"Could not delete v{version.VersionNumber}";
            throw;
        }

        StatusMessage = $"Deleted v{version.VersionNumber}";
        ScheduleBackgroundCleanup();
    }

    private OptimisticDeletionSnapshot RemoveVersionFromUi(FloorPlanLibraryVersionDto version)
    {
        var hostItemIndex = -1;
        FloorPlanLibraryItemDto? hostItem = null;
        int? hostItemRemovalIndex = null;
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Versions.Any(v => v.VersionId == version.VersionId))
            {
                hostItem = Items[i];
                hostItemIndex = i;
                break;
            }
        }

        if (hostItem is null)
        {
            return new OptimisticDeletionSnapshot(version, null, -1, null, SelectedItem, SelectedVersion);
        }

        var remainingVersions = hostItem.Versions.Where(v => v.VersionId != version.VersionId).ToArray();
        if (remainingVersions.Length == 0)
        {
            hostItemRemovalIndex = hostItemIndex;
            Items.RemoveAt(hostItemIndex);
            if (SelectedItem == hostItem)
            {
                SelectedItem = Items.Count > 0 ? Items[Math.Min(hostItemIndex, Items.Count - 1)] : null;
                SelectedVersion = SelectedItem?.Versions.FirstOrDefault(v => v.IsCurrent)
                                  ?? SelectedItem?.Versions.FirstOrDefault();
            }
            return new OptimisticDeletionSnapshot(version, hostItem, hostItemIndex, hostItemRemovalIndex, SelectedItem, SelectedVersion);
        }

        var nextCurrentVersion = hostItem.CurrentVersionId == version.VersionId
            ? remainingVersions.FirstOrDefault()
            : remainingVersions.FirstOrDefault(v => v.VersionId == hostItem.CurrentVersionId);
        var updated = hostItem with
        {
            VersionCount = remainingVersions.Length,
            CurrentVersionId = nextCurrentVersion?.VersionId,
            CurrentVersionNumber = nextCurrentVersion?.VersionNumber,
            Versions = remainingVersions
        };
        Items[hostItemIndex] = updated;

        if (SelectedItem?.TemplateId == updated.TemplateId)
        {
            SelectedItem = updated;
            SelectedVersion = SelectedVersion?.VersionId == version.VersionId
                ? updated.Versions.FirstOrDefault(v => v.IsCurrent) ?? updated.Versions.FirstOrDefault()
                : SelectedVersion;
        }

        return new OptimisticDeletionSnapshot(version, hostItem, hostItemIndex, null, SelectedItem, SelectedVersion);
    }

    private void RestoreVersionInUi(OptimisticDeletionSnapshot snapshot)
    {
        if (snapshot.HostItem is null)
        {
            return;
        }

        if (snapshot.HostItemRemovalIndex is { } removalIndex)
        {
            var insertionIndex = Math.Min(removalIndex, Items.Count);
            Items.Insert(insertionIndex, snapshot.HostItem);
        }
        else if (snapshot.HostItemIndex >= 0 && snapshot.HostItemIndex < Items.Count)
        {
            Items[snapshot.HostItemIndex] = snapshot.HostItem;
        }

        SelectedItem = snapshot.PriorSelectedItem;
        SelectedVersion = snapshot.PriorSelectedVersion;
    }

    private void ScheduleBackgroundCleanup()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<IFloorPlanVersionCleanupService>();
                await cleanup.CleanupAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "Floor plan version cleanup failed after delete: {0}",
                    exception);
            }
        });
    }

    private sealed record OptimisticDeletionSnapshot(
        FloorPlanLibraryVersionDto Version,
        FloorPlanLibraryItemDto? HostItem,
        int HostItemIndex,
        int? HostItemRemovalIndex,
        FloorPlanLibraryItemDto? PriorSelectedItem,
        FloorPlanLibraryVersionDto? PriorSelectedVersion);

    private async Task RefreshItemsAsync(CancellationToken cancellationToken)
    {
        var selectedTemplateId = SelectedItem?.TemplateId;
        var selectedVersionId = SelectedVersion?.VersionId;
        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetFloorPlanLibraryHandler>();
        var items = await handler.HandleAsync(cancellationToken);

        Items.Clear();

        foreach (var item in items)
        {
            Items.Add(item);
        }

        SelectedItem = selectedTemplateId is null
            ? Items.FirstOrDefault()
            : Items.FirstOrDefault(item => item.TemplateId == selectedTemplateId);
        SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == selectedVersionId)
            ?? SelectedItem?.Versions.FirstOrDefault(item => item.IsCurrent)
            ?? SelectedItem?.Versions.FirstOrDefault();
    }

    private async Task ExtractByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sourceReader = scope.ServiceProvider.GetRequiredService<IFloorPlanExtractionSourceReader>();
        var source = await sourceReader.GetCurrentSourceAsync(templateId, cancellationToken)
            ?? throw new InvalidOperationException("Current floor plan version was not found for extraction.");

        var handler = scope.ServiceProvider.GetRequiredService<ExtractWallCandidatesHandler>();
        await handler.HandleAsync(source.FloorPlanVersionId, source.ManagedFilePath, cancellationToken);
    }

    private async Task ExtractByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var sourceReader = scope.ServiceProvider.GetRequiredService<IFloorPlanExtractionSourceReader>();
        var source = await sourceReader.GetByVersionAsync(floorPlanVersionId, cancellationToken)
            ?? throw new InvalidOperationException("Selected floor plan version was not found for extraction.");

        var handler = scope.ServiceProvider.GetRequiredService<ExtractWallCandidatesHandler>();
        await handler.HandleAsync(source.FloorPlanVersionId, source.ManagedFilePath, cancellationToken);
    }

    private static bool RequiresDimensionPrecisionRefresh(FloorPlanReviewViewModel reviewViewModel)
    {
        if (reviewViewModel.Dimensions.Count == 0)
        {
            return false;
        }

        return reviewViewModel.Dimensions.All(dimension =>
            dimension.LineSegments.Count == 0 &&
            dimension.RenderTextX is null &&
            dimension.RenderTextY is null);
    }

    partial void OnSelectedItemChanged(FloorPlanLibraryItemDto? value)
    {
        SelectedVersion = value?.Versions.FirstOrDefault(item => item.IsCurrent)
            ?? value?.Versions.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedVersionLabel));
    }

    partial void OnSelectedVersionChanged(FloorPlanLibraryVersionDto? value)
    {
        OnPropertyChanged(nameof(SelectedVersionLabel));
    }

    partial void OnActiveReviewViewModelChanged(FloorPlanReviewViewModel? value)
    {
        NotifyActiveScreenChanged();
    }

    partial void OnActiveSitePlanAdjustmentViewModelChanged(SitePlanAdjustmentViewModel? value)
    {
        NotifyActiveScreenChanged();
    }

    private void NotifyActiveScreenChanged()
    {
        OnPropertyChanged(nameof(ShellTitle));
        OnPropertyChanged(nameof(IsLibraryScreenVisible));
        OnPropertyChanged(nameof(IsReviewScreenVisible));
        OnPropertyChanged(nameof(IsSitePlanAdjustmentScreenVisible));
    }
}
