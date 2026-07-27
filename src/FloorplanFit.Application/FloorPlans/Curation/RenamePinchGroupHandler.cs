using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RenamePinchGroupHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IPinchGroupRepository pinchGroupRepository;
    private readonly IUnitOfWork unitOfWork;

    public RenamePinchGroupHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IPinchGroupRepository pinchGroupRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.pinchGroupRepository = pinchGroupRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid pinchGroupId,
        string name,
        CancellationToken cancellationToken)
    {
        var curation = await floorPlanCurationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can rename pinch groups.");
        }

        var group = await pinchGroupRepository.GetByIdAsync(pinchGroupId, cancellationToken)
            ?? throw new InvalidOperationException("Pinch group was not found.");
        if (group.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Pinch group does not belong to the active curation.");
        }

        // Carry the commissioned closing edge across the rename. UpdateAsync now writes
        // closing_edge, so rebuilding the aggregate without it would overwrite the operator's
        // choice with null on every rename, silently and without failing to compile.
        var updatedGroup = new PinchGroup(
            group.Id,
            group.FloorPlanCurationId,
            name,
            group.AxisTag,
            group.SortOrder,
            group.ClosingEdge);

        await pinchGroupRepository.UpdateAsync(updatedGroup, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
