using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemovePinchGroupHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IPinchGroupRepository pinchGroupRepository;
    private readonly IPinchMarkerRepository pinchMarkerRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemovePinchGroupHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IPinchGroupRepository pinchGroupRepository,
        IPinchMarkerRepository pinchMarkerRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.pinchGroupRepository = pinchGroupRepository;
        this.pinchMarkerRepository = pinchMarkerRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid pinchGroupId,
        CancellationToken cancellationToken)
    {
        var curation = await floorPlanCurationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != Domain.FloorPlans.FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can remove pinch groups.");
        }

        var group = await pinchGroupRepository.GetByIdAsync(pinchGroupId, cancellationToken)
            ?? throw new InvalidOperationException("Pinch group was not found.");
        if (group.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Pinch group does not belong to the active curation.");
        }

        await pinchMarkerRepository.RemoveByGroupAsync(curationId, pinchGroupId, cancellationToken);
        await pinchGroupRepository.RemoveAsync(pinchGroupId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
