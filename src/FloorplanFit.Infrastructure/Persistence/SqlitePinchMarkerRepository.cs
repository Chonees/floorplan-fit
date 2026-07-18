using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqlitePinchMarkerRepository : IPinchMarkerRepository
{
    private readonly SqliteSession session;

    public SqlitePinchMarkerRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(PinchMarker marker, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO pinch_markers (
                id,
                floorplan_curation_id,
                pinch_group_id,
                source_candidate_id,
                geometry_path_id,
                position_ratio,
                max_trim_mm,
                sort_order)
            VALUES (
                $id,
                $floorplan_curation_id,
                $pinch_group_id,
                $source_candidate_id,
                $geometry_path_id,
                $position_ratio,
                $max_trim_mm,
                $sort_order)
            """);

        command.Parameters.AddWithValue("$id", marker.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_curation_id", marker.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$pinch_group_id", marker.PinchGroupId.ToString());
        command.Parameters.AddWithValue("$source_candidate_id", marker.SourceCandidateId.ToString());
        command.Parameters.AddWithValue("$geometry_path_id", marker.GeometryPathId.ToString());
        command.Parameters.AddWithValue("$position_ratio", marker.PositionRatio.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$max_trim_mm", marker.MaxTrimMm.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$sort_order", marker.SortOrder);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(PinchMarker marker, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            UPDATE pinch_markers
            SET max_trim_mm = $max_trim_mm
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", marker.Id.ToString());
        command.Parameters.AddWithValue("$max_trim_mm", marker.MaxTrimMm.ToString(CultureInfo.InvariantCulture));
        var affectedRows = command.ExecuteNonQuery();
        if (affectedRows != 1)
        {
            throw new InvalidOperationException(
                $"Expected to update one pinch marker, but updated {affectedRows}.");
        }

        return Task.CompletedTask;
    }

    public Task<PinchMarker?> GetByIdAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                pinch_group_id,
                source_candidate_id,
                geometry_path_id,
                position_ratio,
                max_trim_mm,
                sort_order
            FROM pinch_markers
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", pinchMarkerId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<PinchMarker?>(null);
        }

        return Task.FromResult<PinchMarker?>(MapMarker(reader));
    }

    public Task<IReadOnlyList<PinchMarker>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                pinch_group_id,
                source_candidate_id,
                geometry_path_id,
                position_ratio,
                max_trim_mm,
                sort_order
            FROM pinch_markers
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

        var items = new List<PinchMarker>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapMarker(reader));
        }

        return Task.FromResult<IReadOnlyList<PinchMarker>>(items);
    }

    public Task RemoveAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            DELETE FROM pinch_markers
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", pinchMarkerId.ToString());
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task RemoveByGroupAsync(Guid curationId, Guid pinchGroupId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            DELETE FROM pinch_markers
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND pinch_group_id = $pinch_group_id
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());
        command.Parameters.AddWithValue("$pinch_group_id", pinchGroupId.ToString());
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            DELETE FROM pinch_markers
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_candidate_id = $source_candidate_id
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());
        command.Parameters.AddWithValue("$source_candidate_id", sourceCandidateId.ToString());
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static PinchMarker MapMarker(SqliteDataReader reader)
    {
        return new PinchMarker(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            Guid.Parse(reader.GetString(3)),
            Guid.Parse(reader.GetString(4)),
            decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
            reader.GetInt32(7));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
