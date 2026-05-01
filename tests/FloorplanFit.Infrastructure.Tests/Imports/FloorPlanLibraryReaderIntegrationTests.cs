using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Imports;

public sealed class FloorPlanLibraryReaderIntegrationTests
{
    [Fact]
    public async Task ListAsync_returns_the_current_library_state_from_sqlite()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-library-reader-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 4, 29, 20, 30, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, now);
            var secondImport = await ExecuteImportAsync(workspace, sourcePath, now.AddMinutes(5));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanLibraryReader(session);

            var items = await reader.ListAsync(CancellationToken.None);

            var item = Assert.Single(items);
            Assert.Equal(secondImport.Item.TemplateId, item.TemplateId);
            Assert.Equal("santa-barbara", item.Code);
            Assert.Equal("SANTA-BARBARA", item.Name);
            Assert.Equal("Imported", item.Status);
            Assert.Equal(2, item.ActiveVersionNumber);
            Assert.Equal("inch", item.SourceUnit);
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

    [Fact]
    public async Task ListAsync_returns_extracted_when_the_current_version_has_wall_candidates()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-library-reader-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 4, 30, 12, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, now);
            await SeedExtractionAsync(workspace, now.AddMinutes(5));

            var item = await ReadSingleLibraryItemAsync(workspace);

            Assert.Equal("Extracted", item.Status);
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

    [Fact]
    public async Task ListAsync_returns_curated_draft_when_the_current_version_has_a_draft_curation()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-library-reader-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 4, 30, 13, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, now);
            await SeedDraftCurationAsync(workspace, now.AddMinutes(5));

            var item = await ReadSingleLibraryItemAsync(workspace);

            Assert.Equal("Curated Draft", item.Status);
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

    [Fact]
    public async Task ListAsync_returns_published_when_the_template_has_an_active_published_curation()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-library-reader-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 4, 30, 14, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, now);
            await SeedPublishedCurationAsync(workspace, now.AddMinutes(5));

            var item = await ReadSingleLibraryItemAsync(workspace);

            Assert.Equal("Published", item.Status);
            Assert.NotNull(item.ActivePublishedCurationId);
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

    private static async Task SeedExtractionAsync(AppWorkspace workspace, DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var template = await new SqliteFloorPlanTemplateRepository(session).GetByCodeAsync("santa-barbara", CancellationToken.None)
            ?? throw new InvalidOperationException("Expected imported template.");
        var versionId = template.CurrentVersionId ?? throw new InvalidOperationException("Expected current version id.");

        var run = new WallExtractionRun(
            Guid.NewGuid(),
            versionId,
            status: "Completed",
            startedAtUtc: now,
            finishedAtUtc: now,
            extractorVersion: "ixmilia-wall-layer-v1",
            errorMessage: null);

        await new SqliteWallExtractionRunRepository(session).AddAsync(run, CancellationToken.None);
        await new SqliteExtractedWallCandidateRepository(session).AddRangeAsync(
        [
            new ExtractedWallCandidate(
                Guid.NewGuid(),
                run.Id,
                "LINE:1",
                "WALLS",
                Guid.Empty,
                null,
                0.95m,
                null,
                ExtractedWallCandidateStatus.Pending,
                1)
        ],
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

        await session.CommitAsync(CancellationToken.None);
    }

    private static async Task SeedDraftCurationAsync(AppWorkspace workspace, DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var template = await new SqliteFloorPlanTemplateRepository(session).GetByCodeAsync("santa-barbara", CancellationToken.None)
            ?? throw new InvalidOperationException("Expected imported template.");
        var versionId = template.CurrentVersionId ?? throw new InvalidOperationException("Expected current version id.");

        await new SqliteFloorPlanCurationRepository(session).AddAsync(
            new FloorPlanCuration(
                Guid.NewGuid(),
                versionId,
                curationVersion: 1,
                FloorPlanCurationStatus.Draft,
                basedOnCurationId: null,
                notes: "draft",
                createdAtUtc: now,
                publishedAtUtc: null),
            CancellationToken.None);

        await session.CommitAsync(CancellationToken.None);
    }

    private static async Task SeedPublishedCurationAsync(AppWorkspace workspace, DateTime now)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var templateRepository = new SqliteFloorPlanTemplateRepository(session);
        var template = await templateRepository.GetByCodeAsync("santa-barbara", CancellationToken.None)
            ?? throw new InvalidOperationException("Expected imported template.");
        var versionId = template.CurrentVersionId ?? throw new InvalidOperationException("Expected current version id.");

        var published = new FloorPlanCuration(
            Guid.NewGuid(),
            versionId,
            curationVersion: 1,
            FloorPlanCurationStatus.Published,
            basedOnCurationId: null,
            notes: "published",
            createdAtUtc: now,
            publishedAtUtc: now.AddMinutes(1));

        await new SqliteFloorPlanCurationRepository(session).AddAsync(published, CancellationToken.None);
        template.SetActivePublishedCuration(published.Id);
        await templateRepository.UpdateAsync(template, CancellationToken.None);
        await session.CommitAsync(CancellationToken.None);
    }

    private static async Task<FloorPlanLibraryItemDto> ReadSingleLibraryItemAsync(AppWorkspace workspace)
    {
        await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
        var reader = new SqliteFloorPlanLibraryReader(session);
        var items = await reader.ListAsync(CancellationToken.None);
        return Assert.Single(items);
    }

    private sealed class FixedClock : Application.Abstractions.IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
