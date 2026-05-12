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

public sealed class CanonicalArtifactPositionReviewSessionIntegrationTests
{
    [Fact]
    public async Task GetByTemplateAsync_returns_resolved_label_coordinates_sizes_and_translated_curated_geometry()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-position-review-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");
        var now = new DateTime(2026, 5, 11, 22, 0, 0, DateTimeKind.Utc);

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var import = await ExecuteImportAsync(workspace, sourcePath, now);
            var seeded = await SeedExtractionDraftAndPositionOverlayAsync(workspace, now.AddMinutes(5));

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var reader = new SqliteFloorPlanReviewSessionReader(session);
            var reviewSession = await reader.GetByTemplateAsync(import.Item.TemplateId, CancellationToken.None);

            Assert.NotNull(reviewSession);

            var roomLabel = Assert.Single(reviewSession.RoomLabels);
            Assert.True(roomLabel.HasManualPosition);
            Assert.True(roomLabel.HasManualTextHeight);
            Assert.Equal(280m, roomLabel.X);
            Assert.Equal(156m, roomLabel.Y);
            Assert.Equal(240m, roomLabel.DetectedX);
            Assert.Equal(180m, roomLabel.DetectedY);
            Assert.Equal(6m, roomLabel.TextHeight);
            Assert.Equal(8m, roomLabel.DetectedTextHeight);

            var openingLabel = Assert.Single(reviewSession.OpeningLabels);
            Assert.True(openingLabel.HasManualPosition);
            Assert.True(openingLabel.HasManualTextHeight);
            Assert.Equal(640m, openingLabel.X);
            Assert.Equal(96m, openingLabel.Y);
            Assert.Equal(600m, openingLabel.DetectedX);
            Assert.Equal(120m, openingLabel.DetectedY);
            Assert.Equal(3m, openingLabel.TextHeight);
            Assert.Equal(5m, openingLabel.DetectedTextHeight);

            var curatedOpening = Assert.Single(reviewSession.CuratedPlanArtifacts, item =>
                item.SourceArtifactKind == FloorPlanArtifactSourceKinds.OpeningCandidate);
            Assert.True(curatedOpening.HasManualPosition);
            Assert.Equal(24m, curatedOpening.TranslationDx);
            Assert.Equal(-12m, curatedOpening.TranslationDy);

            var movedOpeningPath = Assert.Single(reviewSession.GeometryPaths, path => path.Id == seeded.OpeningGeometryPathId);
            Assert.Equal(64m, movedOpeningPath.Segments.Single().StartX);
            Assert.Equal(-12m, movedOpeningPath.Segments.Single().StartY);

            using var geometryCommand = session.Connection.CreateCommand();
            geometryCommand.Transaction = session.Transaction;
            geometryCommand.CommandText = "SELECT start_x, start_y FROM geometry_segments WHERE geometry_path_id = $id ORDER BY sort_order LIMIT 1";
            geometryCommand.Parameters.AddWithValue("$id", seeded.OpeningGeometryPathId.ToString());
            using var geometryReader = geometryCommand.ExecuteReader();
            Assert.True(geometryReader.Read());
            Assert.Equal("40", geometryReader.GetString(0));
            Assert.Equal("0", geometryReader.GetString(1));
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

    private static async Task<ImportFloorPlanResponse> ExecuteImportAsync(AppWorkspace workspace, string sourcePath, DateTime now)
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

    private static async Task<SeededArtifacts> SeedExtractionDraftAndPositionOverlayAsync(AppWorkspace workspace, DateTime now)
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

        await new SqliteExtractedWallCandidateRepository(session).AddRangeAsync(
            [
                new ExtractedWallCandidate(
                    Guid.NewGuid(),
                    extractionRun.Id,
                    "LINE:1",
                    "WALLS",
                    Guid.Empty,
                    null,
                    0.95m,
                    null,
                    ExtractedWallCandidateStatus.Accepted,
                    1)
            ],
            [
                new DetectedWallCandidate("LINE:1", "WALLS", [new GeometryPoint(0m, 0m), new GeometryPoint(120m, 0m)], null, 0.95m, null)
            ],
            CancellationToken.None);

        var roomLabelId = Guid.NewGuid();
        await new SqliteExtractedRoomLabelRepository(session).AddRangeAsync(
            [
                new ExtractedRoomLabel(
                    roomLabelId,
                    extractionRun.Id,
                    "TEXT:ROOM:1",
                    "ROOM LBLS",
                    "KITCHEN",
                    240m,
                    180m,
                    0.95m,
                    null,
                    1,
                    textHeight: 8m)
            ],
            CancellationToken.None);

