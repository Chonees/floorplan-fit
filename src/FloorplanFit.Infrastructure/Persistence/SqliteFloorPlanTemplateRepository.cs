using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteFloorPlanTemplateRepository : IFloorPlanTemplateRepository
{
    private readonly SqliteSession session;

    public SqliteFloorPlanTemplateRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            SELECT id, code, name, current_version_id, is_active
            FROM floorplan_templates
            WHERE code = $code
            LIMIT 1
            """);
        command.Parameters.AddWithValue("$code", code);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return Task.FromResult<FloorPlanTemplate?>(null);
        }

        var template = new FloorPlanTemplate(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetInt32(4) == 1);

        if (!reader.IsDBNull(3))
        {
            template.SetCurrentVersion(Guid.Parse(reader.GetString(3)));
        }

        return Task.FromResult<FloorPlanTemplate?>(template);
    }

    public Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            INSERT INTO floorplan_templates (
                id,
                code,
                name,
                current_version_id,
                is_active)
            VALUES (
                $id,
                $code,
                $name,
                $current_version_id,
                $is_active)
            """);

        command.Parameters.AddWithValue("$id", template.Id.ToString());
        command.Parameters.AddWithValue("$code", template.Code);
        command.Parameters.AddWithValue("$name", template.Name);
        command.Parameters.AddWithValue("$current_version_id", (object?)template.CurrentVersionId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$is_active", template.IsActive ? 1 : 0);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = CreateCommand(
            """
            UPDATE floorplan_templates
            SET current_version_id = $current_version_id,
                name = $name,
                is_active = $is_active
            WHERE id = $id
            """);

        command.Parameters.AddWithValue("$id", template.Id.ToString());
        command.Parameters.AddWithValue("$current_version_id", (object?)template.CurrentVersionId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$name", template.Name);
        command.Parameters.AddWithValue("$is_active", template.IsActive ? 1 : 0);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private SqliteCommand CreateCommand(string sql)
    {
        var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText = sql;
        return command;
    }
}
