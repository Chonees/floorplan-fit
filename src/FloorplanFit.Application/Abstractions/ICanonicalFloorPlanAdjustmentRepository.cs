using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface ICanonicalFloorPlanAdjustmentRepository
{
    Task AddAsync(CanonicalFloorPlanAdjustment adjustment, CancellationToken cancellationToken);

    Task UpdateExportPathAsync(Guid adjustmentId, string finalPath, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    Task<CanonicalFloorPlanAdjustment?> GetByIdAsync(Guid adjustmentId, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