        var openingLabelId = Guid.NewGuid();
        await new SqliteExtractedOpeningLabelRepository(session).AddRangeAsync(
            [
                new ExtractedOpeningLabel(
                    openingLabelId,
                    extractionRun.Id,
                    "TEXT:OPENING:1",
                    "DOORTEXT",
                    "Door",
                    "2668",
                    600m,
                    120m,
                    0.95m,
                    null,
                    1,
                    textHeight: 5m)
            ],
            CancellationToken.None);

        var openingCandidateId = Guid.NewGuid();
        await new SqliteExtractedOpeningCandidateRepository(session).AddRangeAsync(
            [
                new ExtractedOpeningCandidate(
                    openingCandidateId,
                    extractionRun.Id,
                    "LINE:DOOR:1",
                    "DOORS",
                    "Door",
                    "LINE",
                    Guid.Empty,
                    0.95m,
                    null,
                    1)
            ],
            [
                new DetectedOpeningCandidate("LINE:DOOR:1", "DOORS", "Door", "LINE", [new GeometryPoint(40m, 0m), new GeometryPoint(76m, 0m)], 0.95m, null)
            ],
            CancellationToken.None);
        var opening = (await new SqliteExtractedOpeningCandidateRepository(session).ListByExtractionRunAsync(extractionRun.Id, CancellationToken.None)).Single();

        var draft = new FloorPlanCuration(Guid.NewGuid(), versionId, 1, FloorPlanCurationStatus.Draft, null, null, now.AddMinutes(1), null);
        await new SqliteFloorPlanCurationRepository(session).AddAsync(draft, CancellationToken.None);

        var classificationRepository = new SqliteFloorPlanArtifactClassificationRepository(session);
        await classificationRepository.UpsertAsync(
            new FloorPlanArtifactClassification(
                draft.Id,
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                opening.Id,
                FloorPlanArtifactTaxonomy.OpeningFamily,
                FloorPlanArtifactTaxonomy.OpeningCategory,
                FloorPlanArtifactTaxonomy.DoorType,
                FloorPlanArtifactDecisionState.DetectedDefault,
                now.AddMinutes(2)),
            CancellationToken.None);

        var positionRepository = new SqliteFloorPlanArtifactPositionRepository(session);
        await positionRepository.UpsertAsync(
            FloorPlanArtifactPosition.CreateAbsolutePoint(
                draft.Id,
                FloorPlanArtifactPositionSourceKinds.RoomLabel,
                roomLabelId,
                280m,
                156m,
                now.AddMinutes(3)),
            CancellationToken.None);
        await positionRepository.UpsertAsync(
            FloorPlanArtifactPosition.CreateAbsolutePoint(
                draft.Id,
                FloorPlanArtifactPositionSourceKinds.OpeningLabel,
                openingLabelId,
                640m,
                96m,
                now.AddMinutes(4)),
            CancellationToken.None);
        await positionRepository.UpsertAsync(
            FloorPlanArtifactPosition.CreateTranslation(
                draft.Id,
                FloorPlanArtifactPositionSourceKinds.OpeningCandidate,
                opening.Id,
                24m,
                -12m,
                now.AddMinutes(5)),
            CancellationToken.None);
        var labelOverrideRepository = new SqliteFloorPlanLabelOverrideRepository(session);
        await labelOverrideRepository.UpsertAsync(
            FloorPlanLabelOverride.CreateResolvedTextHeight(
                draft.Id,
                FloorPlanLabelOverrideSourceKinds.RoomLabel,
                roomLabelId,
                6m,
                now.AddMinutes(6)),
            CancellationToken.None);
        await labelOverrideRepository.UpsertAsync(
            FloorPlanLabelOverride.CreateResolvedTextHeight(
                draft.Id,
                FloorPlanLabelOverrideSourceKinds.OpeningLabel,
                openingLabelId,
                3m,
                now.AddMinutes(7)),
            CancellationToken.None);

        await session.CommitAsync(CancellationToken.None);
        return new SeededArtifacts(opening.GeometryPathId ?? Guid.Empty);
    }

    private sealed record SeededArtifacts(Guid OpeningGeometryPathId);

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
