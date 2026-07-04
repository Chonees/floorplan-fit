using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets;

public sealed class PlanSetExportTests
{
    [Fact]
    public void Constructor_rejects_null_exported_sheets()
    {
        Assert.Throws<ArgumentNullException>(() => new PlanSetExport(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            PlanSetExportStatus.ReadyForExport,
            "{}",
            packageManifestPath: null,
            new DateTime(2026, 7, 1, 19, 0, 0, DateTimeKind.Utc),
            sheets: null!));
    }
}
