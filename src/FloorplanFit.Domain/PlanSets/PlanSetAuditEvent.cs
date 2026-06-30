namespace FloorplanFit.Domain.PlanSets;

public sealed class PlanSetAuditEvent
{
    public PlanSetAuditEvent(
        Guid id,
        string aggregateType,
        Guid aggregateId,
        string eventType,
        string payloadJson,
        DateTime occurredAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Audit event id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(aggregateType))
        {
            throw new ArgumentException("Aggregate type is required.", nameof(aggregateType));
        }

        if (aggregateId == Guid.Empty)
        {
            throw new ArgumentException("Aggregate id is required.", nameof(aggregateId));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type is required.", nameof(eventType));
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ArgumentException("Payload JSON is required.", nameof(payloadJson));
        }

        Id = id;
        AggregateType = aggregateType.Trim();
        AggregateId = aggregateId;
        EventType = eventType.Trim();
        PayloadJson = payloadJson.Trim();
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; }

    public string AggregateType { get; }

    public Guid AggregateId { get; }

    public string EventType { get; }

    public string PayloadJson { get; }

    public DateTime OccurredAtUtc { get; }
}
