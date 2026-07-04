using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Domain.Measurement;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanLibraryReader : IFloorPlanLibraryReader
{
    private readonly SqliteSession session;

    public SqliteFloorPlanLibraryReader(SqliteSession session)
    {
        this.session = session;
    }

    public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            $"""
            SELECT
                t.id,
                t.code,
                t.name,
                t.current_version_id,
                v.id,
                v.version_number,
                v.created_at_utc,
                mc.source_unit,
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
                END AS derived_status,
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
                (
                    SELECT c.curation_version
                    FROM floorplan_curations c
                    WHERE c.id = t.active_published_curation_id
                      AND c.floorplan_version_id = v.id
                    LIMIT 1
                ) AS active_published_curation_version,
                (
                    SELECT COUNT(*)
                    FROM floorplan_curations c
                    WHERE c.floorplan_version_id = v.id
                      AND c.status = {(int)FloorPlanCurationStatus.Published}
                ) AS published_curation_count,
                (
                    SELECT MAX(c.curation_version)
                    FROM floorplan_curations c
                    WHERE c.floorplan_version_id = v.id
                      AND c.status = {(int)FloorPlanCurationStatus.Published}
                ) AS latest_published_curation_version,
                (
                    SELECT MAX(c.curation_version)
                    FROM floorplan_curations c
                    WHERE c.floorplan_version_id = v.id
                      AND c.status = {(int)FloorPlanCurationStatus.Draft}
                ) AS latest_draft_curation_version
            FROM floorplan_templates t
            JOIN floorplan_versions v ON v.floorplan_template_id = t.id
                AND v.deleted_at_utc IS NULL
            JOIN imported_documents d ON d.id = v.imported_document_id
            JOIN measurement_contexts mc ON mc.id = d.measurement_context_id
            WHERE t.is_active = 1
            ORDER BY t.code ASC, v.version_number DESC
            """);

        var templates = new Dictionary<Guid, TemplateAccumulator>();
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var templateId = Guid.Parse(reader.GetString(0));
            if (!templates.TryGetValue(templateId, out var template))
            {
                template = new TemplateAccumulator(
                    templateId,
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)));
                templates.Add(templateId, template);
            }

            var versionId = Guid.Parse(reader.GetString(4));
            var sourceUnit = (LengthUnit)reader.GetInt32(7);
            template.Versions.Add(new FloorPlanLibraryVersionDto(
                versionId,
                reader.GetInt32(5),
                reader.GetString(8),
                DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                sourceUnit.ToString().ToLowerInvariant(),
                template.CurrentVersionId == versionId,
                reader.IsDBNull(9) ? null : Guid.Parse(reader.GetString(9)),
                reader.IsDBNull(10) ? null : reader.GetInt32(10),
                reader.GetInt32(11),
                reader.IsDBNull(12) ? null : reader.GetInt32(12),
                reader.IsDBNull(13) ? null : reader.GetInt32(13)));
        }

        var items = templates.Values
            .OrderByDescending(item => item.Versions.FirstOrDefault(version => version.IsCurrent)?.ImportedAtUtc ?? DateTime.MinValue)
            .ThenBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .Select(item => new FloorPlanLibraryItemDto(
                item.TemplateId,
                item.Code,
                item.Name,
                item.Versions.Count,
                item.CurrentVersionId,
                item.Versions.FirstOrDefault(version => version.IsCurrent)?.VersionNumber,
                item.Versions))
            .ToArray();

        return Task.FromResult<IReadOnlyList<FloorPlanLibraryItemDto>>(items);
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }

    private sealed record TemplateAccumulator(
        Guid TemplateId,
        string Code,
        string Name,
        Guid? CurrentVersionId)
    {
        public List<FloorPlanLibraryVersionDto> Versions { get; } = [];
    }
}
