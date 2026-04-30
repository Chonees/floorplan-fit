using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class UpdateCuratedWallMetadataHandler
{
    private readonly ICuratedWallRepository curatedWallRepository;
    private readonly IUnitOfWork unitOfWork;

    public UpdateCuratedWallMetadataHandler(
        ICuratedWallRepository curatedWallRepository,
        IUnitOfWork unitOfWork)
    {
        this.curatedWallRepository = curatedWallRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(
        Guid curatedWallId,
        WallRole wallRole,
        WallMobilityLevel mobilityLevel,
        WallProtectionLevel protectionLevel,
        decimal thicknessMm,
        string assemblyCode,
        decimal? heightMm,
        bool isExterior,
        bool isStructuralHint,
        string? notes,
        CancellationToken cancellationToken)
    {
        var wall = await curatedWallRepository.GetByIdAsync(curatedWallId, cancellationToken)
            ?? throw new InvalidOperationException("Curated wall was not found.");

        wall.UpdateMetadata(wallRole, mobilityLevel, protectionLevel, thicknessMm, assemblyCode, heightMm, isExterior, isStructuralHint, notes);

        await curatedWallRepository.UpdateAsync(wall, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
