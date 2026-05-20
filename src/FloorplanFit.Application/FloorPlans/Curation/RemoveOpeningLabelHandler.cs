using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemoveOpeningLabelHandler
{
    private readonly IExtractedOpeningLabelRepository openingLabelRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveOpeningLabelHandler(
        IExtractedOpeningLabelRepository openingLabelRepository,
        IUnitOfWork unitOfWork)
    {
        this.openingLabelRepository = openingLabelRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid openingLabelId, CancellationToken cancellationToken)
    {
        await openingLabelRepository.RemoveAsync(openingLabelId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
