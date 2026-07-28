using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class SetPinchGroupClosingEdgeHandler
{
    private readonly IFloorPlanCurationRepository floorPlanCurationRepository;
    private readonly IPinchGroupRepository pinchGroupRepository;
    private readonly IUnitOfWork unitOfWork;

    public SetPinchGroupClosingEdgeHandler(
        IFloorPlanCurationRepository floorPlanCurationRepository,
        IPinchGroupRepository pinchGroupRepository,
        IUnitOfWork unitOfWork)
    {
        this.floorPlanCurationRepository = floorPlanCurationRepository;
        this.pinchGroupRepository = pinchGroupRepository;
        this.unitOfWork = unitOfWork;
    }

    // Groups created before the closing edge existed carry null, so this is the seam that lets an
    // operator commission one after the fact. The value is not validated here: the PinchGroup
    // constructor normalizes it per axis (Left/Right for Width, Top/Bottom for Height) and throws
    // ArgumentException otherwise. A second rule here would only be able to disagree with it.
    public async Task HandleAsync(
        Guid curationId,
        Guid pinchGroupId,
        string? closingEdge,
        CancellationToken cancellationToken)
    {
        var curation = await floorPlanCurationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can change pinch group closing edges.");
        }

        var group = await pinchGroupRepository.GetByIdAsync(pinchGroupId, cancellationToken)
            ?? throw new InvalidOperationException("Pinch group was not found.");
        if (group.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Pinch group does not belong to the active curation.");
        }

        // Every constructor parameter is accounted for. Closing edge is the one being replaced; the
        // other five are carried across verbatim, because UpdateAsync writes the whole row and a
        // dropped field would be silently overwritten with null without failing to compile.
        var updatedGroup = new PinchGroup(
            group.Id,
            group.FloorPlanCurationId,
            group.Name,
            group.AxisTag,
            group.SortOrder,
            closingEdge);

        await pinchGroupRepository.UpdateAsync(updatedGroup, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
