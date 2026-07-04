using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IProjectedPlanSheetExporter
{
    Task ExportAsync(
        string sourceFilePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
        CancellationToken cancellationToken);
}
