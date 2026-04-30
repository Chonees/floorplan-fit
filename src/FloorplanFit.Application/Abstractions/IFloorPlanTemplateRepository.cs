using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanTemplateRepository
{
    Task<FloorPlanTemplate?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(FloorPlanTemplate template, CancellationToken cancellationToken);

    Task UpdateAsync(FloorPlanTemplate template, CancellationToken cancellationToken);
}
