using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanDimensionBindingOverrideRepository : IFloorPlanDimensionBindingOverrideRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanDimensionBindingOverrideRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<IReadOnlyList<FloorPlanDimensionBindingOverride>> ListByCurationAsync(Guid floorPlanCurationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                floorplan_curation_id,
                source_dimension_key,
                binding_kind,
                is_resolved,
                confidence,
                notes,
                axis_tag,
                start_coordinate,
                end_coordinate,
                orientation_degrees,
                updated_at_utc
            FROM floorplan_dimension_binding_overrides
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY updated_at_utc ASC, source_dimension_key ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<FloorPlanDimensionBindingOverride>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var sourceDimensionKey = reader.GetString(1);
            items.Add(FloorPlanDimensionBindingOverride.CreateManualOverride(
                Guid.Parse(reader.GetString(0)),
                sourceDimensionKey,
                reader.GetString(2),
                reader.GetInt32(3) == 1,
                decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                reader.IsDBNull(6)
                    ? null
                    : new FloorPlanDimensionMeasuredSpanOverride(
                        reader.GetString(6),
                        decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                        decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                        decimal.Parse(reader.GetString(9), CultureInfo.InvariantCulture)),
                GetAnchors(floorPlanCurationId, sourceDimensionKey),
                DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        }

        return Task.FromResult<IReadOnlyList<FloorPlanDimensionBindingOverride>>(items);
    }

    public Task UpsertAsync(FloorPlanDimensionBindingOverride bindingOverride, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_dimension_binding_overrides (
                floorplan_curation_id,
                source_dimension_key,
                binding_kind,
                is_resolved,
                confidence,
                notes,
                axis_tag,
                start_coordinate,
                end_coordinate,
                orientation_degrees,
                updated_at_utc
            )
            VALUES (
                $floorplan_curation_id,
                $source_dimension_key,
                $binding_kind,
                $is_resolved,
                $confidence,
                $notes,
                $axis_tag,
                $start_coordinate,
                $end_coordinate,
                $orientation_degrees,
                $updated_at_utc
            )
            ON CONFLICT(floorplan_curation_id, source_dimension_key)
            DO UPDATE SET
                binding_kind = excluded.binding_kind,
                is_resolved = excluded.is_resolved,
                confidence = excluded.confidence,
                notes = excluded.notes,
                axis_tag = excluded.axis_tag,
                start_coordinate = excluded.start_coordinate,
                end_coordinate = excluded.end_coordinate,
                orientation_degrees = excluded.orientation_degrees,
                updated_at_utc = excluded.updated_at_utc
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", bindingOverride.FloorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", bindingOverride.SourceDimensionKey);
        command.Parameters.AddWithValue("$binding_kind", bindingOverride.BindingKind);
        command.Parameters.AddWithValue("$is_resolved", bindingOverride.IsResolved ? 1 : 0);
        command.Parameters.AddWithValue("$confidence", bindingOverride.Confidence.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$notes", bindingOverride.Notes);
        command.Parameters.AddWithValue("$axis_tag", bindingOverride.MeasuredSpan?.AxisTag ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$start_coordinate", ConvertNullableDecimal(bindingOverride.MeasuredSpan?.StartCoordinate));
        command.Parameters.AddWithValue("$end_coordinate", ConvertNullableDecimal(bindingOverride.MeasuredSpan?.EndCoordinate));
        command.Parameters.AddWithValue("$orientation_degrees", ConvertNullableDecimal(bindingOverride.MeasuredSpan?.OrientationDegrees));
        command.Parameters.AddWithValue("$updated_at_utc", bindingOverride.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        DeleteAnchorRows(bindingOverride.FloorPlanCurationId, bindingOverride.SourceDimensionKey);
        foreach (var anchor in bindingOverride.Anchors)
        {
            using var anchorCommand = CreateCommand(
                """
                INSERT INTO floorplan_dimension_binding_override_anchors (
                    floorplan_curation_id,
                    source_dimension_key,
                    sort_order,
                    edge_key,
                    source_artifact_kind,
                    source_artifact_id,
                    geometry_path_id,
                    edge_anchor_kind,
                    anchor_x,
                    anchor_y,
                    distance_source_units,
                    segment_ratio
                )
                VALUES (
                    $floorplan_curation_id,
                    $source_dimension_key,
                    $sort_order,
                    $edge_key,
                    $source_artifact_kind,
                    $source_artifact_id,
                    $geometry_path_id,
                    $edge_anchor_kind,
                    $anchor_x,
                    $anchor_y,
                    $distance_source_units,
                    $segment_ratio
                )
                """);
            anchorCommand.Parameters.AddWithValue("$floorplan_curation_id", bindingOverride.FloorPlanCurationId.ToString());
            anchorCommand.Parameters.AddWithValue("$source_dimension_key", bindingOverride.SourceDimensionKey);
            anchorCommand.Parameters.AddWithValue("$sort_order", anchor.SortOrder);
            anchorCommand.Parameters.AddWithValue("$edge_key", anchor.EdgeKey);
            anchorCommand.Parameters.AddWithValue("$source_artifact_kind", anchor.SourceArtifactKind);
            anchorCommand.Parameters.AddWithValue("$source_artifact_id", anchor.SourceArtifactId.ToString());
            anchorCommand.Parameters.AddWithValue("$geometry_path_id", anchor.GeometryPathId.ToString());
            anchorCommand.Parameters.AddWithValue("$edge_anchor_kind", anchor.EdgeAnchorKind);
            anchorCommand.Parameters.AddWithValue("$anchor_x", anchor.AnchorX.ToString(CultureInfo.InvariantCulture));
            anchorCommand.Parameters.AddWithValue("$anchor_y", anchor.AnchorY.ToString(CultureInfo.InvariantCulture));
            anchorCommand.Parameters.AddWithValue("$distance_source_units", anchor.DistanceSourceUnits.ToString(CultureInfo.InvariantCulture));
            anchorCommand.Parameters.AddWithValue("$segment_ratio", ConvertNullableDecimal(anchor.SegmentRatio));
            anchorCommand.ExecuteNonQuery();
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid floorPlanCurationId, string sourceDimensionKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeleteAnchorRows(floorPlanCurationId, sourceDimensionKey);

        using var command = CreateCommand(
            """
            DELETE FROM floorplan_dimension_binding_overrides
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_dimension_key = $source_dimension_key
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", sourceDimensionKey);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    private IReadOnlyList<FloorPlanDimensionBindingAnchorOverride> GetAnchors(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        using var command = CreateCommand(
            """
            SELECT
                sort_order,
                edge_key,
                source_artifact_kind,
                source_artifact_id,
                geometry_path_id,
                edge_anchor_kind,
                anchor_x,
                anchor_y,
                distance_source_units,
                segment_ratio
            FROM floorplan_dimension_binding_override_anchors
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_dimension_key = $source_dimension_key
            ORDER BY sort_order ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", sourceDimensionKey);

        var items = new List<FloorPlanDimensionBindingAnchorOverride>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new FloorPlanDimensionBindingAnchorOverride(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                Guid.Parse(reader.GetString(3)),
                Guid.Parse(reader.GetString(4)),
                reader.GetString(5),
                decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                reader.IsDBNull(9) ? null : decimal.Parse(reader.GetString(9), CultureInfo.InvariantCulture)));
        }

        return items;
    }

    private void DeleteAnchorRows(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        using var command = CreateCommand(
            """
            DELETE FROM floorplan_dimension_binding_override_anchors
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_dimension_key = $source_dimension_key
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", sourceDimensionKey);
        command.ExecuteNonQuery();
    }

    private static object ConvertNullableDecimal(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? (object)DBNull.Value;

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }
}
