using FloorplanFit.Application.FloorPlans.Curation;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Curation;

public sealed class EditPublishedFloorPlanCurationHandlerIntegrationTests
{
    [Fact]
    public async Task HandleAsync_persists_the_published_fit_copy_when_resuming_an_existing_edit_draft()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-edit-published-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(tempRoot, "app.db");

        try
        {
            await SqliteSchemaInitializer.InitializeAsync(databasePath, CancellationToken.None);
            var seed = await SeedPublishedWithExistingDraftAsync(databasePath);

            await using (var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                var curationRepository = new SqliteFloorPlanCurationRepository(session);
                var handler = new EditPublishedFloorPlanCurationHandler(
                    new SqliteFloorPlanTemplateRepository(session),
                    curationRepository,
                    new SqliteFloorPlanCurationDataCloneService(session),
                    new SqliteFloorPlanReviewSessionReader(session),
                    new FixedClock(new DateTime(2026, 6, 3, 17, 0, 0, DateTimeKind.Utc)),
                    new SqliteUnitOfWork(session));

                var response = await handler.HandleAsync(seed.TemplateId, CancellationToken.None);

                Assert.Equal(seed.DraftCurationId, response.DraftCurationId);
            }

            await using (var verificationSession = await SqliteSession.OpenAsync(databasePath, CancellationToken.None))
            {
                Assert.Equal(1, CountRows(verificationSession, "measurement_corridors", seed.DraftCurationId));
                Assert.Equal(2, CountRows(verificationSession, "measurement_nodes", seed.DraftCurationId));
                Assert.Equal(1, CountRows(verificationSession, "floorplan_dimension_interval_bindings", seed.DraftCurationId));
            }
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

    private static async Task<EditSeed> SeedPublishedWithExistingDraftAsync(string databasePath)
    {
        await using var session = await SqliteSession.OpenAsync(databasePath, CancellationToken.None);
        var templateId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var measurementContextId = Guid.NewGuid();
        var publishedCurationId = Guid.NewGuid();
        var draftCurationId = Guid.NewGuid();
        var geometryPathId = Guid.NewGuid();
        var sourceCandidateId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = $"""
            INSERT INTO measurement_contexts (
                id,
                source_unit,
                to_millimeters_factor,
                linear_tolerance_mm,
                angular_tolerance_deg,
                created_at_utc)
            VALUES (
                '{measurementContextId}',
                1,
                '25.4',
                '1',
                '0.5',
                '2026-06-03T14:00:00.0000000Z');

            INSERT INTO imported_documents (
                id,
                document_type,
                original_file_name,
                storage_path,
                sha256,
                dxf_version,
                measurement_context_id,
                imported_at_utc)
            VALUES (
                '{documentId}',
                1,
                'SEMINOLE2000.dxf',
                'library/raw-dxf/SEMINOLE2000.dxf',
                'hash',
                'AC1027',
                '{measurementContextId}',
                '2026-06-03T14:00:00.0000000Z');

            INSERT INTO floorplan_templates (
                id,
                code,
                name,
                current_version_id,
                active_published_curation_id,
                is_active)
            VALUES (
                '{templateId}',
                'seminole2000',
                'SEMINOLE2000',
                '{versionId}',
                '{publishedCurationId}',
                1);

            INSERT INTO floorplan_versions (
                id,
                floorplan_template_id,
                imported_document_id,
                geometry_fingerprint,
                version_number,
                created_at_utc)
            VALUES (
                '{versionId}',
                '{templateId}',
                '{documentId}',
                'fingerprint',
                1,
                '2026-06-03T14:00:00.0000000Z');

            INSERT INTO geometry_paths (
                id,
                is_closed)
            VALUES (
                '{geometryPathId}',
                0);

            INSERT INTO geometry_segments (
                id,
                geometry_path_id,
                sort_order,
                start_x,
                start_y,
                end_x,
                end_y)
            VALUES (
                '{Guid.NewGuid()}',
                '{geometryPathId}',
                1,
                '0',
                '0',
                '120',
                '0');

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
                    'existing draft with one non-fit override',
                    '2026-06-03T16:00:00.0000000Z',
                    NULL);

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
                '{corridorId}',
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
                    '{startNodeId}',
                    '{publishedCurationId}',
                    '{corridorId}',
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
                    '{endNodeId}',
                    '{publishedCurationId}',
                    '{corridorId}',
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
                '{corridorId}',
                '{startNodeId}',
                '{endNodeId}',
                'ManualVerified',
                '0',
                '120',
                '2026-06-03T15:30:00.0000000Z');
            """;
        command.ExecuteNonQuery();
        await session.CommitAsync(CancellationToken.None);

        return new EditSeed(templateId, draftCurationId);
    }

    private static int CountRows(SqliteSession session, string tableName, Guid curationId)
    {
        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE floorplan_curation_id = $curation_id";
        command.Parameters.AddWithValue("$curation_id", curationId.ToString());
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private sealed class FixedClock : FloorplanFit.Application.Abstractions.IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }

    private sealed record EditSeed(Guid TemplateId, Guid DraftCurationId);
}
