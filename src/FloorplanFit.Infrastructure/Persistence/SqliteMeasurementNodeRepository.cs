using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteMeasurementNodeRepository : IMeasurementNodeRepository
{
    private readonly SqliteSession session;

    public SqliteMeasurementNodeRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(MeasurementNode node, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO measurement_nodes (
                id,
                floorplan_curation_id,
                corridor_id,
                sort_order,
                reference_kind,
                source_artifact_kind,
                source_artifact_id,
                geometry_path_id,
                snap_kind,
                anchor_x,
                anchor_y,
                axis_coordinate,
                offset_along_axis,
                offset_normal,
                position_ratio
            )
            VALUES (
                $id,
                $floorplan_curation_id,
                $corridor_id,
                $sort_order,
                $reference_kind,
                $source_artifact_kind,
                $source_artifact_id,
                $geometry_path_id,
                $snap_kind,
                $anchor_x,
                $anchor_y,
                $axis_coordinate,
                $offset_along_axis,
                $offset_normal,
                $position_ratio
            )
            """);
        command.Parameters.AddWithValue("$id", node.Id.ToString());
        command.Parameters.AddWithValue("$floorplan_curation_id", node.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$corridor_id", node.CorridorId.ToString());
        command.Parameters.AddWithValue("$sort_order", node.SortOrder);
        command.Parameters.AddWithValue("$reference_kind", node.ReferenceKind);
        command.Parameters.AddWithValue("$source_artifact_kind", node.SourceArtifactKind);
        command.Parameters.AddWithValue("$source_artifact_id", node.SourceArtifactId.ToString());
        command.Parameters.AddWithValue("$geometry_path_id", node.GeometryPathId.ToString());
        command.Parameters.AddWithValue("$snap_kind", node.SnapKind);
        command.Parameters.AddWithValue("$anchor_x", node.AnchorX.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$anchor_y", node.AnchorY.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$axis_coordinate", node.AxisCoordinate.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$offset_along_axis", node.OffsetAlongAxis.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$offset_normal", node.OffsetNormal.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$position_ratio", node.PositionRatio.ToString(System.Globalization.CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task DeleteByCorridorAsync(Guid corridorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            DELETE FROM measurement_nodes
            WHERE corridor_id = $corridor_id
            """);
        command.Parameters.AddWithValue("$corridor_id", corridorId.ToString());
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<MeasurementNode?> GetByIdAsync(Guid nodeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                corridor_id,
                sort_order,
                reference_kind,
                source_artifact_kind,
                source_artifact_id,
                geometry_path_id,
                snap_kind,
                anchor_x,
                anchor_y,
                axis_coordinate,
                offset_along_axis,
                offset_normal,
                position_ratio
            FROM measurement_nodes
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", nodeId.ToString());

        using var reader = command.ExecuteReader();
        return Task.FromResult(reader.Read() ? Map(reader) : null);
    }

    public Task<IReadOnlyList<MeasurementNode>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                corridor_id,
                sort_order,
                reference_kind,
                source_artifact_kind,
                source_artifact_id,
                geometry_path_id,
                snap_kind,
                anchor_x,
                anchor_y,
                axis_coordinate,
                offset_along_axis,
                offset_normal,
                position_ratio
            FROM measurement_nodes
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY corridor_id ASC, sort_order ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());
        return Task.FromResult<IReadOnlyList<MeasurementNode>>(ReadMany(command));
    }

    public Task<IReadOnlyList<MeasurementNode>> ListByCorridorAsync(Guid corridorId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var command = CreateCommand(
            """
            SELECT
                id,
                floorplan_curation_id,
                corridor_id,
                sort_order,
                reference_kind,
                source_artifact_kind,
                source_artifact_id,
                geometry_path_id,
                snap_kind,
                anchor_x,
                anchor_y,
                axis_coordinate,
                offset_along_axis,
                offset_normal,
                position_ratio
            FROM measurement_nodes
            WHERE corridor_id = $corridor_id
            ORDER BY sort_order ASC
            """);
        command.Parameters.AddWithValue("$corridor_id", corridorId.ToString());
        return Task.FromResult<IReadOnlyList<MeasurementNode>>(ReadMany(command));
    }

    private static List<MeasurementNode> ReadMany(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var items = new List<MeasurementNode>();
        while (reader.Read())
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static MeasurementNode Map(SqliteDataReader reader)
    {
        return new MeasurementNode(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            reader.GetInt32(3),
            reader.GetString(4),
            reader.GetString(5),
            Guid.Parse(reader.GetString(6)),
            Guid.Parse(reader.GetString(7)),
            reader.GetString(8),
            decimal.Parse(reader.GetString(9), System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(10), System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(11), System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(12), System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(13), System.Globalization.CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(14), System.Globalization.CultureInfo.InvariantCulture));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
