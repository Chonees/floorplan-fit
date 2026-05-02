using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanReviewSessionReader : IFloorPlanReviewSessionReader
{
    private readonly SqliteSession session;

    public SqliteFloorPlanReviewSessionReader(SqliteSession session)
    {
        this.session = session;
    }

    public async Task<FloorPlanReviewSessionDto?> GetByTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var summary = GetTemplateSummary(templateId);
        if (summary is null)
        {
            return null;
        }

        var extractionRunId = GetLatestExtractionRunId(summary.CurrentVersionId);
        var wallCandidates = extractionRunId is null
            ? []
            : GetWallCandidates(extractionRunId.Value);

        var curationId = GetDraftCurationId(summary.CurrentVersionId) ?? summary.ActivePublishedCurationId;
        var curatedWalls = curationId is null
            ? []
            : GetCuratedWalls(curationId.Value);

        var geometryPathIds = wallCandidates
            .Select(item => item.GeometryPathId)
            .Concat(curatedWalls.Select(item => item.GeometryPathId))
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();

        var geometryPaths = GetGeometryPaths(geometryPathIds);

        return await Task.FromResult(new FloorPlanReviewSessionDto(
            summary.TemplateId,
            summary.Code,
            summary.Name,
            summary.Status,
            summary.ActiveVersionNumber,
            summary.ActivePublishedCurationId,
            geometryPaths,
            wallCandidates,
            curatedWalls));
    }

    private TemplateSummary? GetTemplateSummary(Guid templateId)
    {
        using var command = CreateCommand(
            $"""
            SELECT
                t.id,
                t.code,
                t.name,
                t.current_version_id,
                v.version_number,
                t.active_published_curation_id,
                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM floorplan_curations c
                        WHERE c.floorplan_version_id = t.current_version_id
                          AND c.status = {(int)FloorPlanCurationStatus.Draft}
                    ) THEN 'Curated Draft'
                    WHEN t.active_published_curation_id IS NOT NULL THEN 'Published'
                    WHEN EXISTS (
                        SELECT 1
                        FROM wall_extraction_runs r
                        JOIN extracted_wall_candidates c ON c.wall_extraction_run_id = r.id
                        WHERE r.floorplan_version_id = t.current_version_id
                    ) THEN 'Extracted'
                    ELSE 'Imported'
                END AS derived_status
            FROM floorplan_templates t
            JOIN floorplan_versions v ON v.id = t.current_version_id
            WHERE t.id = $template_id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$template_id", templateId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new TemplateSummary(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            Guid.Parse(reader.GetString(3)),
            reader.GetInt32(4),
            reader.IsDBNull(5) ? null : Guid.Parse(reader.GetString(5)),
            reader.GetString(6));
    }

    private Guid? GetLatestExtractionRunId(Guid floorPlanVersionId)
    {
        using var command = CreateCommand(
            """
            SELECT id
            FROM wall_extraction_runs
            WHERE floorplan_version_id = $floorplan_version_id
            ORDER BY started_at_utc DESC, id DESC
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

        var result = command.ExecuteScalar() as string;
        return result is null ? null : Guid.Parse(result);
    }

    private Guid? GetDraftCurationId(Guid floorPlanVersionId)
    {
        using var command = CreateCommand(
            $"""
            SELECT id
            FROM floorplan_curations
            WHERE floorplan_version_id = $floorplan_version_id
              AND status = {(int)FloorPlanCurationStatus.Draft}
            ORDER BY curation_version DESC
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

        var result = command.ExecuteScalar() as string;
        return result is null ? null : Guid.Parse(result);
    }

    private IReadOnlyList<WallCandidateDto> GetWallCandidates(Guid extractionRunId)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
                source_entity_ref,
                source_layer,
                status,
                confidence,
                thickness_mm,
                detection_notes,
                geometry_path_id,
                sort_order
            FROM extracted_wall_candidates
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", extractionRunId.ToString());

        var items = new List<WallCandidateDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new WallCandidateDto(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                ((ExtractedWallCandidateStatus)reader.GetInt32(3)).ToString(),
                decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                reader.IsDBNull(5) ? null : decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : Guid.Parse(reader.GetString(7)),
                reader.GetInt32(8)));
        }

        return items;
    }

    private IReadOnlyList<CuratedWallDto> GetCuratedWalls(Guid floorPlanCurationId)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
                stable_wall_id,
                source_candidate_id,
                wall_role,
                mobility_level,
                protection_level,
                thickness_mm,
                assembly_code,
                height_mm,
                is_exterior,
                is_structural_hint,
                geometry_path_id,
                sort_order,
                notes
            FROM curated_walls
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY sort_order ASC, stable_wall_id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<CuratedWallDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new CuratedWallDto(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : Guid.Parse(reader.GetString(2)),
                ((WallRole)reader.GetInt32(3)).ToString(),
                ((WallMobilityLevel)reader.GetInt32(4)).ToString(),
                ((WallProtectionLevel)reader.GetInt32(5)).ToString(),
                reader.IsDBNull(6) ? null : decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                reader.GetInt32(9) == 1,
                reader.GetInt32(10) == 1,
                reader.IsDBNull(11) ? null : Guid.Parse(reader.GetString(11)),
                reader.GetInt32(12),
                reader.IsDBNull(13) ? null : reader.GetString(13)));
        }

        return items;
    }

    private IReadOnlyList<GeometryPathDto> GetGeometryPaths(IReadOnlyList<Guid> geometryPathIds)
    {
        if (geometryPathIds.Count == 0)
        {
            return [];
        }

        var pathLookup = new Dictionary<Guid, bool>();
        using (var pathCommand = CreateCommand(
                   $"SELECT id, is_closed FROM geometry_paths WHERE id IN ({BuildParameterList("$path", geometryPathIds.Count)})"))
        {
            AddGuidParameters(pathCommand, "$path", geometryPathIds);
            using var reader = pathCommand.ExecuteReader();
            while (reader.Read())
            {
                pathLookup[Guid.Parse(reader.GetString(0))] = reader.GetInt32(1) == 1;
            }
        }

        var segmentLookup = geometryPathIds.ToDictionary(id => id, _ => new List<GeometrySegmentDto>());
        using (var segmentCommand = CreateCommand(
                   $"""
                   SELECT geometry_path_id, sort_order, start_x, start_y, end_x, end_y
                   FROM geometry_segments
                   WHERE geometry_path_id IN ({BuildParameterList("$segment_path", geometryPathIds.Count)})
                   ORDER BY geometry_path_id ASC, sort_order ASC
                   """))
        {
            AddGuidParameters(segmentCommand, "$segment_path", geometryPathIds);
            using var reader = segmentCommand.ExecuteReader();
            while (reader.Read())
            {
                var pathId = Guid.Parse(reader.GetString(0));
                segmentLookup[pathId].Add(new GeometrySegmentDto(
                    pathId,
                    reader.GetInt32(1),
                    decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture)));
            }
        }

        return geometryPathIds
            .Where(pathLookup.ContainsKey)
            .Select(id => new GeometryPathDto(id, pathLookup[id], segmentLookup[id]))
            .ToArray();
    }

    private static string BuildParameterList(string prefix, int count)
    {
        return string.Join(", ", Enumerable.Range(0, count).Select(index => $"{prefix}{index}"));
    }

    private static void AddGuidParameters(SqliteCommand command, string prefix, IReadOnlyList<Guid> values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            command.Parameters.AddWithValue($"{prefix}{index}", values[index].ToString());
        }
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private sealed record TemplateSummary(
        Guid TemplateId,
        string Code,
        string Name,
        Guid CurrentVersionId,
        int ActiveVersionNumber,
        Guid? ActivePublishedCurationId,
        string Status);
}
