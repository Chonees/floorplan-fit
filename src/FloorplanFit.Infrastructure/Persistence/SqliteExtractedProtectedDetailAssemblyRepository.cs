using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteExtractedProtectedDetailAssemblyRepository : IExtractedProtectedDetailAssemblyRepository
{
    private readonly SqliteSession session;

    public SqliteExtractedProtectedDetailAssemblyRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddRangeAsync(
        IReadOnlyList<ExtractedProtectedDetailAssembly> domainAssemblies,
        IReadOnlyList<DetectedProtectedDetailAssembly> detectedAssemblies,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (domainAssemblies.Count != detectedAssemblies.Count)
        {
            throw new InvalidOperationException("Domain and detected protected detail assembly counts must match.");
        }

        for (var index = 0; index < domainAssemblies.Count; index++)
        {
            var domainAssembly = domainAssemblies[index];
            var detectedAssembly = detectedAssemblies[index];

            using (var command = CreateCommand(
                       """
                       INSERT INTO extracted_protected_detail_assemblies (
                           id,
                           wall_extraction_run_id,
                           source_entity_ref,
                           source_layer,
                           kind,
                           source_entity_kind,
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
                           $confidence,
                           $detection_notes,
                           $sort_order,
                           $color_argb)
                       """))
            {
                command.Parameters.AddWithValue("$id", domainAssembly.Id.ToString());
                command.Parameters.AddWithValue("$wall_extraction_run_id", domainAssembly.WallExtractionRunId.ToString());
                command.Parameters.AddWithValue("$source_entity_ref", domainAssembly.SourceEntityRef);
                command.Parameters.AddWithValue("$source_layer", (object?)domainAssembly.SourceLayer ?? DBNull.Value);
                command.Parameters.AddWithValue("$kind", domainAssembly.Kind);
                command.Parameters.AddWithValue("$source_entity_kind", (object?)domainAssembly.SourceEntityKind ?? DBNull.Value);
                command.Parameters.AddWithValue("$confidence", domainAssembly.Confidence.ToString(CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$detection_notes", (object?)domainAssembly.DetectionNotes ?? DBNull.Value);
                command.Parameters.AddWithValue("$sort_order", domainAssembly.SortOrder);
                command.Parameters.AddWithValue("$color_argb", (object?)domainAssembly.ColorArgb ?? DBNull.Value);
                command.ExecuteNonQuery();
            }

            for (var pathIndex = 0; pathIndex < detectedAssembly.GeometryPaths.Count; pathIndex++)
            {
                var geometryPathId = PersistGeometryPath(detectedAssembly.GeometryPaths[pathIndex]);
                if (geometryPathId is null)
                {
                    continue;
                }

                using var pathCommand = CreateCommand(
                    """
                    INSERT INTO extracted_protected_detail_assembly_paths (
                        protected_detail_assembly_id,
                        geometry_path_id,
                        sort_order)
                    VALUES (
                        $protected_detail_assembly_id,
                        $geometry_path_id,
                        $sort_order)
                    """);
                pathCommand.Parameters.AddWithValue("$protected_detail_assembly_id", domainAssembly.Id.ToString());
                pathCommand.Parameters.AddWithValue("$geometry_path_id", geometryPathId.Value.ToString());
                pathCommand.Parameters.AddWithValue("$sort_order", pathIndex + 1);
                pathCommand.ExecuteNonQuery();
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExtractedProtectedDetailAssembly>> ListByExtractionRunAsync(
        Guid wallExtractionRunId,
        CancellationToken cancellationToken)
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
                confidence,
                detection_notes,
                sort_order,
                color_argb
            FROM extracted_protected_detail_assemblies
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", wallExtractionRunId.ToString());

        var items = new List<ExtractedProtectedDetailAssembly>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new ExtractedProtectedDetailAssembly(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetString(9)));
        }

        return Task.FromResult<IReadOnlyList<ExtractedProtectedDetailAssembly>>(items);
    }

    public Task RemoveAsync(Guid protectedDetailAssemblyId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var geometryPathIds = GetGeometryPathIds(protectedDetailAssemblyId);

        using (var pathCommand = CreateCommand(
                   """
                   DELETE FROM extracted_protected_detail_assembly_paths
                   WHERE protected_detail_assembly_id = $id
                   """))
        {
            pathCommand.Parameters.AddWithValue("$id", protectedDetailAssemblyId.ToString());
            pathCommand.ExecuteNonQuery();
        }

        using (var command = CreateCommand(
                   """
                   DELETE FROM extracted_protected_detail_assemblies
                   WHERE id = $id
                   """))
        {
            command.Parameters.AddWithValue("$id", protectedDetailAssemblyId.ToString());
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

    private IReadOnlyList<Guid> GetGeometryPathIds(Guid protectedDetailAssemblyId)
    {
        using var command = CreateCommand(
            """
            SELECT geometry_path_id
            FROM extracted_protected_detail_assembly_paths
            WHERE protected_detail_assembly_id = $id
            """);
        command.Parameters.AddWithValue("$id", protectedDetailAssemblyId.ToString());

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
