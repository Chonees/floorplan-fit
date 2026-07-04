using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Export;

public sealed class ExportProjectedPlanSheetHandler
{
    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly IProjectedPlanSheetExporter projectedPlanSheetExporter;

    public ExportProjectedPlanSheetHandler(
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        IProjectedPlanSheetExporter projectedPlanSheetExporter)
    {
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.projectedPlanSheetExporter = projectedPlanSheetExporter;
    }

    public async Task<ExportProjectedPlanSheetResponse> HandleAsync(
        ExportProjectedPlanSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProjectionId == Guid.Empty)
        {
            throw new ArgumentException("Projection id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SourceFilePath))
        {
            throw new ArgumentException("Source sheet path is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OutputFilePath))
        {
            throw new ArgumentException("Output sheet path is required.", nameof(request));
        }

        var projection = await sheetAdjustmentProjectionRepository.GetByIdAsync(
            request.ProjectionId,
            cancellationToken);
        if (projection is null)
        {
            throw new InvalidOperationException("Sheet adjustment projection was not found.");
        }

        if (projection.Status is not SheetAdjustmentProjectionStatus.ReadyForExport)
        {
            throw new InvalidOperationException("Sheet projection requires manual confirmation before export.");
        }

        await projectedPlanSheetExporter.ExportAsync(
            request.SourceFilePath,
            request.OutputFilePath,
            projection.Transform,
            cancellationToken);

        return new ExportProjectedPlanSheetResponse(projection.Id, request.OutputFilePath);
    }
}
