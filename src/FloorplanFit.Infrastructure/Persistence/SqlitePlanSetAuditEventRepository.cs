using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqlitePlanSetAuditEventRepository : IPlanSetAuditEventRepository, IPlanSetAuditEventReader
{
    private readonly SqliteSession session;

    public SqlitePlanSetAuditEventRepository(SqliteSession session)
    {
        this.session = session;
    }

    public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            INSERT INTO audit_events (
                id,
                aggregate_type,
                aggregate_id,
                event_type,
                payload_json,
                occurred_at_utc)
            VALUES (
                $id,
                $aggregate_type,
                $aggregate_id,
                $event_type,
                $payload_json,
                $occurred_at_utc)
            """;
        command.Parameters.AddWithValue("$id", auditEvent.Id.ToString());
        command.Parameters.AddWithValue("$aggregate_type", auditEvent.AggregateType);
        command.Parameters.AddWithValue("$aggregate_id", auditEvent.AggregateId.ToString());
        command.Parameters.AddWithValue("$event_type", auditEvent.EventType);
        command.Parameters.AddWithValue("$payload_json", auditEvent.PayloadJson);
        command.Parameters.AddWithValue("$occurred_at_utc", auditEvent.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PlanSetAuditEvent>> ListQualityEventsByPlanSetVersionAsync(
        Guid planSetVersionId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var command = session.Connection.CreateCommand();
        command.Transaction = session.Transaction;
        command.CommandText =
            """
            SELECT
                id,
                aggregate_type,
                aggregate_id,
                event_type,
                payload_json,
                occurred_at_utc
            FROM audit_events
            WHERE event_type IN (
                'SheetClassificationQualityMeasured',
                'SheetRegistrationQualityMeasured',
                'SheetAdjustmentProjectionQualityMeasured')
              AND payload_json LIKE $plan_set_version
            ORDER BY occurred_at_utc
            """;
        command.Parameters.AddWithValue("$plan_set_version", $"%{planSetVersionId}%");

        using var reader = command.ExecuteReader();
        var events = new List<PlanSetAuditEvent>();
        while (reader.Read())
        {
            events.Add(new PlanSetAuditEvent(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                Guid.Parse(reader.GetString(2)),
                reader.GetString(3),
                reader.GetString(4),
                DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        }

        return Task.FromResult<IReadOnlyList<PlanSetAuditEvent>>(events);
    }
}
