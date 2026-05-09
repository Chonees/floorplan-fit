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

    public async Task<Guid> HandleAsync(
        Guid curationId,
        string name,
        PinchAxisTag axisTag,
        CancellationToken cancellationToken)
    {
        var existingGroups = await pinchGroupRepository.ListByCurationAsync(curationId, cancellationToken);
        var group = new PinchGroup(
            Guid.NewGuid(),
            curationId,
            name,
            axisTag,
            existingGroups.Count + 1);

        await pinchGroupRepository.AddAsync(group, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return group.Id;
    }
}
