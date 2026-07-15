using Avalonia;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Desktop.Composition;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FloorplanFit.Desktop;

internal static class Program
{
    public static IHost? Host { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var localApplicationDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var workspaceRoot = AppWorkspaceDurability.GetStableWorkspaceRoot(localApplicationDataRoot);
        var backupRoot = AppWorkspaceDurability.GetBackupRoot(localApplicationDataRoot);
        var legacyWorkspaceRoot = Path.Combine(AppContext.BaseDirectory, "workspace");

        AppWorkspaceDurability.CopyLegacyWorkspaceIfNeeded(
            legacyWorkspaceRoot,
            workspaceRoot,
            CancellationToken.None);

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddDesktopSlice1(workspaceRoot))
            .Build();

        var workspace = Host.Services.GetRequiredService<AppWorkspace>();
        workspace.EnsureCreated();
        AppWorkspaceDurability.CreatePreMigrationBackupIfNeeded(
            workspace,
            backupRoot,
            SqliteSchemaInitializer.CurrentSchemaVersion,
            CancellationToken.None);
        SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        ScheduleStartupCleanup(Host.Services);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void ScheduleStartupCleanup(IServiceProvider services)
    {
        var cleanupService = services.GetRequiredService<IFloorPlanVersionCleanupService>();
        _ = Task.Run(async () =>
        {
            try
            {
                await cleanupService.CleanupAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.TraceWarning(
                    "Floor plan version cleanup failed on startup: {0}",
                    exception);
            }
        });
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();
}
