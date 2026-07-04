using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Import;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.PlanSets;

public sealed class PlanSheetSourceReaderIntegrationTests
{
    [Fact]
    public async Task GetBySheetIdAsync_returns_imported_dependent_sheet_source_path()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-plan-sheet-source-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var planSetVersionId = Guid.NewGuid();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var planSheetRepository = new SqlitePlanSheetRepository(session);
            var handler = new ImportPlanSheetHandler(
                new IxMiliaDxfGateway(),
                new ManagedFileStorage(workspace),
                new SqliteImportedDocumentRepository(session),
                new SqliteMeasurementContextRepository(session),
                planSheetRepository,
                new SqliteUnitOfWork(session),
                new Sha256FileHashService(),
                new FixedClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc)));

            var imported = await handler.HandleAsync(
                new ImportPlanSheetRequest(
                    planSetVersionId,
                    "ElectricalPlan",
                    sourcePath,
                    "Electrical"),
                CancellationToken.None);

            var source = await planSheetRepository.GetBySheetIdAsync(imported.SheetId, CancellationToken.None);

            Assert.NotNull(source);
            Assert.Equal(imported.SheetId, source.SheetId);
            Assert.Equal("ElectricalPlan", source.SheetType);
            Assert.Equal("Electrical", source.Name);
            Assert.Equal(imported.ImportedDocumentId, source.ImportedDocumentId);
            Assert.StartsWith(workspace.LibraryRawDxfDirectory, source.SourceFilePath, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(source.SourceFilePath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
