using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemoveRoomLabelHandler
{
    private readonly IExtractedRoomLabelRepository extractedRoomLabelRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveRoomLabelHandler(
        IExtractedRoomLabelRepository extractedRoomLabelRepository,
        IUnitOfWork unitOfWork)
    {
        this.extractedRoomLabelRepository = extractedRoomLabelRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid roomLabelId, CancellationToken cancellationToken)
    {
        await extractedRoomLabelRepository.RemoveAsync(roomLabelId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
