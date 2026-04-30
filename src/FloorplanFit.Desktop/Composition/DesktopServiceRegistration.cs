using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Application.FloorPlans.Library;
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
        services.AddSingleton<IFileHashService, Sha256FileHashService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ImportFloorPlanResultFactory>();

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
        services.AddScoped<IFloorPlanLibraryReader, SqliteFloorPlanLibraryReader>();
        services.AddScoped<IUnitOfWork, SqliteUnitOfWork>();
        services.AddScoped<ImportFloorPlanHandler>();
        services.AddScoped<GetFloorPlanLibraryHandler>();

        services.AddSingleton<LibraryViewModel>();

        return services;
    }

    private sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
