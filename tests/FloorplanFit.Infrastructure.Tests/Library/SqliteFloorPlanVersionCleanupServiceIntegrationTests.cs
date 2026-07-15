using FloorplanFit.Application.FloorPlans.Import;
using FloorplanFit.Infrastructure.Dxf;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Security;
using FloorplanFit.Infrastructure.Storage;
using FloorplanFit.Infrastructure.Tests.TestSupport;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.Library;

public sealed class SqliteFloorPlanVersionCleanupServiceIntegrationTests
{
    [Fact]
    public async Task CleanupAsync_hard_deletes_versions_that_were_soft_deleted_and_leaves_active_versions_intact()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-cleanup-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc));
            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 13, 0, 0, DateTimeKind.Utc));

            Guid softDeletedVersionId;
            Guid survivingVersionId;
            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var reader = new SqliteFloorPlanLibraryReader(session);
                var item = Assert.Single(await reader.ListAsync(CancellationToken.None));
                var ordered = item.Versions.OrderByDescending(v => v.VersionNumber).ToArray();
                softDeletedVersionId = ordered[0].VersionId;
                survivingVersionId = ordered[1].VersionId;

                var repository = new SqliteFloorPlanVersionRepository(session);
                await repository.RemoveAsync(softDeletedVersionId, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            Assert.True(VersionRowExists(workspace.DatabasePath, softDeletedVersionId));
            Assert.True(VersionRowExists(workspace.DatabasePath, survivingVersionId));

            var cleanup = new SqliteFloorPlanVersionCleanupService(workspace);
            var result = await cleanup.CleanupAsync(CancellationToken.None);

            Assert.Equal(1, result.VersionsRemoved);
            Assert.False(VersionRowExists(workspace.DatabasePath, softDeletedVersionId));
            Assert.True(VersionRowExists(workspace.DatabasePath, survivingVersionId));

            await using var verifySession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var remainingItem = Assert.Single(await new SqliteFloorPlanLibraryReader(verifySession).ListAsync(CancellationToken.None));
            var remainingVersion = Assert.Single(remainingItem.Versions);
            Assert.Equal(survivingVersionId, remainingVersion.VersionId);
            Assert.True(remainingVersion.IsCurrent);
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
    public async Task CleanupAsync_removes_owned_curation_geometry_without_leaving_orphans()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-cleanup-owned-geometry-{Guid.NewGuid():N}");
        var versionId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var measurementContextId = Guid.NewGuid();
        var curationId = Guid.NewGuid();
        var extractionRunId = Guid.NewGuid();
        var pinchPathId = Guid.NewGuid();
        var corridorPathId = Guid.NewGuid();
        var nodePathId = Guid.NewGuid();
        var anchorPathId = Guid.NewGuid();

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            ExecuteNonQuery(
                workspace.DatabasePath,
                $"""
                INSERT INTO measurement_contexts (
                    id, source_unit, to_millimeters_factor, linear_tolerance_mm, angular_tolerance_deg, created_at_utc
                ) VALUES ('{measurementContextId}', 1, '1', '0.1', '0.1', '2026-07-13T12:00:00Z');
                INSERT INTO imported_documents (
                    id, document_type, original_file_name, storage_path, sha256, dxf_version,
                    measurement_context_id, imported_at_utc
                ) VALUES (
                    '{documentId}', 1, 'plan.dxf', 'plan.dxf', 'hash', NULL,
                    '{measurementContextId}', '2026-07-13T12:00:00Z'
                );
                INSERT INTO floorplan_versions (
                    id, floorplan_template_id, imported_document_id, geometry_fingerprint,
                    version_number, created_at_utc, deleted_at_utc
                ) VALUES (
                    '{versionId}', '{Guid.NewGuid()}', '{documentId}', 'fingerprint',
                    1, '2026-07-13T12:00:00Z', '2026-07-13T13:00:00Z'
                );
                INSERT INTO wall_extraction_runs (
                    id, floorplan_version_id, status, started_at_utc, finished_at_utc,
                    extractor_version, error_message
                ) VALUES (
                    '{extractionRunId}', '{versionId}', 'Completed', '2026-07-13T12:00:00Z',
                    '2026-07-13T12:01:00Z', 'test', NULL
                );
                INSERT INTO floorplan_curations (
                    id, floorplan_version_id, curation_version, status, based_on_curation_id,
                    notes, created_at_utc, published_at_utc
                ) VALUES (
                    '{curationId}', '{versionId}', 1, 1, NULL, NULL,
                    '2026-07-13T12:00:00Z', NULL
                );
                INSERT INTO geometry_paths (id, is_closed) VALUES
                    ('{pinchPathId}', 0),
                    ('{corridorPathId}', 0),
                    ('{nodePathId}', 0),
                    ('{anchorPathId}', 0);
                INSERT INTO geometry_segments (
                    id, geometry_path_id, sort_order, start_x, start_y, end_x, end_y
                ) VALUES
                    ('{Guid.NewGuid()}', '{pinchPathId}', 1, '0', '0', '1', '1'),
                    ('{Guid.NewGuid()}', '{corridorPathId}', 1, '0', '0', '1', '1'),
                    ('{Guid.NewGuid()}', '{nodePathId}', 1, '0', '0', '1', '1'),
                    ('{Guid.NewGuid()}', '{anchorPathId}', 1, '0', '0', '1', '1');
                INSERT INTO pinch_groups (
                    id, floorplan_curation_id, name, axis_tag, sort_order
                ) VALUES ('group-1', '{curationId}', 'Width', 1, 1);
                INSERT INTO pinch_markers (
                    id, floorplan_curation_id, pinch_group_id, source_candidate_id,
                    geometry_path_id, position_ratio, max_trim_mm, sort_order
                ) VALUES (
                    'marker-1', '{curationId}', 'group-1', 'candidate-1',
                    '{pinchPathId}', '0.5', '100', 1
                );
                INSERT INTO measurement_corridors (
                    id, floorplan_curation_id, name, axis_tag, guide_geometry_path_id,
                    band_min_coordinate, band_max_coordinate, status, sort_order
                ) VALUES (
                    'corridor-1', '{curationId}', 'Width', 1, '{corridorPathId}',
                    '0', '100', 'Confirmed', 1
                );
                INSERT INTO measurement_nodes (
                    id, floorplan_curation_id, corridor_id, sort_order, reference_kind,
                    source_artifact_kind, source_artifact_id, geometry_path_id, snap_kind,
                    anchor_x, anchor_y, axis_coordinate, offset_along_axis, offset_normal, position_ratio
                ) VALUES (
                    'node-1', '{curationId}', 'corridor-1', 1, 'Wall',
                    'WallCandidate', 'candidate-1', '{nodePathId}', 'Start',
                    '0', '0', '0', '0', '0', '0'
                );
                INSERT INTO floorplan_dimension_interval_bindings (
                    floorplan_curation_id, dimension_id, corridor_id, start_node_id, end_node_id,
                    binding_status, interval_start_coordinate, interval_end_coordinate, updated_at_utc
                ) VALUES (
                    '{curationId}', 'dimension-1', 'corridor-1', 'node-1', 'node-1',
                    'Confirmed', '0', '1', '2026-07-13T12:00:00Z'
                );
                INSERT INTO floorplan_dimension_binding_overrides (
                    floorplan_curation_id, source_dimension_key, binding_kind, is_resolved,
                    confidence, notes, axis_tag, start_coordinate, end_coordinate,
                    orientation_degrees, updated_at_utc
                ) VALUES (
                    '{curationId}', 'dimension-key', 'Edge', 1,
                    '1', '', 'Width', '0', '1', '0', '2026-07-13T12:00:00Z'
                );
                INSERT INTO floorplan_dimension_binding_override_anchors (
                    floorplan_curation_id, source_dimension_key, sort_order, edge_key,
                    source_artifact_kind, source_artifact_id, geometry_path_id, edge_anchor_kind,
                    anchor_x, anchor_y, distance_source_units, segment_ratio
                ) VALUES (
                    '{curationId}', 'dimension-key', 1, 'edge-1',
                    'WallCandidate', 'candidate-1', '{anchorPathId}', 'Start',
                    '0', '0', '0', NULL
                );
                """);

            var cleanup = new SqliteFloorPlanVersionCleanupService(workspace);
            var result = await cleanup.CleanupAsync(CancellationToken.None);

            Assert.Equal(1, result.VersionsRemoved);
            foreach (var tableName in new[]
                     {
                         "floorplan_dimension_binding_override_anchors",
                         "floorplan_dimension_binding_overrides",
                         "floorplan_dimension_interval_bindings",
                         "measurement_nodes",
                         "measurement_corridors",
                         "pinch_markers",
                         "pinch_groups",
                         "floorplan_curations",
                         "geometry_segments",
                         "geometry_paths"
                     })
            {
                Assert.Equal(0, CountRows(workspace.DatabasePath, tableName));
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

    [Fact]
    public async Task CleanupAsync_is_a_noop_when_no_versions_are_soft_deleted()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-cleanup-noop-{Guid.NewGuid():N}");
        var solutionRoot = RepositoryPaths.FindSolutionRoot();
        var sourcePath = Path.Combine(solutionRoot, "PLANS", "originalFloorPlans", "SANTA-BARBARA.dxf");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            await ExecuteImportAsync(workspace, sourcePath, new DateTime(2026, 5, 9, 12, 0, 0, DateTimeKind.Utc));

            var cleanup = new SqliteFloorPlanVersionCleanupService(workspace);
            var result = await cleanup.CleanupAsync(CancellationToken.None);

            Assert.Equal(0, result.VersionsRemoved);

            await using var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var item = Assert.Single(await new SqliteFloorPlanLibraryReader(session).ListAsync(CancellationToken.None));
            Assert.Single(item.Versions);
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

    private static bool VersionRowExists(string databasePath, Guid versionId)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM floorplan_versions WHERE id = $id";
        command.Parameters.AddWithValue("$id", versionId.ToString());
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static int CountRows(string databasePath, string tableName)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName}";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void ExecuteNonQuery(string databasePath, string sql)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static async Task ExecuteImportAsync(
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

        await handler.HandleAsync(new Contracts.FloorPlans.ImportFloorPlanRequest(sourcePath), CancellationToken.None);
    }

    private sealed class FixedClock : FloorplanFit.Application.Abstractions.IClock
    {
        public FixedClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
