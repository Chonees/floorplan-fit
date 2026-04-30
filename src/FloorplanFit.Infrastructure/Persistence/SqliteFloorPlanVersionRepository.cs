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

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
