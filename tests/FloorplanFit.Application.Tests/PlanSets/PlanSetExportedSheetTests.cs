using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets;

public sealed class PlanSetExportedSheetTests
{
    [Fact]
    public void Constructor_rejects_projected_status_without_projection_id()
    {
        Assert.Throws<ArgumentException>(() => Create(PlanSetExportedSheetStatus.ProjectedAutomatically, sheetProjectionId: null));
        Assert.Throws<ArgumentException>(() => Create(PlanSetExportedSheetStatus.RequiresManualConfirmation, sheetProjectionId: null));
    }

    [Fact]
    public void Constructor_rejects_missing_projection_with_projection_data()
    {
        Assert.Throws<ArgumentException>(() => Create(PlanSetExportedSheetStatus.MissingProjection, sheetProjectionId: Guid.NewGuid(), storagePath: null));
        Assert.Throws<ArgumentException>(() => Create(PlanSetExportedSheetStatus.MissingProjection, sheetProjectionId: null, storagePath: "exports/roof.dxf"));
    }

    [Fact]
    public void Constructor_rejects_automatic_projection_without_output_path()
    {
        Assert.Throws<ArgumentException>(() => Create(
            PlanSetExportedSheetStatus.ProjectedAutomatically,
            sheetProjectionId: Guid.NewGuid(),
            storagePath: null));
    }

    [Fact]
    public void Constructor_rejects_manual_projection_with_output_path()
    {
        Assert.Throws<ArgumentException>(() => Create(
            PlanSetExportedSheetStatus.RequiresManualConfirmation,
            sheetProjectionId: Guid.NewGuid(),
            storagePath: "exports/manual.dxf"));
    }

    private static PlanSetExportedSheet Create(
        PlanSetExportedSheetStatus status,
        Guid? sheetProjectionId,
        string? storagePath = "exports/sheet.dxf")
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            sheetProjectionId,
            "ElectricalPlan",
            storagePath,
            status,
            projectionMethod: "ElectricalWholeSheetSimilarity",
            confidence: 0.9m,
            warning: null,
            ruleSummary: null);
}
