using FloorplanFit.Application.Abstractions;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class RejectWallCandidateHandler
{
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly IPinchMarkerRepository pinchMarkerRepository;
    private readonly IUnitOfWork unitOfWork;

    public RejectWallCandidateHandler(
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        IPinchMarkerRepository pinchMarkerRepository,
        IUnitOfWork unitOfWork)
    {
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.pinchMarkerRepository = pinchMarkerRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid curationId, Guid candidateId, CancellationToken cancellationToken)
    {
        var candidate = await extractedWallCandidateRepository.GetByIdAsync(candidateId, cancellationToken)
            ?? throw new InvalidOperationException("Wall candidate was not found.");
        candidate.Reject();

        await extractedWallCandidateRepository.UpdateAsync(candidate, cancellationToken);
        await pinchMarkerRepository.RemoveBySourceCandidateAsync(curationId, candidate.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
