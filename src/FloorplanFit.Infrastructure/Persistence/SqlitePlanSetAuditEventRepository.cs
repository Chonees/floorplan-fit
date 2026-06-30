using System.Globalization;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Infrastructure.Persistence;

public sealed class SqlitePlanSetAuditEventRepository : IPlanSetAuditEventRepository
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
}
