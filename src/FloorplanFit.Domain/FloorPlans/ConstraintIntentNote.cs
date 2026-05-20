namespace FloorplanFit.Domain.FloorPlans;

public sealed class ConstraintIntentNote
{
    public ConstraintIntentNote(
        Guid id,
        string scopeType,
        Guid scopeId,
        string rawText,
        string status,
        DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(scopeType))
        {
            throw new ArgumentException("Scope type is required.", nameof(scopeType));
        }

        if (string.IsNullOrWhiteSpace(rawText))
        {
            throw new ArgumentException("Raw text is required.", nameof(rawText));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.", nameof(status));
        }

        Id = id;
        ScopeType = scopeType;
        ScopeId = scopeId;
        RawText = rawText;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public string ScopeType { get; }

    public Guid ScopeId { get; }

    public string RawText { get; }

    public string Status { get; }

    public DateTime CreatedAtUtc { get; }
}
