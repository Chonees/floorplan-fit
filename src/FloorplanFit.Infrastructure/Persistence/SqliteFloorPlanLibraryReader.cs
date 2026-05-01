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
                v.version_number,
                v.created_at_utc,
                mc.source_unit,
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
            JOIN imported_documents d ON d.id = v.imported_document_id
            JOIN measurement_contexts mc ON mc.id = d.measurement_context_id
            WHERE t.is_active = 1
            ORDER BY v.created_at_utc DESC, t.code ASC
            """);

        var items = new List<FloorPlanLibraryItemDto>();
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var sourceUnit = (LengthUnit)reader.GetInt32(5);
            Guid? activePublishedCurationId = reader.IsDBNull(6) ? null : Guid.Parse(reader.GetString(6));
            var status = reader.GetString(7);

            items.Add(new FloorPlanLibraryItemDto(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                status,
                reader.GetInt32(3),
                DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                sourceUnit.ToString().ToLowerInvariant(),
                activePublishedCurationId));
        }

        return Task.FromResult<IReadOnlyList<FloorPlanLibraryItemDto>>(items);
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
