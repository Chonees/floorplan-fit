using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemoveMeasurementCorridorHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IMeasurementCorridorRepository measurementCorridorRepository;
    private readonly IMeasurementNodeRepository measurementNodeRepository;
    private readonly IDimensionIntervalBindingRepository dimensionIntervalBindingRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveMeasurementCorridorHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IMeasurementCorridorRepository measurementCorridorRepository,
        IMeasurementNodeRepository measurementNodeRepository,
        IDimensionIntervalBindingRepository dimensionIntervalBindingRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.measurementCorridorRepository = measurementCorridorRepository;
        this.measurementNodeRepository = measurementNodeRepository;
        this.dimensionIntervalBindingRepository = dimensionIntervalBindingRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid corridorId,
        CancellationToken cancellationToken)
    {
        var curation = await floorPlanCurationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != Domain.FloorPlans.FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can remove measurement corridors.");
        }

        var corridor = await measurementCorridorRepository.GetByIdAsync(corridorId, cancellationToken)
            ?? throw new InvalidOperationException("Measurement corridor was not found.");
        if (corridor.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Measurement corridor does not belong to the active curation.");
        }

        await dimensionIntervalBindingRepository.DeleteByCorridorAsync(curationId, corridorId, cancellationToken);
        await measurementNodeRepository.DeleteByCorridorAsync(corridorId, cancellationToken);
        await measurementCorridorRepository.DeleteAsync(corridorId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
