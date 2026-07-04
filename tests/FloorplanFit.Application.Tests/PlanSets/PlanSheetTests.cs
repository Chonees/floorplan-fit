using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets;

public sealed class PlanSheetTests
{
    [Fact]
    public void Constructor_rejects_empty_identity_references()
    {
        var now = new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => Create(
            id: Guid.Empty,
            planSetVersionId: Guid.NewGuid(),
            importedDocumentId: Guid.NewGuid(),
            measurementContextId: Guid.NewGuid(),
            now));
        Assert.Throws<ArgumentException>(() => Create(
            id: Guid.NewGuid(),
            planSetVersionId: Guid.Empty,
            importedDocumentId: Guid.NewGuid(),
            measurementContextId: Guid.NewGuid(),
            now));
        Assert.Throws<ArgumentException>(() => Create(
            id: Guid.NewGuid(),
            planSetVersionId: Guid.NewGuid(),
            importedDocumentId: Guid.Empty,
            measurementContextId: Guid.NewGuid(),
            now));
        Assert.Throws<ArgumentException>(() => Create(
            id: Guid.NewGuid(),
            planSetVersionId: Guid.NewGuid(),
            importedDocumentId: Guid.NewGuid(),
            measurementContextId: Guid.Empty,
            now));
    }

    private static PlanSheet Create(
        Guid id,
        Guid planSetVersionId,
        Guid importedDocumentId,
        Guid measurementContextId,
        DateTime createdAtUtc)
        => new(
            id,
            planSetVersionId,
            PlanSheetType.ElectricalPlan,
            importedDocumentId,
            measurementContextId,
            "Electrical",
            PlanSheetStatus.Imported,
            createdAtUtc);
}
