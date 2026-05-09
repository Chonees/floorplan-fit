using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemovePinchMarkerHandler
{
    private readonly IPinchMarkerRepository pinchMarkerRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemovePinchMarkerHandler(IPinchMarkerRepository pinchMarkerRepository, IUnitOfWork unitOfWork)
    {
        this.pinchMarkerRepository = pinchMarkerRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid pinchMarkerId, CancellationToken cancellationToken)
    {
        await pinchMarkerRepository.RemoveAsync(pinchMarkerId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
