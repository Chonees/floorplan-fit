using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.ExportAudit;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Export;

public sealed class ExportMultiSheetPlanSetPackageHandler
{
    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly IPlanSheetSourceReader planSheetSourceReader;
    private readonly ExportProjectedPlanSheetHandler projectedPlanSheetHandler;
    private readonly CreateMultiSheetExportAuditHandler exportAuditHandler;

    public ExportMultiSheetPlanSetPackageHandler(
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        IPlanSheetSourceReader planSheetSourceReader,
        ExportProjectedPlanSheetHandler projectedPlanSheetHandler,
        CreateMultiSheetExportAuditHandler exportAuditHandler)
    {
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.planSheetSourceReader = planSheetSourceReader;
        this.projectedPlanSheetHandler = projectedPlanSheetHandler;
        this.exportAuditHandler = exportAuditHandler;
    }

    public async Task<MultiSheetExportAuditDto> HandleAsync(
        ExportMultiSheetPlanSetPackageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.DependentProjectionIds);

        if (string.IsNullOrWhiteSpace(request.PackageDirectory))
        {
            throw new ArgumentException("Package directory is required.", nameof(request));
        }

        var exportableProjections = await ResolveExportableProjectionsAsync(request, cancellationToken);
        var exportedProjectionRequests = new List<MultiSheetExportProjectionRequestDto>(exportableProjections.Count);
        foreach (var projection in exportableProjections)
        {
            var sheetSource = await planSheetSourceReader.GetBySheetIdAsync(
                projection.DependentSheetId,
                cancellationToken);
            if (sheetSource is null)
            {
                throw new InvalidOperationException("Dependent sheet source was not found.");
            }

            var exported = await projectedPlanSheetHandler.HandleAsync(
                new ExportProjectedPlanSheetRequest(
                    projection.Id,
                    sheetSource.SourceFilePath,
                    BuildOutputPath(request.PackageDirectory, sheetSource.SourceFilePath, projection.Id)),
                cancellationToken);
            exportedProjectionRequests.Add(new MultiSheetExportProjectionRequestDto(
                exported.ProjectionId,
                exported.OutputFilePath));
        }

        return await exportAuditHandler.HandleAsync(
            new CreateMultiSheetExportAuditRequest(
                request.PlanSetVersionId,
                request.CanonicalFloorPlanVersionId,
                request.CanonicalAdjustmentId,
                request.CanonicalFloorPlanExportPath,
                exportedProjectionRequests)
            {
                DiscoverAllDependentSheets = request.DependentProjectionIds.Count == 0
            },
            cancellationToken);
    }

    private async Task<IReadOnlyList<SheetAdjustmentProjection>> ResolveExportableProjectionsAsync(
        ExportMultiSheetPlanSetPackageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DependentProjectionIds.Count == 0)
        {
            var projections = await sheetAdjustmentProjectionRepository.ListByPlanSetVersionAndCanonicalAdjustmentAsync(
                request.PlanSetVersionId,
                request.CanonicalAdjustmentId,
                cancellationToken);
            return projections
                .GroupBy(projection => projection.DependentSheetId)
                .Select(group => group.OrderBy(projection => projection.CreatedAtUtc).Last())
                .Where(projection => projection.Status is SheetAdjustmentProjectionStatus.ReadyForExport)
                .ToArray();
        }

        var selected = new List<SheetAdjustmentProjection>(request.DependentProjectionIds.Count);
        foreach (var projectionId in request.DependentProjectionIds)
        {
            var projection = await sheetAdjustmentProjectionRepository.GetByIdAsync(projectionId, cancellationToken);
            if (projection is null)
            {
                throw new InvalidOperationException("Sheet adjustment projection was not found.");
            }

            selected.Add(projection);
        }

        return selected;
    }

    private static string BuildOutputPath(string packageDirectory, string sourceFilePath, Guid projectionId)
    {
        var sourceName = Path.GetFileNameWithoutExtension(sourceFilePath);
        var safeName = string.IsNullOrWhiteSpace(sourceName) ? "sheet" : sourceName;
        return Path.Combine(packageDirectory, $"{safeName}-{projectionId:N}.dxf");
    }
}
