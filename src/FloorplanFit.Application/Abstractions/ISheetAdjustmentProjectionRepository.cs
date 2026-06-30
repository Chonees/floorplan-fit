using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface ISheetAdjustmentProjectionRepository
{
    Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken);

    Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        CancellationToken cancellationToken);
}
