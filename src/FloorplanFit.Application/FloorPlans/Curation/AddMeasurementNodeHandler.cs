using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class AddMeasurementNodeHandler
{
    private readonly IMeasurementCorridorRepository measurementCorridorRepository;
    private readonly IMeasurementNodeRepository measurementNodeRepository;
    private readonly IUnitOfWork unitOfWork;

    public AddMeasurementNodeHandler(
        IMeasurementCorridorRepository measurementCorridorRepository,
        IMeasurementNodeRepository measurementNodeRepository,
        IUnitOfWork unitOfWork)
    {
        this.measurementCorridorRepository = measurementCorridorRepository;
        this.measurementNodeRepository = measurementNodeRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(
        Guid curationId,
        Guid corridorId,
        string referenceKind,
        string sourceArtifactKind,
        Guid sourceArtifactId,
        Guid geometryPathId,
        string snapKind,
        decimal anchorX,
        decimal anchorY,
        decimal axisCoordinate,
        decimal offsetAlongAxis,
        decimal offsetNormal,
        decimal positionRatio,
        CancellationToken cancellationToken)
    {
        var corridor = await measurementCorridorRepository.GetByIdAsync(corridorId, cancellationToken)
            ?? throw new InvalidOperationException("Measurement corridor was not found.");
        if (corridor.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Measurement corridor does not belong to the active curation.");
        }

        var existing = await measurementNodeRepository.ListByCorridorAsync(corridorId, cancellationToken);
        if (existing.Count >= 2)
        {
            throw new InvalidOperationException("A measurement corridor can have at most two nodes.");
        }

        var node = new MeasurementNode(
            Guid.NewGuid(),
            curationId,
            corridorId,
            existing.Count + 1,
            referenceKind,
            sourceArtifactKind,
            sourceArtifactId,
            geometryPathId,
            snapKind,
            anchorX,
            anchorY,
            axisCoordinate,
            offsetAlongAxis,
            offsetNormal,
            positionRatio);

        await measurementNodeRepository.AddAsync(node, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return node.Id;
    }
}
