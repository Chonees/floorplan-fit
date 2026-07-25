using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Application.PlanSets.Adjustment;
using FloorplanFit.Application.PlanSets.Classification;
using FloorplanFit.Application.PlanSets.Confirmation;
using FloorplanFit.Application.PlanSets.DataCollection;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Application.PlanSets.ExportAudit;
using FloorplanFit.Application.PlanSets.Import;
using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Application.PlanSets.Registration;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.OpenAi;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FloorplanFit.Desktop.Composition;

public static class DesktopServiceRegistration
{
    public static IServiceCollection AddDesktopSlice1(this IServiceCollection services, string workspaceRoot)
    {
        var adjustedDxfExportDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "exports");

        services.AddSingleton(new AppWorkspace(workspaceRoot, adjustedDxfExportDirectory));
        services.AddSingleton<IManagedFileStorage, ManagedFileStorage>();
        services.AddSingleton<IPlanSetExportManifestWriter, PlanSetExportManifestWriter>();
        services.AddSingleton<IDxfGateway, IxMiliaDxfGateway>();
        services.AddSingleton<ISitePlanPreviewReader, IxMiliaSitePlanPreviewReader>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton(OpenAiAutoFitPlanSuggesterOptions.FromEnvironment());
        services.AddSingleton<IAutoFitPlanSuggester, OpenAiAutoFitPlanSuggester>();
        services.AddSingleton(DxfExtractionProfile.PointeHomes);
        services.AddSingleton<IWallExtractor, IxMiliaWallExtractor>();
        services.AddSingleton<IRoomLabelExtractor, IxMiliaRoomLabelExtractor>();
        services.AddSingleton<IOpeningExtractor, IxMiliaOpeningExtractor>();
        services.AddSingleton<IFixedPlanComponentExtractor, IxMiliaFixedPlanComponentExtractor>();
        services.AddSingleton<IProtectedDetailAssemblyExtractor, IxMiliaProtectedDetailAssemblyExtractor>();
        services.AddSingleton<IDimensionExtractor, IxMiliaDimensionExtractor>();
        services.AddSingleton<IAdjustedDxfExporter, IxMiliaAdjustedDxfExporter>();
        services.AddSingleton<IAdjustedSitePlanExporter, IxMiliaAdjustedSitePlanExporter>();
        services.AddSingleton<IProjectedPlanSheetExporter, ProjectedPlanSheetDxfExporter>();
        services.AddSingleton<IElectricalFloorRegistrationEstimator, DxfElectricalFloorRegistrationEstimator>();
        services.AddSingleton<IFileHashService, Sha256FileHashService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ImportFloorPlanResultFactory>();
        services.AddSingleton<IFloorPlanVersionCleanupService, SqliteFloorPlanVersionCleanupService>();

