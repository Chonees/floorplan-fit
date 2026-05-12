using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanVersionRepository : IFloorPlanVersionRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanVersionRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<FloorPlanVersion?> GetByIdAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_template_id,
                imported_document_id,
                geometry_fingerprint,
                version_number,
                created_at_utc
            FROM floorplan_versions
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", floorPlanVersionId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<FloorPlanVersion?>(null);
        }

        return Task.FromResult<FloorPlanVersion?>(new FloorPlanVersion(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            reader.GetString(3),
            reader.GetInt32(4),
            DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
    }

    public Task<int> GetNextVersionNumberAsync(Guid floorPlanTemplateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT COALESCE(MAX(version_number), 0) + 1
            FROM floorplan_versions
            WHERE floorplan_template_id = $floorplan_template_id
            """);
        command.Parameters.AddWithValue("$floorplan_template_id", floorPlanTemplateId.ToString());

        var result = command.ExecuteScalar();
        return Task.FromResult(Convert.ToInt32(result, CultureInfo.InvariantCulture));
    }

    public Task AddAsync(FloorPlanVersion version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_versions (
                id,
                floorplan_template_id,
                imported_document_id,
                geometry_fingerprint,
                version_number,
                created_at_utc)
            VALUES (
                $id,
                $floorplan_template_id,
                $imported_document_id,
                $geometry_fingerprint,
                $version_number,
                $created_at_utc)
            """);

        command.Parameters.AddWithValue("$id", version.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_template_id", version.FloorPlanTemplateId.ToString());
        command.Parameters.AddWithValue("$imported_document_id", version.ImportedDocumentId.ToString());
        command.Parameters.AddWithValue("$geometry_fingerprint", version.GeometryFingerprint);
        command.Parameters.AddWithValue("$version_number", version.VersionNumber);
        command.Parameters.AddWithValue("$created_at_utc", version.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var deletion = GetDeletionScope(floorPlanVersionId);
        if (deletion is null)
        {
            return Task.CompletedTask;
        }

        var curationIds = GetIds(
            """
            SELECT id
            FROM floorplan_curations
            WHERE floorplan_version_id = $floorplan_version_id
            """,
            ("$floorplan_version_id", floorPlanVersionId.ToString()));
        var extractionRunIds = GetIds(
            """
            SELECT id
            FROM wall_extraction_runs
            WHERE floorplan_version_id = $floorplan_version_id
            """,
            ("$floorplan_version_id", floorPlanVersionId.ToString()));
        var geometryPathIds = GetGeometryPathIds(curationIds, extractionRunIds);

        foreach (var curationId in curationIds)
        {
            ExecuteNonQuery(
                "DELETE FROM pinch_markers WHERE floorplan_curation_id = $curation_id",
                ("$curation_id", curationId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM pinch_groups WHERE floorplan_curation_id = $curation_id",
                ("$curation_id", curationId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM floorplan_dimension_override_primitives WHERE floorplan_curation_id = $curation_id",
                ("$curation_id", curationId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM floorplan_dimension_overrides WHERE floorplan_curation_id = $curation_id",
                ("$curation_id", curationId.ToString()));
        }

        DeleteByIds("floorplan_curations", "id", curationIds);

        foreach (var extractionRunId in extractionRunIds)
        {
            ExecuteNonQuery(
                """
                DELETE FROM extracted_fixed_plan_component_paths
                WHERE fixed_plan_component_id IN (
                    SELECT id
                    FROM extracted_fixed_plan_components
                    WHERE wall_extraction_run_id = $wall_extraction_run_id
                )
                """,
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                """
                DELETE FROM extracted_protected_detail_assembly_paths
                WHERE protected_detail_assembly_id IN (
                    SELECT id
                    FROM extracted_protected_detail_assemblies
                    WHERE wall_extraction_run_id = $wall_extraction_run_id
                )
                """,
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                """
                DELETE FROM extracted_dimension_line_segments
                WHERE dimension_id IN (
                    SELECT id
                    FROM extracted_dimensions
                    WHERE wall_extraction_run_id = $wall_extraction_run_id
                )
                """,
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                """
                DELETE FROM extracted_dimension_primitives
                WHERE dimension_id IN (
                    SELECT id
                    FROM extracted_dimensions
                    WHERE wall_extraction_run_id = $wall_extraction_run_id
                )
                """,
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM extracted_fixed_plan_components WHERE wall_extraction_run_id = $wall_extraction_run_id",
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM extracted_protected_detail_assemblies WHERE wall_extraction_run_id = $wall_extraction_run_id",
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM extracted_opening_labels WHERE wall_extraction_run_id = $wall_extraction_run_id",
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM extracted_opening_candidates WHERE wall_extraction_run_id = $wall_extraction_run_id",
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM extracted_room_labels WHERE wall_extraction_run_id = $wall_extraction_run_id",
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM extracted_wall_candidates WHERE wall_extraction_run_id = $wall_extraction_run_id",
                ("$wall_extraction_run_id", extractionRunId.ToString()));
            ExecuteNonQuery(
                "DELETE FROM extracted_dimensions WHERE wall_extraction_run_id = $wall_extraction_run_id",
                ("$wall_extraction_run_id", extractionRunId.ToString()));
        }

        DeleteByIds("wall_extraction_runs", "id", extractionRunIds);

        foreach (var geometryPathId in geometryPathIds)
        {
            ExecuteNonQuery(
                "DELETE FROM geometry_segments WHERE geometry_path_id = $geometry_path_id",
                ("$geometry_path_id", geometryPathId.ToString()));
        }

        DeleteByIds("geometry_paths", "id", geometryPathIds);
        ExecuteNonQuery(
            "DELETE FROM floorplan_versions WHERE id = $id",
            ("$id", floorPlanVersionId.ToString()));
        UpdateTemplateAfterVersionDeletion(deletion.TemplateId, floorPlanVersionId);
        ExecuteNonQuery(
            "DELETE FROM imported_documents WHERE id = $id",
            ("$id", deletion.ImportedDocumentId.ToString()));
        ExecuteNonQuery(
            """
            DELETE FROM measurement_contexts
            WHERE id = $id
              AND NOT EXISTS (
                  SELECT 1
                  FROM imported_documents
                  WHERE measurement_context_id = $id
              )
            """,
            ("$id", deletion.MeasurementContextId.ToString()));

        return Task.CompletedTask;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }

    private VersionDeletionScope? GetDeletionScope(Guid floorPlanVersionId)
    {
        using var command = CreateCommand(
            """
            SELECT
                v.floorplan_template_id,
                v.imported_document_id,
                d.measurement_context_id
            FROM floorplan_versions v
            JOIN imported_documents d ON d.id = v.imported_document_id
            WHERE v.id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", floorPlanVersionId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new VersionDeletionScope(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)));
    }

    private IReadOnlyList<Guid> GetGeometryPathIds(
        IReadOnlyList<Guid> curationIds,
        IReadOnlyList<Guid> extractionRunIds)
    {
        var ids = new HashSet<Guid>();

        foreach (var curationId in curationIds)
        {
            foreach (var id in GetIds(
                """
                SELECT geometry_path_id
                FROM pinch_markers
                WHERE floorplan_curation_id = $curation_id
                """,
                ("$curation_id", curationId.ToString())))
            {
                ids.Add(id);
            }
        }

        foreach (var extractionRunId in extractionRunIds)
        {
            foreach (var id in GetIds(
                """
                SELECT geometry_path_id
                FROM extracted_wall_candidates
                WHERE wall_extraction_run_id = $wall_extraction_run_id
                  AND geometry_path_id IS NOT NULL
                UNION
                SELECT geometry_path_id
                FROM extracted_opening_candidates
                WHERE wall_extraction_run_id = $wall_extraction_run_id
                  AND geometry_path_id IS NOT NULL
                UNION
                SELECT p.geometry_path_id
                FROM extracted_fixed_plan_component_paths p
                JOIN extracted_fixed_plan_components c ON c.id = p.fixed_plan_component_id
                WHERE c.wall_extraction_run_id = $wall_extraction_run_id
                UNION
                SELECT p.geometry_path_id
                FROM extracted_protected_detail_assembly_paths p
                JOIN extracted_protected_detail_assemblies a ON a.id = p.protected_detail_assembly_id
                WHERE a.wall_extraction_run_id = $wall_extraction_run_id
                """,
                ("$wall_extraction_run_id", extractionRunId.ToString())))
            {
                ids.Add(id);
            }
        }

        return ids.ToArray();
    }

    private IReadOnlyList<Guid> GetIds(string sql, params (string Name, string Value)[] parameters)
    {
        using var command = CreateCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var ids = new List<Guid>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            ids.Add(Guid.Parse(reader.GetString(0)));
        }

        return ids;
    }

    private void DeleteByIds(string tableName, string columnName, IReadOnlyList<Guid> ids)
    {
        foreach (var id in ids)
        {
            ExecuteNonQuery(
                $"DELETE FROM {tableName} WHERE {columnName} = $id",
                ("$id", id.ToString()));
        }
    }

    private void UpdateTemplateAfterVersionDeletion(Guid templateId, Guid deletedVersionId)
    {
        ExecuteNonQuery(
            $"""
            UPDATE floorplan_templates
            SET current_version_id = CASE
                    WHEN current_version_id = $deleted_version_id THEN (
                        SELECT id
                        FROM floorplan_versions
                        WHERE floorplan_template_id = $template_id
                        ORDER BY version_number DESC, created_at_utc DESC, id DESC
                        LIMIT 1
                    )
                    ELSE current_version_id
                END,
                active_published_curation_id = CASE
                    WHEN active_published_curation_id IS NOT NULL
                     AND NOT EXISTS (
                         SELECT 1
                         FROM floorplan_curations
                         WHERE id = active_published_curation_id
                     ) THEN (
                        SELECT c.id
                        FROM floorplan_curations c
                        JOIN floorplan_versions v ON v.id = c.floorplan_version_id
                        WHERE v.floorplan_template_id = $template_id
                          AND c.status = {(int)FloorPlanCurationStatus.Published}
                        ORDER BY c.published_at_utc DESC, c.curation_version DESC, c.id DESC
                        LIMIT 1
                     )
                    ELSE active_published_curation_id
                END
            WHERE id = $template_id
            """,
            ("$template_id", templateId.ToString()),
            ("$deleted_version_id", deletedVersionId.ToString()));
    }

    private void ExecuteNonQuery(string sql, params (string Name, string Value)[] parameters)
    {
        using var command = CreateCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        command.ExecuteNonQuery();
    }

    private sealed record VersionDeletionScope(
        Guid TemplateId,
        Guid ImportedDocumentId,
        Guid MeasurementContextId);
}
