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
                            status: ExtractedWallCandidateStatus.Accepted,
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
                Assert.Equal(ExtractedWallCandidateStatus.Accepted, loadedCandidate.Status);
                Assert.NotEqual(Guid.Empty, loadedCandidate.GeometryPathId);
                Assert.Equal(101.6m, loadedCandidate.ThicknessMm);

                loadedCandidate.Reject();
                await candidateRepository.UpdateAsync(loadedCandidate, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var loadedCandidate = await new SqliteExtractedWallCandidateRepository(session).GetByIdAsync(candidateId, CancellationToken.None);
                Assert.NotNull(loadedCandidate);
                Assert.Equal(ExtractedWallCandidateStatus.Rejected, loadedCandidate.Status);
                Assert.Equal(2, CountRows(session.Connection, session.Transaction, "geometry_segments", "geometry_path_id = (SELECT geometry_path_id FROM extracted_wall_candidates WHERE id = $id)", ("$id", candidateId.ToString())));
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task ExtractedRoomLabelRepository_round_trips_room_labels_by_extraction_run()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var extractionRunId = Guid.NewGuid();
            var labelId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteExtractedRoomLabelRepository(session);
                await repository.AddRangeAsync(
                    [
                        new ExtractedRoomLabel(
                            labelId,
                            extractionRunId,
                            "TEXT:1",
                            "ROOM LBLS",
                            "KITCHEN",
                            125m,
                            784m,
                            0.95m,
                            "Detected from ROOM LBLS text entity.",
                            1,
                            sourceEntityKind: "TEXT",
                            textHeight: 9m,
                            rotationDegrees: 15m,
                            textStyleName: "STANDARD",
                            horizontalAlignment: "Center",
                            verticalAlignment: "Middle",
                            attachmentPoint: null,
                            colorArgb: "#FF000000")
                    ],
                    CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteExtractedRoomLabelRepository(session);
                var labels = await repository.ListByExtractionRunAsync(extractionRunId, CancellationToken.None);

                var label = Assert.Single(labels);
                Assert.Equal(labelId, label.Id);
                Assert.Equal("KITCHEN", label.Text);
                Assert.Equal("ROOM LBLS", label.SourceLayer);
                Assert.Equal(125m, label.X);
                Assert.Equal(784m, label.Y);
                Assert.Equal(0.95m, label.Confidence);
                Assert.Equal("TEXT", label.SourceEntityKind);
                Assert.Equal(9m, label.TextHeight);
                Assert.Equal(15m, label.RotationDegrees);
                Assert.Equal("STANDARD", label.TextStyleName);
                Assert.Equal("Center", label.HorizontalAlignment);
                Assert.Equal("Middle", label.VerticalAlignment);
                Assert.Equal("#FF000000", label.ColorArgb);
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task ExtractedRoomLabelRepository_removes_room_labels_so_false_positives_do_not_persist()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var extractionRunId = Guid.NewGuid();
            var labelId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteExtractedRoomLabelRepository(session);
                await repository.AddRangeAsync(
                    [
                        new ExtractedRoomLabel(
                            labelId,
                            extractionRunId,
                            "TEXT:1",
                            "ROOM LBLS",
                            "KITCHEN",
                            125m,
                            784m,
                            0.95m,
                            "Detected from ROOM LBLS text entity.",
                            1)
                    ],
                    CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteExtractedRoomLabelRepository(session);
                await repository.RemoveAsync(labelId, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteExtractedRoomLabelRepository(session);
                var labels = await repository.ListByExtractionRunAsync(extractionRunId, CancellationToken.None);

                Assert.Empty(labels);
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task ExtractedOpeningRepositories_round_trip_opening_geometry_and_labels_by_extraction_run()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var extractionRunId = Guid.NewGuid();
            var openingId = Guid.NewGuid();
            var openingLabelId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                await new SqliteExtractedOpeningCandidateRepository(session).AddRangeAsync(
                    [
                        new ExtractedOpeningCandidate(
                            openingId,
                            extractionRunId,
                            "ARC:1",
                            "DOORS",
                            "Door",
                            "ARC",
                            Guid.Empty,
                            0.95m,
                            "Detected from DOORS arc entity.",
                            1)
                    ],
                    [
                        new DetectedOpeningCandidate(
                            "ARC:1",
                            "DOORS",
                            "Door",
                            "ARC",
                            [new GeometryPoint(0m, 0m), new GeometryPoint(12m, 8m), new GeometryPoint(24m, 0m)],
                            0.95m,
                            "Detected from DOORS arc entity.")
                    ],
                    CancellationToken.None);

                await new SqliteExtractedOpeningLabelRepository(session).AddRangeAsync(
                    [
                        new ExtractedOpeningLabel(
                            openingLabelId,
                            extractionRunId,
                            "TEXT:1",
                            "DOORTEXT",
                            "Door",
                            "2668",
                            100m,
                            200m,
                            0.95m,
                            "Detected from DOORTEXT text entity.",
                            1,
                            "TEXT",
                            3.5m,
                            90m,
                            "TEXT1",
                            "Left",
                            "Baseline",
                            null,
                            "#FF000000")
                    ],
                    CancellationToken.None);

                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var openings = await new SqliteExtractedOpeningCandidateRepository(session)
                    .ListByExtractionRunAsync(extractionRunId, CancellationToken.None);
                var opening = Assert.Single(openings);
                Assert.Equal(openingId, opening.Id);
                Assert.Equal("Door", opening.Kind);
                Assert.Equal("ARC", opening.SourceEntityKind);
                Assert.NotNull(opening.GeometryPathId);
                Assert.Equal(2, CountRows(session.Connection, session.Transaction, "geometry_segments", "geometry_path_id = $geometry_path_id", ("$geometry_path_id", opening.GeometryPathId!.Value.ToString())));

                var labels = await new SqliteExtractedOpeningLabelRepository(session)
                    .ListByExtractionRunAsync(extractionRunId, CancellationToken.None);
                var label = Assert.Single(labels);
                Assert.Equal(openingLabelId, label.Id);
                Assert.Equal("Door", label.Kind);
                Assert.Equal("2668", label.Text);
                Assert.Equal(3.5m, label.TextHeight);
                Assert.Equal(90m, label.RotationDegrees);
                Assert.Equal("TEXT1", label.TextStyleName);
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task ExtractedOpeningRepositories_remove_opening_geometry_and_labels_so_false_positives_do_not_persist()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var extractionRunId = Guid.NewGuid();
            var openingId = Guid.NewGuid();
            var openingLabelId = Guid.NewGuid();
            Guid persistedGeometryPathId;

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var openingRepository = new SqliteExtractedOpeningCandidateRepository(session);
                await openingRepository.AddRangeAsync(
                    [
                        new ExtractedOpeningCandidate(
                            openingId,
                            extractionRunId,
                            "ARC:1",
                            "DOORS",
                            "Door",
                            "ARC",
                            Guid.Empty,
                            0.95m,
                            "Detected from DOORS arc entity.",
                            1)
                    ],
                    [
                        new DetectedOpeningCandidate(
                            "ARC:1",
                            "DOORS",
                            "Door",
                            "ARC",
                            [new GeometryPoint(0m, 0m), new GeometryPoint(12m, 8m), new GeometryPoint(24m, 0m)],
                            0.95m,
                            "Detected from DOORS arc entity.")
                    ],
                    CancellationToken.None);

                var opening = Assert.Single(await openingRepository.ListByExtractionRunAsync(extractionRunId, CancellationToken.None));
                persistedGeometryPathId = opening.GeometryPathId ?? throw new InvalidOperationException("Expected opening geometry path.");

                await new SqliteExtractedOpeningLabelRepository(session).AddRangeAsync(
                    [
                        new ExtractedOpeningLabel(
                            openingLabelId,
                            extractionRunId,
                            "TEXT:1",
                            "DOORTEXT",
                            "Door",
                            "24\"DR.",
                            100m,
                            200m,
                            0.95m,
                            "Detected from DOORTEXT text entity.",
                            1)
                    ],
                    CancellationToken.None);

                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                await new SqliteExtractedOpeningCandidateRepository(session).RemoveAsync(openingId, CancellationToken.None);
                await new SqliteExtractedOpeningLabelRepository(session).RemoveAsync(openingLabelId, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                Assert.Empty(await new SqliteExtractedOpeningCandidateRepository(session).ListByExtractionRunAsync(extractionRunId, CancellationToken.None));
                Assert.Empty(await new SqliteExtractedOpeningLabelRepository(session).ListByExtractionRunAsync(extractionRunId, CancellationToken.None));
                Assert.Equal(0, CountRows(session.Connection, session.Transaction, "geometry_segments", "geometry_path_id = $geometry_path_id", ("$geometry_path_id", persistedGeometryPathId.ToString())));
                Assert.Equal(0, CountRows(session.Connection, session.Transaction, "geometry_paths", "id = $geometry_path_id", ("$geometry_path_id", persistedGeometryPathId.ToString())));
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task PinchGroupRepository_round_trips_named_groups_by_curation()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var patioGroupId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqlitePinchGroupRepository(session);
                await repository.AddAsync(new PinchGroup(patioGroupId, curationId, "Patio", PinchAxisTag.Width, 1), CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqlitePinchGroupRepository(session);
                var loaded = await repository.GetByIdAsync(patioGroupId, CancellationToken.None);
                var groups = await repository.ListByCurationAsync(curationId, CancellationToken.None);

                Assert.NotNull(loaded);
                Assert.Equal("Patio", loaded.Name);
                Assert.Equal(PinchAxisTag.Width, loaded.AxisTag);
                Assert.Single(groups);
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task PinchMarkerRepository_round_trips_grouped_markers_and_removes_by_source_candidate()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var pinchGroupId = Guid.NewGuid();
            var sourceCandidateId = Guid.NewGuid();
            var pinchMarkerId = Guid.NewGuid();
            var geometryPathId = Guid.NewGuid();

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                await new SqlitePinchGroupRepository(session).AddAsync(
                    new PinchGroup(pinchGroupId, curationId, "Patio", PinchAxisTag.Height, 1),
                    CancellationToken.None);

                var repository = new SqlitePinchMarkerRepository(session);
                var marker = new PinchMarker(
                    pinchMarkerId,
                    curationId,
                    pinchGroupId,
                    sourceCandidateId,
                    geometryPathId,
                    0.5m,
                    120m,
                    1);

                await repository.AddAsync(marker, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqlitePinchMarkerRepository(session);
                var loadedMarker = await repository.GetByIdAsync(pinchMarkerId, CancellationToken.None);

                Assert.NotNull(loadedMarker);
                Assert.Equal(pinchGroupId, loadedMarker.PinchGroupId);
                Assert.Equal(sourceCandidateId, loadedMarker.SourceCandidateId);
                Assert.Equal(geometryPathId, loadedMarker.GeometryPathId);
                Assert.Equal(0.5m, loadedMarker.PositionRatio);
                Assert.Equal(120m, loadedMarker.MaxTrimMm);
                Assert.Single(await repository.ListByCurationAsync(curationId, CancellationToken.None));

                await repository.RemoveBySourceCandidateAsync(curationId, sourceCandidateId, CancellationToken.None);
                await session.CommitAsync(CancellationToken.None);
            }

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqlitePinchMarkerRepository(session);
                Assert.Empty(await repository.ListByCurationAsync(curationId, CancellationToken.None));
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task InitializeAsync_migrates_legacy_pinch_markers_schema_to_the_pinch_native_shape()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var pinchGroupId = Guid.NewGuid();
            var curatedWallId = Guid.NewGuid();
            var sourceCandidateId = Guid.NewGuid();
            var geometryPathId = Guid.NewGuid();
            var pinchMarkerId = Guid.NewGuid();

            await using (var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}"))
            {
                await connection.OpenAsync(CancellationToken.None);

                using var command = connection.CreateCommand();
                command.CommandText = """
                    DROP TABLE pinch_markers;
                    DROP TABLE pinch_groups;

                    CREATE TABLE pinch_groups (
                        id TEXT PRIMARY KEY,
                        floorplan_curation_id TEXT NOT NULL,
                        name TEXT NOT NULL,
                        axis_tag INTEGER NOT NULL,
                        sort_order INTEGER NOT NULL
                    );

                    CREATE TABLE curated_walls (
                        id TEXT PRIMARY KEY,
                        floorplan_curation_id TEXT NOT NULL,
                        stable_wall_id TEXT NOT NULL,
                        source_candidate_id TEXT NULL,
                        source_entity_ref TEXT NULL,
                        geometry_path_id TEXT NULL,
                        wall_role INTEGER NOT NULL,
                        mobility_level INTEGER NOT NULL,
                        protection_level INTEGER NOT NULL,
                        thickness_mm TEXT NULL,
                        assembly_code TEXT NULL,
                        height_mm TEXT NULL,
                        is_exterior INTEGER NOT NULL,
                        is_structural_hint INTEGER NOT NULL,
                        wall_group_id TEXT NULL,
                        sort_order INTEGER NOT NULL,
                        notes TEXT NULL
                    );

                    CREATE TABLE pinch_markers (
                        id TEXT PRIMARY KEY,
                        floorplan_curation_id TEXT NOT NULL,
                        pinch_group_id TEXT NOT NULL,
                        curated_wall_id TEXT NOT NULL,
                        geometry_path_id TEXT NOT NULL,
                        position_ratio TEXT NOT NULL,
                        max_trim_mm TEXT NOT NULL,
                        sort_order INTEGER NOT NULL
                    );

                    INSERT INTO pinch_groups (id, floorplan_curation_id, name, axis_tag, sort_order)
                    VALUES ($pinch_group_id, $floorplan_curation_id, 'Height pinches', 2, 1);

                    INSERT INTO curated_walls (
                        id,
                        floorplan_curation_id,
                        stable_wall_id,
                        source_candidate_id,
                        source_entity_ref,
                        geometry_path_id,
                        wall_role,
                        mobility_level,
                        protection_level,
                        thickness_mm,
                        assembly_code,
                        height_mm,
                        is_exterior,
                        is_structural_hint,
                        wall_group_id,
                        sort_order,
                        notes
                    )
                    VALUES (
                        $curated_wall_id,
                        $floorplan_curation_id,
                        'W-001',
                        $source_candidate_id,
                        'LINE:1',
                        $geometry_path_id,
                        1,
                        1,
                        1,
                        NULL,
                        NULL,
                        NULL,
                        0,
                        0,
                        NULL,
                        1,
                        NULL
                    );

                    INSERT INTO pinch_markers (
                        id,
                        floorplan_curation_id,
                        pinch_group_id,
                        curated_wall_id,
                        geometry_path_id,
                        position_ratio,
                        max_trim_mm,
                        sort_order
                    )
                    VALUES (
                        $pinch_marker_id,
                        $floorplan_curation_id,
                        $pinch_group_id,
                        $curated_wall_id,
                        $geometry_path_id,
                        '0.5',
                        '120',
                        1
                    );
                    """;
                command.Parameters.AddWithValue("$pinch_group_id", pinchGroupId.ToString());
                command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());
                command.Parameters.AddWithValue("$curated_wall_id", curatedWallId.ToString());
                command.Parameters.AddWithValue("$source_candidate_id", sourceCandidateId.ToString());
                command.Parameters.AddWithValue("$geometry_path_id", geometryPathId.ToString());
                command.Parameters.AddWithValue("$pinch_marker_id", pinchMarkerId.ToString());
                command.ExecuteNonQuery();
            }

            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqlitePinchMarkerRepository(session);
                var markers = await repository.ListByCurationAsync(curationId, CancellationToken.None);

                var marker = Assert.Single(markers);
                Assert.Equal(pinchMarkerId, marker.Id);
                Assert.Equal(pinchGroupId, marker.PinchGroupId);
                Assert.Equal(sourceCandidateId, marker.SourceCandidateId);
                Assert.Equal(geometryPathId, marker.GeometryPathId);
                Assert.Equal(0.5m, marker.PositionRatio);
                Assert.Equal(120m, marker.MaxTrimMm);
            }

            await using (var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}"))
            {
                await connection.OpenAsync(CancellationToken.None);
                using var command = connection.CreateCommand();
                command.CommandText = "PRAGMA table_info(pinch_markers)";

                var columnNames = new List<string>();
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    columnNames.Add(reader.GetString(1));
                }

                Assert.Contains("source_candidate_id", columnNames);
                Assert.Contains("pinch_group_id", columnNames);
                Assert.DoesNotContain("axis_tag", columnNames);
                Assert.DoesNotContain("curated_wall_id", columnNames);
            }
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task InitializeAsync_migrates_axis_tagged_pinch_markers_to_default_groups()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var sourceCandidateId = Guid.NewGuid();
            var geometryPathId = Guid.NewGuid();
            var pinchMarkerId = Guid.NewGuid();

            await using (var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}"))
            {
                await connection.OpenAsync(CancellationToken.None);

                using var command = connection.CreateCommand();
                command.CommandText = """
                    DROP TABLE pinch_markers;
                    DELETE FROM pinch_groups;

                    CREATE TABLE pinch_markers (
                        id TEXT PRIMARY KEY,
                        floorplan_curation_id TEXT NOT NULL,
                        source_candidate_id TEXT NOT NULL,
                        geometry_path_id TEXT NOT NULL,
                        axis_tag INTEGER NOT NULL,
                        position_ratio TEXT NOT NULL,
                        max_trim_mm TEXT NOT NULL,
                        sort_order INTEGER NOT NULL
                    );

                    INSERT INTO pinch_markers (
                        id,
                        floorplan_curation_id,
                        source_candidate_id,
                        geometry_path_id,
                        axis_tag,
                        position_ratio,
                        max_trim_mm,
                        sort_order
                    )
                    VALUES (
                        $pinch_marker_id,
                        $floorplan_curation_id,
                        $source_candidate_id,
                        $geometry_path_id,
                        2,
                        '0.35',
                        '90',
                        1
                    );
                    """;
                command.Parameters.AddWithValue("$pinch_marker_id", pinchMarkerId.ToString());
                command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());
                command.Parameters.AddWithValue("$source_candidate_id", sourceCandidateId.ToString());
                command.Parameters.AddWithValue("$geometry_path_id", geometryPathId.ToString());
                command.ExecuteNonQuery();
            }

            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var groups = await new SqlitePinchGroupRepository(session).ListByCurationAsync(curationId, CancellationToken.None);
            var markers = await new SqlitePinchMarkerRepository(session).ListByCurationAsync(curationId, CancellationToken.None);

            var group = Assert.Single(groups);
            Assert.Equal("Height", group.Name);
            Assert.Equal(PinchAxisTag.Height, group.AxisTag);

            var marker = Assert.Single(markers);
            Assert.Equal(pinchMarkerId, marker.Id);
            Assert.Equal(group.Id, marker.PinchGroupId);
            Assert.Equal(sourceCandidateId, marker.SourceCandidateId);
            Assert.Equal(0.35m, marker.PositionRatio);
            Assert.Equal(90m, marker.MaxTrimMm);
        }
        finally
        {
            DeleteTempRoot(tempRoot);
        }
    }

    [Fact]
    public async Task InitializeAsync_migrates_axis_tagged_pinch_markers_when_existing_groups_share_the_same_axis()
    {
        var tempRoot = CreateTempRoot();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var curationId = Guid.NewGuid();
            var firstGroupId = Guid.NewGuid();
            var secondGroupId = Guid.NewGuid();
            var sourceCandidateId = Guid.NewGuid();
            var geometryPathId = Guid.NewGuid();
            var pinchMarkerId = Guid.NewGuid();

            await using (var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}"))
            {
                await connection.OpenAsync(CancellationToken.None);

                using var command = connection.CreateCommand();
                command.CommandText = """
                    DROP TABLE pinch_markers;
                    DELETE FROM pinch_groups;

                    INSERT INTO pinch_groups (id, floorplan_curation_id, name, axis_tag, sort_order)
                    VALUES
                        ($first_group_id, $floorplan_curation_id, 'Patio', 1, 1),
                        ($second_group_id, $floorplan_curation_id, 'Main Width', 1, 2);

                    CREATE TABLE pinch_markers (
                        id TEXT PRIMARY KEY,
                        floorplan_curation_id TEXT NOT NULL,
                        source_candidate_id TEXT NOT NULL,
                        geometry_path_id TEXT NOT NULL,
                        axis_tag INTEGER NOT NULL,
                        position_ratio TEXT NOT NULL,
                        max_trim_mm TEXT NOT NULL,
                        sort_order INTEGER NOT NULL
                    );

                    INSERT INTO pinch_markers (
                        id,
                        floorplan_curation_id,
                        source_candidate_id,
                        geometry_path_id,
                        axis_tag,
                        position_ratio,
                        max_trim_mm,
                        sort_order
                    )
                    VALUES (
                        $pinch_marker_id,
                        $floorplan_curation_id,
                        $source_candidate_id,
                        $geometry_path_id,
                        1,
                        '0.45',
                        '120',
                        1
                    );
                    """;
                command.Parameters.AddWithValue("$first_group_id", firstGroupId.ToString());
                command.Parameters.AddWithValue("$second_group_id", secondGroupId.ToString());
                command.Parameters.AddWithValue("$pinch_marker_id", pinchMarkerId.ToString());
                command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());
                command.Parameters.AddWithValue("$source_candidate_id", sourceCandidateId.ToString());
                command.Parameters.AddWithValue("$geometry_path_id", geometryPathId.ToString());
                command.ExecuteNonQuery();
            }

            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var markers = await new SqlitePinchMarkerRepository(session).ListByCurationAsync(curationId, CancellationToken.None);

            var marker = Assert.Single(markers);
            Assert.Equal(pinchMarkerId, marker.Id);
            Assert.Equal(firstGroupId, marker.PinchGroupId);
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
