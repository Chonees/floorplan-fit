using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemoveMeasurementNodeHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IMeasurementNodeRepository measurementNodeRepository;
    private readonly IDimensionIntervalBindingRepository dimensionIntervalBindingRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveMeasurementNodeHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IMeasurementNodeRepository measurementNodeRepository,
        IDimensionIntervalBindingRepository dimensionIntervalBindingRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.measurementNodeRepository = measurementNodeRepository;
        this.dimensionIntervalBindingRepository = dimensionIntervalBindingRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid nodeId,
        CancellationToken cancellationToken)
    {
        var curation = await floorPlanCurationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != Domain.FloorPlans.FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can remove measurement nodes.");
        }

        var node = await measurementNodeRepository.GetByIdAsync(nodeId, cancellationToken)
            ?? throw new InvalidOperationException("Measurement node was not found.");
        if (node.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Measurement node does not belong to the active curation.");
        }

        await dimensionIntervalBindingRepository.DeleteByNodeAsync(curationId, nodeId, cancellationToken);
        await measurementNodeRepository.DeleteAsync(nodeId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
