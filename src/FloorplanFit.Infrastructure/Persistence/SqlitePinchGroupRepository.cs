using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqlitePinchGroupRepository : IPinchGroupRepository
{
    private readonly SqliteSession session;

    public SqlitePinchGroupRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(PinchGroup group, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO pinch_groups (
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                sort_order)
            VALUES (
                $id,
                $floorplan_curation_id,
                $name,
                $axis_tag,
                $sort_order)
            """);

        command.Parameters.AddWithValue("$id", group.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_curation_id", group.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$name", group.Name);
        command.Parameters.AddWithValue("$axis_tag", (int)group.AxisTag);
        command.Parameters.AddWithValue("$sort_order", group.SortOrder);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<PinchGroup?> GetByIdAsync(Guid pinchGroupId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                sort_order
            FROM pinch_groups
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", pinchGroupId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<PinchGroup?>(null);
        }

        return Task.FromResult<PinchGroup?>(MapGroup(reader));
    }

    public Task<IReadOnlyList<PinchGroup>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                name,
                axis_tag,
                sort_order
            FROM pinch_groups
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

        var items = new List<PinchGroup>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapGroup(reader));
        }

        return Task.FromResult<IReadOnlyList<PinchGroup>>(items);
    }

    public Task RemoveAsync(Guid pinchGroupId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            DELETE FROM pinch_groups
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", pinchGroupId.ToString());
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static PinchGroup MapGroup(SqliteDataReader reader)
    {
        return new PinchGroup(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            (PinchAxisTag)reader.GetInt32(3),
            reader.GetInt32(4));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
