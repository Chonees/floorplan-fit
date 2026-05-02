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
                d.storage_path
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
                reader.GetString(2)));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }
}
