using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface ICanonicalFloorPlanAdjustmentRepository
{
    Task AddAsync(CanonicalFloorPlanAdjustment adjustment, CancellationToken cancellationToken);
}
