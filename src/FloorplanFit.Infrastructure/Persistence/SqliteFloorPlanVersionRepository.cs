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
              AND deleted_at_utc IS NULL
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
              AND deleted_at_utc IS NULL
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

        var templateId = GetActiveTemplateId(floorPlanVersionId);
        if (templateId is null)
        {
            return Task.CompletedTask;
        }

        ExecuteNonQuery(
            """
            UPDATE floorplan_versions
            SET deleted_at_utc = $deleted_at_utc
            WHERE id = $id
              AND deleted_at_utc IS NULL
            """,
            ("$id", floorPlanVersionId.ToString()),
            ("$deleted_at_utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)));

        UpdateTemplateAfterVersionDeletion(templateId.Value, floorPlanVersionId);

        return Task.CompletedTask;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }

    private Guid? GetActiveTemplateId(Guid floorPlanVersionId)
    {
        using var command = CreateCommand(
            """
            SELECT floorplan_template_id
            FROM floorplan_versions
            WHERE id = $id
              AND deleted_at_utc IS NULL
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", floorPlanVersionId.ToString());

        using var reader = command.ExecuteReader();
        return reader.Read() ? Guid.Parse(reader.GetString(0)) : null;
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
                          AND deleted_at_utc IS NULL
                          AND id <> $deleted_version_id
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
                          AND v.deleted_at_utc IS NULL
                          AND v.id <> $deleted_version_id
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
}
