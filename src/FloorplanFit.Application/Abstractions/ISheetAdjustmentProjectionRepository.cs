using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface ISheetAdjustmentProjectionRepository
{
    Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken);

    Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken);

    Task UpdateAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAsync(
        Guid planSetVersionId,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();
}
