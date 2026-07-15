using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqlitePlanSetVersionRepository : IPlanSetVersionRepository
{
    private readonly SqliteSession session;

    public SqlitePlanSetVersionRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(PlanSetVersion version, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            INSERT INTO plan_set_versions (
                id,
                house_plan_set_id,
                canonical_floor_plan_version_id,
                version_number,
                created_at_utc)
            VALUES (
                $id,
                $house_plan_set_id,
                $canonical_floor_plan_version_id,
                $version_number,
                $created_at_utc)
            """;
        command.Parameters.AddWithValue("$id", version.Id.ToString());
        command.Parameters.AddWithValue("$house_plan_set_id", version.HousePlanSetId.ToString());
        command.Parameters.AddWithValue("$canonical_floor_plan_version_id", version.CanonicalFloorPlanVersionId.ToString());
        command.Parameters.AddWithValue("$version_number", version.VersionNumber);
        command.Parameters.AddWithValue("$created_at_utc", version.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<PlanSetVersion?> GetByIdAsync(
        Guid planSetVersionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            SELECT id, house_plan_set_id, canonical_floor_plan_version_id, version_number, created_at_utc
            FROM plan_set_versions
            WHERE id = $id
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$id", planSetVersionId.ToString());

        using var reader = command.ExecuteReader();
        return Task.FromResult(reader.Read() ? Map(reader) : null);
    }

    public Task<PlanSetVersion?> GetByCanonicalFloorPlanVersionAsync(
        Guid canonicalFloorPlanVersionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            SELECT id, house_plan_set_id, canonical_floor_plan_version_id, version_number, created_at_utc
            FROM plan_set_versions
            WHERE canonical_floor_plan_version_id = $canonical_floor_plan_version_id
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$canonical_floor_plan_version_id", canonicalFloorPlanVersionId.ToString());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<PlanSetVersion?>(null);
        }

        return Task.FromResult<PlanSetVersion?>(Map(reader));
    }

    private static PlanSetVersion Map(Microsoft.Data.Sqlite.SqliteDataReader reader)
        => new(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            Guid.Parse(reader.GetString(2)),
            reader.GetInt32(3),
            DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
}
