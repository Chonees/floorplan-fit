using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class UpdatePinchMarkerMaxTrimHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IPinchMarkerRepository pinchMarkerRepository;
    private readonly IUnitOfWork unitOfWork;

    public UpdatePinchMarkerMaxTrimHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IPinchMarkerRepository pinchMarkerRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.pinchMarkerRepository = pinchMarkerRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid pinchMarkerId,
        decimal maxTrimMm,
        CancellationToken cancellationToken)
    {
        if (maxTrimMm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTrimMm), "Max trim must be positive.");
        }

        var curation = await floorPlanCurationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can update pinch markers.");
        }

        var marker = await pinchMarkerRepository.GetByIdAsync(pinchMarkerId, cancellationToken)
            ?? throw new InvalidOperationException("Pinch marker was not found.");
        if (marker.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Pinch marker does not belong to the active curation.");
        }

        marker.UpdateMaxTrim(maxTrimMm);
        await pinchMarkerRepository.UpdateAsync(marker, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
