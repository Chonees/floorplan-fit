using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class FloorPlanCurationPersistenceIntegrationTests
{
    [Fact]
    public async Task FloorPlanTemplateRepository_round_trips_active_published_curation_id()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var repository = new SqliteFloorPlanTemplateRepository(session);

            var template = new FloorPlanTemplate(Guid.NewGuid(), "santa-barbara", "SANTA-BARBARA", isActive: true);
            template.SetCurrentVersion(Guid.NewGuid());
            template.SetActivePublishedCuration(Guid.NewGuid());

            await repository.AddAsync(template, CancellationToken.None);
            await session.CommitAsync(CancellationToken.None);

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var readRepository = new SqliteFloorPlanTemplateRepository(readSession);

            var loaded = await readRepository.GetByCodeAsync("santa-barbara", CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal("santa-barbara", loaded.Code);
            Assert.NotNull(loaded.CurrentVersionId);
            Assert.NotNull(loaded.ActivePublishedCurationId);
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task FloorPlanCurationRepository_round_trips_draft_and_published_curations()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var floorPlanVersionId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteFloorPlanCurationRepository(session);
                var draft = new FloorPlanCuration(
                    Guid.NewGuid(),
                    floorPlanVersionId,
                    curationVersion: 1,
                    FloorPlanCurationStatus.Draft,
                    basedOnCurationId: null,
                    notes: "draft",
                    createdAtUtc: new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc),
                    publishedAtUtc: null);

                await repository.AddAsync(draft, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteFloorPlanCurationRepository(session);

                var loadedDraft = await repository.GetDraftAsync(floorPlanVersionId, CancellationToken.None);

                Assert.NotNull(loadedDraft);
                Assert.Equal(FloorPlanCurationStatus.Draft, loadedDraft.Status);
                Assert.Equal(2, await repository.GetNextCurationVersionAsync(floorPlanVersionId, CancellationToken.None));

                loadedDraft.UpdateNotes("published draft");
                loadedDraft.Publish(new DateTime(2026, 4, 30, 22, 0, 0, DateTimeKind.Utc));

                await repository.UpdateAsync(loadedDraft, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteFloorPlanCurationRepository(session);

                Assert.Null(await repository.GetDraftAsync(floorPlanVersionId, CancellationToken.None));

                var published = await repository.GetPublishedAsync(floorPlanVersionId, CancellationToken.None);

                Assert.NotNull(published);
                Assert.Equal(FloorPlanCurationStatus.Published, published.Status);
                Assert.Equal("published draft", published.Notes);
                Assert.Equal(new DateTime(2026, 4, 30, 22, 0, 0, DateTimeKind.Utc), published.PublishedAtUtc);
                Assert.Equal(2, await repository.GetNextCurationVersionAsync(floorPlanVersionId, CancellationToken.None));
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task ExtractedWallCandidateRepository_persists_geometry_and_status()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var floorPlanVersionId = Guid.NewGuid();
            var extractionRun = new WallExtractionRun(
                Guid.NewGuid(),
                floorPlanVersionId,
                status: "Completed",
                startedAtUtc: new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc),
                finishedAtUtc: new DateTime(2026, 4, 30, 21, 0, 0, DateTimeKind.Utc),
                extractorVersion: "ixmilia-line-segments",
                errorMessage: null);
            var candidateId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var runRepository = new SqliteWallExtractionRunRepository(session);
                var candidateRepository = new SqliteExtractedWallCandidateRepository(session);

                await runRepository.AddAsync(extractionRun, CancellationToken.None);
                await candidateRepository.AddRangeAsync(
                    [
                        new ExtractedWallCandidate(
                            candidateId,
                            extractionRun.Id,
                            sourceEntityRef: "LINE:12",
                            sourceLayer: "A-WALL",
                            geometryPathId: Guid.Empty,
                            thicknessMm: 101.6m,
                            confidence: 0.95m,
                            detectionNotes: "seeded",
                            status: ExtractedWallCandidateStatus.Pending,
                            sortOrder: 1)
                    ],
                    [
                        new DetectedWallCandidate(
                            "LINE:12",
                            "A-WALL",
                            [new GeometryPoint(0m, 0m), new GeometryPoint(10m, 0m), new GeometryPoint(10m, 5m)],
                            101.6m,
                            0.95m,
                            "seeded")
                    ],
                    CancellationToken.None);

                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var candidateRepository = new SqliteExtractedWallCandidateRepository(session);
                var loadedCandidate = await candidateRepository.GetByIdAsync(candidateId, CancellationToken.None);

                Assert.NotNull(loadedCandidate);
                Assert.Equal(ExtractedWallCandidateStatus.Pending, loadedCandidate.Status);
                Assert.NotEqual(Guid.Empty, loadedCandidate.GeometryPathId);
                Assert.Equal(101.6m, loadedCandidate.ThicknessMm);

                loadedCandidate.Accept();
                await candidateRepository.UpdateAsync(loadedCandidate, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var loadedCandidate = await new SqliteExtractedWallCandidateRepository(session).GetByIdAsync(candidateId, CancellationToken.None);
                Assert.NotNull(loadedCandidate);
                Assert.Equal(ExtractedWallCandidateStatus.Accepted, loadedCandidate.Status);
                Assert.Equal(2, CountRows(session.Connection, session.Transaction, "geometry_segments", "geometry_path_id = (SELECT geometry_path_id FROM extracted_wall_candidates WHERE id = $id)", ("$id", candidateId.ToString())));
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task CuratedWallRepository_round_trips_updates_and_removes_by_source_candidate()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var sourceCandidateId = Guid.NewGuid();
            var wallId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteCuratedWallRepository(session);
                var wall = new CuratedWall(
                    wallId,
                    curationId,
                    "W-001",
                    sourceCandidateId,
                    "LINE:12",
                    Guid.NewGuid(),
                    WallRole.Partition,
                    WallMobilityLevel.Flexible,
                    WallProtectionLevel.None,
                    thicknessMm: 101.6m,
                    assemblyCode: "2x4",
                    heightMm: null,
                    isExterior: false,
                    isStructuralHint: false,
                    wallGroupId: null,
                    sortOrder: 1,
                    notes: null);

                await repository.AddAsync(wall, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteCuratedWallRepository(session);
                var loadedWall = await repository.GetByIdAsync(wallId, CancellationToken.None);

                Assert.NotNull(loadedWall);
                Assert.Equal("W-001", loadedWall.StableWallId);
                Assert.Single(await repository.ListByCurationAsync(curationId, CancellationToken.None));

                loadedWall.UpdateMetadata(
                    WallRole.Exterior,
                    WallMobilityLevel.Locked,
                    WallProtectionLevel.Critical,
                    thicknessMm: 152.4m,
                    assemblyCode: "2x6",
                    heightMm: 2743.2m,
                    isExterior: true,
                    isStructuralHint: true,
                    notes: "Do not move facade.");

                await repository.UpdateAsync(loadedWall, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteCuratedWallRepository(session);
                var updated = await repository.GetByIdAsync(wallId, CancellationToken.None);

                Assert.NotNull(updated);
                Assert.Equal(WallRole.Exterior, updated.WallRole);
                Assert.Equal(WallMobilityLevel.Locked, updated.MobilityLevel);
                Assert.Equal(WallProtectionLevel.Critical, updated.ProtectionLevel);
                Assert.Equal(152.4m, updated.ThicknessMm);
                Assert.Equal("2x6", updated.AssemblyCode);
                Assert.Equal(2743.2m, updated.HeightMm);
                Assert.True(updated.IsExterior);
                Assert.True(updated.IsStructuralHint);
                Assert.Equal("Do not move facade.", updated.Notes);

                await repository.RemoveBySourceCandidateAsync(curationId, sourceCandidateId, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteCuratedWallRepository(session);
                Assert.Empty(await repository.ListByCurationAsync(curationId, CancellationToken.None));
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    private static string CreateTempRoot()
    {
        return Path.Combine(Path.GetTempPath(), $"floorplan-fit-curation-{Guid.NewGuid():N}");
    }

    private static void DeleteTempRoot(string tempRoot)
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static int CountRows(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        string whereClause,
        params (string Name, object Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}";

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
