using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanCurationRepository : IFloorPlanCurationRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanCurationRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<FloorPlanCuration?> GetDraftAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetSingleAsync(floorPlanVersionId, FloorPlanCurationStatus.Draft, cancellationToken);
    }

    public Task<FloorPlanCuration?> GetPublishedAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetSingleAsync(floorPlanVersionId, FloorPlanCurationStatus.Published, cancellationToken);
    }

    public Task<int> GetNextCurationVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT COALESCE(MAX(curation_version), 0) + 1
            FROM floorplan_curations
            WHERE floorplan_version_id = $floorplan_version_id
            """);
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

        return Task.FromResult(Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture));
    }

    public Task AddAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_curations (
                id,
                floorplan_version_id,
                curation_version,
                status,
                based_on_curation_id,
                notes,
                created_at_utc,
                published_at_utc)
            VALUES (
                $id,
                $floorplan_version_id,
                $curation_version,
                $status,
                $based_on_curation_id,
                $notes,
                $created_at_utc,
                $published_at_utc)
            """);

        command.Parameters.AddWithValue("$id", curation.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_version_id", curation.FloorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$curation_version", curation.CurationVersion);
        command.Parameters.AddWithValue("$status", (int)curation.Status);
        command.Parameters.AddWithValue("$based_on_curation_id", (object?)curation.BasedOnCurationId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)curation.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$created_at_utc", curation.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$published_at_utc", (object?)curation.PublishedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(FloorPlanCuration curation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            UPDATE floorplan_curations
            SET status = $status,
                notes = $notes,
                published_at_utc = $published_at_utc
            WHERE id = $id
            """);

        command.Parameters.AddWithValue("$id", curation.Id.ToString());
        command.Parameters.AddWithValue("$status", (int)curation.Status);
        command.Parameters.AddWithValue("$notes", (object?)curation.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$published_at_utc", (object?)curation.PublishedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private Task<FloorPlanCuration?> GetSingleAsync(Guid floorPlanVersionId, FloorPlanCurationStatus status, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_version_id,
                curation_version,
                status,
                based_on_curation_id,
                notes,
                created_at_utc,
                published_at_utc
            FROM floorplan_curations
            WHERE floorplan_version_id = $floorplan_version_id
              AND status = $status
            ORDER BY curation_version DESC
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$status", (int)status);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<FloorPlanCuration?>(null);
        }

        return Task.FromResult<FloorPlanCuration?>(MapCuration(reader));
    }

    private static FloorPlanCuration MapCuration(SqliteDataReader reader)
    {
        return new FloorPlanCuration(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetInt32(2),
            (FloorPlanCurationStatus)reader.GetInt32(3),
            reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4)),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
