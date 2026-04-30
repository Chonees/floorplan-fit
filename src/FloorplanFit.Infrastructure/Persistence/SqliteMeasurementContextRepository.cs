using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.Measurement;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteMeasurementContextRepository : IMeasurementContextRepository
{
    private readonly SqliteSession session;

    public SqliteMeasurementContextRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(MeasurementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO measurement_contexts (
                id,
                source_unit,
                to_millimeters_factor,
                linear_tolerance_mm,
                angular_tolerance_deg,
                created_at_utc)
            VALUES (
                $id,
                $source_unit,
                $to_millimeters_factor,
                $linear_tolerance_mm,
                $angular_tolerance_deg,
                $created_at_utc)
            """);

        command.Parameters.AddWithValue("$id", context.Id.ToString());
        command.Parameters.AddWithValue("$source_unit", (int)context.SourceUnit);
        command.Parameters.AddWithValue("$to_millimeters_factor", context.ToMillimetersFactor.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$linear_tolerance_mm", context.LinearToleranceMm.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$angular_tolerance_deg", context.AngularToleranceDeg.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$created_at_utc", context.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
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
