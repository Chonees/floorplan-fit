using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class AddMeasurementCorridorHandler
{
    private readonly IMeasurementCorridorRepository measurementCorridorRepository;
    private readonly IUnitOfWork unitOfWork;

    public AddMeasurementCorridorHandler(
        IMeasurementCorridorRepository measurementCorridorRepository,
        IUnitOfWork unitOfWork)
    {
        this.measurementCorridorRepository = measurementCorridorRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(
        Guid curationId,
        string name,
        PinchAxisTag axisTag,
        Guid guideGeometryPathId,
        decimal bandMinCoordinate,
        decimal bandMaxCoordinate,
        CancellationToken cancellationToken)
    {
        var existing = await measurementCorridorRepository.ListByCurationAsync(curationId, cancellationToken);
        var corridor = new MeasurementCorridor(
            Guid.NewGuid(),
            curationId,
            name,
            axisTag,
            guideGeometryPathId,
            bandMinCoordinate,
            bandMaxCoordinate,
            "Verified",
            existing.Count + 1);

        await measurementCorridorRepository.AddAsync(corridor, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return corridor.Id;
    }
}
