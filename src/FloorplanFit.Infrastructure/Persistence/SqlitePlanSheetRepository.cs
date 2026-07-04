using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqlitePlanSheetRepository : IPlanSheetRepository, IPlanSheetReader, IPlanSheetSourceReader
{
    private readonly SqliteSession session;

    public SqlitePlanSheetRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO plan_sheets (
                id,
                plan_set_version_id,
                sheet_type,
                imported_document_id,
                measurement_context_id,
                name,
                status,
                created_at_utc)
            VALUES (
                $id,
                $plan_set_version_id,
                $sheet_type,
                $imported_document_id,
                $measurement_context_id,
                $name,
                $status,
                $created_at_utc)
            """);
        command.Parameters.AddWithValue("$id", sheet.Id.ToString());
        command.Parameters.AddWithValue("$plan_set_version_id", sheet.PlanSetVersionId.ToString());
        command.Parameters.AddWithValue("$sheet_type", (int)sheet.SheetType);
        command.Parameters.AddWithValue("$imported_document_id", sheet.ImportedDocumentId.ToString());
        command.Parameters.AddWithValue("$measurement_context_id", sheet.MeasurementContextId.ToString());
        command.Parameters.AddWithValue("$name", sheet.Name);
        command.Parameters.AddWithValue("$status", sheet.Status.ToString());
        command.Parameters.AddWithValue("$created_at_utc", sheet.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT id, plan_set_version_id, sheet_type, imported_document_id, measurement_context_id, name, status, created_at_utc
            FROM plan_sheets
            WHERE id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", sheetId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<PlanSheet?>(null);
        }

        return Task.FromResult<PlanSheet?>(MapSheet(reader));
    }

    public Task RemoveAsync(Guid sheetId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            DELETE FROM plan_sheets
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", sheetId.ToString());
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlanSheet sheet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            UPDATE plan_sheets
            SET sheet_type = $sheet_type
            WHERE id = $id
            """);
        command.Parameters.AddWithValue("$id", sheet.Id.ToString());
        command.Parameters.AddWithValue("$sheet_type", (int)sheet.SheetType);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new InvalidOperationException("Plan sheet was not found.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
        IReadOnlyCollection<Guid> planSetVersionIds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (planSetVersionIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>>(
                new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>());
        }

        var ids = planSetVersionIds.ToArray();
        var parameterNames = ids.Select((_, index) => $"$id{index}").ToArray();
        using var command = CreateCommand(
            $"""
            SELECT id, plan_set_version_id, sheet_type, imported_document_id, measurement_context_id, name, status, created_at_utc
            FROM plan_sheets
            WHERE plan_set_version_id IN ({string.Join(", ", parameterNames)})
            ORDER BY created_at_utc ASC, name ASC
            """);

        for (var index = 0; index < ids.Length; index++)
        {
            command.Parameters.AddWithValue(parameterNames[index], ids[index].ToString());
        }

        using var reader = command.ExecuteReader();
        var mutable = new Dictionary<Guid, List<PlanSetSheetDto>>();
        while (reader.Read())
        {
            var sheet = MapSheet(reader);
            if (!mutable.TryGetValue(sheet.PlanSetVersionId, out var items))
            {
                items = [];
                mutable[sheet.PlanSetVersionId] = items;
            }

            items.Add(new PlanSetSheetDto(
                sheet.Id,
                sheet.SheetType.ToString(),
                sheet.Name,
                sheet.ImportedDocumentId,
                null,
                IsCanonical: false,
                "Unregistered",
                "NotProjected"));
        }

        return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>>(
            mutable.ToDictionary(item => item.Key, item => (IReadOnlyList<PlanSetSheetDto>)item.Value));
    }

    public Task<PlanSheetSourceDto?> GetBySheetIdAsync(Guid sheetId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT ps.id, ps.sheet_type, ps.name, ps.imported_document_id, d.storage_path
            FROM plan_sheets ps
            JOIN imported_documents d ON d.id = ps.imported_document_id
            WHERE ps.id = $id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$id", sheetId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<PlanSheetSourceDto?>(null);
        }

        return Task.FromResult<PlanSheetSourceDto?>(new PlanSheetSourceDto(
            Guid.Parse(reader.GetString(0)),
            ((PlanSheetType)reader.GetInt32(1)).ToString(),
            reader.GetString(2),
            Guid.Parse(reader.GetString(3)),
            reader.GetString(4)));
    }

    private static PlanSheet MapSheet(SqliteDataReader reader)
    {
        return new PlanSheet(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            (PlanSheetType)reader.GetInt32(2),
            Guid.Parse(reader.GetString(3)),
            Guid.Parse(reader.GetString(4)),
            reader.GetString(5),
            Enum.Parse<PlanSheetStatus>(reader.GetString(6)),
            DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
