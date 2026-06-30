namespace FloorplanFit.Domain.PlanSets;

public sealed class PlanSheet
{
    public PlanSheet(
        Guid id,
        Guid planSetVersionId,
        PlanSheetType sheetType,
        Guid importedDocumentId,
        Guid measurementContextId,
        string name,
        PlanSheetStatus status,
        DateTime createdAtUtc)
    {
        if (sheetType is PlanSheetType.Unknown)
        {
            throw new ArgumentException("A dependent plan sheet must have an explicit sheet type.", nameof(sheetType));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Sheet name is required.", nameof(name));
        }

        Id = id;
        PlanSetVersionId = planSetVersionId;
        SheetType = sheetType;
        ImportedDocumentId = importedDocumentId;
        MeasurementContextId = measurementContextId;
        Name = name;
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    public Guid PlanSetVersionId { get; }

    public PlanSheetType SheetType { get; }

    public Guid ImportedDocumentId { get; }

    public Guid MeasurementContextId { get; }

    public string Name { get; }

    public PlanSheetStatus Status { get; }

    public DateTime CreatedAtUtc { get; }
}
