using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteExtractedFixedPlanComponentRepository : IExtractedFixedPlanComponentRepository
{
    private readonly SqliteSession session;

    public SqliteExtractedFixedPlanComponentRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddRangeAsync(
        IReadOnlyList<ExtractedFixedPlanComponent> domainComponents,
        IReadOnlyList<DetectedFixedPlanComponent> detectedComponents,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (domainComponents.Count != detectedComponents.Count)
        {
            throw new InvalidOperationException("Domain and detected fixed plan component counts must match.");
        }

        for (var index = 0; index < domainComponents.Count; index++)
        {
            var domainComponent = domainComponents[index];
            var detectedComponent = detectedComponents[index];

            using (var command = CreateCommand(
                       """
                       INSERT INTO extracted_fixed_plan_components (
                           id,
                           wall_extraction_run_id,
                           source_entity_ref,
                           source_layer,
                           kind,
                           source_entity_kind,
                           source_block_name,
                           confidence,
                           detection_notes,
                           sort_order,
                           color_argb)
                       VALUES (
                           $id,
                           $wall_extraction_run_id,
                           $source_entity_ref,
                           $source_layer,
                           $kind,
                           $source_entity_kind,
                           $source_block_name,
                           $confidence,
                           $detection_notes,
                           $sort_order,
                           $color_argb)
                       """))
            {
                command.Parameters.AddWithValue("$id", domainComponent.Id.ToString());
                command.Parameters.AddWithValue("$wall_extraction_run_id", domainComponent.WallExtractionRunId.ToString());
                command.Parameters.AddWithValue("$source_entity_ref", domainComponent.SourceEntityRef);
                command.Parameters.AddWithValue("$source_layer", (object?)domainComponent.SourceLayer ?? DBNull.Value);
                command.Parameters.AddWithValue("$kind", domainComponent.Kind);
                command.Parameters.AddWithValue("$source_entity_kind", (object?)domainComponent.SourceEntityKind ?? DBNull.Value);
                command.Parameters.AddWithValue("$source_block_name", (object?)domainComponent.SourceBlockName ?? DBNull.Value);
                command.Parameters.AddWithValue("$confidence", domainComponent.Confidence.ToString(CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$detection_notes", (object?)domainComponent.DetectionNotes ?? DBNull.Value);
                command.Parameters.AddWithValue("$sort_order", domainComponent.SortOrder);
                command.Parameters.AddWithValue("$color_argb", (object?)domainComponent.ColorArgb ?? DBNull.Value);
                command.ExecuteNonQuery();
            }

            for (var pathIndex = 0; pathIndex < detectedComponent.GeometryPaths.Count; pathIndex++)
            {
                var geometryPathId = PersistGeometryPath(detectedComponent.GeometryPaths[pathIndex]);
                if (geometryPathId is null)
                {
                    continue;
                }

                using var pathCommand = CreateCommand(
                    """
                    INSERT INTO extracted_fixed_plan_component_paths (
                        fixed_plan_component_id,
                        geometry_path_id,
                        sort_order)
                    VALUES (
                        $fixed_plan_component_id,
                        $geometry_path_id,
                        $sort_order)
                    """);
                pathCommand.Parameters.AddWithValue("$fixed_plan_component_id", domainComponent.Id.ToString());
                pathCommand.Parameters.AddWithValue("$geometry_path_id", geometryPathId.Value.ToString());
                pathCommand.Parameters.AddWithValue("$sort_order", pathIndex + 1);
                pathCommand.ExecuteNonQuery();
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExtractedFixedPlanComponent>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                wall_extraction_run_id,
                source_entity_ref,
                source_layer,
                kind,
                source_entity_kind,
                source_block_name,
                confidence,
                detection_notes,
                sort_order,
                color_argb
            FROM extracted_fixed_plan_components
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", wallExtractionRunId.ToString());

        var items = new List<ExtractedFixedPlanComponent>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new ExtractedFixedPlanComponent(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10)));
        }

        return Task.FromResult<IReadOnlyList<ExtractedFixedPlanComponent>>(items);
    }

    public Task RemoveAsync(Guid fixedPlanComponentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var geometryPathIds = GetGeometryPathIds(fixedPlanComponentId);

        using (var pathCommand = CreateCommand(
                   """
                   DELETE FROM extracted_fixed_plan_component_paths
                   WHERE fixed_plan_component_id = $id
                   """))
        {
            pathCommand.Parameters.AddWithValue("$id", fixedPlanComponentId.ToString());
            pathCommand.ExecuteNonQuery();
        }

        using (var command = CreateCommand(
                   """
                   DELETE FROM extracted_fixed_plan_components
                   WHERE id = $id
                   """))
        {
            command.Parameters.AddWithValue("$id", fixedPlanComponentId.ToString());
            command.ExecuteNonQuery();
        }

        foreach (var geometryPathId in geometryPathIds)
        {
            DeleteGeometryPath(geometryPathId);
        }

        return Task.CompletedTask;
    }

    private Guid? PersistGeometryPath(IReadOnlyList<GeometryPoint> points)
    {
        if (points.Count < 2)
        {
            return null;
        }

        var geometryPathId = Guid.NewGuid();
        using (var pathCommand = CreateCommand(
                   """
                   INSERT INTO geometry_paths (id, is_closed)
                   VALUES ($id, $is_closed)
                   """))
        {
            pathCommand.Parameters.AddWithValue("$id", geometryPathId.ToString());
            pathCommand.Parameters.AddWithValue("$is_closed", PointsFormClosedPath(points) ? 1 : 0);
            pathCommand.ExecuteNonQuery();
        }

        for (var index = 0; index < points.Count - 1; index++)
        {
            var start = points[index];
            var end = points[index + 1];

            using var segmentCommand = CreateCommand(
                """
                INSERT INTO geometry_segments (
                    id,
                    geometry_path_id,
                    sort_order,
                    start_x,
                    start_y,
                    end_x,
                    end_y)
                VALUES (
                    $id,
                    $geometry_path_id,
                    $sort_order,
                    $start_x,
                    $start_y,
                    $end_x,
                    $end_y)
                """);

            segmentCommand.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            segmentCommand.Parameters.AddWithValue("$geometry_path_id", geometryPathId.ToString());
            segmentCommand.Parameters.AddWithValue("$sort_order", index + 1);
            segmentCommand.Parameters.AddWithValue("$start_x", start.X.ToString(CultureInfo.InvariantCulture));
            segmentCommand.Parameters.AddWithValue("$start_y", start.Y.ToString(CultureInfo.InvariantCulture));
            segmentCommand.Parameters.AddWithValue("$end_x", end.X.ToString(CultureInfo.InvariantCulture));
            segmentCommand.Parameters.AddWithValue("$end_y", end.Y.ToString(CultureInfo.InvariantCulture));
            segmentCommand.ExecuteNonQuery();
        }

        return geometryPathId;
    }

    private IReadOnlyList<Guid> GetGeometryPathIds(Guid fixedPlanComponentId)
    {
        using var command = CreateCommand(
            """
            SELECT geometry_path_id
            FROM extracted_fixed_plan_component_paths
            WHERE fixed_plan_component_id = $id
            """);
        command.Parameters.AddWithValue("$id", fixedPlanComponentId.ToString());

        var items = new List<Guid>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(Guid.Parse(reader.GetString(0)));
        }

        return items;
    }

    private void DeleteGeometryPath(Guid geometryPathId)
    {
        using (var segmentCommand = CreateCommand(
                   """
                   DELETE FROM geometry_segments
                   WHERE geometry_path_id = $geometry_path_id
                   """))
        {
            segmentCommand.Parameters.AddWithValue("$geometry_path_id", geometryPathId.ToString());
            segmentCommand.ExecuteNonQuery();
        }

        using var pathCommand = CreateCommand(
            """
            DELETE FROM geometry_paths
            WHERE id = $geometry_path_id
            """);
        pathCommand.Parameters.AddWithValue("$geometry_path_id", geometryPathId.ToString());
        pathCommand.ExecuteNonQuery();
    }

    private static bool PointsFormClosedPath(IReadOnlyList<GeometryPoint> points)
    {
        return points.Count > 2 &&
               points[0].X == points[^1].X &&
               points[0].Y == points[^1].Y;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
