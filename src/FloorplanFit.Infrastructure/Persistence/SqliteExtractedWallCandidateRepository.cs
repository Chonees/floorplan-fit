using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteExtractedWallCandidateRepository : IExtractedWallCandidateRepository
{
    private readonly SqliteSession session;

    public SqliteExtractedWallCandidateRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddRangeAsync(
        IReadOnlyList<ExtractedWallCandidate> domainCandidates,
        IReadOnlyList<DetectedWallCandidate> detectedCandidates,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (domainCandidates.Count != detectedCandidates.Count)
        {
            throw new InvalidOperationException("Domain and detected wall candidate counts must match.");
        }

        for (var index = 0; index < domainCandidates.Count; index++)
        {
            var domainCandidate = domainCandidates[index];
            var detectedCandidate = detectedCandidates[index];
            var geometryPathId = PersistGeometryPath(detectedCandidate, domainCandidate.GeometryPathId);

            using var command = CreateCommand(
                """
                INSERT INTO extracted_wall_candidates (
                    id,
                    wall_extraction_run_id,
                    source_entity_ref,
                    source_layer,
                    geometry_path_id,
                    thickness_mm,
                    confidence,
                    detection_notes,
                    status,
                    sort_order)
                VALUES (
                    $id,
                    $wall_extraction_run_id,
                    $source_entity_ref,
                    $source_layer,
                    $geometry_path_id,
                    $thickness_mm,
                    $confidence,
                    $detection_notes,
                    $status,
                    $sort_order)
                """);

            command.Parameters.AddWithValue("$id", domainCandidate.Id.ToString());
            command.Parameters.AddWithValue("$wall_extraction_run_id", domainCandidate.WallExtractionRunId.ToString());
            command.Parameters.AddWithValue("$source_entity_ref", domainCandidate.SourceEntityRef);
            command.Parameters.AddWithValue("$source_layer", (object?)domainCandidate.SourceLayer ?? DBNull.Value);
            command.Parameters.AddWithValue("$geometry_path_id", (object?)geometryPathId?.ToString() ?? DBNull.Value);
            command.Parameters.AddWithValue("$thickness_mm", (object?)domainCandidate.ThicknessMm?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
            command.Parameters.AddWithValue("$confidence", domainCandidate.Confidence.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$detection_notes", (object?)domainCandidate.DetectionNotes ?? DBNull.Value);
            command.Parameters.AddWithValue("$status", (int)domainCandidate.Status);
            command.Parameters.AddWithValue("$sort_order", domainCandidate.SortOrder);
            command.ExecuteNonQuery();
        }

        return Task.CompletedTask;
    }

    public Task<ExtractedWallCandidate?> GetByIdAsync(Guid candidateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                id,
                wall_extraction_run_id,
                source_entity_ref,
                source_layer,
                geometry_path_id,
                thickness_mm,
                confidence,
                detection_notes,
                status,
                sort_order
            FROM extracted_wall_candidates
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", candidateId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<ExtractedWallCandidate?>(null);
        }

        return Task.FromResult<ExtractedWallCandidate?>(MapCandidate(reader));
    }

    public Task UpdateAsync(ExtractedWallCandidate candidate, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            UPDATE extracted_wall_candidates
            SET status = $status
            WHERE id = $id
            """);

        command.Parameters.AddWithValue("$id", candidate.Id.ToString());
        command.Parameters.AddWithValue("$status", (int)candidate.Status);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private Guid? PersistGeometryPath(DetectedWallCandidate detectedCandidate, Guid? existingGeometryPathId)
    {
        if (detectedCandidate.Points.Count < 2)
        {
            return existingGeometryPathId is { } pathId && pathId != Guid.Empty ? pathId : null;
        }

        var geometryPathId = existingGeometryPathId is { } value && value != Guid.Empty ? value : Guid.NewGuid();

        using (var pathCommand = CreateCommand(
                   """
                   INSERT INTO geometry_paths (id, is_closed)
                   VALUES ($id, $is_closed)
                   """))
        {
            pathCommand.Parameters.AddWithValue("$id", geometryPathId.ToString());
            pathCommand.Parameters.AddWithValue("$is_closed", 0);
            pathCommand.ExecuteNonQuery();
        }

        for (var index = 0; index < detectedCandidate.Points.Count - 1; index++)
        {
            var start = detectedCandidate.Points[index];
            var end = detectedCandidate.Points[index + 1];

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

    private static ExtractedWallCandidate MapCandidate(SqliteDataReader reader)
    {
        return new ExtractedWallCandidate(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4)),
            reader.IsDBNull(5) ? null : decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
            reader.IsDBNull(7) ? null : reader.GetString(7),
            (ExtractedWallCandidateStatus)reader.GetInt32(8),
            reader.GetInt32(9));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
