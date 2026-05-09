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
        var roomLabels = extractionRunId is null
            ? []
            : GetRoomLabels(extractionRunId.Value);
        var openingCandidates = extractionRunId is null
            ? []
            : GetOpeningCandidates(extractionRunId.Value);
        var openingLabels = extractionRunId is null
            ? []
            : GetOpeningLabels(extractionRunId.Value);
        var fixedPlanComponents = extractionRunId is null
            ? []
            : GetFixedPlanComponents(extractionRunId.Value);
        var protectedDetailAssemblies = extractionRunId is null
            ? []
            : GetProtectedDetailAssemblies(extractionRunId.Value);

        var curationId = GetDraftCurationId(summary.CurrentVersionId) ?? summary.ActivePublishedCurationId;
        var pinchGroups = curationId is null
            ? []
            : GetPinchGroups(curationId.Value);
        var pinchMarkers = curationId is null
            ? []
            : GetPinchMarkers(curationId.Value);

        var geometryPathIds = wallCandidates
            .Select(item => item.GeometryPathId)
            .Concat(openingCandidates.Select(item => item.GeometryPathId))
            .Concat(fixedPlanComponents.SelectMany(item => item.GeometryPathIds).Select(item => (Guid?)item))
            .Concat(protectedDetailAssemblies.SelectMany(item => item.GeometryPathIds).Select(item => (Guid?)item))
            .Concat(pinchMarkers.Select(item => (Guid?)item.GeometryPathId))
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
            roomLabels,
            openingCandidates,
            openingLabels,
            fixedPlanComponents,
            protectedDetailAssemblies,
            wallCandidates,
            pinchGroups,
            pinchMarkers));
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

    private IReadOnlyList<RoomLabelDto> GetRoomLabels(Guid extractionRunId)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
                source_entity_ref,
                source_layer,
                text,
                x,
                y,
                confidence,
                detection_notes,
                sort_order,
                source_entity_kind,
                text_height,
                rotation_degrees,
                text_style_name,
                horizontal_alignment,
                vertical_alignment,
                attachment_point,
                color_argb
            FROM extracted_room_labels
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", extractionRunId.ToString());

        var items = new List<RoomLabelDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new RoomLabelDto(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.GetString(3),
                decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetInt32(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                reader.IsDBNull(10) ? null : decimal.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
                reader.IsDBNull(11) ? 0m : decimal.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
                reader.IsDBNull(12) ? null : reader.GetString(12),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16)));
        }

        return items;
    }

    private IReadOnlyList<OpeningCandidateDto> GetOpeningCandidates(Guid extractionRunId)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
                source_entity_ref,
                source_layer,
                kind,
                source_entity_kind,
                geometry_path_id,
                confidence,
                detection_notes,
                sort_order
            FROM extracted_opening_candidates
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", extractionRunId.ToString());

        var items = new List<OpeningCandidateDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new OpeningCandidateDto(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                reader.IsDBNull(5) ? null : Guid.Parse(reader.GetString(5)),
                decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.GetInt32(8)));
        }

        return items;
    }

    private IReadOnlyList<OpeningLabelDto> GetOpeningLabels(Guid extractionRunId)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
                source_entity_ref,
                source_layer,
                kind,
                text,
                x,
                y,
                confidence,
                detection_notes,
                sort_order,
                source_entity_kind,
                text_height,
                rotation_degrees,
                text_style_name,
                horizontal_alignment,
                vertical_alignment,
                attachment_point,
                color_argb
            FROM extracted_opening_labels
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", extractionRunId.ToString());

        var items = new List<OpeningLabelDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new OpeningLabelDto(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.GetInt32(9),
                reader.IsDBNull(10) ? null : reader.GetString(10),
                reader.IsDBNull(11) ? null : decimal.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
                reader.IsDBNull(12) ? 0m : decimal.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                reader.IsDBNull(13) ? null : reader.GetString(13),
                reader.IsDBNull(14) ? null : reader.GetString(14),
                reader.IsDBNull(15) ? null : reader.GetString(15),
                reader.IsDBNull(16) ? null : reader.GetString(16),
                reader.IsDBNull(17) ? null : reader.GetString(17)));
        }

        return items;
    }

    private IReadOnlyList<FixedPlanComponentDto> GetFixedPlanComponents(Guid extractionRunId)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
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
        command.Parameters.AddWithValue("$wall_extraction_run_id", extractionRunId.ToString());

        var rows = new List<FixedPlanComponentRow>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                rows.Add(new FixedPlanComponentRow(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                    reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.GetInt32(8),
                    reader.IsDBNull(9) ? null : reader.GetString(9)));
            }
        }

        return rows
            .Select(row => new FixedPlanComponentDto(
                row.Id,
                row.SourceEntityRef,
                row.SourceLayer,
                row.Kind,
                row.SourceEntityKind,
                row.SourceBlockName,
                GetFixedPlanComponentGeometryPathIds(row.Id),
                row.Confidence,
                row.DetectionNotes,
                row.SortOrder,
                row.ColorArgb))
            .ToArray();
    }

    private IReadOnlyList<Guid> GetFixedPlanComponentGeometryPathIds(Guid componentId)
    {
        using var command = CreateCommand(
            """
            SELECT geometry_path_id
            FROM extracted_fixed_plan_component_paths
            WHERE fixed_plan_component_id = $fixed_plan_component_id
            ORDER BY sort_order ASC, geometry_path_id ASC
            """);
        command.Parameters.AddWithValue("$fixed_plan_component_id", componentId.ToString());

        var items = new List<Guid>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(Guid.Parse(reader.GetString(0)));
        }

        return items;
    }

    private IReadOnlyList<ProtectedDetailAssemblyDto> GetProtectedDetailAssemblies(Guid extractionRunId)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
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
        command.Parameters.AddWithValue("$wall_extraction_run_id", extractionRunId.ToString());

        var rows = new List<ProtectedDetailAssemblyRow>();
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                rows.Add(new ProtectedDetailAssemblyRow(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetInt32(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8)));
            }
        }

        return rows
            .Select(row => new ProtectedDetailAssemblyDto(
                row.Id,
                row.SourceEntityRef,
                row.SourceLayer,
                row.Kind,
                row.SourceEntityKind,
                GetProtectedDetailAssemblyGeometryPathIds(row.Id),
                row.Confidence,
                row.DetectionNotes,
                row.SortOrder,
                row.ColorArgb))
            .ToArray();
    }

    private IReadOnlyList<Guid> GetProtectedDetailAssemblyGeometryPathIds(Guid assemblyId)
    {
        using var command = CreateCommand(
            """
            SELECT geometry_path_id
            FROM extracted_protected_detail_assembly_paths
            WHERE protected_detail_assembly_id = $protected_detail_assembly_id
            ORDER BY sort_order ASC, geometry_path_id ASC
            """);
        command.Parameters.AddWithValue("$protected_detail_assembly_id", assemblyId.ToString());

        var items = new List<Guid>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(Guid.Parse(reader.GetString(0)));
        }

        return items;
    }

    private IReadOnlyList<PinchGroupDto> GetPinchGroups(Guid floorPlanCurationId)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
                name,
                axis_tag,
                sort_order
            FROM pinch_groups
            WHERE floorplan_curation_id = $floorplan_curation_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<PinchGroupDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new PinchGroupDto(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                ((PinchAxisTag)reader.GetInt32(2)).ToString(),
                reader.GetInt32(3)));
        }

        return items;
    }

    private IReadOnlyList<PinchMarkerDto> GetPinchMarkers(Guid floorPlanCurationId)
    {
        using var command = CreateCommand(
            """
            SELECT
                markers.id,
                markers.pinch_group_id,
                groups.name,
                markers.source_candidate_id,
                markers.geometry_path_id,
                groups.axis_tag,
                markers.position_ratio,
                markers.max_trim_mm,
                markers.sort_order
            FROM pinch_markers AS markers
            INNER JOIN pinch_groups AS groups ON groups.id = markers.pinch_group_id
            WHERE markers.floorplan_curation_id = $floorplan_curation_id
            ORDER BY groups.sort_order ASC, markers.sort_order ASC, markers.id ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());

        var items = new List<PinchMarkerDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new PinchMarkerDto(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                Guid.Parse(reader.GetString(3)),
                Guid.Parse(reader.GetString(4)),
                ((PinchAxisTag)reader.GetInt32(5)).ToString(),
                decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                reader.GetInt32(8)));
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

    private sealed record FixedPlanComponentRow(
        Guid Id,
        string SourceEntityRef,
        string SourceLayer,
        string Kind,
        string SourceEntityKind,
        string? SourceBlockName,
        decimal Confidence,
        string? DetectionNotes,
        int SortOrder,
        string? ColorArgb);

    private sealed record ProtectedDetailAssemblyRow(
        Guid Id,
        string SourceEntityRef,
        string SourceLayer,
        string Kind,
        string SourceEntityKind,
        decimal Confidence,
        string? DetectionNotes,
        int SortOrder,
        string? ColorArgb);
}
