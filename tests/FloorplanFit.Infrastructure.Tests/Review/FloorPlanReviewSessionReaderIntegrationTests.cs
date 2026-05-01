using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Review;

public sealed class FloorPlanReviewSessionReaderIntegrationTests
{
    [Fact]
    public async Task GetByTemplateAsync_returns_candidates_curated_walls_and_geometry_for_the_current_floor_plan()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-review-reader-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 4, 30, 16, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var import = await ExecuteImportAsync(workspace, sourcePath, now);
            await SeedExtractionAndDraftAsync(workspace, now.AddMinutes(5));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanReviewSessionReader(session);

            var reviewSession = await reader.GetByTemplateAsync(import.Item.TemplateId, CancellationToken.None);

            Assert.NotNull(reviewSession);
            Assert.Equal(import.Item.TemplateId, reviewSession.TemplateId);
            Assert.Equal("Curated Draft", reviewSession.Status);
            Assert.NotEmpty(reviewSession.WallCandidates);
            Assert.Single(reviewSession.CuratedWalls);
            Assert.NotEmpty(reviewSession.GeometryPaths);

            var candidate = reviewSession.WallCandidates.Single();
            Assert.Equal("WALLS", candidate.SourceLayer);
            Assert.NotNull(candidate.GeometryPathId);

            var curatedWall = reviewSession.CuratedWalls.Single();
            Assert.Equal(candidate.CandidateId, curatedWall.SourceCandidateId);
            Assert.Equal("W-001", curatedWall.StableWallId);

            var path = Assert.Single(reviewSession.GeometryPaths, item => item.Id == candidate.GeometryPathId);
            Assert.Single(path.Segments);
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

    private static async Task<ImportFloorPlanResponse> ExecuteImportAsync(
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

        return await handler.HandleAsync(new ImportFloorPlanRequest(sourcePath), CancellationToken.None);
    }

    private static async Task SeedExtractionAndDraftAsync(AppWorkspace workspace, DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var templateRepository = new SqliteFloorPlanTemplateRepository(session);
        var template = await templateRepository.GetByCodeAsync("santa-barbara", CancellationToken.None)
            ?? throw new InvalidOperationException("Expected imported template.");
        var versionId = template.CurrentVersionId ?? throw new InvalidOperationException("Expected current version id.");

        var extractionRun = new WallExtractionRun(
            Guid.NewGuid(),
            versionId,
            "Completed",
            now,
            now,
            "ixmilia-wall-layer-v1",
            null);

        await new SqliteWallExtractionRunRepository(session).AddAsync(extractionRun, CancellationToken.None);

        var candidate = new ExtractedWallCandidate(
            Guid.NewGuid(),
            extractionRun.Id,
            "LINE:1",
            "WALLS",
            Guid.Empty,
            null,
            0.95m,
            null,
            ExtractedWallCandidateStatus.Pending,
            1);

        await new SqliteExtractedWallCandidateRepository(session).AddRangeAsync(
            [candidate],
            [
                new DetectedWallCandidate(
                    "LINE:1",
                    "WALLS",
                    [new GeometryPoint(0m, 0m), new GeometryPoint(120m, 0m)],
                    null,
                    0.95m,
                    null)
            ],
            CancellationToken.None);

        var draft = new FloorPlanCuration(
            Guid.NewGuid(),
            versionId,
            curationVersion: 1,
            FloorPlanCurationStatus.Draft,
            basedOnCurationId: null,
            notes: "draft",
            createdAtUtc: now.AddMinutes(1),
            publishedAtUtc: null);

        await new SqliteFloorPlanCurationRepository(session).AddAsync(draft, CancellationToken.None);

        await new SqliteCuratedWallRepository(session).AddAsync(
            new CuratedWall(
                Guid.NewGuid(),
                draft.Id,
                "W-001",
                candidate.Id,
                candidate.SourceEntityRef,
                null,
                WallRole.Partition,
                WallMobilityLevel.Flexible,
                WallProtectionLevel.None,
                101.6m,
                "2x4",
                null,
                false,
                false,
                null,
                1,
                null),
            CancellationToken.None);

        await session.CommitAsync(CancellationToken.None);
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
