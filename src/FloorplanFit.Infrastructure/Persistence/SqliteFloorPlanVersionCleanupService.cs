using FloorplanFit.Application.Abstractions;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanVersionCleanupService : IFloorPlanVersionCleanupService
{
    private readonly AppWorkspace workspace;

    public SqliteFloorPlanVersionCleanupService(AppWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public Task<FloorPlanVersionCleanupResult> CleanupAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = new SqliteConnection($"Data Source={workspace.DatabasePath}");
        connection.Open();

        var pendingVersionIds = LoadPendingVersionIds(connection);
        if (pendingVersionIds.Count == 0)
        {
            return Task.FromResult(new FloorPlanVersionCleanupResult(VersionsRemoved: 0));
        }

        var removed = 0;
        foreach (var versionId in pendingVersionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var transaction = connection.BeginTransaction();
            try
            {
                HardDeleteVersion(connection, transaction, versionId);
                transaction.Commit();
                removed++;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        return Task.FromResult(new FloorPlanVersionCleanupResult(VersionsRemoved: removed));
    }

    private static IReadOnlyList<string> LoadPendingVersionIds(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id
            FROM floorplan_versions
            WHERE deleted_at_utc IS NOT NULL
            ORDER BY deleted_at_utc ASC
            """;

        var ids = new List<string>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }
        return ids;
    }

    private static void HardDeleteVersion(SqliteConnection connection, SqliteTransaction transaction, string versionId)
    {
        var documentId = ReadScalarString(
            connection,
            transaction,
            """
            SELECT imported_document_id FROM floorplan_versions WHERE id = $version_id LIMIT 1
            """,
            ("$version_id", versionId));
        if (documentId is null)
        {
            return;
        }

        var measurementContextId = ReadScalarString(
            connection,
            transaction,
            """
            SELECT measurement_context_id FROM imported_documents WHERE id = $document_id LIMIT 1
            """,
            ("$document_id", documentId));

        var curationFilter =
            "(SELECT id FROM floorplan_curations WHERE floorplan_version_id = $version_id)";
        var runFilter =
            "(SELECT id FROM wall_extraction_runs WHERE floorplan_version_id = $version_id)";
        var fixedComponentFilter =
            $"(SELECT id FROM extracted_fixed_plan_components WHERE wall_extraction_run_id IN {runFilter})";
        var protectedDetailFilter =
            $"(SELECT id FROM extracted_protected_detail_assemblies WHERE wall_extraction_run_id IN {runFilter})";
        var extractedDimensionFilter =
            $"(SELECT id FROM extracted_dimensions WHERE wall_extraction_run_id IN {runFilter})";

        var statements = new[]
        {
            $"DELETE FROM pinch_markers WHERE floorplan_curation_id IN {curationFilter}",
            $"DELETE FROM pinch_groups WHERE floorplan_curation_id IN {curationFilter}",
            $"DELETE FROM floorplan_dimension_override_primitives WHERE floorplan_curation_id IN {curationFilter}",
            $"DELETE FROM floorplan_dimension_overrides WHERE floorplan_curation_id IN {curationFilter}",
            $"DELETE FROM floorplan_artifact_classifications WHERE floorplan_curation_id IN {curationFilter}",
            $"DELETE FROM floorplan_artifact_positions WHERE floorplan_curation_id IN {curationFilter}",
            $"DELETE FROM floorplan_label_overrides WHERE floorplan_curation_id IN {curationFilter}",
            "DELETE FROM floorplan_curations WHERE floorplan_version_id = $version_id",

            $"DELETE FROM extracted_fixed_plan_component_paths WHERE fixed_plan_component_id IN {fixedComponentFilter}",
            $"DELETE FROM extracted_protected_detail_assembly_paths WHERE protected_detail_assembly_id IN {protectedDetailFilter}",
            $"DELETE FROM extracted_dimension_line_segments WHERE dimension_id IN {extractedDimensionFilter}",
            $"DELETE FROM extracted_dimension_primitives WHERE dimension_id IN {extractedDimensionFilter}",
            $"DELETE FROM extracted_fixed_plan_components WHERE wall_extraction_run_id IN {runFilter}",
            $"DELETE FROM extracted_protected_detail_assemblies WHERE wall_extraction_run_id IN {runFilter}",
            $"DELETE FROM extracted_opening_labels WHERE wall_extraction_run_id IN {runFilter}",
            $"DELETE FROM extracted_opening_candidates WHERE wall_extraction_run_id IN {runFilter}",
            $"DELETE FROM extracted_room_labels WHERE wall_extraction_run_id IN {runFilter}",
            $"DELETE FROM extracted_wall_candidates WHERE wall_extraction_run_id IN {runFilter}",
            $"DELETE FROM extracted_dimensions WHERE wall_extraction_run_id IN {runFilter}",
            "DELETE FROM wall_extraction_runs WHERE floorplan_version_id = $version_id",

            $"""
            DELETE FROM geometry_segments
            WHERE geometry_path_id IN (
                SELECT geometry_path_id FROM pinch_markers WHERE floorplan_curation_id IN {curationFilter}
                UNION
                SELECT geometry_path_id FROM extracted_wall_candidates
                    WHERE wall_extraction_run_id IN {runFilter} AND geometry_path_id IS NOT NULL
                UNION
                SELECT geometry_path_id FROM extracted_opening_candidates
                    WHERE wall_extraction_run_id IN {runFilter} AND geometry_path_id IS NOT NULL
                UNION
                SELECT p.geometry_path_id FROM extracted_fixed_plan_component_paths p
                    JOIN extracted_fixed_plan_components c ON c.id = p.fixed_plan_component_id
                    WHERE c.wall_extraction_run_id IN {runFilter}
                UNION
                SELECT p.geometry_path_id FROM extracted_protected_detail_assembly_paths p
                    JOIN extracted_protected_detail_assemblies a ON a.id = p.protected_detail_assembly_id
                    WHERE a.wall_extraction_run_id IN {runFilter}
            )
            """,

            $"""
            DELETE FROM geometry_paths
            WHERE id IN (
                SELECT geometry_path_id FROM pinch_markers WHERE floorplan_curation_id IN {curationFilter}
                UNION
                SELECT geometry_path_id FROM extracted_wall_candidates
                    WHERE wall_extraction_run_id IN {runFilter} AND geometry_path_id IS NOT NULL
                UNION
                SELECT geometry_path_id FROM extracted_opening_candidates
                    WHERE wall_extraction_run_id IN {runFilter} AND geometry_path_id IS NOT NULL
                UNION
                SELECT p.geometry_path_id FROM extracted_fixed_plan_component_paths p
                    JOIN extracted_fixed_plan_components c ON c.id = p.fixed_plan_component_id
                    WHERE c.wall_extraction_run_id IN {runFilter}
                UNION
                SELECT p.geometry_path_id FROM extracted_protected_detail_assembly_paths p
                    JOIN extracted_protected_detail_assemblies a ON a.id = p.protected_detail_assembly_id
                    WHERE a.wall_extraction_run_id IN {runFilter}
            )
            """,

            "DELETE FROM floorplan_versions WHERE id = $version_id"
        };

        foreach (var sql in statements)
        {
            ExecuteNonQuery(connection, transaction, sql, ("$version_id", versionId));
        }

        ExecuteNonQuery(
            connection,
            transaction,
            "DELETE FROM imported_documents WHERE id = $document_id",
            ("$document_id", documentId));

        if (measurementContextId is not null)
        {
            ExecuteNonQuery(
                connection,
                transaction,
                """
                DELETE FROM measurement_contexts
                WHERE id = $measurement_context_id
                  AND NOT EXISTS (
                      SELECT 1
                      FROM imported_documents
                      WHERE measurement_context_id = $measurement_context_id
                  )
                """,
                ("$measurement_context_id", measurementContextId));
        }
    }

    private static string? ReadScalarString(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        params (string Name, string Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }
        return command.ExecuteScalar() as string;
    }

    private static void ExecuteNonQuery(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        params (string Name, string Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }
        command.ExecuteNonQuery();
    }
}
