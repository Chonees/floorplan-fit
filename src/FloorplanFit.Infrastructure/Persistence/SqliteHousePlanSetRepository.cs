using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqliteHousePlanSetRepository : IHousePlanSetRepository
{
    private readonly SqliteSession session;

    public SqliteHousePlanSetRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(HousePlanSet housePlanSet, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            INSERT INTO house_plan_sets (
                id,
                source_floorplan_template_id,
                code,
                name,
                created_at_utc)
            VALUES (
                $id,
                $source_floorplan_template_id,
                $code,
                $name,
                $created_at_utc)
            """;
        command.Parameters.AddWithValue("$id", housePlanSet.Id.ToString());
        command.Parameters.AddWithValue("$source_floorplan_template_id", housePlanSet.SourceFloorPlanTemplateId.ToString());
        command.Parameters.AddWithValue("$code", housePlanSet.Code);
        command.Parameters.AddWithValue("$name", housePlanSet.Name);
        command.Parameters.AddWithValue("$created_at_utc", housePlanSet.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<HousePlanSet?> GetBySourceFloorPlanTemplateAsync(
        Guid sourceFloorPlanTemplateId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            SELECT id, source_floorplan_template_id, code, name, created_at_utc
            FROM house_plan_sets
            WHERE source_floorplan_template_id = $source_floorplan_template_id
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$source_floorplan_template_id", sourceFloorPlanTemplateId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<HousePlanSet?>(null);
        }

        return Task.FromResult<HousePlanSet?>(new HousePlanSet(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.GetString(3),
            DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
    }
}