        services.AddScoped<SqliteSession>(provider =>
            SqliteSession.OpenAsync(
                    provider.GetRequiredService<AppWorkspace>().DatabasePath,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult());
        services.AddScoped<IMeasurementContextRepository, SqliteMeasurementContextRepository>();
        services.AddScoped<IImportedDocumentRepository, SqliteImportedDocumentRepository>();
        services.AddScoped<IFloorPlanTemplateRepository, SqliteFloorPlanTemplateRepository>();
        services.AddScoped<IFloorPlanVersionRepository, SqliteFloorPlanVersionRepository>();
        services.AddScoped<IWallExtractionRunRepository, SqliteWallExtractionRunRepository>();
        services.AddScoped<IExtractedWallCandidateRepository, SqliteExtractedWallCandidateRepository>();
        services.AddScoped<IExtractedRoomLabelRepository, SqliteExtractedRoomLabelRepository>();
        services.AddScoped<IExtractedOpeningCandidateRepository, SqliteExtractedOpeningCandidateRepository>();
        services.AddScoped<IExtractedOpeningLabelRepository, SqliteExtractedOpeningLabelRepository>();
        services.AddScoped<IExtractedFixedPlanComponentRepository, SqliteExtractedFixedPlanComponentRepository>();
        services.AddScoped<IExtractedProtectedDetailAssemblyRepository, SqliteExtractedProtectedDetailAssemblyRepository>();
        services.AddScoped<IExtractedDimensionRepository, SqliteExtractedDimensionRepository>();
        services.AddScoped<IFloorPlanArtifactClassificationRepository, SqliteFloorPlanArtifactClassificationRepository>();
        services.AddScoped<IFloorPlanArtifactPositionRepository, SqliteFloorPlanArtifactPositionRepository>();
        services.AddScoped<IFloorPlanLabelOverrideRepository, SqliteFloorPlanLabelOverrideRepository>();
        services.AddScoped<IFloorPlanDimensionOverrideRepository, SqliteFloorPlanDimensionOverrideRepository>();
        services.AddScoped<IFloorPlanDimensionBindingOverrideRepository, SqliteFloorPlanDimensionBindingOverrideRepository>();
        services.AddScoped<IMeasurementCorridorRepository, SqliteMeasurementCorridorRepository>();
        services.AddScoped<IMeasurementNodeRepository, SqliteMeasurementNodeRepository>();
        services.AddScoped<IDimensionIntervalBindingRepository, SqliteDimensionIntervalBindingRepository>();
        services.AddScoped<IFloorPlanCurationRepository, SqliteFloorPlanCurationRepository>();
        services.AddScoped<IPinchGroupRepository, SqlitePinchGroupRepository>();
        services.AddScoped<IPinchMarkerRepository, SqlitePinchMarkerRepository>();
        services.AddScoped<ICommissionedHouseAdaptationProfileRepository, SqliteCommissionedHouseAdaptationProfileRepository>();
        services.AddScoped<IFloorPlanLibraryReader, SqliteFloorPlanLibraryReader>();
        services.AddScoped<IHousePlanSetRepository, SqliteHousePlanSetRepository>();
        services.AddScoped<SqlitePlanSheetRepository>();
        services.AddScoped<IPlanSetVersionRepository, SqlitePlanSetVersionRepository>();
        services.AddScoped<IPlanSheetRepository>(provider => provider.GetRequiredService<SqlitePlanSheetRepository>());
        services.AddScoped<IPlanSheetReader>(provider => provider.GetRequiredService<SqlitePlanSheetRepository>());
        services.AddScoped<IPlanSheetSourceReader>(provider => provider.GetRequiredService<SqlitePlanSheetRepository>());
        services.AddScoped<ISheetRegistrationRepository, SqliteSheetRegistrationRepository>();
        services.AddScoped<ISheetAdjustmentProjectionRepository, SqliteSheetAdjustmentProjectionRepository>();
        services.AddScoped<ICanonicalFloorPlanAdjustmentRepository, SqliteCanonicalFloorPlanAdjustmentRepository>();
        services.AddScoped<IPlanSetExportRepository, SqlitePlanSetExportRepository>();
        services.AddScoped<SqlitePlanSetAuditEventRepository>();
        services.AddScoped<IPlanSetAuditEventRepository>(provider => provider.GetRequiredService<SqlitePlanSetAuditEventRepository>());
        services.AddScoped<IPlanSetAuditEventReader>(provider => provider.GetRequiredService<SqlitePlanSetAuditEventRepository>());
        services.AddScoped<IFloorPlanExtractionSourceReader, SqliteFloorPlanExtractionSourceReader>();
        services.AddScoped<IFloorPlanReviewSessionReader, SqliteFloorPlanReviewSessionReader>();
        services.AddScoped<IFloorPlanCurationDataCloneService, SqliteFloorPlanCurationDataCloneService>();
        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
        services.AddScoped<ImportFloorPlanHandler>();
        services.AddScoped<ImportPlanSheetHandler>();
        services.AddScoped<ClassifyPlanSheetHandler>();
        services.AddScoped<CorrectPlanSheetTypeHandler>();
        services.AddScoped<RegisterElectricalSheetHandler>();
        services.AddScoped<RegisterFacadeElevationSheetHandler>();
        services.AddScoped<RegisterRoofSheetHandler>();
        services.AddScoped<ProjectElectricalSheetAdjustmentHandler>();
        services.AddScoped<ProjectFacadeElevationSheetAdjustmentHandler>();
        services.AddScoped<ProjectRoofSheetAdjustmentHandler>();
        services.AddScoped<ProjectRegisteredPlanSetSheetsHandler>();
        services.AddScoped<ConfirmSheetRegistrationHandler>();
        services.AddScoped<RejectSheetRegistrationHandler>();
        services.AddScoped<ConfirmSheetAdjustmentProjectionHandler>();
        services.AddScoped<RecordCanonicalFloorPlanAdjustmentHandler>();
        services.AddScoped<ExportProjectedPlanSheetHandler>();
        services.AddScoped<ExportMultiSheetPlanSetPackageHandler>();
        services.AddScoped<CreateMultiSheetExportAuditHandler>();
        services.AddScoped<GetPlanSetQualityReportHandler>();
        services.AddScoped<FloorplanFit.Application.PlanSets.Library.ResolveHousePlanSetHandler>();
        services.AddScoped<FloorplanFit.Application.PlanSets.Library.ResolvePlanSetVersionHandler>();
        services.AddScoped<FloorplanFit.Application.PlanSets.Library.GetPlanSetLibraryHandler>();
        services.AddScoped<FloorplanFit.Application.PlanSets.Library.UnlinkPlanSheetHandler>();
        services.AddScoped<ExtractWallCandidatesHandler>();
        services.AddScoped<GetFloorPlanLibraryHandler>();
        services.AddScoped<RemoveFloorPlanVersionHandler>();
        services.AddScoped<GetFloorPlanReviewSessionHandler>();
        services.AddScoped<OpenFloorPlanReviewSessionHandler>();
        services.AddScoped<EditPublishedFloorPlanCurationHandler>();
        services.AddScoped<StartOrResumeCurationHandler>();
        services.AddScoped<AddPinchGroupHandler>();
        services.AddScoped<AddManualWallCandidateHandler>();
        services.AddScoped<AddPinchMarkerHandler>();
        services.AddScoped<UpdatePinchMarkerMaxTrimHandler>();
        services.AddScoped<AddMeasurementCorridorHandler>();
        services.AddScoped<ChangeMeasurementCorridorAxisHandler>();
        services.AddScoped<AddMeasurementNodeHandler>();
        services.AddScoped<SaveDimensionIntervalBindingHandler>();
        services.AddScoped<RestoreDimensionIntervalBindingHandler>();
        services.AddScoped<RemoveMeasurementCorridorHandler>();
        services.AddScoped<RemoveMeasurementNodeHandler>();
        services.AddScoped<RemovePinchGroupHandler>();
        services.AddScoped<RenamePinchGroupHandler>();
        services.AddScoped<RemovePinchMarkerHandler>();
        services.AddScoped<RemoveRoomLabelHandler>();
        services.AddScoped<RemoveOpeningCandidateHandler>();
        services.AddScoped<RemoveOpeningLabelHandler>();
        services.AddScoped<RemoveFixedPlanComponentHandler>();
        services.AddScoped<RemoveProtectedDetailAssemblyHandler>();
        services.AddScoped<SaveCuratedArtifactClassificationHandler>();
        services.AddScoped<ExcludeCuratedArtifactHandler>();
        services.AddScoped<RestoreCuratedArtifactClassificationHandler>();
        services.AddScoped<SaveFloorPlanArtifactPositionHandler>();
        services.AddScoped<RestoreFloorPlanArtifactPositionHandler>();
        services.AddScoped<SaveFloorPlanLabelTextHeightHandler>();
        services.AddScoped<RestoreFloorPlanLabelTextHeightHandler>();
        services.AddScoped<SaveFloorPlanDimensionOverrideHandler>();
        services.AddScoped<RestoreFloorPlanDimensionOverrideHandler>();
        services.AddScoped<ExportAdjustedDxfHandler>();
        services.AddScoped<RejectWallCandidateHandler>();
        services.AddScoped<PublishFloorPlanCurationHandler>();
        services.AddScoped<SaveCommissionedHouseAdaptationProfileHandler>();
        services.AddScoped<GetCommissionedHouseAdaptationReadinessHandler>();
        services.AddScoped<GetCommissionedHouseAdaptationProfileHandler>();

        services.AddSingleton<LibraryViewModel>();

        return services;
    }

    private sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
