using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RestoreDimensionIntervalBindingHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IDimensionIntervalBindingRepository dimensionIntervalBindingRepository;
    private readonly IUnitOfWork unitOfWork;

    public RestoreDimensionIntervalBindingHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IDimensionIntervalBindingRepository dimensionIntervalBindingRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.dimensionIntervalBindingRepository = dimensionIntervalBindingRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid dimensionId,
        CancellationToken cancellationToken)
    {
        var curation = await floorPlanCurationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != Domain.FloorPlans.FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can restore interval bindings.");
        }

        await dimensionIntervalBindingRepository.DeleteAsync(curationId, dimensionId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
