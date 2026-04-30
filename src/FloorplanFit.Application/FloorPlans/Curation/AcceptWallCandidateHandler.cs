using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class AcceptWallCandidateHandler
{
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly ICuratedWallRepository curatedWallRepository;
    private readonly IUnitOfWork unitOfWork;

    public AcceptWallCandidateHandler(
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        ICuratedWallRepository curatedWallRepository,
        IUnitOfWork unitOfWork)
    {
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.curatedWallRepository = curatedWallRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(Guid curationId, Guid candidateId, string stableWallId, CancellationToken cancellationToken)
    {
        var candidate = await extractedWallCandidateRepository.GetByIdAsync(candidateId, cancellationToken)
            ?? throw new InvalidOperationException("Wall candidate was not found.");
        candidate.Accept();

        var wall = new CuratedWall(
            Guid.NewGuid(),
            curationId,
            stableWallId,
            candidate.Id,
            candidate.SourceEntityRef,
            candidate.GeometryPathId,
            WallRole.Partition,
            WallMobilityLevel.Flexible,
            WallProtectionLevel.None,
            candidate.ThicknessMm ?? 101.6m,
            "2x4",
            null,
            isExterior: false,
            isStructuralHint: false,
            wallGroupId: null,
            sortOrder: candidate.SortOrder,
            notes: null);

        await extractedWallCandidateRepository.UpdateAsync(candidate, cancellationToken);
        await curatedWallRepository.AddAsync(wall, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
