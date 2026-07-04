using FloorplanFit.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanCurationDataCloneService : IFloorPlanCurationDataCloneService
{
    private static readonly string[] SimpleCurationTables =
    [
        "floorplan_artifact_classifications",
        "floorplan_artifact_positions",
        "floorplan_label_overrides",
        "floorplan_dimension_overrides",
        "floorplan_dimension_override_primitives",
        "floorplan_dimension_binding_overrides",
        "floorplan_dimension_binding_override_anchors"
    ];

    private static readonly string[] FitCurationTables =
    [
        "pinch_groups",
        "pinch_markers",
        "measurement_corridors",
        "measurement_nodes",
        "floorplan_dimension_interval_bindings"
    ];

    private readonly SqliteSession session;

    public SqliteFloorPlanCurationDataCloneService(SqliteSession session)
    {
        this.session = session;
    }

    public Task EnsureClonedAsync(
        Guid sourceCurationId,
        Guid destinationCurationId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (sourceCurationId == destinationCurationId)
        {
            return Task.CompletedTask;
        }

        if (!DestinationHasAnyData(destinationCurationId, FitCurationTables))
        {
            var pinchGroupIdMap = ClonePinchGroups(sourceCurationId, destinationCurationId);
            ClonePinchMarkers(sourceCurationId, destinationCurationId, pinchGroupIdMap);

            var corridorIdMap = CloneMeasurementCorridors(sourceCurationId, destinationCurationId);
            var nodeIdMap = CloneMeasurementNodes(sourceCurationId, destinationCurationId, corridorIdMap);
            CloneDimensionIntervalBindings(sourceCurationId, destinationCurationId, corridorIdMap, nodeIdMap);
        }

        foreach (var tableName in SimpleCurationTables)
        {
            CloneSimpleCurationTable(tableName, sourceCurationId, destinationCurationId);
        }

        return Task.CompletedTask;
    }

    private bool DestinationHasAnyData(Guid destinationCurationId, IReadOnlyList<string> tableNames)
    {
        foreach (var tableName in tableNames)
        {
            using var command = CreateCommand(
                $"SELECT COUNT(*) FROM {tableName} WHERE floorplan_curation_id = $destination_curation_id");
            command.Parameters.AddWithValue("$destination_curation_id", destinationCurationId.ToString());

            if (Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private Dictionary<string, string> ClonePinchGroups(Guid sourceCurationId, Guid destinationCurationId)
    {
        var rows = QueryRows(
            """
            SELECT id, name, axis_tag, sort_order
            FROM pinch_groups
            WHERE floorplan_curation_id = $source_curation_id
            ORDER BY sort_order ASC, id ASC
            """,
            sourceCurationId);
        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var clonedId = Guid.NewGuid().ToString();
            idMap[row["id"]] = clonedId;

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
            command.Parameters.AddWithValue("$id", clonedId);
            command.Parameters.AddWithValue("$floorplan_curation_id", destinationCurationId.ToString());
            command.Parameters.AddWithValue("$name", row["name"]);
            command.Parameters.AddWithValue("$axis_tag", row["axis_tag"]);
            command.Parameters.AddWithValue("$sort_order", row["sort_order"]);
            command.ExecuteNonQuery();
        }

        return idMap;
    }

    private void ClonePinchMarkers(
        Guid sourceCurationId,
        Guid destinationCurationId,
        IReadOnlyDictionary<string, string> pinchGroupIdMap)
    {
        var rows = QueryRows(
            """
            SELECT
                pinch_group_id,
                source_candidate_id,
                geometry_path_id,
                position_ratio,
                max_trim_mm,
                sort_order
            FROM pinch_markers
            WHERE floorplan_curation_id = $source_curation_id
            ORDER BY sort_order ASC, id ASC
            """,
            sourceCurationId);

        foreach (var row in rows)
        {
            if (!pinchGroupIdMap.TryGetValue(row["pinch_group_id"], out var clonedGroupId))
            {
                continue;
            }

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
            command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$floorplan_curation_id", destinationCurationId.ToString());
            command.Parameters.AddWithValue("$pinch_group_id", clonedGroupId);
            command.Parameters.AddWithValue("$source_candidate_id", row["source_candidate_id"]);
            command.Parameters.AddWithValue("$geometry_path_id", row["geometry_path_id"]);
            command.Parameters.AddWithValue("$position_ratio", row["position_ratio"]);
            command.Parameters.AddWithValue("$max_trim_mm", row["max_trim_mm"]);
            command.Parameters.AddWithValue("$sort_order", row["sort_order"]);
            command.ExecuteNonQuery();
        }
    }

    private Dictionary<string, string> CloneMeasurementCorridors(Guid sourceCurationId, Guid destinationCurationId)
    {
        var rows = QueryRows(
            """
            SELECT
                id,
                name,
                axis_tag,
                guide_geometry_path_id,
                band_min_coordinate,
                band_max_coordinate,
                status,
                sort_order
            FROM measurement_corridors
            WHERE floorplan_curation_id = $source_curation_id
            ORDER BY sort_order ASC, id ASC
            """,
            sourceCurationId);
        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var clonedId = Guid.NewGuid().ToString();
            idMap[row["id"]] = clonedId;

            using var command = CreateCommand(
                """
                INSERT INTO measurement_corridors (
                    id,
                    floorplan_curation_id,
                    name,
                    axis_tag,
                    guide_geometry_path_id,
                    band_min_coordinate,
                    band_max_coordinate,
                    status,
                    sort_order)
                VALUES (
                    $id,
                    $floorplan_curation_id,
                    $name,
                    $axis_tag,
                    $guide_geometry_path_id,
                    $band_min_coordinate,
                    $band_max_coordinate,
                    $status,
                    $sort_order)
                """);
            command.Parameters.AddWithValue("$id", clonedId);
            command.Parameters.AddWithValue("$floorplan_curation_id", destinationCurationId.ToString());
            command.Parameters.AddWithValue("$name", row["name"]);
            command.Parameters.AddWithValue("$axis_tag", row["axis_tag"]);
            command.Parameters.AddWithValue("$guide_geometry_path_id", row["guide_geometry_path_id"]);
            command.Parameters.AddWithValue("$band_min_coordinate", row["band_min_coordinate"]);
            command.Parameters.AddWithValue("$band_max_coordinate", row["band_max_coordinate"]);
            command.Parameters.AddWithValue("$status", row["status"]);
            command.Parameters.AddWithValue("$sort_order", row["sort_order"]);
            command.ExecuteNonQuery();
        }

        return idMap;
    }

    private Dictionary<string, string> CloneMeasurementNodes(
        Guid sourceCurationId,
        Guid destinationCurationId,
        IReadOnlyDictionary<string, string> corridorIdMap)
    {
        var rows = QueryRows(
            """
            SELECT
                id,
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
            WHERE floorplan_curation_id = $source_curation_id
            ORDER BY corridor_id ASC, sort_order ASC, id ASC
            """,
            sourceCurationId);
        var idMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            if (!corridorIdMap.TryGetValue(row["corridor_id"], out var clonedCorridorId))
            {
                continue;
            }

            var clonedId = Guid.NewGuid().ToString();
            idMap[row["id"]] = clonedId;

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
                    position_ratio)
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
                    $position_ratio)
                """);
            command.Parameters.AddWithValue("$id", clonedId);
            command.Parameters.AddWithValue("$floorplan_curation_id", destinationCurationId.ToString());
            command.Parameters.AddWithValue("$corridor_id", clonedCorridorId);
            command.Parameters.AddWithValue("$sort_order", row["sort_order"]);
            command.Parameters.AddWithValue("$reference_kind", row["reference_kind"]);
            command.Parameters.AddWithValue("$source_artifact_kind", row["source_artifact_kind"]);
            command.Parameters.AddWithValue("$source_artifact_id", row["source_artifact_id"]);
            command.Parameters.AddWithValue("$geometry_path_id", row["geometry_path_id"]);
            command.Parameters.AddWithValue("$snap_kind", row["snap_kind"]);
            command.Parameters.AddWithValue("$anchor_x", row["anchor_x"]);
            command.Parameters.AddWithValue("$anchor_y", row["anchor_y"]);
            command.Parameters.AddWithValue("$axis_coordinate", row["axis_coordinate"]);
            command.Parameters.AddWithValue("$offset_along_axis", row["offset_along_axis"]);
            command.Parameters.AddWithValue("$offset_normal", row["offset_normal"]);
            command.Parameters.AddWithValue("$position_ratio", row["position_ratio"]);
            command.ExecuteNonQuery();
        }

        return idMap;
    }

    private void CloneDimensionIntervalBindings(
        Guid sourceCurationId,
        Guid destinationCurationId,
        IReadOnlyDictionary<string, string> corridorIdMap,
        IReadOnlyDictionary<string, string> nodeIdMap)
    {
        var rows = QueryRows(
            """
            SELECT
                dimension_id,
                corridor_id,
                start_node_id,
                end_node_id,
                binding_status,
                interval_start_coordinate,
                interval_end_coordinate,
                updated_at_utc
            FROM floorplan_dimension_interval_bindings
            WHERE floorplan_curation_id = $source_curation_id
            ORDER BY updated_at_utc ASC, dimension_id ASC
            """,
            sourceCurationId);

        foreach (var row in rows)
        {
            if (!corridorIdMap.TryGetValue(row["corridor_id"], out var clonedCorridorId) ||
                !nodeIdMap.TryGetValue(row["start_node_id"], out var clonedStartNodeId) ||
                !nodeIdMap.TryGetValue(row["end_node_id"], out var clonedEndNodeId))
            {
                continue;
            }

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
                    updated_at_utc)
                VALUES (
                    $floorplan_curation_id,
                    $dimension_id,
                    $corridor_id,
                    $start_node_id,
                    $end_node_id,
                    $binding_status,
                    $interval_start_coordinate,
                    $interval_end_coordinate,
                    $updated_at_utc)
                """);
            command.Parameters.AddWithValue("$floorplan_curation_id", destinationCurationId.ToString());
            command.Parameters.AddWithValue("$dimension_id", row["dimension_id"]);
            command.Parameters.AddWithValue("$corridor_id", clonedCorridorId);
            command.Parameters.AddWithValue("$start_node_id", clonedStartNodeId);
            command.Parameters.AddWithValue("$end_node_id", clonedEndNodeId);
            command.Parameters.AddWithValue("$binding_status", row["binding_status"]);
            command.Parameters.AddWithValue("$interval_start_coordinate", row["interval_start_coordinate"]);
            command.Parameters.AddWithValue("$interval_end_coordinate", row["interval_end_coordinate"]);
            command.Parameters.AddWithValue("$updated_at_utc", row["updated_at_utc"]);
            command.ExecuteNonQuery();
        }
    }

    private void CloneSimpleCurationTable(string tableName, Guid sourceCurationId, Guid destinationCurationId)
    {
        var columns = GetColumnNames(tableName);
        if (columns.Count == 0)
        {
            return;
        }

        var insertColumns = string.Join(", ", columns);
        var selectColumns = string.Join(
            ", ",
            columns.Select(column => string.Equals(column, "floorplan_curation_id", StringComparison.OrdinalIgnoreCase)
                ? "$destination_curation_id"
                : column));

        using var command = CreateCommand(
            $"""
            INSERT OR IGNORE INTO {tableName} ({insertColumns})
            SELECT {selectColumns}
            FROM {tableName}
            WHERE floorplan_curation_id = $source_curation_id
            """);
        command.Parameters.AddWithValue("$destination_curation_id", destinationCurationId.ToString());
        command.Parameters.AddWithValue("$source_curation_id", sourceCurationId.ToString());
        command.ExecuteNonQuery();
    }

    private IReadOnlyList<string> GetColumnNames(string tableName)
    {
        using var command = CreateCommand($"PRAGMA table_info({tableName})");
        using var reader = command.ExecuteReader();
        var columns = new List<string>();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }

        return columns;
    }

    private IReadOnlyList<Dictionary<string, string>> QueryRows(string sql, Guid sourceCurationId)
    {
        using var command = CreateCommand(sql);
        command.Parameters.AddWithValue("$source_curation_id", sourceCurationId.ToString());

        using var reader = command.ExecuteReader();
        var rows = new List<Dictionary<string, string>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < reader.FieldCount; index++)
            {
                row[reader.GetName(index)] = reader.GetString(index);
            }

            rows.Add(row);
        }

        return rows;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
