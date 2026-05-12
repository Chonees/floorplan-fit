using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RestoreFloorPlanDimensionOverrideHandler
{
    private readonly IFloorPlanCurationRepository curationRepository;
    private readonly IFloorPlanDimensionOverrideRepository dimensionOverrideRepository;
    private readonly IUnitOfWork unitOfWork;

    public RestoreFloorPlanDimensionOverrideHandler(
        IFloorPlanCurationRepository curationRepository,
        IFloorPlanDimensionOverrideRepository dimensionOverrideRepository,
        IUnitOfWork unitOfWork)
    {
        this.curationRepository = curationRepository;
        this.dimensionOverrideRepository = dimensionOverrideRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid curationId, string sourceDimensionKey, CancellationToken cancellationToken)
    {
        var curation = await curationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }

        await dimensionOverrideRepository.DeleteAsync(curationId, sourceDimensionKey, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
