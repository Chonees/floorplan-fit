using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteDimensionIntervalBindingRepository : IDimensionIntervalBindingRepository
{
    private readonly SqliteSession session;

    public SqliteDimensionIntervalBindingRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<IReadOnlyList<DimensionIntervalBinding>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                floorplan_curation_id,
                dimension_id,
                corridor_id,
                start_node_id,
                end_node_id,
                binding_status,
                interval_start_coordinate,
                interval_end_coordinate,
                updated_at_utc
            FROM floorplan_dimension_interval_bindings
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY updated_at_utc DESC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

        using var reader = command.ExecuteReader();
        var items = new List<DimensionIntervalBinding>();
        while (reader.Read())
        {
            items.Add(Map(reader));
        }

        return Task.FromResult<IReadOnlyList<DimensionIntervalBinding>>(items);
    }

    public Task UpsertAsync(DimensionIntervalBinding binding, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var command = CreateCommand(
            """
            INSERT INTO floorplan_dimension_interval_bindings (
                floorplan_curation_id,
                dimension_id,
                corridor_id,
                start_node_id,
                end_node_id,
                binding_status,
                interval_start_coordinate,
                interval_end_coordinate,
                updated_at_utc
            )
            VALUES (
                $floorplan_curation_id,
                $dimension_id,
                $corridor_id,
                $start_node_id,
                $end_node_id,
                $binding_status,
                $interval_start_coordinate,
                $interval_end_coordinate,
                $updated_at_utc
            )
            ON CONFLICT(floorplan_curation_id, dimension_id) DO UPDATE SET
                corridor_id = excluded.corridor_id,
                start_node_id = excluded.start_node_id,
                end_node_id = excluded.end_node_id,
                binding_status = excluded.binding_status,
                interval_start_coordinate = excluded.interval_start_coordinate,
                interval_end_coordinate = excluded.interval_end_coordinate,
                updated_at_utc = excluded.updated_at_utc
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", binding.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$dimension_id", binding.DimensionId.ToString());
        command.Parameters.AddWithValue("$corridor_id", binding.CorridorId.ToString());
        command.Parameters.AddWithValue("$start_node_id", binding.StartNodeId.ToString());
        command.Parameters.AddWithValue("$end_node_id", binding.EndNodeId.ToString());
        command.Parameters.AddWithValue("$binding_status", binding.BindingStatus);
        command.Parameters.AddWithValue("$interval_start_coordinate", binding.IntervalStartCoordinate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$interval_end_coordinate", binding.IntervalEndCoordinate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updated_at_utc", binding.UpdatedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid floorPlanCurationId, Guid dimensionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var command = CreateCommand(
            """
            DELETE FROM floorplan_dimension_interval_bindings
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND dimension_id = $dimension_id
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$dimension_id", dimensionId.ToString());
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task DeleteByCorridorAsync(Guid floorPlanCurationId, Guid corridorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var command = CreateCommand(
            """
            DELETE FROM floorplan_dimension_interval_bindings
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND corridor_id = $corridor_id
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$corridor_id", corridorId.ToString());
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    private static DimensionIntervalBinding Map(SqliteDataReader reader)
    {
        return new DimensionIntervalBinding(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            Guid.Parse(reader.GetString(3)),
            Guid.Parse(reader.GetString(4)),
            reader.GetString(5),
            decimal.Parse(reader.GetString(6), System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(7), System.Globalization.CultureInfo.InvariantCulture),
            DateTime.Parse(reader.GetString(8), null, System.Globalization.DateTimeStyles.RoundtripKind));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
