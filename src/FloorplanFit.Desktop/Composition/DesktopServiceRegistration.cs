using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Infrastructure.Dxf;
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
        services.AddSingleton(new AppWorkspace(workspaceRoot));
        services.AddSingleton<IManagedFileStorage, ManagedFileStorage>();
        services.AddSingleton<IDxfGateway, IxMiliaDxfGateway>();
        services.AddSingleton(DxfExtractionProfile.PointeHomes);
        services.AddSingleton<IWallExtractor, IxMiliaWallExtractor>();
        services.AddSingleton<IRoomLabelExtractor, IxMiliaRoomLabelExtractor>();
        services.AddSingleton<IOpeningExtractor, IxMiliaOpeningExtractor>();
        services.AddSingleton<IFixedPlanComponentExtractor, IxMiliaFixedPlanComponentExtractor>();
        services.AddSingleton<IProtectedDetailAssemblyExtractor, IxMiliaProtectedDetailAssemblyExtractor>();
        services.AddSingleton<IDimensionExtractor, IxMiliaDimensionExtractor>();
        services.AddSingleton<IAdjustedDxfExporter, IxMiliaAdjustedDxfExporter>();
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
        services.AddScoped<IFloorPlanLibraryReader, SqliteFloorPlanLibraryReader>();
        services.AddScoped<IFloorPlanExtractionSourceReader, SqliteFloorPlanExtractionSourceReader>();
        services.AddScoped<IFloorPlanReviewSessionReader, SqliteFloorPlanReviewSessionReader>();
        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
        services.AddScoped<ImportFloorPlanHandler>();
        services.AddScoped<ExtractWallCandidatesHandler>();
        services.AddScoped<GetFloorPlanLibraryHandler>();
        services.AddScoped<RemoveFloorPlanVersionHandler>();
        services.AddScoped<GetFloorPlanReviewSessionHandler>();
        services.AddScoped<OpenFloorPlanReviewSessionHandler>();
        services.AddScoped<StartOrResumeCurationHandler>();
        services.AddScoped<AddPinchGroupHandler>();
        services.AddScoped<AddPinchMarkerHandler>();
        services.AddScoped<AddMeasurementCorridorHandler>();
        services.AddScoped<AddMeasurementNodeHandler>();
        services.AddScoped<SaveDimensionIntervalBindingHandler>();
        services.AddScoped<RestoreDimensionIntervalBindingHandler>();
        services.AddScoped<RemoveMeasurementCorridorHandler>();
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

        services.AddSingleton<LibraryViewModel>();

        return services;
    }

    private sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
