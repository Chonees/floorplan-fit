using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RemoveOpeningCandidateHandler
{
    private readonly IExtractedOpeningCandidateRepository openingCandidateRepository;
    private readonly IUnitOfWork unitOfWork;

    public RemoveOpeningCandidateHandler(
        IExtractedOpeningCandidateRepository openingCandidateRepository,
        IUnitOfWork unitOfWork)
    {
        this.openingCandidateRepository = openingCandidateRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid openingCandidateId, CancellationToken cancellationToken)
    {
        await openingCandidateRepository.RemoveAsync(openingCandidateId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
