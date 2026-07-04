using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class SqliteFloorPlanCurationDataCloneServiceIntegrationTests
{
    [Fact]
    public async Task EnsureClonedAsync_copies_published_fit_rows_to_the_edit_draft_and_remaps_owned_ids()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-clone-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "app.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);
            var seed = await SeedPublishedAndDraftAsync(databasePath);

            await using var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            var cloneService = new SqliteFloorPlanCurationDataCloneService(session);

            await cloneService.EnsureClonedAsync(seed.PublishedCurationId, seed.DraftCurationId, CancellationToken.None);

            var draftGroup = await QuerySingleAsync(
                session,
                "SELECT id, floorplan_curation_id, name, axis_tag FROM pinch_groups WHERE floorplan_curation_id = $curation_id",
                seed.DraftCurationId);
            var draftMarker = await QuerySingleAsync(
                session,
                "SELECT id, floorplan_curation_id, pinch_group_id FROM pinch_markers WHERE floorplan_curation_id = $curation_id",
                seed.DraftCurationId);
            var draftCorridor = await QuerySingleAsync(
                session,
                "SELECT id, floorplan_curation_id, name FROM measurement_corridors WHERE floorplan_curation_id = $curation_id",
                seed.DraftCurationId);
            var draftNodes = await QueryRowsAsync(
                session,
                "SELECT id, floorplan_curation_id, corridor_id, sort_order FROM measurement_nodes WHERE floorplan_curation_id = $curation_id ORDER BY sort_order",
                seed.DraftCurationId);
            var draftBinding = await QuerySingleAsync(
                session,
                "SELECT floorplan_curation_id, corridor_id, start_node_id, end_node_id FROM floorplan_dimension_interval_bindings WHERE floorplan_curation_id = $curation_id",
                seed.DraftCurationId);

            Assert.NotEqual(seed.PublishedPinchGroupId.ToString(), draftGroup["id"]);
            Assert.Equal(seed.DraftCurationId.ToString(), draftGroup["floorplan_curation_id"]);
            Assert.Equal("Published Group", draftGroup["name"]);
            Assert.Equal(draftGroup["id"], draftMarker["pinch_group_id"]);
            Assert.NotEqual(seed.PublishedCorridorId.ToString(), draftCorridor["id"]);
            Assert.Equal(seed.DraftCurationId.ToString(), draftCorridor["floorplan_curation_id"]);
            Assert.Equal(2, draftNodes.Count);
            Assert.All(draftNodes, node => Assert.Equal(draftCorridor["id"], node["corridor_id"]));
            Assert.Equal(draftCorridor["id"], draftBinding["corridor_id"]);
            Assert.Equal(draftNodes[0]["id"], draftBinding["start_node_id"]);
            Assert.Equal(draftNodes[1]["id"], draftBinding["end_node_id"]);
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
    public async Task EnsureClonedAsync_copies_published_fit_rows_when_the_edit_draft_already_has_non_fit_data()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-clone-existing-data-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "app.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);
            var seed = await SeedPublishedAndDraftAsync(databasePath, seedDraftPositionOverride: true);

            await using var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
            var cloneService = new SqliteFloorPlanCurationDataCloneService(session);

            await cloneService.EnsureClonedAsync(seed.PublishedCurationId, seed.DraftCurationId, CancellationToken.None);

            var draftCorridor = await QuerySingleAsync(
                session,
                "SELECT id, floorplan_curation_id, name FROM measurement_corridors WHERE floorplan_curation_id = $curation_id",
                seed.DraftCurationId);
            var draftNodes = await QueryRowsAsync(
                session,
                "SELECT id, floorplan_curation_id, corridor_id, sort_order FROM measurement_nodes WHERE floorplan_curation_id = $curation_id ORDER BY sort_order",
                seed.DraftCurationId);

            Assert.Equal(seed.DraftCurationId.ToString(), draftCorridor["floorplan_curation_id"]);
            Assert.Equal("Published Franja", draftCorridor["name"]);
            Assert.Equal(2, draftNodes.Count);
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

    private static async Task<CloneSeed> SeedPublishedAndDraftAsync(
        string databasePath,
        bool seedDraftPositionOverride = false)
    {
        await using var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
        var versionId = Guid.NewGuid();
        var publishedCurationId = Guid.NewGuid();
        var draftCurationId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var sourceCandidateId = Guid.NewGuid();
        var publishedPinchGroupId = Guid.NewGuid();
        var publishedPinchMarkerId = Guid.NewGuid();
        var publishedCorridorId = Guid.NewGuid();
        var publishedStartNodeId = Guid.NewGuid();
        var publishedEndNodeId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = $"""
            INSERT INTO floorplan_curations (
                id,
                floorplan_version_id,
                curation_version,
                status,
                based_on_curation_id,
                notes,
                created_at_utc,
                published_at_utc)
            VALUES
                (
                    '{publishedCurationId}',
                    '{versionId}',
                    1,
                    {(int)FloorPlanCurationStatus.Published},
                    NULL,
                    'published',
                    '2026-06-03T14:00:00.0000000Z',
                    '2026-06-03T15:00:00.0000000Z'),
                (
                    '{draftCurationId}',
                    '{versionId}',
                    2,
                    {(int)FloorPlanCurationStatus.Draft},
                    '{publishedCurationId}',
                    'draft',
                    '2026-06-03T16:00:00.0000000Z',
                    NULL);

            INSERT INTO pinch_groups (
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                sort_order)
            VALUES (
                '{publishedPinchGroupId}',
                '{publishedCurationId}',
                'Published Group',
                {(int)PinchAxisTag.Width},
                1);

            INSERT INTO pinch_markers (
                id,
                floorplan_curation_id,
                pinch_group_id,
                source_candidate_id,
                geometry_path_id,
                position_ratio,
                max_trim_mm,
                sort_order)
            VALUES (
                '{publishedPinchMarkerId}',
                '{publishedCurationId}',
                '{publishedPinchGroupId}',
                '{sourceCandidateId}',
                '{geometryPathId}',
                '0.5',
                '120',
                1);

            INSERT INTO measurement_corridors (
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                guide_geometry_path_id,
                band_min_coordinate,
                band_max_coordinate,
                status,
                sort_order)
            VALUES (
                '{publishedCorridorId}',
                '{publishedCurationId}',
                'Published Franja',
                {(int)PinchAxisTag.Width},
                '{geometryPathId}',
                '0',
                '120',
                'Verified',
                1);

            INSERT INTO measurement_nodes (
                id,
                floorplan_curation_id,
                corridor_id,
                sort_order,
                reference_kind,
                source_artifact_kind,
                source_artifact_id,
                geometry_path_id,
                snap_kind,
                anchor_x,
                anchor_y,
                axis_coordinate,
                offset_along_axis,
                offset_normal,
                position_ratio)
            VALUES
                (
                    '{publishedStartNodeId}',
                    '{publishedCurationId}',
                    '{publishedCorridorId}',
                    1,
                    'ProjectedGeometry',
                    '{FloorPlanArtifactSourceKinds.WallCandidate}',
                    '{sourceCandidateId}',
                    '{geometryPathId}',
                    'Projected',
                    '0',
                    '0',
                    '0',
                    '0',
                    '0',
                    '0'),
                (
                    '{publishedEndNodeId}',
                    '{publishedCurationId}',
                    '{publishedCorridorId}',
                    2,
                    'ProjectedGeometry',
                    '{FloorPlanArtifactSourceKinds.WallCandidate}',
                    '{sourceCandidateId}',
                    '{geometryPathId}',
                    'Projected',
                    '120',
                    '0',
                    '120',
                    '0',
                    '0',
                    '1');

            INSERT INTO floorplan_dimension_interval_bindings (
                floorplan_curation_id,
                dimension_id,
                corridor_id,
                start_node_id,
                end_node_id,
                binding_status,
                interval_start_coordinate,
                interval_end_coordinate,
                updated_at_utc)
            VALUES (
                '{publishedCurationId}',
                '{dimensionId}',
                '{publishedCorridorId}',
                '{publishedStartNodeId}',
                '{publishedEndNodeId}',
                'ManualVerified',
                '0',
                '120',
                '2026-06-03T15:30:00.0000000Z');
            """;
        command.ExecuteNonQuery();

        if (seedDraftPositionOverride)
        {
            command.CommandText = $"""
                INSERT INTO floorplan_artifact_positions (
                    floorplan_curation_id,
                    source_artifact_kind,
                    source_artifact_id,
                    position_mode,
                    resolved_x,
                    resolved_y,
                    translation_dx,
                    translation_dy,
                    updated_at_utc)
                VALUES (
                    '{draftCurationId}',
                    '{FloorPlanArtifactPositionSourceKinds.RoomLabel}',
                    '{Guid.NewGuid()}',
                    1,
                    '10',
                    '20',
                    NULL,
                    NULL,
                    '2026-06-03T16:10:00.0000000Z');
                """;
            command.ExecuteNonQuery();
        }

        await session.CommitAsync(CancellationToken.None);

        return new CloneSeed(
            publishedCurationId,
            draftCurationId,
            publishedPinchGroupId,
            publishedPinchMarkerId,
            publishedCorridorId);
    }

    private static async Task<Dictionary<string, string>> QuerySingleAsync(
        SqliteSession session,
        string sql,
        Guid curationId)
    {
        var rows = await QueryRowsAsync(session, sql, curationId);
        return Assert.Single(rows);
    }

    private static Task<List<Dictionary<string, string>>> QueryRowsAsync(
        SqliteSession session,
        string sql,
        Guid curationId)
    {
        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        command.Parameters.AddWithValue("$curation_id", curationId.ToString());

        using var reader = command.ExecuteReader();
        var rows = new List<Dictionary<string, string>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                row[reader.GetName(index)] = reader.GetString(index);
            }

            rows.Add(row);
        }

        return Task.FromResult(rows);
    }

    private sealed record CloneSeed(
        Guid PublishedCurationId,
        Guid DraftCurationId,
        Guid PublishedPinchGroupId,
        Guid PublishedPinchMarkerId,
        Guid PublishedCorridorId);
}
