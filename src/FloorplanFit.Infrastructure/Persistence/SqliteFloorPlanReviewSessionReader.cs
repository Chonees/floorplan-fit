using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.Review;
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

        return await GetByVersionSummaryAsync(summary, cancellationToken);
    }

    public async Task<FloorPlanReviewSessionDto?> GetByVersionAsync(
        Guid templateId,
        Guid floorPlanVersionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var summary = GetVersionSummary(templateId, floorPlanVersionId);
        if (summary is null)
        {
            return null;
        }

        return await GetByVersionSummaryAsync(summary, cancellationToken);
    }

    private async Task<FloorPlanReviewSessionDto?> GetByVersionSummaryAsync(
        TemplateSummary? summary,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (summary is null)
        {
            return null;
        }

        var extractionRunId = GetLatestExtractionRunId(summary.FloorPlanVersionId);
        var measurementContext = GetMeasurementContext(summary.FloorPlanVersionId);
        var curationContext = GetCurationContext(summary.FloorPlanVersionId, summary.ActivePublishedCurationId);
        var positionOverrides = GetArtifactPositionOverrides(curationContext.LineageCurationIds);
        var labelOverrides = GetLabelOverrides(curationContext.LineageCurationIds);
        var dimensionOverrides = GetDimensionOverrides(curationContext.LineageCurationIds);
        var wallCandidates = extractionRunId is null
            ? []
            : GetWallCandidates(extractionRunId.Value);
        var roomLabels = extractionRunId is null
            ? []
            : GetRoomLabels(extractionRunId.Value)
                .Select(label => ResolvedFloorPlanArtifactPositionProjector.Resolve(
                    label,
                    positionOverrides.GetValueOrDefault((FloorPlanArtifactPositionSourceKinds.RoomLabel, label.RoomLabelId)),
                    labelOverrides.GetValueOrDefault((FloorPlanLabelOverrideSourceKinds.RoomLabel, label.RoomLabelId))))
                .ToArray();
        var openingCandidates = extractionRunId is null
            ? []
            : GetOpeningCandidates(extractionRunId.Value);
        var openingLabels = extractionRunId is null
            ? []
            : GetOpeningLabels(extractionRunId.Value)
                .Select(label => ResolvedFloorPlanArtifactPositionProjector.Resolve(
                    label,
                    positionOverrides.GetValueOrDefault((FloorPlanArtifactPositionSourceKinds.OpeningLabel, label.OpeningLabelId)),
                    labelOverrides.GetValueOrDefault((FloorPlanLabelOverrideSourceKinds.OpeningLabel, label.OpeningLabelId))))
                .ToArray();
        var fixedPlanComponents = extractionRunId is null
            ? []
            : GetFixedPlanComponents(extractionRunId.Value);
        var protectedDetailAssemblies = extractionRunId is null
            ? []
            : GetProtectedDetailAssemblies(extractionRunId.Value);
        var dimensions = extractionRunId is null
            ? []
            : GetDimensions(extractionRunId.Value, dimensionOverrides);
        var pinchGroups = curationContext.ActiveCurationId is null
            ? []
            : GetPinchGroups(curationContext.ActiveCurationId.Value);
        var pinchMarkers = curationContext.ActiveCurationId is null
            ? []
            : GetPinchMarkers(curationContext.ActiveCurationId.Value);
        var curatedPlanArtifacts = BuildCuratedPlanArtifacts(
            openingCandidates,
            fixedPlanComponents,
            protectedDetailAssemblies,
            GetArtifactClassificationOverrides(curationContext.LineageCurationIds),
            positionOverrides);

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

        var geometryPaths = ResolvedFloorPlanArtifactPositionProjector.ApplyGeometryTranslations(
            GetGeometryPaths(geometryPathIds),
            curatedPlanArtifacts);
        var measurableEdges = MeasurableEdgeProjector.Build(
            geometryPaths,
            wallCandidates,
            openingCandidates,
            measurementContext);
        var dimensionAssociations = DimensionAssociationProjector.Build(
            dimensions,
            measurableEdges,
            measurementContext);

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
            pinchMarkers,
            curatedPlanArtifacts)
        {
            Dimensions = dimensions,
            MeasurementContext = measurementContext,
            MeasurableEdges = measurableEdges,
            DimensionAssociations = dimensionAssociations
        });
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
                CASE
                    WHEN t.active_published_curation_id IS NOT NULL
                     AND EXISTS (
                         SELECT 1
                         FROM floorplan_curations c
                         WHERE c.id = t.active_published_curation_id
                           AND c.floorplan_version_id = v.id
                     ) THEN t.active_published_curation_id
                    ELSE NULL
                END AS active_published_curation_id,
                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM floorplan_curations c
                        WHERE c.floorplan_version_id = t.current_version_id
                          AND c.status = {(int)FloorPlanCurationStatus.Draft}
                    ) THEN 'Curated Draft'
                    WHEN EXISTS (
                        SELECT 1
                        FROM floorplan_curations c
                        WHERE c.floorplan_version_id = v.id
                          AND c.status = {(int)FloorPlanCurationStatus.Published}
                    ) THEN 'Published'
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

    private TemplateSummary? GetVersionSummary(Guid templateId, Guid floorPlanVersionId)
    {
        using var command = CreateCommand(
            $"""
            SELECT
                t.id,
                t.code,
                t.name,
                v.id,
                v.version_number,
                CASE
                    WHEN t.active_published_curation_id IS NOT NULL
                     AND EXISTS (
                         SELECT 1
                         FROM floorplan_curations c
                         WHERE c.id = t.active_published_curation_id
                           AND c.floorplan_version_id = v.id
                     ) THEN t.active_published_curation_id
                    ELSE NULL
                END AS active_published_curation_id,
                CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM floorplan_curations c
                        WHERE c.floorplan_version_id = v.id
                          AND c.status = {(int)FloorPlanCurationStatus.Draft}
                    ) THEN 'Curated Draft'
                    WHEN EXISTS (
                        SELECT 1
                        FROM floorplan_curations c
                        WHERE c.floorplan_version_id = v.id
                          AND c.status = {(int)FloorPlanCurationStatus.Published}
                    ) THEN 'Published'
                    WHEN EXISTS (
                        SELECT 1
                        FROM wall_extraction_runs r
                        JOIN extracted_wall_candidates c ON c.wall_extraction_run_id = r.id
                        WHERE r.floorplan_version_id = v.id
                    ) THEN 'Extracted'
                    ELSE 'Imported'
                END AS derived_status
            FROM floorplan_templates t
            JOIN floorplan_versions v ON v.floorplan_template_id = t.id
            WHERE t.id = $template_id
              AND v.id = $floorplan_version_id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$template_id", templateId.ToString());
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

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

    private MeasurementContextDto? GetMeasurementContext(Guid floorPlanVersionId)
    {
        using var command = CreateCommand(
            """
            SELECT
                m.source_unit,
                m.to_millimeters_factor,
                m.linear_tolerance_mm,
                m.angular_tolerance_deg
            FROM floorplan_versions v
            JOIN imported_documents d ON d.id = v.imported_document_id
            JOIN measurement_contexts m ON m.id = d.measurement_context_id
            WHERE v.id = $floorplan_version_id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new MeasurementContextDto(
            MapSourceUnit(reader.GetInt32(0)),
            decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
            decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture));
    }

    private CurationContext GetCurationContext(Guid floorPlanVersionId, Guid? activePublishedCurationId)
    {
        using var command = CreateCommand(
            $"""
            SELECT
                id,
                based_on_curation_id
            FROM floorplan_curations
            WHERE floorplan_version_id = $floorplan_version_id
              AND status = {(int)FloorPlanCurationStatus.Draft}
            ORDER BY curation_version DESC
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var draftId = Guid.Parse(reader.GetString(0));
            var lineageIds = new List<Guid>();
            if (!reader.IsDBNull(1))
            {
                lineageIds.Add(Guid.Parse(reader.GetString(1)));
            }

            lineageIds.Add(draftId);
            return new CurationContext(draftId, lineageIds);
        }

        return activePublishedCurationId is null
            ? new CurationContext(null, [])
            : new CurationContext(activePublishedCurationId.Value, [activePublishedCurationId.Value]);
    }

    private IReadOnlyList<WallCandidateDto> GetWallCandidates(Guid extractionRunId)
    {
        using var command = CreateCommand(
            $"""
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
              AND status <> {(int)ExtractedWallCandidateStatus.Rejected}
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

    private IReadOnlyDictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactClassification> GetArtifactClassificationOverrides(
        IReadOnlyList<Guid> lineageCurationIds)
    {
        var items = new Dictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactClassification>();
        foreach (var curationId in lineageCurationIds)
        {
            using var command = CreateCommand(
                """
                SELECT
                    floorplan_curation_id,
                    source_artifact_kind,
                    source_artifact_id,
                    resolved_family,
                    resolved_category,
                    resolved_type,
                    decision_state,
                    updated_at_utc
                FROM floorplan_artifact_classifications
                WHERE floorplan_curation_id = $floorplan_curation_id
                ORDER BY updated_at_utc ASC, source_artifact_kind ASC, source_artifact_id ASC
                """);
            command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var classification = new FloorPlanArtifactClassification(
                    Guid.Parse(reader.GetString(0)),
                    reader.GetString(1),
                    Guid.Parse(reader.GetString(2)),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    (FloorPlanArtifactDecisionState)reader.GetInt32(6),
                    DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
                items[(classification.SourceArtifactKind, classification.SourceArtifactId)] = classification;
            }
        }

        return items;
    }

    private IReadOnlyList<DimensionDto> GetDimensions(
        Guid extractionRunId,
        IReadOnlyDictionary<string, FloorPlanDimensionOverride> overrides)
    {
        using var command = CreateCommand(
            """
            SELECT
                id,
                source_entity_ref,
                source_handle,
                source_layer,
                source_entity_kind,
                geometry_block_name,
                display_text,
                display_text_source,
                raw_text_override,
                measurement_source_units,
                measurement_millimeters,
                source_unit,
                dim_type,
                angle,
                oblique_angle,
                def_point_x,
                def_point_y,
                def_point_z,
                def_point2_x,
                def_point2_y,
                def_point2_z,
                def_point3_x,
                def_point3_y,
                def_point3_z,
                render_text_x,
                render_text_y,
                render_text_height,
                render_text_rotation_degrees,
                render_text_style_name,
                render_text_horizontal_alignment,
                render_text_vertical_alignment,
                render_text_attachment_point,
                confidence,
                detection_notes,
                sort_order
            FROM extracted_dimensions
            WHERE wall_extraction_run_id = $wall_extraction_run_id
            ORDER BY sort_order ASC, id ASC
            """);
        command.Parameters.AddWithValue("$wall_extraction_run_id", extractionRunId.ToString());

        var items = new List<DimensionDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var dimensionId = Guid.Parse(reader.GetString(0));
            var baseDimension = new DimensionDto(
                dimensionId,
                reader.GetString(1),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8),
                decimal.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
                reader.GetString(11),
                reader.GetInt32(12),
                decimal.Parse(reader.GetString(13), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(14), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(15), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(16), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(17), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(18), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(19), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(20), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(21), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(22), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(23), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(32), CultureInfo.InvariantCulture),
                reader.IsDBNull(33) ? null : reader.GetString(33),
                reader.GetInt32(34))
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                RenderTextX = reader.IsDBNull(24) ? null : decimal.Parse(reader.GetString(24), CultureInfo.InvariantCulture),
                RenderTextY = reader.IsDBNull(25) ? null : decimal.Parse(reader.GetString(25), CultureInfo.InvariantCulture),
                RenderTextHeight = reader.IsDBNull(26) ? null : decimal.Parse(reader.GetString(26), CultureInfo.InvariantCulture),
                RenderTextRotationDegrees = reader.IsDBNull(27) ? null : decimal.Parse(reader.GetString(27), CultureInfo.InvariantCulture),
                RenderTextStyleName = reader.IsDBNull(28) ? null : reader.GetString(28),
                RenderTextHorizontalAlignment = reader.IsDBNull(29) ? null : reader.GetString(29),
                RenderTextVerticalAlignment = reader.IsDBNull(30) ? null : reader.GetString(30),
                RenderTextAttachmentPoint = reader.IsDBNull(31) ? null : reader.GetString(31),
                LineSegments = GetDimensionLineSegments(dimensionId),
                LinePrimitives = GetDimensionLinePrimitives(dimensionId),
                TextPrimitives = GetDimensionTextPrimitives(dimensionId),
                InsertPrimitives = GetDimensionInsertPrimitives(dimensionId),
                CirclePrimitives = GetDimensionCirclePrimitives(dimensionId),
                ArcPrimitives = GetDimensionArcPrimitives(dimensionId),
                SolidPrimitives = GetDimensionSolidPrimitives(dimensionId)
            };
            items.Add(ResolvedFloorPlanDimensionProjector.Resolve(baseDimension, overrides.GetValueOrDefault(ResolveSourceDimensionKey(baseDimension))));
        }

        return items;
    }

    private static string ResolveSourceDimensionKey(DimensionDto dimension)
    {
        return !string.IsNullOrWhiteSpace(dimension.SourceHandle)
            ? dimension.SourceHandle
            : dimension.SourceEntityRef;
    }

    private IReadOnlyList<DimensionLineSegmentDto> GetDimensionLineSegments(Guid dimensionId)
    {
        using var command = CreateCommand(
            """
            SELECT
                start_x,
                start_y,
                end_x,
                end_y
            FROM extracted_dimension_line_segments
            WHERE dimension_id = $dimension_id
            ORDER BY sort_order ASC
            """);
        command.Parameters.AddWithValue("$dimension_id", dimensionId.ToString());

        var items = new List<DimensionLineSegmentDto>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new DimensionLineSegmentDto(
                decimal.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture)));
        }

        return items;
    }

    private IReadOnlyList<DimensionLinePrimitiveDto> GetDimensionLinePrimitives(Guid dimensionId)
    {
        return GetDimensionPrimitives(
            dimensionId,
            "LINE",
            reader => new DimensionLinePrimitiveDto(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 4) ?? 0m,
                ParseNullableDecimal(reader, 5) ?? 0m,
                ParseNullableDecimal(reader, 6) ?? 0m,
                ParseNullableDecimal(reader, 7) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
    }

    private IReadOnlyList<DimensionTextPrimitiveDto> GetDimensionTextPrimitives(Guid dimensionId)
    {
        return GetDimensionPrimitives(
            dimensionId,
            "TEXT",
            reader => new DimensionTextPrimitiveDto(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                ParseNullableDecimal(reader, 9) ?? 0m,
                ParseNullableDecimal(reader, 10) ?? 0m,
                ParseNullableDecimal(reader, 12) ?? 0m,
                ParseNullableDecimal(reader, 13) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3),
                StyleName = reader.IsDBNull(14) ? null : reader.GetString(14),
                HorizontalAlignment = reader.IsDBNull(15) ? null : reader.GetString(15),
                VerticalAlignment = reader.IsDBNull(16) ? null : reader.GetString(16),
                AttachmentPoint = reader.IsDBNull(17) ? null : reader.GetString(17)
            });
    }

    private IReadOnlyList<DimensionInsertPrimitiveDto> GetDimensionInsertPrimitives(Guid dimensionId)
    {
        return GetDimensionPrimitives(
            dimensionId,
            "INSERT",
            reader => new DimensionInsertPrimitiveDto(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                ParseNullableDecimal(reader, 9) ?? 0m,
                ParseNullableDecimal(reader, 10) ?? 0m,
                ParseNullableDecimal(reader, 11) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3),
                RotationDegrees = ParseNullableDecimal(reader, 13) ?? 0m,
                ScaleX = ParseNullableDecimal(reader, 19) ?? 1m,
                ScaleY = ParseNullableDecimal(reader, 20) ?? 1m,
                ScaleZ = ParseNullableDecimal(reader, 21) ?? 1m
            });
    }

    private IReadOnlyList<DimensionCirclePrimitiveDto> GetDimensionCirclePrimitives(Guid dimensionId)
    {
        return GetDimensionPrimitives(
            dimensionId,
            "CIRCLE",
            reader => new DimensionCirclePrimitiveDto(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 22) ?? 0m,
                ParseNullableDecimal(reader, 23) ?? 0m,
                ParseNullableDecimal(reader, 24) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
    }

    private IReadOnlyList<DimensionArcPrimitiveDto> GetDimensionArcPrimitives(Guid dimensionId)
    {
        return GetDimensionPrimitives(
            dimensionId,
            "ARC",
            reader => new DimensionArcPrimitiveDto(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 22) ?? 0m,
                ParseNullableDecimal(reader, 23) ?? 0m,
                ParseNullableDecimal(reader, 24) ?? 0m,
                ParseNullableDecimal(reader, 25) ?? 0m,
                ParseNullableDecimal(reader, 26) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
    }

    private IReadOnlyList<DimensionSolidPrimitiveDto> GetDimensionSolidPrimitives(Guid dimensionId)
    {
        return GetDimensionPrimitives(
            dimensionId,
            "SOLID",
            reader => new DimensionSolidPrimitiveDto(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 27) ?? 0m,
                ParseNullableDecimal(reader, 28) ?? 0m,
                ParseNullableDecimal(reader, 29) ?? 0m,
                ParseNullableDecimal(reader, 30) ?? 0m,
                ParseNullableDecimal(reader, 31) ?? 0m,
                ParseNullableDecimal(reader, 32) ?? 0m,
                ParseNullableDecimal(reader, 33) ?? 0m,
                ParseNullableDecimal(reader, 34) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
    }

    private IReadOnlyList<T> GetDimensionPrimitives<T>(Guid dimensionId, string primitiveKind, Func<SqliteDataReader, T> map)
    {
        using var command = CreateCommand(
            """
            SELECT
                primitive_key,
                sort_order,
                source_handle,
                source_layer,
                start_x,
                start_y,
                end_x,
                end_y,
                text_value,
                x,
                y,
                z,
                height,
                rotation_degrees,
                style_name,
                horizontal_alignment,
                vertical_alignment,
                attachment_point,
                insert_name,
                scale_x,
                scale_y,
                scale_z,
                center_x,
                center_y,
                radius,
                start_angle_degrees,
                end_angle_degrees,
                point1_x,
                point1_y,
                point2_x,
                point2_y,
                point3_x,
                point3_y,
                point4_x,
                point4_y
            FROM extracted_dimension_primitives
            WHERE dimension_id = $dimension_id
              AND primitive_kind = $primitive_kind
            ORDER BY sort_order ASC, primitive_key ASC
            """);
        command.Parameters.AddWithValue("$dimension_id", dimensionId.ToString());
        command.Parameters.AddWithValue("$primitive_kind", primitiveKind);

        var items = new List<T>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(map(reader));
        }

        return items;
    }

    private IReadOnlyList<ExtractedDimensionLinePrimitive> GetDimensionOverrideLinePrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        return GetDimensionOverridePrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "LINE",
            reader => new ExtractedDimensionLinePrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 4) ?? 0m,
                ParseNullableDecimal(reader, 5) ?? 0m,
                ParseNullableDecimal(reader, 6) ?? 0m,
                ParseNullableDecimal(reader, 7) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
    }

    private IReadOnlyList<ExtractedDimensionTextPrimitive> GetDimensionOverrideTextPrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        return GetDimensionOverridePrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "TEXT",
            reader => new ExtractedDimensionTextPrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                ParseNullableDecimal(reader, 9) ?? 0m,
                ParseNullableDecimal(reader, 10) ?? 0m,
                ParseNullableDecimal(reader, 12) ?? 0m,
                ParseNullableDecimal(reader, 13) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3),
                StyleName = reader.IsDBNull(14) ? null : reader.GetString(14),
                HorizontalAlignment = reader.IsDBNull(15) ? null : reader.GetString(15),
                VerticalAlignment = reader.IsDBNull(16) ? null : reader.GetString(16),
                AttachmentPoint = reader.IsDBNull(17) ? null : reader.GetString(17)
            });
    }

    private IReadOnlyList<ExtractedDimensionInsertPrimitive> GetDimensionOverrideInsertPrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        return GetDimensionOverridePrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "INSERT",
            reader => new ExtractedDimensionInsertPrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.IsDBNull(18) ? string.Empty : reader.GetString(18),
                ParseNullableDecimal(reader, 9) ?? 0m,
                ParseNullableDecimal(reader, 10) ?? 0m,
                ParseNullableDecimal(reader, 11) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3),
                RotationDegrees = ParseNullableDecimal(reader, 13) ?? 0m,
                ScaleX = ParseNullableDecimal(reader, 19) ?? 1m,
                ScaleY = ParseNullableDecimal(reader, 20) ?? 1m,
                ScaleZ = ParseNullableDecimal(reader, 21) ?? 1m
            });
    }

    private IReadOnlyList<ExtractedDimensionCirclePrimitive> GetDimensionOverrideCirclePrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        return GetDimensionOverridePrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "CIRCLE",
            reader => new ExtractedDimensionCirclePrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 22) ?? 0m,
                ParseNullableDecimal(reader, 23) ?? 0m,
                ParseNullableDecimal(reader, 24) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
    }

    private IReadOnlyList<ExtractedDimensionArcPrimitive> GetDimensionOverrideArcPrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        return GetDimensionOverridePrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "ARC",
            reader => new ExtractedDimensionArcPrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 22) ?? 0m,
                ParseNullableDecimal(reader, 23) ?? 0m,
                ParseNullableDecimal(reader, 24) ?? 0m,
                ParseNullableDecimal(reader, 25) ?? 0m,
                ParseNullableDecimal(reader, 26) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
    }

    private IReadOnlyList<ExtractedDimensionSolidPrimitive> GetDimensionOverrideSolidPrimitives(Guid floorPlanCurationId, string sourceDimensionKey)
    {
        return GetDimensionOverridePrimitives(
            floorPlanCurationId,
            sourceDimensionKey,
            "SOLID",
            reader => new ExtractedDimensionSolidPrimitive(
                reader.GetString(0),
                reader.GetInt32(1),
                ParseNullableDecimal(reader, 27) ?? 0m,
                ParseNullableDecimal(reader, 28) ?? 0m,
                ParseNullableDecimal(reader, 29) ?? 0m,
                ParseNullableDecimal(reader, 30) ?? 0m,
                ParseNullableDecimal(reader, 31) ?? 0m,
                ParseNullableDecimal(reader, 32) ?? 0m,
                ParseNullableDecimal(reader, 33) ?? 0m,
                ParseNullableDecimal(reader, 34) ?? 0m)
            {
                SourceHandle = reader.IsDBNull(2) ? null : reader.GetString(2),
                SourceLayer = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
    }

    private IReadOnlyList<T> GetDimensionOverridePrimitives<T>(
        Guid floorPlanCurationId,
        string sourceDimensionKey,
        string primitiveKind,
        Func<SqliteDataReader, T> map)
    {
        using var command = CreateCommand(
            """
            SELECT
                primitive_key,
                sort_order,
                source_handle,
                source_layer,
                start_x,
                start_y,
                end_x,
                end_y,
                text_value,
                x,
                y,
                z,
                height,
                rotation_degrees,
                style_name,
                horizontal_alignment,
                vertical_alignment,
                attachment_point,
                insert_name,
                scale_x,
                scale_y,
                scale_z,
                center_x,
                center_y,
                radius,
                start_angle_degrees,
                end_angle_degrees,
                point1_x,
                point1_y,
                point2_x,
                point2_y,
                point3_x,
                point3_y,
                point4_x,
                point4_y
            FROM floorplan_dimension_override_primitives
            WHERE floorplan_curation_id = $floorplan_curation_id
              AND source_dimension_key = $source_dimension_key
              AND primitive_kind = $primitive_kind
            ORDER BY sort_order ASC, primitive_key ASC
            """);
        command.Parameters.AddWithValue("$floorplan_curation_id", floorPlanCurationId.ToString());
        command.Parameters.AddWithValue("$source_dimension_key", sourceDimensionKey);
        command.Parameters.AddWithValue("$primitive_kind", primitiveKind);

        var items = new List<T>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(map(reader));
        }

        return items;
    }

    private static decimal? ParseNullableDecimal(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal)
            ? null
            : decimal.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture);
    }

    private static string MapSourceUnit(int sourceUnitCode)
    {
        return sourceUnitCode switch
        {
            1 => "Millimeter",
            2 => "Centimeter",
            3 => "Meter",
            4 => "Inch",
            5 => "Foot",
            _ => "Unknown"
        };
    }

    private IReadOnlyDictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactPosition> GetArtifactPositionOverrides(
        IReadOnlyList<Guid> lineageCurationIds)
    {
        var items = new Dictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactPosition>();
        foreach (var curationId in lineageCurationIds)
        {
            using var command = CreateCommand(
                """
                SELECT
                    floorplan_curation_id,
                    source_artifact_kind,
                    source_artifact_id,
                    position_mode,
                    resolved_x,
                    resolved_y,
                    translation_dx,
                    translation_dy,
                    updated_at_utc
                FROM floorplan_artifact_positions
                WHERE floorplan_curation_id = $floorplan_curation_id
                ORDER BY updated_at_utc ASC, source_artifact_kind ASC, source_artifact_id ASC
                """);
            command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var positionMode = (FloorPlanArtifactPositionMode)reader.GetInt32(3);
                var position = positionMode == FloorPlanArtifactPositionMode.AbsolutePoint
                    ? FloorPlanArtifactPosition.CreateAbsolutePoint(
                        Guid.Parse(reader.GetString(0)),
                        reader.GetString(1),
                        Guid.Parse(reader.GetString(2)),
                        decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                        decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                        DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
                    : FloorPlanArtifactPosition.CreateTranslation(
                        Guid.Parse(reader.GetString(0)),
                        reader.GetString(1),
                        Guid.Parse(reader.GetString(2)),
                        decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                        decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                        DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
                items[(position.SourceArtifactKind, position.SourceArtifactId)] = position;
            }
        }

        return items;
    }

    private IReadOnlyDictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanLabelOverride> GetLabelOverrides(
        IReadOnlyList<Guid> lineageCurationIds)
    {
        var items = new Dictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanLabelOverride>();
        foreach (var curationId in lineageCurationIds)
        {
            using var command = CreateCommand(
                """
                SELECT
                    floorplan_curation_id,
                    source_artifact_kind,
                    source_artifact_id,
                    resolved_text_height,
                    updated_at_utc
                FROM floorplan_label_overrides
                WHERE floorplan_curation_id = $floorplan_curation_id
                ORDER BY updated_at_utc ASC, source_artifact_kind ASC, source_artifact_id ASC
                """);
            command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var labelOverride = reader.IsDBNull(3)
                    ? FloorPlanLabelOverride.CreateDetectedDefault(
                        Guid.Parse(reader.GetString(0)),
                        reader.GetString(1),
                        Guid.Parse(reader.GetString(2)),
                        DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
                    : FloorPlanLabelOverride.CreateResolvedTextHeight(
                        Guid.Parse(reader.GetString(0)),
                        reader.GetString(1),
                        Guid.Parse(reader.GetString(2)),
                        decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                        DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
                items[(labelOverride.SourceArtifactKind, labelOverride.SourceArtifactId)] = labelOverride;
            }
        }

        return items;
    }

    private IReadOnlyDictionary<string, FloorPlanDimensionOverride> GetDimensionOverrides(IReadOnlyList<Guid> lineageCurationIds)
    {
        var items = new Dictionary<string, FloorPlanDimensionOverride>(StringComparer.Ordinal);
        foreach (var curationId in lineageCurationIds)
        {
            using var command = CreateCommand(
                """
                SELECT
                    floorplan_curation_id,
                    source_dimension_key,
                    source_entity_ref,
                    source_handle,
                    display_text,
                    def_point_x,
                    def_point_y,
                    def_point_z,
                    def_point2_x,
                    def_point2_y,
                    def_point2_z,
                    def_point3_x,
                    def_point3_y,
                    def_point3_z,
                    render_text_x,
                    render_text_y,
                    render_text_height,
                    render_text_rotation_degrees,
                    render_text_style_name,
                    render_text_horizontal_alignment,
                    render_text_vertical_alignment,
                    render_text_attachment_point,
                    updated_at_utc,
                    last_exported_at_utc
                FROM floorplan_dimension_overrides
                WHERE floorplan_curation_id = $floorplan_curation_id
                ORDER BY updated_at_utc ASC, source_dimension_key ASC
                """);
            command.Parameters.AddWithValue("$floorplan_curation_id", curationId.ToString());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var sourceDimensionKey = reader.GetString(1);
                items[sourceDimensionKey] = FloorPlanDimensionOverride.CreateManualSnapshot(
                    Guid.Parse(reader.GetString(0)),
                    sourceDimensionKey,
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.GetString(4),
                    decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(10), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(11), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                    decimal.Parse(reader.GetString(13), CultureInfo.InvariantCulture),
                    ParseNullableDecimal(reader, 14),
                    ParseNullableDecimal(reader, 15),
                    ParseNullableDecimal(reader, 16),
                    ParseNullableDecimal(reader, 17),
                    reader.IsDBNull(18) ? null : reader.GetString(18),
                    reader.IsDBNull(19) ? null : reader.GetString(19),
                    reader.IsDBNull(20) ? null : reader.GetString(20),
                    reader.IsDBNull(21) ? null : reader.GetString(21),
                    GetDimensionOverrideLinePrimitives(curationId, sourceDimensionKey),
                    GetDimensionOverrideTextPrimitives(curationId, sourceDimensionKey),
                    GetDimensionOverrideInsertPrimitives(curationId, sourceDimensionKey),
                    GetDimensionOverrideCirclePrimitives(curationId, sourceDimensionKey),
                    GetDimensionOverrideArcPrimitives(curationId, sourceDimensionKey),
                    GetDimensionOverrideSolidPrimitives(curationId, sourceDimensionKey),
                    DateTime.Parse(reader.GetString(22), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    reader.IsDBNull(23) ? null : DateTime.Parse(reader.GetString(23), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
            }
        }

        return items;
    }

    private IReadOnlyList<CuratedPlanArtifactDto> BuildCuratedPlanArtifacts(
        IReadOnlyList<OpeningCandidateDto> openingCandidates,
        IReadOnlyList<FixedPlanComponentDto> fixedPlanComponents,
        IReadOnlyList<ProtectedDetailAssemblyDto> protectedDetailAssemblies,
        IReadOnlyDictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactClassification> classifications,
        IReadOnlyDictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactPosition> positions)
    {
        var items = new List<CuratedPlanArtifactDto>();

        foreach (var opening in openingCandidates)
        {
            var detected = FloorPlanArtifactTaxonomy.ResolveDetectedOpeningClassification(opening.Kind);
            items.Add(CreateCuratedPlanArtifact(
                opening.OpeningCandidateId,
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                opening.SourceEntityRef,
                opening.SourceLayer,
                opening.SourceEntityKind,
                sourceBlockName: null,
                opening.GeometryPathId is null ? [] : [opening.GeometryPathId.Value],
                opening.Confidence,
                opening.DetectionNotes,
                opening.SortOrder,
                detected,
                classifications,
                positions));
        }

        foreach (var component in fixedPlanComponents)
        {
            var detected = FloorPlanArtifactTaxonomy.ResolveDetectedFixedClassification(component.Kind);
            items.Add(CreateCuratedPlanArtifact(
                component.FixedPlanComponentId,
                FloorPlanArtifactSourceKinds.FixedPlanComponent,
                component.SourceEntityRef,
                component.SourceLayer,
                component.SourceEntityKind,
                component.SourceBlockName,
                component.GeometryPathIds,
                component.Confidence,
                component.DetectionNotes,
                component.SortOrder,
                detected,
                classifications,
                positions));
        }

        foreach (var assembly in protectedDetailAssemblies)
        {
            var detected = FloorPlanArtifactTaxonomy.ResolveDetectedProtectedClassification(assembly.Kind);
            items.Add(CreateCuratedPlanArtifact(
                assembly.ProtectedDetailAssemblyId,
                FloorPlanArtifactSourceKinds.ProtectedDetailAssembly,
                assembly.SourceEntityRef,
                assembly.SourceLayer,
                assembly.SourceEntityKind,
                sourceBlockName: null,
                assembly.GeometryPathIds,
                assembly.Confidence,
                assembly.DetectionNotes,
                assembly.SortOrder,
                detected,
                classifications,
                positions));
        }

        return items
            .OrderBy(item => FloorPlanArtifactTaxonomy.ResolveFamilySortOrder(item.ResolvedFamily))
            .ThenBy(item => FloorPlanArtifactTaxonomy.ResolveCategorySortOrder(item.ResolvedFamily, item.ResolvedCategory))
            .ThenBy(item => item.SortOrder)
            .ThenBy(item => item.SourceEntityRef, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static CuratedPlanArtifactDto CreateCuratedPlanArtifact(
        Guid sourceArtifactId,
        string sourceArtifactKind,
        string sourceEntityRef,
        string sourceLayer,
        string sourceEntityKind,
        string? sourceBlockName,
        IReadOnlyList<Guid> geometryPathIds,
        decimal confidence,
        string? detectionNotes,
        int sortOrder,
        (string Family, string Category, string Type) detected,
        IReadOnlyDictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactClassification> classifications,
        IReadOnlyDictionary<(string SourceArtifactKind, Guid SourceArtifactId), FloorPlanArtifactPosition> positions)
    {
        var decisionState = FloorPlanArtifactDecisionState.DetectedDefault;
        var resolved = detected;
        if (classifications.TryGetValue((sourceArtifactKind, sourceArtifactId), out var classification))
        {
            decisionState = classification.DecisionState;
            if (classification.DecisionState != FloorPlanArtifactDecisionState.DetectedDefault)
            {
                resolved = (classification.ResolvedFamily, classification.ResolvedCategory, classification.ResolvedType);
            }
        }
        var artifact = new CuratedPlanArtifactDto(
            sourceArtifactId,
            sourceArtifactKind,
            sourceEntityRef,
            sourceLayer,
            sourceEntityKind,
            sourceBlockName,
            geometryPathIds,
            confidence,
            detectionNotes,
            sortOrder,
            detected.Family,
            detected.Category,
            detected.Type,
            resolved.Family,
            resolved.Category,
            resolved.Type,
            decisionState.ToString(),
            FloorPlanArtifactTaxonomy.ResolveColorArgb(resolved.Family, resolved.Category, resolved.Type));

        return ResolvedFloorPlanArtifactPositionProjector.Resolve(
            artifact,
            positions.GetValueOrDefault((sourceArtifactKind, sourceArtifactId)));
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
        Guid FloorPlanVersionId,
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

    private sealed record CurationContext(Guid? ActiveCurationId, IReadOnlyList<Guid> LineageCurationIds);
}
