using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class SaveDimensionIntervalBindingHandler
{
    private readonly IMeasurementCorridorRepository measurementCorridorRepository;
    private readonly IMeasurementNodeRepository measurementNodeRepository;
    private readonly IDimensionIntervalBindingRepository dimensionIntervalBindingRepository;
    private readonly IClock clock;
    private readonly IUnitOfWork unitOfWork;

    public SaveDimensionIntervalBindingHandler(
        IMeasurementCorridorRepository measurementCorridorRepository,
        IMeasurementNodeRepository measurementNodeRepository,
        IDimensionIntervalBindingRepository dimensionIntervalBindingRepository,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        this.measurementCorridorRepository = measurementCorridorRepository;
        this.measurementNodeRepository = measurementNodeRepository;
        this.dimensionIntervalBindingRepository = dimensionIntervalBindingRepository;
        this.clock = clock;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid dimensionId,
        Guid corridorId,
        Guid startNodeId,
        Guid endNodeId,
        CancellationToken cancellationToken)
    {
        var corridor = await measurementCorridorRepository.GetByIdAsync(corridorId, cancellationToken)
            ?? throw new InvalidOperationException("Measurement corridor was not found.");
        if (corridor.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Measurement corridor does not belong to the active curation.");
        }

        var startNode = await measurementNodeRepository.GetByIdAsync(startNodeId, cancellationToken)
            ?? throw new InvalidOperationException("Start measurement node was not found.");
        var endNode = await measurementNodeRepository.GetByIdAsync(endNodeId, cancellationToken)
            ?? throw new InvalidOperationException("End measurement node was not found.");

        if (startNode.CorridorId != corridorId || endNode.CorridorId != corridorId)
        {
            throw new InvalidOperationException("Measurement nodes must belong to the selected corridor.");
        }

        var binding = new DimensionIntervalBinding(
            curationId,
            dimensionId,
            corridorId,
            startNodeId,
            endNodeId,
            "ManualVerified",
            startNode.AxisCoordinate,
            endNode.AxisCoordinate,
            clock.UtcNow);

        await dimensionIntervalBindingRepository.UpsertAsync(binding, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
