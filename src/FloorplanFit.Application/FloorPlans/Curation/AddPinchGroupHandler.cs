using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class AddPinchGroupHandler
{
    private readonly IPinchGroupRepository pinchGroupRepository;
    private readonly IUnitOfWork unitOfWork;

    public AddPinchGroupHandler(IPinchGroupRepository pinchGroupRepository, IUnitOfWork unitOfWork)
    {
        this.pinchGroupRepository = pinchGroupRepository;
        this.unitOfWork = unitOfWork;
    }

    // closingEdge is trailing and optional so every existing call site keeps compiling. It carries the
    // operator's decision about which side of the house absorbs the reduction; the domain constructor
    // is the single authority that normalizes it per axis, so nothing is validated here.
    public async Task<Guid> HandleAsync(
        Guid curationId,
        string name,
        PinchAxisTag axisTag,
        CancellationToken cancellationToken,
        string? closingEdge = null)
    {
        var existingGroups = await pinchGroupRepository.ListByCurationAsync(curationId, cancellationToken);
        var group = new PinchGroup(
            Guid.NewGuid(),
            curationId,
            name,
            axisTag,
            existingGroups.Count + 1,
            closingEdge);

        await pinchGroupRepository.AddAsync(group, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return group.Id;
    }
}
