using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteWallExtractionRunRepository : IWallExtractionRunRepository
{
    private readonly SqliteSession session;

    public SqliteWallExtractionRunRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(WallExtractionRun run, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO wall_extraction_runs (
                id,
                floorplan_version_id,
                status,
                started_at_utc,
                finished_at_utc,
                extractor_version,
                error_message)
            VALUES (
                $id,
                $floorplan_version_id,
                $status,
                $started_at_utc,
                $finished_at_utc,
                $extractor_version,
                $error_message)
            """);

        command.Parameters.AddWithValue("$id", run.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_version_id", run.FloorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$status", run.Status);
        command.Parameters.AddWithValue("$started_at_utc", run.StartedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$finished_at_utc", (object?)run.FinishedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$extractor_version", run.ExtractorVersion);
        command.Parameters.AddWithValue("$error_message", (object?)run.ErrorMessage ?? DBNull.Value);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<WallExtractionRun?> GetLatestByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_version_id,
                status,
                started_at_utc,
                finished_at_utc,
                extractor_version,
                error_message
            FROM wall_extraction_runs
            WHERE floorplan_version_id = $floorplan_version_id
            ORDER BY started_at_utc DESC, id DESC
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<WallExtractionRun?>(null);
        }

        return Task.FromResult<WallExtractionRun?>(new WallExtractionRun(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.IsDBNull(4)
                ? null
                : DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6)));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
