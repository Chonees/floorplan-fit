namespace FloorplanFit.Domain.FloorPlans;

public sealed class StructuredConstraint
{
    public StructuredConstraint(
        Guid id,
        string scopeType,
        Guid scopeId,
        ConstraintKind constraintKind,
        ConstraintStrength constraintStrength,
        string targetType,
        Guid? targetId,
        decimal weight,
        string? payloadJson,
        string source,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(scopeType))
        {
            throw new ArgumentException("Scope type is required.", nameof(scopeType));
        }

        if (string.IsNullOrWhiteSpace(targetType))
        {
            throw new ArgumentException("Target type is required.", nameof(targetType));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source is required.", nameof(source));
        }

        Id = id;
        ScopeType = scopeType;
        ScopeId = scopeId;
        ConstraintKind = constraintKind;
        ConstraintStrength = constraintStrength;
        TargetType = targetType;
        TargetId = targetId;
        Weight = weight;
        PayloadJson = payloadJson;
        Source = source;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string ScopeType { get; }

    public Guid ScopeId { get; }

    public ConstraintKind ConstraintKind { get; }

    public ConstraintStrength ConstraintStrength { get; }

    public string TargetType { get; }

    public Guid? TargetId { get; }

    public decimal Weight { get; }

    public string? PayloadJson { get; }

    public string Source { get; }

    public DateTime CreatedAtUtc { get; }
}
