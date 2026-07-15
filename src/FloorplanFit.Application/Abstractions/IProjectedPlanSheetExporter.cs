using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IProjectedPlanSheetExporter
{
    Task ExportAsync(
        string sourceFilePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
        CancellationToken cancellationToken);

    Task ExportAsync(
        string sourceFilePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
        => ExportAsync(sourceFilePath, outputFilePath, transform, cancellationToken);

    async Task<ProjectedPlanSheetExportAuditDto?> ExportWithAuditAsync(
        string sourceFilePath,
        string outputFilePath,
        SheetAdjustmentProjectionTransform transform,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
    {
        await ExportAsync(sourceFilePath, outputFilePath, transform, recipe, cancellationToken);
        return null;
    }
}
