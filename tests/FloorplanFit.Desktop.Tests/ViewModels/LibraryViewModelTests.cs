using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Application.PlanSets.Classification;
using FloorplanFit.Application.PlanSets.Confirmation;
using FloorplanFit.Application.PlanSets.Import;
using FloorplanFit.Application.PlanSets.Library;
using FloorplanFit.Application.PlanSets.Registration;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Domain.Documents;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Domain.Measurement;
using FloorplanFit.Domain.PlanSets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class LibraryViewModelTests
{
    private static FloorPlanLibraryItemDto CreateLibraryItem(
        Guid templateId,
        Guid versionId,
        string status,
        Guid? activePublishedCurationId = null,
        int? activePublishedCurationVersion = null,
        int publishedCurationCount = 0,
        int? latestPublishedCurationVersion = null,
        int? latestDraftCurationVersion = null)
    {
        return new FloorPlanLibraryItemDto(
            templateId,
            "santa-barbara",
            "SANTA-BARBARA",
            VersionCount: 1,
            CurrentVersionId: versionId,
            CurrentVersionNumber: 1,
            Versions:
            [
                new FloorPlanLibraryVersionDto(
                    versionId,
                    VersionNumber: 1,
                    status,
                    new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
                    "inch",
                    IsCurrent: true,
                    activePublishedCurationId,
                    activePublishedCurationVersion,
                    publishedCurationCount,
                    latestPublishedCurationVersion,
                    latestDraftCurationVersion)
            ]);
    }

    private static FloorPlanLibraryVersionDto LibraryVersion(
        Guid versionId,
        int versionNumber,
        Guid publishedCurationId,
        bool isCurrent = false)
        => new(
            versionId,
            versionNumber,
            "Published",
            new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc),
            "inch",
            isCurrent,
            publishedCurationId);

    private static LibraryViewModel CreateReadinessLibraryViewModel(
        FloorPlanLibraryItemDto item,
        ICommissionedHouseAdaptationProfileRepository profiles)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton(profiles);
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetCommissionedHouseAdaptationReadinessHandler>();
        var provider = services.BuildServiceProvider();
        return new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private static CommissionedHouseAdaptationProfile CreateReadyAutoFitProfile(
        Guid versionId,
        Guid publishedCurationId)
    {
        const string protectedLabel = "LABEL:A";
        return new CommissionedHouseAdaptationProfile(
            versionId,
            publishedCurationId,
            SourceToMillimetersFactor: 25.4m,
            Variables:
            [
                new CommissionedAdaptationVariable(
                    "width",
                    "Width",
                    HouseAdaptationAxis.Width,
                    1,
                    [ReadyAction("width", "Width", "Right", protectedLabel)]),
                new CommissionedAdaptationVariable(
                    "depth",
                    "Depth",
                    HouseAdaptationAxis.Depth,
                    1,
                    [ReadyAction("depth", "Height", "Top", protectedLabel)])
            ],
            ImmutableSizeEntityRefs: [],
            ProtectedEntityRefs: [protectedLabel])
        {
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    protectedLabel,
                    CommissionExistingCurationAuxiliaryEntityKind.Label,
                    new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                    GeometryPathId: null,
                    SegmentSortOrder: null,
                    IsImmutableSize: false,
                    IsProtected: true)
            ]
        };
    }

    private static AdjustmentRecipeStretchActionDto ReadyAction(
        string id,
        string axis,
        string edge,
        string protectedLabel)
    {
        var firstPath = Guid.NewGuid();
        var secondPath = Guid.NewGuid();
        var firstEntity = $"{id}:A";
        var secondEntity = $"{id}:B";
        return new AdjustmentRecipeStretchActionDto(
            id,
            axis,
            edge,
            CutCoordinate: 10m,
            DeltaSourceUnits: 0m,
            MaxDeltaSourceUnits: 1m,
            CoordinateTolerance: 0.01m,
            new AdjustmentRecipeBoundsDto(0m, 0m, 10m, 10m),
            [
                new AdjustmentRecipeTargetSpanDto(firstEntity, firstPath, 1, 0m, 0m, 10m, 0m, 1),
                new AdjustmentRecipeTargetSpanDto(secondEntity, secondPath, 1, 0m, 10m, 10m, 10m, 1)
            ],
            [
                new AdjustmentRecipeEntityRoleDto(firstEntity, firstPath, 1, "Stretch", [1]),
                new AdjustmentRecipeEntityRoleDto(secondEntity, secondPath, 1, "Stretch", [1]),
                new AdjustmentRecipeEntityRoleDto(protectedLabel, null, null, "Fixed", [])
            ]);
    }

    [Fact]
    public void FloorPlanLibraryVersionDto_requires_exact_auto_fit_readiness_for_site_plan_adjustment()
    {
        var published = new FloorPlanLibraryVersionDto(
            Guid.NewGuid(),
            VersionNumber: 3,
            Status: "Published",
            new DateTime(2026, 6, 7, 12, 0, 0, DateTimeKind.Utc),
            "inch",
            IsCurrent: true,
            ActivePublishedCurationId: Guid.NewGuid());
        var ready = published with { IsAutoFitReady = true };
        var draft = new FloorPlanLibraryVersionDto(
            Guid.NewGuid(),
            VersionNumber: 4,
            Status: "Curated Draft",
            new DateTime(2026, 6, 7, 13, 0, 0, DateTimeKind.Utc),
            "inch",
            IsCurrent: true);

        Assert.False(published.CanAdjustToSitePlan);
        Assert.Equal("Setup required", published.AutoFitReadinessLabel);
        Assert.True(ready.CanAdjustToSitePlan);
        Assert.Equal("Auto-fit ready", ready.AutoFitReadinessLabel);
        Assert.False(draft.CanAdjustToSitePlan);
    }

    [Fact]
    public async Task LoadAsync_marks_only_the_exact_commissioned_version_auto_fit_ready()
    {
        var templateId = Guid.NewGuid();
        var uncommissionedVersionId = Guid.NewGuid();
        var failedVersionId = Guid.NewGuid();
        var readyVersionId = Guid.NewGuid();
        var uncommissionedCurationId = Guid.NewGuid();
        var failedCurationId = Guid.NewGuid();
        var readyCurationId = Guid.NewGuid();
        var readyProfile = CreateReadyAutoFitProfile(readyVersionId, readyCurationId);
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "house",
            "House",
            VersionCount: 3,
            CurrentVersionId: readyVersionId,
            CurrentVersionNumber: 3,
            Versions:
            [
                LibraryVersion(uncommissionedVersionId, 1, uncommissionedCurationId),
                LibraryVersion(failedVersionId, 2, failedCurationId),
                LibraryVersion(readyVersionId, 3, readyCurationId, isCurrent: true)
            ]);
        var profiles = new AutoFitProfileRepository(
            [readyProfile],
            failedVersionIds: [failedVersionId]);
        var viewModel = CreateReadinessLibraryViewModel(item, profiles);

        await viewModel.LoadAsync(CancellationToken.None);

        var versions = Assert.Single(viewModel.Items).Versions;
        Assert.False(versions.Single(version => version.VersionId == uncommissionedVersionId).CanAdjustToSitePlan);
        Assert.False(versions.Single(version => version.VersionId == failedVersionId).CanAdjustToSitePlan);
        Assert.True(versions.Single(version => version.VersionId == readyVersionId).CanAdjustToSitePlan);
        Assert.Equal(
            new (Guid VersionId, Guid PublishedCurationId)[]
            {
                (uncommissionedVersionId, uncommissionedCurationId),
                (failedVersionId, failedCurationId),
                (readyVersionId, readyCurationId)
            },
            profiles.Requests);
    }

    [Fact]
    public async Task OpenVersionSitePlanAdjustmentAsync_blocks_setup_required_version_with_visible_reason()
    {
        var versionId = Guid.NewGuid();
        var curationId = Guid.NewGuid();
        var item = CreateLibraryItem(
            Guid.NewGuid(),
            versionId,
            "Published",
            activePublishedCurationId: curationId);
        var viewModel = CreateReadinessLibraryViewModel(item, new AutoFitProfileRepository([]));
        await viewModel.LoadAsync(CancellationToken.None);
        var loadedItem = Assert.Single(viewModel.Items);
        var version = Assert.Single(loadedItem.Versions);
        Assert.Equal("Setup required", version.AutoFitReadinessLabel);
        Assert.False(version.CanAdjustToSitePlan);

        var adjustment = await viewModel.OpenVersionSitePlanAdjustmentAsync(
            loadedItem,
            version,
            "site.dxf",
            CancellationToken.None);

        Assert.Null(adjustment);
        Assert.Equal(
            "Complete Auto-fit setup before adjusting this floor plan to a site plan.",
            viewModel.StatusMessage);
        Assert.Null(viewModel.ActiveSitePlanAdjustmentViewModel);
    }

    [Fact]
    public async Task LoadAsync_propagates_auto_fit_readiness_cancellation()
    {
        var versionId = Guid.NewGuid();
        var curationId = Guid.NewGuid();
        var item = CreateLibraryItem(
            Guid.NewGuid(),
            versionId,
            "Published",
            activePublishedCurationId: curationId);
        var viewModel = CreateReadinessLibraryViewModel(item, new AutoFitProfileRepository([]));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => viewModel.LoadAsync(cancellation.Token));
    }

    [Fact]
    public void FloorPlanLibraryVersionDto_surfaces_curation_history_and_blocks_reextract()
    {
        var version = new FloorPlanLibraryVersionDto(
            Guid.NewGuid(),
            VersionNumber: 1,
            Status: "Curated Draft",
            new DateTime(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc),
            "inch",
            IsCurrent: true,
            ActivePublishedCurationId: Guid.NewGuid(),
            ActivePublishedCurationVersion: 11,
            PublishedCurationCount: 11,
            LatestPublishedCurationVersion: 11,
            LatestDraftCurationVersion: 12);

        Assert.Equal("Published v11 · Draft v12 · 11 published total", version.CurationHistoryLabel);
        Assert.False(version.CanExtract);
    }

    [Fact]
    public async Task ImportDependentSheetAsync_imports_classified_sheet_into_selected_plan_set_version()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var unitOfWork = new FakeUnitOfWork();
        var housePlanSets = new InMemoryHousePlanSetRepository();
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        var planSheets = new CapturingPlanSheetRepository();

        var services = new ServiceCollection();
        services.AddSingleton<IDxfGateway>(new FakeDxfGateway());
        services.AddSingleton<IManagedFileStorage>(new FakeManagedFileStorage());
        services.AddSingleton<IImportedDocumentRepository>(new CapturingImportedDocumentRepository());
        services.AddSingleton<IMeasurementContextRepository>(new CapturingMeasurementContextRepository());
        services.AddSingleton<IPlanSheetRepository>(planSheets);
        services.AddSingleton<IPlanSheetReader>(planSheets);
        services.AddSingleton<IFileHashService>(new FakeHashService());
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddTransient<ClassifyPlanSheetHandler>();
        services.AddTransient<ImportPlanSheetHandler>();
        services.AddTransient<ResolveHousePlanSetHandler>();
        services.AddTransient<ResolvePlanSetVersionHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        var response = await viewModel.ImportDependentSheetAsync(
            @"C:\plans\seminole-electrical.dxf",
            sheetType: string.Empty,
            name: null,
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("ElectricalPlan", response!.SheetType);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Single(housePlanSets.Items);
        var planSetVersion = Assert.Single(planSetVersions.Items);
        var sheet = Assert.Single(planSheets.Items);
        Assert.Equal(planSetVersion.Id, sheet.PlanSetVersionId);
        Assert.Equal(PlanSheetType.ElectricalPlan, sheet.SheetType);
        Assert.Equal("seminole-electrical", sheet.Name);
        Assert.Contains("ElectricalPlan", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(templateId, viewModel.SelectedItem?.TemplateId);
        Assert.Equal(versionId, viewModel.SelectedVersion?.VersionId);
        Assert.Contains(viewModel.SelectedPlanSetSheets, sheet => !sheet.IsCanonical && sheet.SheetType == "ElectricalPlan");
        Assert.Contains("2 sheet", viewModel.SelectedPlanSetSheetsLabel, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnlinkDependentSheetAsync_removes_unregistered_dependent_sheet_and_refreshes_selected_set()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            templateId,
            "santa-barbara",
            "SANTA-BARBARA",
            new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        var planSetVersion = new PlanSetVersion(
            planSetVersionId,
            housePlanSet.Id,
            versionId,
            versionNumber: 1,
            new DateTime(2026, 7, 1, 12, 5, 0, DateTimeKind.Utc));
        var electricalSheet = new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            PlanSheetType.ElectricalPlan,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ELECTRICAL PLAN SEMINOLE 2000",
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 12, 10, 0, DateTimeKind.Utc));
        var dependentSheet = new PlanSetSheetDto(
            electricalSheet.Id,
            "ElectricalPlan",
            electricalSheet.Name,
            electricalSheet.ImportedDocumentId,
            SourceFloorPlanVersionId: null,
            IsCanonical: false,
            RegistrationStatus: "Unregistered",
            ProjectionStatus: "NotProjected");
        var unitOfWork = new FakeUnitOfWork();
        var housePlanSets = new InMemoryHousePlanSetRepository();
        housePlanSets.Items.Add(housePlanSet);
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        planSetVersions.Items.Add(planSetVersion);
        var planSheets = new CapturingPlanSheetRepository();
        planSheets.Items.Add(electricalSheet);
        var registrations = new CapturingSheetRegistrationRepository();
        var projections = new CapturingSheetAdjustmentProjectionRepository();

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton<IPlanSheetRepository>(planSheets);
        services.AddSingleton<IPlanSheetReader>(planSheets);
        services.AddSingleton<ISheetRegistrationRepository>(registrations);
        services.AddSingleton<ISheetAdjustmentProjectionRepository>(projections);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddTransient<UnlinkPlanSheetHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        await viewModel.UnlinkDependentSheetAsync(dependentSheet, CancellationToken.None);

        Assert.Empty(planSheets.Items);
        Assert.Empty(registrations.Items);
        Assert.Empty(projections.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Equal(templateId, viewModel.SelectedItem?.TemplateId);
        Assert.Equal(versionId, viewModel.SelectedVersion?.VersionId);
        Assert.DoesNotContain(viewModel.SelectedPlanSetSheets, sheet => sheet.SheetId == electricalSheet.Id);
        Assert.Contains("Unlinked ElectricalPlan sheet", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnlinkDependentSheetAsync_delegates_confirmed_projected_sheet_to_safe_handler_and_refreshes_selected_set()
    {
        Assert.Equal(
            new[] { "SheetId" },
            typeof(UnlinkPlanSheetRequest)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());

        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            templateId,
            "santa-barbara",
            "SANTA-BARBARA",
            new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        var planSetVersion = new PlanSetVersion(
            planSetVersionId,
            housePlanSet.Id,
            versionId,
            versionNumber: 1,
            new DateTime(2026, 7, 1, 12, 5, 0, DateTimeKind.Utc));
        var electricalSheet = new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            PlanSheetType.ElectricalPlan,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "ELECTRICAL PLAN SEMINOLE 2000",
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 12, 10, 0, DateTimeKind.Utc));
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            planSetVersionId,
            electricalSheet.Id,
            versionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.95m,
            SheetRegistrationStatus.Confirmed,
            new DateTime(2026, 7, 1, 12, 15, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 1, 12, 16, 0, DateTimeKind.Utc),
            warning: null);
        var projection = new SheetAdjustmentProjection(
            Guid.NewGuid(),
            planSetVersionId,
            electricalSheet.Id,
            registration.Id,
            Guid.NewGuid(),
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
            0.95m,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            warning: null,
            canonicalCompressionStepCount: 0,
            new DateTime(2026, 7, 1, 12, 20, 0, DateTimeKind.Utc));
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var dependentSheet = new PlanSetSheetDto(
            electricalSheet.Id,
            "ElectricalPlan",
            electricalSheet.Name,
            electricalSheet.ImportedDocumentId,
            SourceFloorPlanVersionId: versionId,
            IsCanonical: false,
            RegistrationStatus: "Confirmed",
            ProjectionStatus: "ReadyForExport",
            SheetRegistrationId: registration.Id,
            SheetProjectionId: projection.Id);
        var unitOfWork = new FakeUnitOfWork();
        var planSheets = new CapturingPlanSheetRepository();
        planSheets.Items.Add(electricalSheet);
        var registrations = new CapturingSheetRegistrationRepository();
        registrations.Items.Add(registration);
        var projections = new CapturingSheetAdjustmentProjectionRepository();
        projections.Items.Add(projection);
        var housePlanSets = new InMemoryHousePlanSetRepository();
        housePlanSets.Items.Add(housePlanSet);
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        planSetVersions.Items.Add(planSetVersion);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton<IPlanSheetRepository>(planSheets);
        services.AddSingleton<IPlanSheetReader>(planSheets);
        services.AddSingleton<ISheetRegistrationRepository>(registrations);
        services.AddSingleton<ISheetAdjustmentProjectionRepository>(projections);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddTransient<UnlinkPlanSheetHandler>();
        services.AddTransient<ResolveHousePlanSetHandler>();
        services.AddTransient<ResolvePlanSetVersionHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        await viewModel.UnlinkDependentSheetAsync(dependentSheet, CancellationToken.None);

        Assert.Empty(planSheets.Items);
        Assert.Empty(registrations.Items);
        Assert.Empty(projections.Items);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Equal(templateId, viewModel.SelectedItem?.TemplateId);
        Assert.Equal(versionId, viewModel.SelectedVersion?.VersionId);
        Assert.DoesNotContain(viewModel.SelectedPlanSetSheets, sheet => sheet.SheetId == electricalSheet.Id);
        Assert.Contains("Unlinked ElectricalPlan sheet", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CorrectDependentSheetTypeAsync_updates_unregistered_sheet_type_and_refreshes_selected_set()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            templateId,
            "santa-barbara",
            "SANTA-BARBARA",
            new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc));
        var planSetVersion = new PlanSetVersion(
            planSetVersionId,
            housePlanSet.Id,
            versionId,
            versionNumber: 1,
            new DateTime(2026, 7, 1, 12, 5, 0, DateTimeKind.Utc));
        var roofSheet = new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            PlanSheetType.RoofPlan,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Wrong type",
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 12, 10, 0, DateTimeKind.Utc));
        var dependentSheet = new PlanSetSheetDto(
            roofSheet.Id,
            "RoofPlan",
            roofSheet.Name,
            roofSheet.ImportedDocumentId,
            SourceFloorPlanVersionId: null,
            IsCanonical: false,
            RegistrationStatus: "Unregistered",
            ProjectionStatus: "NotProjected");
        var unitOfWork = new FakeUnitOfWork();
        var housePlanSets = new InMemoryHousePlanSetRepository();
        housePlanSets.Items.Add(housePlanSet);
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        planSetVersions.Items.Add(planSetVersion);
        var planSheets = new CapturingPlanSheetRepository();
        planSheets.Items.Add(roofSheet);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton<IPlanSheetRepository>(planSheets);
        services.AddSingleton<IPlanSheetReader>(planSheets);
        services.AddSingleton<ISheetRegistrationRepository>(new CapturingSheetRegistrationRepository());
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 1, 12, 30, 0, DateTimeKind.Utc)));
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddTransient<CorrectPlanSheetTypeHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();
        services.AddTransient<ResolveHousePlanSetHandler>();
        services.AddTransient<ResolvePlanSetVersionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        var response = await viewModel.CorrectDependentSheetTypeAsync(
            dependentSheet,
            "ElectricalPlan",
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("ElectricalPlan", response!.SheetType);
        Assert.Equal(PlanSheetType.ElectricalPlan, Assert.Single(planSheets.Items).SheetType);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Equal(templateId, viewModel.SelectedItem?.TemplateId);
        Assert.Equal(versionId, viewModel.SelectedVersion?.VersionId);
        Assert.Contains(viewModel.SelectedPlanSetSheets, sheet =>
            sheet.SheetId == roofSheet.Id &&
            sheet.SheetType == "ElectricalPlan" &&
            sheet.RegistrationStatus == "Unregistered");
        Assert.Contains("Changed RoofPlan sheet Wrong type to ElectricalPlan", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RegisterDependentSheetAsync_ignores_caller_geometry_and_uses_electrical_estimator_evidence()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        const string canonicalFloorSourcePath = @"C:\library\canonical-floor-source.dxf";
        const string electricalSourcePath = @"C:\library\dependent-electrical-source.dxf";
        var estimatedTransform = new SheetRegistrationTransform(0.98m, 90m, 7m, -4m);
        const string estimatorEvidence = "Unique generic structural evidence.";
        var estimator = new FakeElectricalFloorRegistrationEstimator(
            new ElectricalFloorRegistrationEstimate(
                Status: ElectricalFloorRegistrationEstimateStatus.Estimated,
                Transform: estimatedTransform,
                Confidence: 0.94m,
                ObservedScaleX: 0.98m,
                ObservedScaleY: 0.98m,
                HorizontalCoverage: 0.96m,
                VerticalCoverage: 0.93m,
                RootMeanSquareResidual: 0.01m,
                MaximumResidual: 0.02m,
                EvidenceSummary: estimatorEvidence,
                Candidates:
                [
                    new ElectricalFloorRegistrationCandidateEvidence(
                        RotationDegrees: 90m,
                        Accepted: true,
                        Scale: 0.98m,
                        TranslateX: 7m,
                        TranslateY: -4m,
                        HorizontalCoverage: 0.96m,
                        VerticalCoverage: 0.93m,
                        RootMeanSquareResidual: 0.01m,
                        MaximumResidual: 0.02m,
                        LeftEdgeResidual: 0.03m,
                        RightEdgeResidual: 0.04m,
                        BottomEdgeResidual: 0.05m,
                        TopEdgeResidual: 0.06m,
                        Reason: "Accepted unique generic evidence.")
                ],
                CanonicalSourceSha256: new string('a', 64),
                DependentSourceSha256: new string('b', 64)));
        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            templateId,
            "sample-house",
            "Sample House",
            new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc));
        var electricalSheet = new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            PlanSheetType.ElectricalPlan,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Electrical Sheet",
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 11, 30, 0, DateTimeKind.Utc));
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var dependentSheet = new PlanSetSheetDto(
            electricalSheet.Id,
            "ElectricalPlan",
            "Electrical Sheet",
            electricalSheet.ImportedDocumentId,
            versionId,
            IsCanonical: false,
            RegistrationStatus: "Unregistered",
            ProjectionStatus: "NotProjected");
        var sheetRepository = new CapturingPlanSheetRepository();
        sheetRepository.Items.Add(electricalSheet);
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var housePlanSets = new InMemoryHousePlanSetRepository();
        housePlanSets.Items.Add(housePlanSet);
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        planSetVersions.Items.Add(new PlanSetVersion(
            planSetVersionId,
            housePlanSet.Id,
            versionId,
            versionNumber: 1,
            new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc)));

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton<IPlanSheetRepository>(sheetRepository);
        services.AddSingleton<IPlanSheetReader>(sheetRepository);
        services.AddSingleton<ISheetRegistrationRepository>(registrationRepository);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 1, 11, 45, 0, DateTimeKind.Utc)));
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddSingleton<IFloorPlanExtractionSourceReader>(
            new ExactFloorPlanExtractionSourceReader(
                new FloorPlanExtractionSource(templateId, versionId, canonicalFloorSourcePath)));
        services.AddSingleton<IPlanSheetSourceReader>(
            new ExactPlanSheetSourceReader(
                new PlanSheetSourceDto(
                    electricalSheet.Id,
                    "ElectricalPlan",
                    "Electrical Sheet",
                    electricalSheet.ImportedDocumentId,
                    electricalSourcePath)));
        services.AddSingleton<IElectricalFloorRegistrationEstimator>(estimator);
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();
        services.AddTransient<RegisterElectricalSheetHandler>();
        services.AddTransient<RegisterRoofSheetHandler>();
        services.AddTransient<RegisterFacadeElevationSheetHandler>();
        services.AddTransient<ResolveHousePlanSetHandler>();
        services.AddTransient<ResolvePlanSetVersionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        var response = await viewModel.RegisterDependentSheetAsync(
            dependentSheet,
            new SheetRegistrationTransformDto(1.75m, 17m, 125m, -80m),
            confidence: 0.12m,
            confirmRegistration: false,
            overhangInches: 0m,
            horizontalReferenceName: null,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("WholeSheetSimilarity", response!.Method);
        Assert.Equal("PendingConfirmation", response.Status);
        Assert.Equal(planSetVersionId, response.PlanSetVersionId);
        Assert.Equal(electricalSheet.Id, response.DependentSheetId);
        Assert.Equal(estimatedTransform.Scale, response.Transform.Scale);
        Assert.Equal(estimatedTransform.RotationDegrees, response.Transform.RotationDegrees);
        Assert.Equal(estimatedTransform.TranslateX, response.Transform.TranslateX);
        Assert.Equal(estimatedTransform.TranslateY, response.Transform.TranslateY);
        Assert.Equal(0.94m, response.Confidence);
        Assert.Equal(estimatorEvidence, response.RuleSummary);
        var registration = Assert.Single(registrationRepository.Items);
        Assert.Equal(SheetRegistrationMethod.WholeSheetSimilarity, registration.Method);
        Assert.Equal(estimatedTransform.Scale, registration.Transform.Scale);
        Assert.Equal(estimatedTransform.RotationDegrees, registration.Transform.RotationDegrees);
        Assert.Equal(estimatedTransform.TranslateX, registration.Transform.TranslateX);
        Assert.Equal(estimatedTransform.TranslateY, registration.Transform.TranslateY);
        Assert.Equal(0.94m, registration.Confidence);
        Assert.Equal(estimatorEvidence, registration.RuleSummary);
        Assert.NotEqual(1.75m, registration.Transform.Scale);
        Assert.NotEqual(0.12m, registration.Confidence);
        Assert.Equal(canonicalFloorSourcePath, estimator.FloorPlanSourcePath);
        Assert.Equal(electricalSourcePath, estimator.ElectricalSheetSourcePath);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Contains("PendingConfirmation", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains(viewModel.SelectedPlanSetSheets, sheet =>
            sheet.SheetId == electricalSheet.Id &&
            sheet.RegistrationStatus == "PendingConfirmation" &&
            sheet.ProjectionStatus == "NotProjected");
    }

    [Theory]
    [InlineData("RoofPlan", "RoofFootprintWithOverhang")]
    [InlineData("FacadeElevation", "FacadeHorizontalReference")]
    public async Task RegisterDependentSheetAsync_registers_roof_and_facade_sheets_against_selected_plan_set_version(
        string sheetType,
        string expectedMethod)
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            templateId,
            "seminole",
            "Seminole",
            new DateTime(2026, 7, 1, 15, 0, 0, DateTimeKind.Utc));
        var planSheetType = Enum.Parse<PlanSheetType>(sheetType);
        var dependentPlanSheet = new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            planSheetType,
            Guid.NewGuid(),
            Guid.NewGuid(),
            sheetType,
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 15, 5, 0, DateTimeKind.Utc));
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var dependentSheet = new PlanSetSheetDto(
            dependentPlanSheet.Id,
            sheetType,
            sheetType,
            dependentPlanSheet.ImportedDocumentId,
            versionId,
            IsCanonical: false,
            RegistrationStatus: "Unregistered",
            ProjectionStatus: "NotProjected");
        var sheetRepository = new CapturingPlanSheetRepository();
        sheetRepository.Items.Add(dependentPlanSheet);
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var housePlanSets = new InMemoryHousePlanSetRepository();
        housePlanSets.Items.Add(housePlanSet);
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        planSetVersions.Items.Add(new PlanSetVersion(
            planSetVersionId,
            housePlanSet.Id,
            versionId,
            versionNumber: 1,
            new DateTime(2026, 7, 1, 15, 0, 0, DateTimeKind.Utc)));

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton<IPlanSheetRepository>(sheetRepository);
        services.AddSingleton<IPlanSheetReader>(sheetRepository);
        services.AddSingleton<ISheetRegistrationRepository>(registrationRepository);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 1, 15, 10, 0, DateTimeKind.Utc)));
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();
        services.AddTransient<RegisterElectricalSheetHandler>();
        services.AddTransient<RegisterRoofSheetHandler>();
        services.AddTransient<RegisterFacadeElevationSheetHandler>();
        services.AddTransient<ResolveHousePlanSetHandler>();
        services.AddTransient<ResolvePlanSetVersionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        var response = await viewModel.RegisterDependentSheetAsync(
            dependentSheet,
            new SheetRegistrationTransformDto(1m, 0m, 0m, 0m),
            confidence: 0.25m,
            confirmRegistration: false,
            overhangInches: 0m,
            horizontalReferenceName: null,
            cancellationToken: CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(expectedMethod, response!.Method);
        Assert.Equal("PendingConfirmation", response.Status);
        Assert.Equal(planSetVersionId, response.PlanSetVersionId);
        Assert.Equal(dependentPlanSheet.Id, response.DependentSheetId);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Contains(viewModel.SelectedPlanSetSheets, sheet =>
            sheet.SheetId == dependentPlanSheet.Id &&
            sheet.RegistrationStatus == "PendingConfirmation");
    }

    [Fact]
    public async Task ConfirmDependentSheetRegistrationAsync_confirms_pending_registration_and_refreshes_selected_set()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            templateId,
            "seminole",
            "Seminole",
            new DateTime(2026, 7, 1, 13, 0, 0, DateTimeKind.Utc));
        var electricalSheet = new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            PlanSheetType.ElectricalPlan,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Electrical",
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 13, 5, 0, DateTimeKind.Utc));
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            planSetVersionId,
            electricalSheet.Id,
            versionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.25m,
            SheetRegistrationStatus.PendingConfirmation,
            new DateTime(2026, 7, 1, 13, 10, 0, DateTimeKind.Utc),
            confirmedAtUtc: null,
            warning: "Needs manual review");
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var dependentSheet = new PlanSetSheetDto(
            electricalSheet.Id,
            "ElectricalPlan",
            "Electrical",
            electricalSheet.ImportedDocumentId,
            versionId,
            IsCanonical: false,
            RegistrationStatus: "PendingConfirmation",
            ProjectionStatus: "NotProjected",
            SheetRegistrationId: registration.Id);
        var sheetRepository = new CapturingPlanSheetRepository();
        sheetRepository.Items.Add(electricalSheet);
        var registrationRepository = new CapturingSheetRegistrationRepository();
        registrationRepository.Items.Add(registration);
        var unitOfWork = new FakeUnitOfWork();
        var housePlanSets = new InMemoryHousePlanSetRepository();
        housePlanSets.Items.Add(housePlanSet);
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        planSetVersions.Items.Add(new PlanSetVersion(
            planSetVersionId,
            housePlanSet.Id,
            versionId,
            versionNumber: 1,
            new DateTime(2026, 7, 1, 13, 0, 0, DateTimeKind.Utc)));

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton<IPlanSheetRepository>(sheetRepository);
        services.AddSingleton<IPlanSheetReader>(sheetRepository);
        services.AddSingleton<ISheetRegistrationRepository>(registrationRepository);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 1, 13, 15, 0, DateTimeKind.Utc)));
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddTransient<ConfirmSheetRegistrationHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        await viewModel.ConfirmDependentSheetRegistrationAsync(dependentSheet, CancellationToken.None);

        var confirmed = Assert.Single(registrationRepository.Items);
        Assert.Equal(SheetRegistrationStatus.Confirmed, confirmed.Status);
        Assert.NotNull(confirmed.ConfirmedAtUtc);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Contains(viewModel.SelectedPlanSetSheets, sheet =>
            sheet.SheetId == electricalSheet.Id &&
            sheet.RegistrationStatus == "Confirmed" &&
            sheet.SheetRegistrationId == registration.Id);
        Assert.Contains("Confirmed ElectricalPlan sheet", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RejectDependentSheetRegistrationAsync_rejects_pending_registration_and_refreshes_selected_set()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            templateId,
            "seminole",
            "Seminole",
            new DateTime(2026, 7, 1, 13, 30, 0, DateTimeKind.Utc));
        var electricalSheet = new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            PlanSheetType.ElectricalPlan,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Electrical",
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 13, 35, 0, DateTimeKind.Utc));
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            planSetVersionId,
            electricalSheet.Id,
            versionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.25m,
            SheetRegistrationStatus.PendingConfirmation,
            new DateTime(2026, 7, 1, 13, 40, 0, DateTimeKind.Utc),
            confirmedAtUtc: null,
            warning: "Needs manual review");
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var dependentSheet = new PlanSetSheetDto(
            electricalSheet.Id,
            "ElectricalPlan",
            "Electrical",
            electricalSheet.ImportedDocumentId,
            versionId,
            IsCanonical: false,
            RegistrationStatus: "PendingConfirmation",
            ProjectionStatus: "NotProjected",
            SheetRegistrationId: registration.Id);
        var sheetRepository = new CapturingPlanSheetRepository();
        sheetRepository.Items.Add(electricalSheet);
        var registrationRepository = new CapturingSheetRegistrationRepository();
        registrationRepository.Items.Add(registration);
        var unitOfWork = new FakeUnitOfWork();
        var housePlanSets = new InMemoryHousePlanSetRepository();
        housePlanSets.Items.Add(housePlanSet);
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        planSetVersions.Items.Add(new PlanSetVersion(
            planSetVersionId,
            housePlanSet.Id,
            versionId,
            versionNumber: 1,
            new DateTime(2026, 7, 1, 13, 30, 0, DateTimeKind.Utc)));

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton<IPlanSheetRepository>(sheetRepository);
        services.AddSingleton<IPlanSheetReader>(sheetRepository);
        services.AddSingleton<ISheetRegistrationRepository>(registrationRepository);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 1, 13, 45, 0, DateTimeKind.Utc)));
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddTransient<RejectSheetRegistrationHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        var response = await viewModel.RejectDependentSheetRegistrationAsync(dependentSheet, CancellationToken.None);

        Assert.NotNull(response);
        var rejected = Assert.Single(registrationRepository.Items);
        Assert.Equal(SheetRegistrationStatus.Rejected, rejected.Status);
        Assert.Null(rejected.ConfirmedAtUtc);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Contains(viewModel.SelectedPlanSetSheets, sheet =>
            sheet.SheetId == electricalSheet.Id &&
            sheet.RegistrationStatus == "Rejected" &&
            sheet.CanCorrectSheetType &&
            sheet.CanRegisterDependent &&
            sheet.CanUnlink);
        Assert.Contains("Rejected ElectricalPlan registration", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfirmDependentSheetProjectionAsync_confirms_manual_projection_and_refreshes_selected_set()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var housePlanSet = new HousePlanSet(
            Guid.NewGuid(),
            templateId,
            "seminole",
            "Seminole",
            new DateTime(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc));
        var electricalSheet = new PlanSheet(
            Guid.NewGuid(),
            planSetVersionId,
            PlanSheetType.ElectricalPlan,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Electrical",
            PlanSheetStatus.Imported,
            new DateTime(2026, 7, 1, 14, 5, 0, DateTimeKind.Utc));
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            planSetVersionId,
            electricalSheet.Id,
            versionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.25m,
            SheetRegistrationStatus.Confirmed,
            new DateTime(2026, 7, 1, 14, 10, 0, DateTimeKind.Utc),
            confirmedAtUtc: new DateTime(2026, 7, 1, 14, 12, 0, DateTimeKind.Utc),
            warning: "Needs manual review");
        var projection = new SheetAdjustmentProjection(
            Guid.NewGuid(),
            planSetVersionId,
            electricalSheet.Id,
            registration.Id,
            Guid.NewGuid(),
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
            0.25m,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            "Low confidence projection requires review.",
            canonicalCompressionStepCount: 0,
            new DateTime(2026, 7, 1, 14, 20, 0, DateTimeKind.Utc));
        var item = CreateLibraryItem(
            templateId,
            versionId,
            "Published",
            activePublishedCurationId: Guid.NewGuid());
        var dependentSheet = new PlanSetSheetDto(
            electricalSheet.Id,
            "ElectricalPlan",
            "Electrical",
            electricalSheet.ImportedDocumentId,
            versionId,
            IsCanonical: false,
            RegistrationStatus: "Confirmed",
            ProjectionStatus: "RequiresManualConfirmation",
            SheetRegistrationId: registration.Id,
            SheetProjectionId: projection.Id);
        var sheetRepository = new CapturingPlanSheetRepository();
        sheetRepository.Items.Add(electricalSheet);
        var registrationRepository = new CapturingSheetRegistrationRepository();
        registrationRepository.Items.Add(registration);
        var projectionRepository = new CapturingSheetAdjustmentProjectionRepository();
        projectionRepository.Items.Add(projection);
        var unitOfWork = new FakeUnitOfWork();
        var housePlanSets = new InMemoryHousePlanSetRepository();
        housePlanSets.Items.Add(housePlanSet);
        var planSetVersions = new InMemoryPlanSetVersionRepository();
        planSetVersions.Items.Add(new PlanSetVersion(
            planSetVersionId,
            housePlanSet.Id,
            versionId,
            versionNumber: 1,
            new DateTime(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc)));

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader([item]));
        services.AddSingleton<IPlanSheetRepository>(sheetRepository);
        services.AddSingleton<IPlanSheetReader>(sheetRepository);
        services.AddSingleton<ISheetRegistrationRepository>(registrationRepository);
        services.AddSingleton<ISheetAdjustmentProjectionRepository>(projectionRepository);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 7, 1, 14, 25, 0, DateTimeKind.Utc)));
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IHousePlanSetRepository>(housePlanSets);
        services.AddSingleton<IPlanSetVersionRepository>(planSetVersions);
        services.AddTransient<ConfirmSheetAdjustmentProjectionHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();
        services.AddTransient<GetPlanSetLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = item,
            SelectedVersion = item.Versions[0]
        };

        await viewModel.ConfirmDependentSheetProjectionAsync(dependentSheet, CancellationToken.None);

        var confirmed = Assert.Single(projectionRepository.Items);
        Assert.Equal(SheetAdjustmentProjectionStatus.ReadyForExport, confirmed.Status);
        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Contains(viewModel.SelectedPlanSetSheets, sheet =>
            sheet.SheetId == electricalSheet.Id &&
            sheet.ProjectionStatus == "ReadyForExport" &&
            sheet.SheetProjectionId == projection.Id);
        Assert.Contains("Confirmed ElectricalPlan projection", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractSelectedAsync_uses_the_current_version_source_and_refreshes_the_library()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var extractionSource = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SANTA-BARBARA.dxf");
        var unitOfWork = new FakeUnitOfWork();
        var runRepository = new InMemoryWallExtractionRunRepository();
        var candidateRepository = new InMemoryExtractedWallCandidateRepository();
        var roomLabelRepository = new InMemoryExtractedRoomLabelRepository();
        var fixedPlanComponentRepository = new InMemoryExtractedFixedPlanComponentRepository();
        var protectedDetailRepository = new InMemoryExtractedProtectedDetailAssemblyRepository();
        var dimensionRepository = new InMemoryExtractedDimensionRepository();

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanExtractionSourceReader>(new FakeFloorPlanExtractionSourceReader(extractionSource));
        services.AddSingleton<IWallExtractor>(new FakeWallExtractor(
        [
            new DetectedWallCandidate(
                "LINE:1",
                "WALLS",
                [new GeometryPoint(0m, 0m), new GeometryPoint(120m, 0m)],
                null,
                0.95m,
                null)
        ]));
        services.AddSingleton<IRoomLabelExtractor>(new FakeRoomLabelExtractor(
        [
            new DetectedRoomLabel(
                "TEXT:1",
                "ROOM LBLS",
                "KITCHEN",
                10m,
                20m,
                0.95m,
                null)
        ]));
        services.AddSingleton<IOpeningExtractor>(new FakeOpeningExtractor(new DetectedOpeningExtraction([], [])));
        services.AddSingleton<IFixedPlanComponentExtractor>(new FakeFixedPlanComponentExtractor(
        [
            new DetectedFixedPlanComponent(
                "INSERT:1",
                "FIXTURES",
                "Toilet",
                "INSERT",
                "TOILET1",
                [[new GeometryPoint(40m, 10m), new GeometryPoint(76m, 10m)]],
                0.95m,
                null)
        ]));
        services.AddSingleton<IProtectedDetailAssemblyExtractor>(new FakeProtectedDetailAssemblyExtractor([]));
        services.AddSingleton<IDimensionExtractor>(new FakeDimensionExtractor(
        [
            new DetectedDimension(
                "DIMENSION:1",
                "DIMS",
                "DIMENSION",
                "*D169",
                "10'-4\"",
                "GeometryBlock",
                string.Empty,
                123.810387305188m,
                3144.7838375517752m,
                "Inch",
                0,
                0m,
                0m,
                94.5741888255622m,
                516.95664946623m,
                0m,
                218.38457613075m,
                524.795084103958m,
                0m,
                94.5741888255622m,
                537.195356591169m,
                0.0000000000000074m,
                0.99m,
                null)
        ]));
        services.AddSingleton<IWallExtractionRunRepository>(runRepository);
        services.AddSingleton<IExtractedWallCandidateRepository>(candidateRepository);
        services.AddSingleton<IExtractedRoomLabelRepository>(roomLabelRepository);
        services.AddSingleton<IExtractedOpeningCandidateRepository>(new InMemoryExtractedOpeningCandidateRepository());
        services.AddSingleton<IExtractedOpeningLabelRepository>(new InMemoryExtractedOpeningLabelRepository());
        services.AddSingleton<IExtractedFixedPlanComponentRepository>(fixedPlanComponentRepository);
        services.AddSingleton<IExtractedProtectedDetailAssemblyRepository>(protectedDetailRepository);
        services.AddSingleton<IExtractedDimensionRepository>(dimensionRepository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 4, 30, 19, 30, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanLibraryReader>(new FakeFloorPlanLibraryReader(
        [
            CreateLibraryItem(templateId, versionId, "Extracted")
        ]));
        services.AddTransient<ExtractWallCandidatesHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = CreateLibraryItem(templateId, versionId, "Imported")
        };

        await viewModel.ExtractSelectedAsync(CancellationToken.None);

        Assert.True(unitOfWork.SaveChangesCalled);
        Assert.Single(runRepository.Items);
        Assert.Single(candidateRepository.Items);
        Assert.Single(roomLabelRepository.Items);
        Assert.Single(fixedPlanComponentRepository.Items);
        Assert.Single(dimensionRepository.Items);
        Assert.Single(viewModel.Items);
        Assert.Equal("Extracted", viewModel.Items[0].Status);
    }

    [Fact]
    public async Task ExtractSelectedAsync_blocks_reextract_when_selected_version_has_curation_history()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var services = new ServiceCollection();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = CreateLibraryItem(
                templateId,
                versionId,
                "Curated Draft",
                activePublishedCurationId: Guid.NewGuid(),
                activePublishedCurationVersion: 11,
                publishedCurationCount: 11,
                latestPublishedCurationVersion: 11,
                latestDraftCurationVersion: 12)
        };

        await viewModel.ExtractSelectedAsync(CancellationToken.None);

        Assert.StartsWith("Re-extract blocked", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Contains("Published v11 · Draft v12", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    private sealed class FakeRoomLabelExtractor : IRoomLabelExtractor
    {
        private readonly IReadOnlyList<DetectedRoomLabel> items;

        public FakeRoomLabelExtractor(IReadOnlyList<DetectedRoomLabel> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedRoomLabel>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class FakeOpeningExtractor : IOpeningExtractor
    {
        private readonly DetectedOpeningExtraction extraction;

        public FakeOpeningExtractor(DetectedOpeningExtraction extraction)
        {
            this.extraction = extraction;
        }

        public Task<DetectedOpeningExtraction> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(extraction);
        }
    }

    private sealed class FakeFixedPlanComponentExtractor : IFixedPlanComponentExtractor
    {
        private readonly IReadOnlyList<DetectedFixedPlanComponent> items;

        public FakeFixedPlanComponentExtractor(IReadOnlyList<DetectedFixedPlanComponent> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedFixedPlanComponent>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class FakeProtectedDetailAssemblyExtractor : IProtectedDetailAssemblyExtractor
    {
        private readonly IReadOnlyList<DetectedProtectedDetailAssembly> items;

        public FakeProtectedDetailAssemblyExtractor(IReadOnlyList<DetectedProtectedDetailAssembly> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedProtectedDetailAssembly>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class FakeDimensionExtractor : IDimensionExtractor
    {
        private readonly IReadOnlyList<DetectedDimension> items;

        public FakeDimensionExtractor(IReadOnlyList<DetectedDimension> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedDimension>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    [Fact]
    public async Task OpenSelectedReviewAsync_returns_a_loaded_review_view_model()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var template = new FloorPlanTemplate(templateId, "santa-barbara", "SANTA-BARBARA", isActive: true);
        template.SetCurrentVersion(versionId);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanVersionRepository>(new InMemoryFloorPlanVersionRepository(
            new FloorPlanVersion(
                versionId,
                templateId,
                Guid.NewGuid(),
                "fingerprint",
                1,
                new DateTime(2026, 4, 30, 17, 0, 0, DateTimeKind.Utc))));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(new FakeFloorPlanReviewSessionReader(
            new FloorPlanReviewSessionDto(
                templateId,
                "santa-barbara",
                "SANTA-BARBARA",
                "Extracted",
                1,
                null,
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [],
                [])));
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = CreateLibraryItem(templateId, versionId, "Extracted")
        };

        var reviewViewModel = await viewModel.OpenSelectedReviewAsync(CancellationToken.None);

        Assert.NotNull(reviewViewModel);
        Assert.Equal("santa-barbara", reviewViewModel.Code);
        Assert.Equal("SANTA-BARBARA", reviewViewModel.Name);
    }

    [Fact]
    public async Task OpenSelectedReviewAsync_reextracts_when_loaded_review_has_stale_dimension_payload()
    {
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var extractionSource = new FloorPlanExtractionSource(templateId, versionId, @"C:\workspace\library\raw-dxf\SEMINOLE2000.dxf");
        var template = new FloorPlanTemplate(templateId, "seminole2000", "SEMINOLE2000", isActive: true);
        template.SetCurrentVersion(versionId);
        var runRepository = new InMemoryWallExtractionRunRepository();
        var unitOfWork = new FakeUnitOfWork();

        var staleSession = new FloorPlanReviewSessionDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            "Extracted",
            1,
            null,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [])
        {
            Dimensions =
            [
                new DimensionDto(
                    Guid.NewGuid(),
                    "DIMENSION:1",
                    "DIMS",
                    "DIMENSION",
                    "*D169",
                    "5'-8\"",
                    "GeometryBlock",
                    string.Empty,
                    68m,
                    1727.2m,
                    "Inch",
                    160,
                    0m,
                    0m,
                    440m,
                    520m,
                    0m,
                    372m,
                    526m,
                    0m,
                    372m,
                    516m,
                    0m,
                    0.99m,
                    null,
                    1)
            ]
        };

        var refreshedSession = staleSession with
        {
            Dimensions =
            [
                staleSession.Dimensions[0] with
                {
                    RenderTextX = 408.8m,
                    RenderTextY = 518.9m,
                    RenderTextHeight = 3.5m,
                    RenderTextAttachmentPoint = "MiddleCenter",
                    LineSegments =
                    [
                        new DimensionLineSegmentDto(440m, 520m, 440m, 513m),
                        new DimensionLineSegmentDto(372m, 525m, 372m, 513m),
                        new DimensionLineSegmentDto(437m, 517m, 376m, 517m)
                    ]
                }
            ]
        };

        var reviewReader = new SequencedFloorPlanReviewSessionReader(staleSession, refreshedSession);

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanTemplateRepository>(new InMemoryFloorPlanTemplateRepository(template));
        services.AddSingleton<IFloorPlanVersionRepository>(new InMemoryFloorPlanVersionRepository(
            new FloorPlanVersion(
                versionId,
                templateId,
                Guid.NewGuid(),
                "fingerprint",
                1,
                new DateTime(2026, 4, 30, 17, 0, 0, DateTimeKind.Utc))));
        services.AddSingleton<IFloorPlanCurationRepository>(new InMemoryFloorPlanCurationRepository());
        services.AddSingleton<IFloorPlanExtractionSourceReader>(new FakeFloorPlanExtractionSourceReader(extractionSource));
        services.AddSingleton<IWallExtractor>(new FakeWallExtractor(
        [
            new DetectedWallCandidate("LINE:1", "WALLS", [new GeometryPoint(0m, 0m), new GeometryPoint(120m, 0m)], null, 0.95m, null)
        ]));
        services.AddSingleton<IRoomLabelExtractor>(new FakeRoomLabelExtractor([]));
        services.AddSingleton<IOpeningExtractor>(new FakeOpeningExtractor(new DetectedOpeningExtraction([], [])));
        services.AddSingleton<IFixedPlanComponentExtractor>(new FakeFixedPlanComponentExtractor([]));
        services.AddSingleton<IProtectedDetailAssemblyExtractor>(new FakeProtectedDetailAssemblyExtractor([]));
        services.AddSingleton<IDimensionExtractor>(new FakeDimensionExtractor(
        [
            new DetectedDimension(
                "DIMENSION:1",
                "DIMS",
                "DIMENSION",
                "*D169",
                "5'-8\"",
                "GeometryBlock",
                string.Empty,
                68m,
                1727.2m,
                "Inch",
                160,
                0m,
                0m,
                440m,
                520m,
                0m,
                372m,
                526m,
                0m,
                372m,
                516m,
                0m,
                0.99m,
                null)
            {
                RenderTextX = 408.8m,
                RenderTextY = 518.9m,
                RenderTextHeight = 3.5m,
                RenderTextAttachmentPoint = "MiddleCenter",
                LineSegments =
                [
                    new DetectedDimensionLineSegment(440m, 520m, 440m, 513m),
                    new DetectedDimensionLineSegment(372m, 525m, 372m, 513m),
                    new DetectedDimensionLineSegment(437m, 517m, 376m, 517m)
                ]
            }
        ]));
        services.AddSingleton<IWallExtractionRunRepository>(runRepository);
        services.AddSingleton<IExtractedWallCandidateRepository>(new InMemoryExtractedWallCandidateRepository());
        services.AddSingleton<IExtractedRoomLabelRepository>(new InMemoryExtractedRoomLabelRepository());
        services.AddSingleton<IExtractedOpeningCandidateRepository>(new InMemoryExtractedOpeningCandidateRepository());
        services.AddSingleton<IExtractedOpeningLabelRepository>(new InMemoryExtractedOpeningLabelRepository());
        services.AddSingleton<IExtractedFixedPlanComponentRepository>(new InMemoryExtractedFixedPlanComponentRepository());
        services.AddSingleton<IExtractedProtectedDetailAssemblyRepository>(new InMemoryExtractedProtectedDetailAssemblyRepository());
        services.AddSingleton<IExtractedDimensionRepository>(new InMemoryExtractedDimensionRepository());
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IClock>(new FakeClock(new DateTime(2026, 4, 30, 20, 0, 0, DateTimeKind.Utc)));
        services.AddSingleton<IFloorPlanReviewSessionReader>(reviewReader);
        services.AddTransient<StartOrResumeCurationHandler>();
        services.AddTransient<OpenFloorPlanReviewSessionHandler>();
        services.AddTransient<GetFloorPlanReviewSessionHandler>();
        services.AddTransient<ExtractWallCandidatesHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>())
        {
            SelectedItem = CreateLibraryItem(templateId, versionId, "Extracted")
        };

        var reviewViewModel = await viewModel.OpenSelectedReviewAsync(CancellationToken.None);

        Assert.NotNull(reviewViewModel);
        Assert.Equal(4, reviewReader.CallCount);
        Assert.Single(runRepository.Items);
        var dimension = Assert.Single(reviewViewModel.Dimensions);
        Assert.Equal(3, dimension.LineSegments.Count);
        Assert.Equal(408.8m, dimension.RenderTextX);
    }

    [Fact]
    public async Task DeleteVersionAsync_removes_the_selected_version_and_refreshes_the_library()
    {
        var templateId = Guid.NewGuid();
        var versionOneId = Guid.NewGuid();
        var versionTwoId = Guid.NewGuid();
        var versionRepository = new InMemoryFloorPlanVersionRepository(
            new FloorPlanVersion(
                versionOneId,
                templateId,
                Guid.NewGuid(),
                "fingerprint-one",
                1,
                new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc)),
            new FloorPlanVersion(
                versionTwoId,
                templateId,
                Guid.NewGuid(),
                "fingerprint-two",
                2,
                new DateTime(2026, 5, 9, 13, 0, 0, DateTimeKind.Utc)));
        var unitOfWork = new FakeUnitOfWork();

        var services = new ServiceCollection();
        services.AddSingleton<IFloorPlanVersionRepository>(versionRepository);
        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddSingleton<IFloorPlanLibraryReader>(
            new RepositoryBackedFloorPlanLibraryReader(templateId, versionRepository));
        services.AddTransient<RemoveFloorPlanVersionHandler>();
        services.AddTransient<GetFloorPlanLibraryHandler>();

        using var provider = services.BuildServiceProvider();
        var viewModel = new LibraryViewModel(provider.GetRequiredService<IServiceScopeFactory>());
        await viewModel.LoadAsync(CancellationToken.None);

        Assert.Equal("Selected: santa-barbara v2", viewModel.SelectedVersionLabel);

        await viewModel.DeleteVersionAsync(viewModel.SelectedVersion!, CancellationToken.None);

        Assert.All(versionRepository.Items, item => Assert.NotEqual(versionTwoId, item.Id));
        Assert.True(unitOfWork.SaveChangesCalled);
        var remainingItem = Assert.Single(viewModel.Items);
        var remainingVersion = Assert.Single(remainingItem.Versions);
        Assert.Equal(versionOneId, remainingVersion.VersionId);
        Assert.Equal("Selected: santa-barbara v1", viewModel.SelectedVersionLabel);
        Assert.Equal("Deleted v2", viewModel.StatusMessage);
    }

    private sealed class InMemoryExtractedRoomLabelRepository : IExtractedRoomLabelRepository
    {
        public List<ExtractedRoomLabel> Items { get; } = [];

        public Task AddRangeAsync(IReadOnlyList<ExtractedRoomLabel> labels, CancellationToken cancellationToken)
        {
            Items.AddRange(labels);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedRoomLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedRoomLabel>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid roomLabelId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == roomLabelId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedOpeningCandidateRepository : IExtractedOpeningCandidateRepository
    {
        public Task AddRangeAsync(
            IReadOnlyList<ExtractedOpeningCandidate> domainCandidates,
            IReadOnlyList<DetectedOpeningCandidate> detectedCandidates,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedOpeningCandidate>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedOpeningCandidate>>([]);
        }

        public Task RemoveAsync(Guid openingCandidateId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedOpeningLabelRepository : IExtractedOpeningLabelRepository
    {
        public Task AddRangeAsync(IReadOnlyList<ExtractedOpeningLabel> labels, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedOpeningLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedOpeningLabel>>([]);
        }

        public Task RemoveAsync(Guid openingLabelId, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedFixedPlanComponentRepository : IExtractedFixedPlanComponentRepository
    {
        public List<ExtractedFixedPlanComponent> Items { get; } = [];

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedFixedPlanComponent> domainComponents,
            IReadOnlyList<DetectedFixedPlanComponent> detectedComponents,
            CancellationToken cancellationToken)
        {
            Items.AddRange(domainComponents);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedFixedPlanComponent>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedFixedPlanComponent>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid fixedPlanComponentId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == fixedPlanComponentId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedProtectedDetailAssemblyRepository : IExtractedProtectedDetailAssemblyRepository
    {
        public List<ExtractedProtectedDetailAssembly> Items { get; } = [];

        public Task AddRangeAsync(
            IReadOnlyList<ExtractedProtectedDetailAssembly> domainAssemblies,
            IReadOnlyList<DetectedProtectedDetailAssembly> detectedAssemblies,
            CancellationToken cancellationToken)
        {
            Items.AddRange(domainAssemblies);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedProtectedDetailAssembly>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedProtectedDetailAssembly>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }

        public Task RemoveAsync(Guid protectedDetailAssemblyId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == protectedDetailAssemblyId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryExtractedDimensionRepository : IExtractedDimensionRepository
    {
        public List<ExtractedDimension> Items { get; } = [];

        public Task AddRangeAsync(IReadOnlyList<ExtractedDimension> dimensions, CancellationToken cancellationToken)
        {
            Items.AddRange(dimensions);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExtractedDimension>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<ExtractedDimension>>(
                Items.Where(item => item.WallExtractionRunId == wallExtractionRunId).ToArray());
        }
    }

    private sealed class FakeFloorPlanExtractionSourceReader : IFloorPlanExtractionSourceReader
    {
        private readonly FloorPlanExtractionSource source;

        public FakeFloorPlanExtractionSourceReader(FloorPlanExtractionSource source)
        {
            this.source = source;
        }

        public Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanExtractionSource?>(source);
        }

        public Task<FloorPlanExtractionSource?> GetByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanExtractionSource?>(
                source.FloorPlanVersionId == floorPlanVersionId ? source : null);
        }
    }

    private sealed class ExactFloorPlanExtractionSourceReader : IFloorPlanExtractionSourceReader
    {
        private readonly FloorPlanExtractionSource source;

        public ExactFloorPlanExtractionSourceReader(FloorPlanExtractionSource source)
        {
            this.source = source;
        }

        public Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(
            Guid templateId,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("The exact floor-plan version must be requested.");

        public Task<FloorPlanExtractionSource?> GetByVersionAsync(
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<FloorPlanExtractionSource?>(
                source.FloorPlanVersionId == floorPlanVersionId ? source : null);
    }

    private sealed class ExactPlanSheetSourceReader : IPlanSheetSourceReader
    {
        private readonly PlanSheetSourceDto source;

        public ExactPlanSheetSourceReader(PlanSheetSourceDto source)
        {
            this.source = source;
        }

        public Task<PlanSheetSourceDto?> GetBySheetIdAsync(
            Guid sheetId,
            CancellationToken cancellationToken)
            => Task.FromResult<PlanSheetSourceDto?>(
                source.SheetId == sheetId ? source : null);
    }

    private sealed class FakeElectricalFloorRegistrationEstimator : IElectricalFloorRegistrationEstimator
    {
        private readonly ElectricalFloorRegistrationEstimate estimate;

        public FakeElectricalFloorRegistrationEstimator(ElectricalFloorRegistrationEstimate estimate)
        {
            this.estimate = estimate;
        }

        public string? FloorPlanSourcePath { get; private set; }

        public string? ElectricalSheetSourcePath { get; private set; }

        public Task<ElectricalFloorRegistrationEstimate> EstimateAsync(
            string floorPlanSourcePath,
            string electricalSheetSourcePath,
            CancellationToken cancellationToken)
        {
            FloorPlanSourcePath = floorPlanSourcePath;
            ElectricalSheetSourcePath = electricalSheetSourcePath;
            return Task.FromResult(estimate);
        }
    }

    private sealed class FakeWallExtractor : IWallExtractor
    {
        private readonly IReadOnlyList<DetectedWallCandidate> items;

        public FakeWallExtractor(IReadOnlyList<DetectedWallCandidate> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<DetectedWallCandidate>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class InMemoryWallExtractionRunRepository : IWallExtractionRunRepository
    {
        public List<WallExtractionRun> Items { get; } = [];

        public Task AddAsync(WallExtractionRun run, CancellationToken cancellationToken)
        {
            Items.Add(run);
            return Task.CompletedTask;
        }

        public Task<WallExtractionRun?> GetLatestByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.LastOrDefault(item => item.FloorPlanVersionId == floorPlanVersionId));
        }
    }

    private sealed class InMemoryExtractedWallCandidateRepository : IExtractedWallCandidateRepository
    {
        public List<ExtractedWallCandidate> Items { get; } = [];

        public Task AddRangeAsync(IReadOnlyList<ExtractedWallCandidate> domainCandidates, IReadOnlyList<DetectedWallCandidate> detectedCandidates, CancellationToken cancellationToken)
        {
            Items.AddRange(domainCandidates);
            return Task.CompletedTask;
        }

        public Task AddAsync(ExtractedWallCandidate domainCandidate, DetectedWallCandidate detectedCandidate, CancellationToken cancellationToken)
        {
            Items.Add(domainCandidate);
            return Task.CompletedTask;
        }

        public Task<int> GetNextSortOrderAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.Count(item => item.WallExtractionRunId == wallExtractionRunId) + 1);
        }

        public Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == candidateId));
        }

        public Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFloorPlanLibraryReader : IFloorPlanLibraryReader
    {
        private readonly IReadOnlyList<FloorPlanLibraryItemDto> items;

        public FakeFloorPlanLibraryReader(IReadOnlyList<FloorPlanLibraryItemDto> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class AutoFitProfileRepository : ICommissionedHouseAdaptationProfileRepository
    {
        private readonly IReadOnlyDictionary<Guid, CommissionedHouseAdaptationProfile> profiles;
        private readonly IReadOnlySet<Guid> failedVersionIds;

        public AutoFitProfileRepository(
            IEnumerable<CommissionedHouseAdaptationProfile> profiles,
            IEnumerable<Guid>? failedVersionIds = null)
        {
            this.profiles = profiles.ToDictionary(profile => profile.FloorPlanVersionId);
            this.failedVersionIds = (failedVersionIds ?? []).ToHashSet();
        }

        public List<(Guid VersionId, Guid PublishedCurationId)> Requests { get; } = [];

        public Task UpsertAsync(
            CommissionedHouseAdaptationProfile profile,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<CommissionedHouseAdaptationProfile?> GetByFloorPlanVersionIdAsync(
            Guid floorPlanVersionId,
            Guid expectedPublishedCurationId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add((floorPlanVersionId, expectedPublishedCurationId));
            if (failedVersionIds.Contains(floorPlanVersionId))
            {
                throw new InvalidOperationException("Readiness probe failed.");
            }

            profiles.TryGetValue(floorPlanVersionId, out var profile);
            return Task.FromResult(
                profile?.PublishedCurationId == expectedPublishedCurationId ? profile : null);
        }

        public Task RemoveByFloorPlanVersionIdAsync(
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeDxfGateway : IDxfGateway
    {
        public Task<DetectedFloorPlanDocument> ReadFloorPlanAsync(string filePath, CancellationToken cancellationToken)
            => Task.FromResult(new DetectedFloorPlanDocument(
                Path.GetFileName(filePath),
                Path.GetFileNameWithoutExtension(filePath),
                LengthUnit.Inch,
                25.4m,
                "AC1032",
                "bbox:0,0,10,10"));
    }

    private sealed class FakeManagedFileStorage : IManagedFileStorage
    {
        public Task<string> CopyIntoLibraryAsync(string sourceFilePath, CancellationToken cancellationToken)
            => Task.FromResult(Path.Combine(@"C:\managed", Path.GetFileName(sourceFilePath)));

        public Task<string> ReserveAdjustedDxfPathAsync(string sourceFileName, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakeHashService : IFileHashService
    {
        public Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
            => Task.FromResult("hash");
    }

    private sealed class CapturingMeasurementContextRepository : IMeasurementContextRepository
    {
        public List<MeasurementContext> Items { get; } = [];

        public Task AddAsync(MeasurementContext context, CancellationToken cancellationToken)
        {
            Items.Add(context);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingImportedDocumentRepository : IImportedDocumentRepository
    {
        public List<ImportedDocument> Items { get; } = [];

        public Task AddAsync(ImportedDocument document, CancellationToken cancellationToken)
        {
            Items.Add(document);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingPlanSheetRepository : IPlanSheetRepository, IPlanSheetReader
    {
        public List<PlanSheet> Items { get; } = [];

        public Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken)
        {
            Items.Add(sheet);
            return Task.CompletedTask;
        }

        public Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == sheetId));

        public Task RemoveAsync(Guid sheetId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == sheetId);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(PlanSheet sheet, CancellationToken cancellationToken)
        {
            var index = Items.FindIndex(item => item.Id == sheet.Id);
            if (index >= 0)
            {
                Items[index] = sheet;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
            IReadOnlyCollection<Guid> planSetVersionIds,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> result = Items
                .Where(sheet => planSetVersionIds.Contains(sheet.PlanSetVersionId))
                .GroupBy(sheet => sheet.PlanSetVersionId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<PlanSetSheetDto>)group
                        .Select(sheet => new PlanSetSheetDto(
                            sheet.Id,
                            sheet.SheetType.ToString(),
                            sheet.Name,
                            sheet.ImportedDocumentId,
                            SourceFloorPlanVersionId: null,
                            IsCanonical: false,
                            sheet.Status.ToString(),
                            "NotProjected"))
                        .ToArray());

            return Task.FromResult(result);
        }
    }

    private sealed class CapturingSheetRegistrationRepository : ISheetRegistrationRepository
    {
        public List<SheetRegistration> Items { get; } = [];

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
        {
            Items.Add(registration);
            return Task.CompletedTask;
        }

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == registrationId));

        public Task<IReadOnlyList<SheetRegistration>> ListByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetRegistration>>(
                Items.Where(item => item.PlanSetVersionId == planSetVersionId).ToArray());

        public Task UpdateAsync(SheetRegistration registration, CancellationToken cancellationToken)
        {
            var index = Items.FindIndex(item => item.Id == registration.Id);
            if (index >= 0)
            {
                Items[index] = registration;
            }

            return Task.CompletedTask;
        }

        public Task RemoveByDependentSheetIdAsync(Guid dependentSheetId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.DependentSheetId == dependentSheetId);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        public List<SheetAdjustmentProjection> Items { get; } = [];

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            Items.Add(projection);
            return Task.CompletedTask;
        }

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == projectionId));

        public Task UpdateAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            var index = Items.FindIndex(item => item.Id == projection.Id);
            if (index >= 0)
            {
                Items[index] = projection;
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(
                Items
                    .Where(item => item.PlanSetVersionId == planSetVersionId &&
                                   item.CanonicalAdjustmentId == canonicalAdjustmentId)
                    .ToArray());

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(
                Items.Where(item => item.PlanSetVersionId == planSetVersionId).ToArray());

        public Task RemoveByDependentSheetIdAsync(Guid dependentSheetId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.DependentSheetId == dependentSheetId);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryHousePlanSetRepository : IHousePlanSetRepository
    {
        public List<HousePlanSet> Items { get; } = [];

        public Task<HousePlanSet?> GetBySourceFloorPlanTemplateAsync(
            Guid sourceFloorPlanTemplateId,
            CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.SourceFloorPlanTemplateId == sourceFloorPlanTemplateId));

        public Task AddAsync(HousePlanSet housePlanSet, CancellationToken cancellationToken)
        {
            Items.Add(housePlanSet);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPlanSetVersionRepository : IPlanSetVersionRepository
    {
        public List<PlanSetVersion> Items { get; } = [];

        public Task<PlanSetVersion?> GetByIdAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == planSetVersionId));

        public Task<PlanSetVersion?> GetByCanonicalFloorPlanVersionAsync(
            Guid canonicalFloorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.CanonicalFloorPlanVersionId == canonicalFloorPlanVersionId));

        public Task AddAsync(PlanSetVersion version, CancellationToken cancellationToken)
        {
            Items.Add(version);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly FloorPlanReviewSessionDto session;

        public FakeFloorPlanReviewSessionReader(FloorPlanReviewSessionDto session)
        {
            this.session = session;
        }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
        }

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<FloorPlanReviewSessionDto?>(session);
        }
    }

    private sealed class SequencedFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
    {
        private readonly Queue<FloorPlanReviewSessionDto> sessions;
        private bool pendingNonPublishedOpenProbe;

        public SequencedFloorPlanReviewSessionReader(params FloorPlanReviewSessionDto[] sessions)
        {
            this.sessions = new Queue<FloorPlanReviewSessionDto>(sessions);
        }

        public int CallCount { get; private set; }

        public Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult<FloorPlanReviewSessionDto?>(ReadNextSession());
        }

        public Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
            Guid templateId,
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult<FloorPlanReviewSessionDto?>(ReadNextSession());
        }

        private FloorPlanReviewSessionDto ReadNextSession()
        {
            var session = sessions.Peek();
            if (session.ActivePublishedCurationId is null && !pendingNonPublishedOpenProbe)
            {
                pendingNonPublishedOpenProbe = true;
                return session;
            }

            pendingNonPublishedOpenProbe = false;
            return sessions.Count > 1 ? sessions.Dequeue() : session;
        }
    }

    private sealed class InMemoryFloorPlanTemplateRepository : IFloorPlanTemplateRepository
    {
        private readonly List<FloorPlanTemplate> items;

        public InMemoryFloorPlanTemplateRepository(params FloorPlanTemplate[] items)
        {
            this.items = items.ToList();
        }

        public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item => item.Code == code));
        }

        public Task<FloorPlanTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item => item.Id == templateId));
        }

        public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            items.Add(template);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFloorPlanVersionRepository : IFloorPlanVersionRepository
    {
        public InMemoryFloorPlanVersionRepository(params FloorPlanVersion[] items)
        {
            Items = items.ToList();
        }

        public List<FloorPlanVersion> Items { get; }

        public Task<FloorPlanVersion?> GetByIdAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == floorPlanVersionId));
        }

        public Task<int> GetNextVersionNumberAsync(Guid floorPlanTemplateId, CancellationToken cancellationToken)
        {
            var nextVersion = Items
                .Where(item => item.FloorPlanTemplateId == floorPlanTemplateId)
                .Select(item => item.VersionNumber)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return Task.FromResult(nextVersion);
        }

        public Task AddAsync(FloorPlanVersion version, CancellationToken cancellationToken)
        {
            Items.Add(version);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            Items.RemoveAll(item => item.Id == floorPlanVersionId);
            return Task.CompletedTask;
        }
    }

    private sealed class RepositoryBackedFloorPlanLibraryReader : IFloorPlanLibraryReader
    {
        private readonly Guid templateId;
        private readonly InMemoryFloorPlanVersionRepository versionRepository;

        public RepositoryBackedFloorPlanLibraryReader(
            Guid templateId,
            InMemoryFloorPlanVersionRepository versionRepository)
        {
            this.templateId = templateId;
            this.versionRepository = versionRepository;
        }

        public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
        {
            var versions = versionRepository.Items
                .Where(item => item.FloorPlanTemplateId == templateId)
                .OrderByDescending(item => item.VersionNumber)
                .Select((item, index) => new FloorPlanLibraryVersionDto(
                    item.Id,
                    item.VersionNumber,
                    "Imported",
                    item.CreatedAtUtc,
                    "inch",
                    IsCurrent: index == 0))
                .ToArray();

            if (versions.Length == 0)
            {
                return Task.FromResult<IReadOnlyList<FloorPlanLibraryItemDto>>([]);
            }

            return Task.FromResult<IReadOnlyList<FloorPlanLibraryItemDto>>(
            [
                new FloorPlanLibraryItemDto(
                    templateId,
                    "santa-barbara",
                    "SANTA-BARBARA",
                    versions.Length,
                    versions[0].VersionId,
                    versions[0].VersionNumber,
                    versions)
            ]);
        }
    }

    private sealed class InMemoryFloorPlanCurationRepository : IFloorPlanCurationRepository
    {
        private readonly List<FloorPlanCuration> items = [];

        public Task<FloorPlanCuration?> GetByIdAsync(Guid curationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item => item.Id == curationId));
        }

        public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item =>
                item.FloorPlanVersionId == floorPlanVersionId &&
                item.Status == FloorPlanCurationStatus.Draft));
        }

        public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(items.SingleOrDefault(item =>
                item.FloorPlanVersionId == floorPlanVersionId &&
                item.Status == FloorPlanCurationStatus.Published));
        }

        public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
        {
            var nextVersion = items
                .Where(item => item.FloorPlanVersionId == floorPlanVersionId)
                .Select(item => item.CurationVersion)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return Task.FromResult(nextVersion);
        }

        public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            items.Add(curation);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool SaveChangesCalled { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
