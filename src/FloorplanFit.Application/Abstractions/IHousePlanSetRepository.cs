using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IHousePlanSetRepository
{
    Task<HousePlanSet?> GetBySourceFloorPlanTemplateAsync(
        Guid sourceFloorPlanTemplateId,
        CancellationToken cancellationToken);

    Task AddAsync(HousePlanSet housePlanSet, CancellationToken cancellationToken);
}
