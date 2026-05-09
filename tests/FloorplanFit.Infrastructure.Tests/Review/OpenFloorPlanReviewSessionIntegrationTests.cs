using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Application.FloorPlans.Extraction;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Review;

public sealed class OpenFloorPlanReviewSessionIntegrationTests
{
    [Fact]
    public async Task HandleAsync_opens_review_after_creating_the_first_draft_in_the_same_sqlite_scope()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-open-review-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 5, 2, 15, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var import = await ExecuteImportAsync(workspace, sourcePath, now);
            await SeedExtractionAsync(workspace, import.Item.TemplateId, now.AddMinutes(5));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var templateRepository = new SqliteFloorPlanTemplateRepository(session);
            var curationRepository = new SqliteFloorPlanCurationRepository(session);
            var unitOfWork = new SqliteUnitOfWork(session);
            var clock = new FixedClock(now.AddMinutes(10));
            var startOrResume = new StartOrResumeCurationHandler(curationRepository, clock, unitOfWork);
            var reader = new SqliteFloorPlanReviewSessionReader(session);
            var handler = new OpenFloorPlanReviewSessionHandler(templateRepository, reader, startOrResume);

            var response = await handler.HandleAsync(import.Item.TemplateId, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, response.DraftCurationId);
            Assert.Equal(import.Item.TemplateId, response.Session.TemplateId);
            Assert.Equal("Curated Draft", response.Session.Status);
            Assert.NotEmpty(response.Session.WallCandidates);
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

    private static async Task<Contracts.FloorPlans.ImportFloorPlanResponse> ExecuteImportAsync(
        AppWorkspace workspace,
        string sourcePath,
        DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);

        var handler = new ImportFloorPlanHandler(
            new IxMiliaDxfGateway(),
            new ManagedFileStorage(workspace),
            new SqliteFloorPlanTemplateRepository(session),
            new SqliteFloorPlanVersionRepository(session),
            new SqliteImportedDocumentRepository(session),
            new SqliteMeasurementContextRepository(session),
            new SqliteUnitOfWork(session),
            new Sha256FileHashService(),
            new FixedClock(now),
            new ImportFloorPlanResultFactory());

        return await handler.HandleAsync(new Contracts.FloorPlans.ImportFloorPlanRequest(sourcePath), CancellationToken.None);
    }

    private static async Task SeedExtractionAsync(AppWorkspace workspace, Guid templateId, DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var sourceReader = new SqliteFloorPlanExtractionSourceReader(session);
        var source = await sourceReader.GetCurrentSourceAsync(templateId, CancellationToken.None)
            ?? throw new InvalidOperationException("Expected extraction source for imported template.");

        var handler = new ExtractWallCandidatesHandler(
            new IxMiliaWallExtractor(),
            new IxMiliaRoomLabelExtractor(),
            new IxMiliaOpeningExtractor(),
            new IxMiliaFixedPlanComponentExtractor(),
            new IxMiliaProtectedDetailAssemblyExtractor(),
            new SqliteWallExtractionRunRepository(session),
            new SqliteExtractedWallCandidateRepository(session),
            new SqliteExtractedRoomLabelRepository(session),
            new SqliteExtractedOpeningCandidateRepository(session),
            new SqliteExtractedOpeningLabelRepository(session),
            new SqliteExtractedFixedPlanComponentRepository(session),
            new SqliteExtractedProtectedDetailAssemblyRepository(session),
            new SqliteUnitOfWork(session),
            new FixedClock(now));

        await handler.HandleAsync(source.FloorPlanVersionId, source.ManagedFilePath, CancellationToken.None);
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
