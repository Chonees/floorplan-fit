using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Application.PlanSets.Adjustment;
using FloorplanFit.Application.PlanSets.Classification;
using FloorplanFit.Application.PlanSets.Confirmation;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Application.PlanSets.Import;
using FloorplanFit.Application.PlanSets.Library;
using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Application.PlanSets.Registration;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.ViewModels;

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly IServiceScopeFactory scopeFactory;
    private IReadOnlyList<PlanSetLibraryItemDto> planSetItems = [];

    public LibraryViewModel(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public ObservableCollection<FloorPlanLibraryItemDto> Items { get; } = [];

    public ObservableCollection<PlanSetSheetDto> SelectedPlanSetSheets { get; } = [];

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

    private IServiceScope? activeSitePlanAdjustmentScope;

    public string SelectedVersionLabel => SelectedItem is null || SelectedVersion is null
        ? "No version selected"
        : $"Selected: {SelectedItem.Code} v{SelectedVersion.VersionNumber}";

    public string SelectedPlanSetSheetsLabel => SelectedItem is null || SelectedVersion is null
        ? "Plan Set Sheets"
        : $"Plan Set Sheets: {SelectedPlanSetSheets.Count} sheet(s)";

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

    public async Task<ImportPlanSheetResponse?> ImportDependentSheetAsync(
        string filePath,
        string? sheetType,
        string? name,
        CancellationToken cancellationToken)
    {
        if (SelectedItem is null || SelectedVersion is null)
        {
            StatusMessage = "Select a floor plan version before importing dependent sheets.";
            return null;
        }

        var templateId = SelectedItem.TemplateId;
        var versionId = SelectedVersion.VersionId;
        StatusMessage = $"Importing dependent sheet for {SelectedItem.Code} v{SelectedVersion.VersionNumber}...";

        using var scope = scopeFactory.CreateScope();
        var housePlanSetId = await ResolveHousePlanSetIdAsync(
            scope.ServiceProvider.GetService<ResolveHousePlanSetHandler>(),
            SelectedItem.TemplateId,
            SelectedItem.Code,
            SelectedItem.Name,
            cancellationToken);
        var planSetVersionId = await ResolvePlanSetVersionIdAsync(
            scope.ServiceProvider.GetService<ResolvePlanSetVersionHandler>(),
            housePlanSetId ?? SelectedItem.TemplateId,
            SelectedVersion.VersionId,
            cancellationToken);
        if (!planSetVersionId.HasValue)
        {
            StatusMessage = "Could not resolve the selected HousePlanSet version.";
            return null;
        }

        var handler = scope.ServiceProvider.GetRequiredService<ImportPlanSheetHandler>();
        var response = await handler.HandleAsync(
            new ImportPlanSheetRequest(
                planSetVersionId.Value,
                sheetType ?? string.Empty,
                filePath,
                name),
            cancellationToken);

        await RefreshItemsAsync(cancellationToken);
        SelectedItem = Items.FirstOrDefault(item => item.TemplateId == templateId) ?? SelectedItem;
        SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == versionId)
            ?? SelectedItem?.Versions.FirstOrDefault(item => item.IsCurrent);
        StatusMessage = $"Imported {response.SheetType} sheet {response.Name}";
        return response;
    }

    public async Task<SheetRegistrationDto?> RegisterDependentSheetAsync(
        PlanSetSheetDto sheet,
        SheetRegistrationTransformDto transform,
        decimal confidence,
        bool confirmRegistration,
        decimal overhangInches,
        string? horizontalReferenceName,
        CancellationToken cancellationToken)
    {
        if (SelectedItem is null || SelectedVersion is null)
        {
            StatusMessage = "Select a floor plan version before registering dependent sheets.";
            return null;
        }

        var templateId = SelectedItem.TemplateId;
        var versionId = SelectedVersion.VersionId;

        using var scope = scopeFactory.CreateScope();
        var housePlanSetId = await ResolveHousePlanSetIdAsync(
            scope.ServiceProvider.GetService<ResolveHousePlanSetHandler>(),
            SelectedItem.TemplateId,
            SelectedItem.Code,
            SelectedItem.Name,
            cancellationToken);
        var planSetVersionId = await ResolvePlanSetVersionIdAsync(
            scope.ServiceProvider.GetService<ResolvePlanSetVersionHandler>(),
            housePlanSetId ?? SelectedItem.TemplateId,
            SelectedVersion.VersionId,
            cancellationToken);
        if (!planSetVersionId.HasValue)
        {
            StatusMessage = "Could not resolve the selected HousePlanSet version.";
            return null;
        }

        var registration = sheet.SheetType switch
        {
            "ElectricalPlan" => await scope.ServiceProvider
                .GetRequiredService<RegisterElectricalSheetHandler>()
                .HandleAsync(
                    new RegisterElectricalSheetRequest(
                        planSetVersionId.Value,
                        sheet.SheetId,
                        transform.Scale,
                        transform.RotationDegrees,
                        transform.TranslateX,
                        transform.TranslateY,
                        confidence,
                        confirmRegistration),
                    cancellationToken),
            "RoofPlan" => await scope.ServiceProvider
                .GetRequiredService<RegisterRoofSheetHandler>()
                .HandleAsync(
                    new RegisterRoofSheetRequest(
                        planSetVersionId.Value,
                        sheet.SheetId,
                        transform.Scale,
                        transform.RotationDegrees,
                        transform.TranslateX,
                        transform.TranslateY,
                        confidence,
                        overhangInches,
                        confirmRegistration),
                    cancellationToken),
            "FacadeElevation" => await scope.ServiceProvider
                .GetRequiredService<RegisterFacadeElevationSheetHandler>()
                .HandleAsync(
                    new RegisterFacadeElevationSheetRequest(
                        planSetVersionId.Value,
                        sheet.SheetId,
                        transform.Scale,
                        transform.TranslateX,
                        confidence,
                        confirmRegistration,
                        horizontalReferenceName),
                    cancellationToken),
            _ => throw new ArgumentException("Unsupported dependent sheet type.", nameof(sheet))
        };

        await RefreshItemsAsync(cancellationToken);
        SelectedItem = Items.FirstOrDefault(item => item.TemplateId == templateId) ?? SelectedItem;
        SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == versionId)
            ?? SelectedVersion;
        RefreshSelectedPlanSetSheets();
        StatusMessage = $"Registered {sheet.SheetType} sheet {sheet.Name}: {registration.Status}";
        return registration;
    }

    public async Task<SheetRegistrationDto?> ConfirmDependentSheetRegistrationAsync(
        PlanSetSheetDto sheet,
        CancellationToken cancellationToken)
    {
        if (!sheet.CanConfirmRegistration || !sheet.SheetRegistrationId.HasValue)
        {
            StatusMessage = "Only pending dependent sheet registrations can be confirmed here.";
            return null;
        }

        var templateId = SelectedItem?.TemplateId;
        var versionId = SelectedVersion?.VersionId;

        using var scope = scopeFactory.CreateScope();
        var response = await scope.ServiceProvider
            .GetRequiredService<ConfirmSheetRegistrationHandler>()
            .HandleAsync(new ConfirmSheetRegistrationRequest(sheet.SheetRegistrationId.Value), cancellationToken);

        await RefreshItemsAsync(cancellationToken);
        if (templateId.HasValue)
        {
            SelectedItem = Items.FirstOrDefault(item => item.TemplateId == templateId.Value) ?? SelectedItem;
        }

        if (versionId.HasValue)
        {
            SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == versionId.Value)
                ?? SelectedVersion;
        }

        RefreshSelectedPlanSetSheets();
        StatusMessage = $"Confirmed {sheet.SheetType} sheet {sheet.Name}";
        return response;
    }

    public async Task<SheetRegistrationDto?> RejectDependentSheetRegistrationAsync(
        PlanSetSheetDto sheet,
        CancellationToken cancellationToken)
    {
        if (!sheet.CanRejectRegistration || !sheet.SheetRegistrationId.HasValue)
        {
            StatusMessage = "Only pending dependent sheet registrations can be rejected here.";
            return null;
        }

        var templateId = SelectedItem?.TemplateId;
        var versionId = SelectedVersion?.VersionId;

        using var scope = scopeFactory.CreateScope();
        var response = await scope.ServiceProvider
            .GetRequiredService<RejectSheetRegistrationHandler>()
            .HandleAsync(new RejectSheetRegistrationRequest(sheet.SheetRegistrationId.Value), cancellationToken);

        await RefreshItemsAsync(cancellationToken);
        if (templateId.HasValue)
        {
            SelectedItem = Items.FirstOrDefault(item => item.TemplateId == templateId.Value) ?? SelectedItem;
        }

        if (versionId.HasValue)
        {
            SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == versionId.Value)
                ?? SelectedVersion;
        }

        RefreshSelectedPlanSetSheets();
        StatusMessage = $"Rejected {sheet.SheetType} registration {sheet.Name}";
        return response;
    }

    public async Task<SheetAdjustmentProjectionDto?> ConfirmDependentSheetProjectionAsync(
        PlanSetSheetDto sheet,
        CancellationToken cancellationToken)
    {
        if (!sheet.CanConfirmProjection || !sheet.SheetProjectionId.HasValue)
        {
            StatusMessage = "Only manual-review dependent sheet projections can be confirmed here.";
            return null;
        }

        var templateId = SelectedItem?.TemplateId;
        var versionId = SelectedVersion?.VersionId;

        using var scope = scopeFactory.CreateScope();
        var response = await scope.ServiceProvider
            .GetRequiredService<ConfirmSheetAdjustmentProjectionHandler>()
            .HandleAsync(new ConfirmSheetAdjustmentProjectionRequest(sheet.SheetProjectionId.Value), cancellationToken);

        await RefreshItemsAsync(cancellationToken);
        if (templateId.HasValue)
        {
            SelectedItem = Items.FirstOrDefault(item => item.TemplateId == templateId.Value) ?? SelectedItem;
        }

        if (versionId.HasValue)
        {
            SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == versionId.Value)
                ?? SelectedVersion;
        }

        RefreshSelectedPlanSetSheets();
        StatusMessage = $"Confirmed {sheet.SheetType} projection {sheet.Name}";
        return response;
    }

    public async Task UnlinkDependentSheetAsync(PlanSetSheetDto sheet, CancellationToken cancellationToken)
    {
        if (!sheet.CanUnlink)
        {
            StatusMessage = sheet.IsCanonical
                ? "Canonical floor plan cannot be unlinked from its plan set."
                : "Only unregistered, not-projected dependent sheets can be unlinked here.";
            return;
        }

        var templateId = SelectedItem?.TemplateId;
        var versionId = SelectedVersion?.VersionId;

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlanSheetRepository>();
        var existing = await repository.GetByIdAsync(sheet.SheetId, cancellationToken);
        if (existing is null)
        {
            StatusMessage = $"{sheet.SheetType} sheet {sheet.Name} was already unlinked.";
            await RefreshItemsAsync(cancellationToken);
            return;
        }

        await repository.RemoveAsync(sheet.SheetId, cancellationToken);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);

        await RefreshItemsAsync(cancellationToken);
        if (templateId.HasValue)
        {
            SelectedItem = Items.FirstOrDefault(item => item.TemplateId == templateId.Value) ?? SelectedItem;
        }

        if (versionId.HasValue)
        {
            SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == versionId.Value)
                ?? SelectedVersion;
        }

        RefreshSelectedPlanSetSheets();
        StatusMessage = $"Unlinked {sheet.SheetType} sheet {sheet.Name}";
    }

    public async Task<CorrectPlanSheetTypeResponse?> CorrectDependentSheetTypeAsync(
        PlanSetSheetDto sheet,
        string sheetType,
        CancellationToken cancellationToken)
    {
        if (!sheet.CanCorrectSheetType)
        {
            StatusMessage = "Only unregistered, not-projected dependent sheets can change sheet type here.";
            return null;
        }

        var templateId = SelectedItem?.TemplateId;
        var versionId = SelectedVersion?.VersionId;
        if (!templateId.HasValue || !versionId.HasValue || SelectedItem is null)
        {
            StatusMessage = "Select a floor plan version before changing dependent sheet type.";
            return null;
        }

        using var scope = scopeFactory.CreateScope();
        var housePlanSetId = await ResolveHousePlanSetIdAsync(
            scope.ServiceProvider.GetService<ResolveHousePlanSetHandler>(),
            SelectedItem.TemplateId,
            SelectedItem.Code,
            SelectedItem.Name,
            cancellationToken);
        var planSetVersionId = await ResolvePlanSetVersionIdAsync(
            scope.ServiceProvider.GetService<ResolvePlanSetVersionHandler>(),
            housePlanSetId ?? SelectedItem.TemplateId,
            versionId.Value,
            cancellationToken);
        if (!planSetVersionId.HasValue)
        {
            StatusMessage = "Could not resolve the selected HousePlanSet version.";
            return null;
        }

        var response = await scope.ServiceProvider
            .GetRequiredService<CorrectPlanSheetTypeHandler>()
            .HandleAsync(
                new CorrectPlanSheetTypeRequest(
                    planSetVersionId.Value,
                    sheet.SheetId,
                    sheetType,
                    $"Changed from {sheet.SheetType} in library."),
                cancellationToken);

        await RefreshItemsAsync(cancellationToken);
        if (templateId.HasValue)
        {
            SelectedItem = Items.FirstOrDefault(item => item.TemplateId == templateId.Value) ?? SelectedItem;
        }

        SelectedVersion = SelectedItem?.Versions.FirstOrDefault(item => item.VersionId == versionId.Value)
            ?? SelectedVersion;
        RefreshSelectedPlanSetSheets();
        StatusMessage = $"Changed {sheet.SheetType} sheet {sheet.Name} to {response.SheetType}";
        return response;
    }

    public async Task ExtractSelectedAsync(CancellationToken cancellationToken)
    {
        if (SelectedItem is null || SelectedVersion is null)
        {
            return;
        }

        if (!SelectedVersion.CanExtract)
        {
            StatusMessage = $"Re-extract blocked for {SelectedItem.Code} v{SelectedVersion.VersionNumber}: {SelectedVersion.CurationHistoryLabel}";
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

        DisposeActiveSitePlanAdjustmentScope();
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
        DisposeActiveSitePlanAdjustmentScope();
        var scope = scopeFactory.CreateScope();
        try
        {
            var sitePlanReader = scope.ServiceProvider.GetRequiredService<ISitePlanPreviewReader>();
            var sitePlan = await sitePlanReader.ReadAsync(sitePlanFilePath, cancellationToken);
            var reviewViewModel = await OpenSelectedReviewAsync(cancellationToken);
            if (reviewViewModel is null)
            {
                return null;
            }

            var autoFitPlanSuggester = scope.ServiceProvider.GetRequiredService<IAutoFitPlanSuggester>();
            var adjustedSitePlanExporter = scope.ServiceProvider.GetRequiredService<IAdjustedSitePlanExporter>();
            var canonicalAdjustmentRecorder = scope.ServiceProvider.GetService<RecordCanonicalFloorPlanAdjustmentHandler>();
            var planSetPackageExporter = scope.ServiceProvider.GetService<ExportMultiSheetPlanSetPackageHandler>();
            var registeredSheetProjector = scope.ServiceProvider.GetService<ProjectRegisteredPlanSetSheetsHandler>();
            var confirmSheetProjectionHandler = scope.ServiceProvider.GetService<ConfirmSheetAdjustmentProjectionHandler>();
            var housePlanSetResolver = scope.ServiceProvider.GetService<ResolveHousePlanSetHandler>();
            var planSetVersionResolver = scope.ServiceProvider.GetService<ResolvePlanSetVersionHandler>();
            var extractionSourceReader = scope.ServiceProvider.GetRequiredService<IFloorPlanExtractionSourceReader>();
            var extractionSource = await extractionSourceReader.GetByVersionAsync(version.VersionId, cancellationToken);
            var housePlanSetId = await ResolveHousePlanSetIdAsync(
                housePlanSetResolver,
                item.TemplateId,
                item.Code,
                item.Name,
                cancellationToken);
            var planSetVersionId = await ResolvePlanSetVersionIdAsync(
                planSetVersionResolver,
                housePlanSetId ?? item.TemplateId,
                version.VersionId,
                cancellationToken);
            StatusMessage = $"Previewing {item.Code} v{version.VersionNumber} over {sitePlan.FileName}";
            var adjustmentViewModel = SitePlanAdjustmentPreviewProjector.Build(
                item,
                version,
                reviewViewModel,
                sitePlan,
                autoFitPlanSuggester,
                extractionSource?.ManagedFilePath,
                sitePlanFilePath,
                adjustedSitePlanExporter,
                canonicalAdjustmentRecorder,
                planSetPackageExporter,
                planSetVersionId,
                registeredSheetProjector,
                confirmSheetProjectionHandler);
            activeSitePlanAdjustmentScope = scope;
            return adjustmentViewModel;
        }
        finally
        {
            if (activeSitePlanAdjustmentScope != scope)
            {
                scope.Dispose();
            }
        }
    }

    private static async Task<Guid?> ResolveHousePlanSetIdAsync(
        ResolveHousePlanSetHandler? resolver,
        Guid sourceFloorPlanTemplateId,
        string code,
        string name,
        CancellationToken cancellationToken)
    {
        if (resolver is null || sourceFloorPlanTemplateId == Guid.Empty)
        {
            return null;
        }

        var response = await resolver.HandleAsync(
            new ResolveHousePlanSetRequest(sourceFloorPlanTemplateId, code, name),
            cancellationToken);
        return response.HousePlanSetId;
    }

    private static async Task<Guid?> ResolvePlanSetVersionIdAsync(
        ResolvePlanSetVersionHandler? resolver,
        Guid housePlanSetId,
        Guid canonicalFloorPlanVersionId,
        CancellationToken cancellationToken)
    {
        if (resolver is null || housePlanSetId == Guid.Empty || canonicalFloorPlanVersionId == Guid.Empty)
        {
            return null;
        }

        var response = await resolver.HandleAsync(
            new ResolvePlanSetVersionRequest(housePlanSetId, canonicalFloorPlanVersionId),
            cancellationToken);
        return response.PlanSetVersionId;
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
        DisposeActiveSitePlanAdjustmentScope();
        ActiveSitePlanAdjustmentViewModel = null;
        await LoadAsync(cancellationToken);
    }

    private void DisposeActiveSitePlanAdjustmentScope()
    {
        activeSitePlanAdjustmentScope?.Dispose();
        activeSitePlanAdjustmentScope = null;
    }

    public void SelectVersion(FloorPlanLibraryItemDto item, FloorPlanLibraryVersionDto version)
    {
        SelectedItem = item;
        SelectedVersion = version;
        StatusMessage = $"Selected {item.Code} v{version.VersionNumber}";
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
        await RefreshPlanSetItemsAsync(scope, cancellationToken);
        RefreshSelectedPlanSetSheets();
    }

    private async Task RefreshPlanSetItemsAsync(
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var handler = scope.ServiceProvider.GetService<GetPlanSetLibraryHandler>();
        planSetItems = handler is null
            ? []
            : await handler.HandleAsync(cancellationToken);
    }

    private void RefreshSelectedPlanSetSheets()
    {
        SelectedPlanSetSheets.Clear();

        var selectedPlanSet = FindSelectedPlanSet();
        if (selectedPlanSet is not null)
        {
            foreach (var sheet in selectedPlanSet.Sheets)
            {
                SelectedPlanSetSheets.Add(sheet);
            }
        }

        OnPropertyChanged(nameof(SelectedPlanSetSheetsLabel));
    }

    private PlanSetLibraryItemDto? FindSelectedPlanSet()
    {
        if (SelectedItem is null)
        {
            return null;
        }

        return planSetItems.FirstOrDefault(planSet =>
                   planSet.CanonicalFloorPlanVersionId == SelectedVersion?.VersionId) ??
               planSetItems.FirstOrDefault(planSet =>
                   string.Equals(planSet.Code, SelectedItem.Code, StringComparison.OrdinalIgnoreCase));
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
        RefreshSelectedPlanSetSheets();
    }

    partial void OnSelectedVersionChanged(FloorPlanLibraryVersionDto? value)
    {
        OnPropertyChanged(nameof(SelectedVersionLabel));
        RefreshSelectedPlanSetSheets();
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
