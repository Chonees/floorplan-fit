using FloorplanFit.Application.Abstractions;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanExtractionSourceReader : IFloorPlanExtractionSourceReader
{
    private readonly SqliteSession session;

    public SqliteFloorPlanExtractionSourceReader(SqliteSession session)
    {
        this.session = session;
    }

    public Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(Guid templateId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                t.id,
                v.id,
                d.storage_path,
                d.id,
                d.measurement_context_id,
                d.original_file_name,
                d.dxf_version
            FROM floorplan_templates t
            JOIN floorplan_versions v ON v.id = t.current_version_id
            JOIN imported_documents d ON d.id = v.imported_document_id
            WHERE t.id = $template_id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$template_id", templateId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<FloorPlanExtractionSource?>(null);
        }

        return Task.FromResult<FloorPlanExtractionSource?>(
            new FloorPlanExtractionSource(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2))
            {
                ImportedDocumentId = Guid.Parse(reader.GetString(3)),
                MeasurementContextId = Guid.Parse(reader.GetString(4)),
                OriginalFileName = reader.GetString(5),
                DxfVersion = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
    }

    public Task<FloorPlanExtractionSource?> GetByVersionAsync(Guid floorPlanVersionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT
                t.id,
                v.id,
                d.storage_path,
                d.id,
                d.measurement_context_id,
                d.original_file_name,
                d.dxf_version
            FROM floorplan_versions v
            JOIN floorplan_templates t ON t.id = v.floorplan_template_id
            JOIN imported_documents d ON d.id = v.imported_document_id
            WHERE v.id = $floorplan_version_id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$floorplan_version_id", floorPlanVersionId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<FloorPlanExtractionSource?>(null);
        }

        return Task.FromResult<FloorPlanExtractionSource?>(
            new FloorPlanExtractionSource(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2))
            {
                ImportedDocumentId = Guid.Parse(reader.GetString(3)),
                MeasurementContextId = Guid.Parse(reader.GetString(4)),
                OriginalFileName = reader.GetString(5),
                DxfVersion = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
