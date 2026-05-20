using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Library;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Desktop.Composition;
using FloorplanFit.Desktop.ViewModels;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Composition;

public sealed class DesktopServiceRegistrationTests
{
    [Fact]
    public async Task AddDesktopSlice1_registers_review_session_services()
    {
        var workspaceRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-desktop-services-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(workspaceRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var services = new ServiceCollection();
            services.AddDesktopSlice1(workspaceRoot);

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            Assert.NotNull(scope.ServiceProvider.GetRequiredService<LibraryViewModel>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<OpenFloorPlanReviewSessionHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<GetFloorPlanReviewSessionHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RemoveFloorPlanVersionHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<StartOrResumeCurationHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<AddPinchGroupHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<AddPinchMarkerHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RemovePinchMarkerHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RemoveRoomLabelHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RemoveProtectedDetailAssemblyHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<RejectWallCandidateHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<PublishFloorPlanCurationHandler>());
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<ExtractWallCandidatesHandler>());
            Assert.Same(DxfExtractionProfile.PointeHomes, scope.ServiceProvider.GetRequiredService<DxfExtractionProfile>());
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(workspaceRoot))
            {
                Directory.Delete(workspaceRoot, recursive: true);
            }
        }
    }
}
