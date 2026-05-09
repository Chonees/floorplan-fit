using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class AddPinchMarkerHandler
{
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly IPinchGroupRepository pinchGroupRepository;
    private readonly IPinchMarkerRepository pinchMarkerRepository;
    private readonly IUnitOfWork unitOfWork;

    public AddPinchMarkerHandler(
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        IPinchGroupRepository pinchGroupRepository,
        IPinchMarkerRepository pinchMarkerRepository,
        IUnitOfWork unitOfWork)
    {
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.pinchGroupRepository = pinchGroupRepository;
        this.pinchMarkerRepository = pinchMarkerRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curationId,
        Guid sourceCandidateId,
        Guid pinchGroupId,
        decimal positionRatio,
        decimal maxTrimMm,
        CancellationToken cancellationToken)
    {
        var group = await pinchGroupRepository.GetByIdAsync(pinchGroupId, cancellationToken)
            ?? throw new InvalidOperationException("Pinch group was not found.");

        if (group.FloorPlanCurationId != curationId)
        {
            throw new InvalidOperationException("Pinch group does not belong to the active curation.");
        }

        var candidate = await extractedWallCandidateRepository.GetByIdAsync(sourceCandidateId, cancellationToken)
            ?? throw new InvalidOperationException("Wall candidate was not found.");

        if (candidate.GeometryPathId is null)
        {
            throw new InvalidOperationException("Selected candidate does not have geometry for pinch placement.");
        }

        var existingMarkers = await pinchMarkerRepository.ListByCurationAsync(curationId, cancellationToken);
        var marker = new PinchMarker(
            Guid.NewGuid(),
            curationId,
            group.Id,
            candidate.Id,
            candidate.GeometryPathId.Value,
            positionRatio,
            maxTrimMm,
            existingMarkers.Count + 1);

        await pinchMarkerRepository.AddAsync(marker, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
