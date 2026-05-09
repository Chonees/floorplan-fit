using FloorplanFit.Application.Abstractions;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Extraction;

public sealed class ExtractWallCandidatesHandler
{
    private readonly IWallExtractor wallExtractor;
    private readonly IRoomLabelExtractor roomLabelExtractor;
    private readonly IOpeningExtractor openingExtractor;
    private readonly IFixedPlanComponentExtractor fixedPlanComponentExtractor;
    private readonly IProtectedDetailAssemblyExtractor protectedDetailAssemblyExtractor;
    private readonly IWallExtractionRunRepository wallExtractionRunRepository;
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly IExtractedRoomLabelRepository extractedRoomLabelRepository;
    private readonly IExtractedOpeningCandidateRepository extractedOpeningCandidateRepository;
    private readonly IExtractedOpeningLabelRepository extractedOpeningLabelRepository;
    private readonly IExtractedFixedPlanComponentRepository extractedFixedPlanComponentRepository;
    private readonly IExtractedProtectedDetailAssemblyRepository extractedProtectedDetailAssemblyRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public ExtractWallCandidatesHandler(
        IWallExtractor wallExtractor,
        IRoomLabelExtractor roomLabelExtractor,
        IOpeningExtractor openingExtractor,
        IFixedPlanComponentExtractor fixedPlanComponentExtractor,
        IProtectedDetailAssemblyExtractor protectedDetailAssemblyExtractor,
        IWallExtractionRunRepository wallExtractionRunRepository,
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        IExtractedRoomLabelRepository extractedRoomLabelRepository,
        IExtractedOpeningCandidateRepository extractedOpeningCandidateRepository,
        IExtractedOpeningLabelRepository extractedOpeningLabelRepository,
        IExtractedFixedPlanComponentRepository extractedFixedPlanComponentRepository,
        IExtractedProtectedDetailAssemblyRepository extractedProtectedDetailAssemblyRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.wallExtractor = wallExtractor;
        this.roomLabelExtractor = roomLabelExtractor;
        this.openingExtractor = openingExtractor;
        this.fixedPlanComponentExtractor = fixedPlanComponentExtractor;
        this.protectedDetailAssemblyExtractor = protectedDetailAssemblyExtractor;
        this.wallExtractionRunRepository = wallExtractionRunRepository;
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.extractedRoomLabelRepository = extractedRoomLabelRepository;
        this.extractedOpeningCandidateRepository = extractedOpeningCandidateRepository;
        this.extractedOpeningLabelRepository = extractedOpeningLabelRepository;
        this.extractedFixedPlanComponentRepository = extractedFixedPlanComponentRepository;
        this.extractedProtectedDetailAssemblyRepository = extractedProtectedDetailAssemblyRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task<(Guid ExtractionRunId, string Status)> HandleAsync(
        Guid floorPlanVersionId,
        string managedFilePath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(managedFilePath);

        var startedAtUtc = clock.UtcNow;
        var run = new WallExtractionRun(
            Guid.NewGuid(),
            floorPlanVersionId,
            status: "Completed",
            startedAtUtc,
            finishedAtUtc: startedAtUtc,
            extractorVersion: "ixmilia-line-segments",
            errorMessage: null);

        var detectedCandidates = await wallExtractor.ExtractAsync(managedFilePath, cancellationToken);
        var detectedRoomLabels = await roomLabelExtractor.ExtractAsync(managedFilePath, cancellationToken);
        var detectedOpenings = await openingExtractor.ExtractAsync(managedFilePath, cancellationToken);
        var detectedFixedComponents = await fixedPlanComponentExtractor.ExtractAsync(managedFilePath, cancellationToken);
        var detectedProtectedDetails = await protectedDetailAssemblyExtractor.ExtractAsync(managedFilePath, cancellationToken);
        var domainCandidates = detectedCandidates
            .Select((item, index) => new ExtractedWallCandidate(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                geometryPathId: Guid.Empty,
                item.ThicknessMm,
                item.Confidence,
                item.DetectionNotes,
                ExtractedWallCandidateStatus.Pending,
                sortOrder: index + 1))
            .ToArray();
        var roomLabels = detectedRoomLabels
            .Select((item, index) => new ExtractedRoomLabel(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                item.Text,
                item.X,
                item.Y,
                item.Confidence,
                item.DetectionNotes,
                sortOrder: index + 1,
                item.SourceEntityKind,
                item.TextHeight,
                item.RotationDegrees,
                item.TextStyleName,
                item.HorizontalAlignment,
                item.VerticalAlignment,
                item.AttachmentPoint,
                item.ColorArgb))
            .ToArray();
        var openingCandidates = detectedOpenings.Candidates
            .Select((item, index) => new ExtractedOpeningCandidate(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                item.Kind,
                item.SourceEntityKind,
                geometryPathId: Guid.Empty,
                item.Confidence,
                item.DetectionNotes,
                sortOrder: index + 1))
            .ToArray();
        var openingLabels = detectedOpenings.Labels
            .Select((item, index) => new ExtractedOpeningLabel(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                item.Kind,
                item.Text,
                item.X,
                item.Y,
                item.Confidence,
                item.DetectionNotes,
                sortOrder: index + 1,
                item.SourceEntityKind,
                item.TextHeight,
                item.RotationDegrees,
                item.TextStyleName,
                item.HorizontalAlignment,
                item.VerticalAlignment,
                item.AttachmentPoint,
                item.ColorArgb))
            .ToArray();
        var fixedComponents = detectedFixedComponents
            .Select((item, index) => new ExtractedFixedPlanComponent(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                item.Kind,
                item.SourceEntityKind,
                item.SourceBlockName,
                item.Confidence,
                item.DetectionNotes,
                sortOrder: index + 1,
                colorArgb: item.ColorArgb))
            .ToArray();
        var protectedDetails = detectedProtectedDetails
            .Select((item, index) => new ExtractedProtectedDetailAssembly(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                item.Kind,
                item.SourceEntityKind,
                item.Confidence,
                item.DetectionNotes,
                sortOrder: index + 1,
                colorArgb: item.ColorArgb))
            .ToArray();

        await wallExtractionRunRepository.AddAsync(run, cancellationToken);
        await extractedWallCandidateRepository.AddRangeAsync(domainCandidates, detectedCandidates, cancellationToken);
        await extractedRoomLabelRepository.AddRangeAsync(roomLabels, cancellationToken);
        await extractedOpeningCandidateRepository.AddRangeAsync(openingCandidates, detectedOpenings.Candidates, cancellationToken);
        await extractedOpeningLabelRepository.AddRangeAsync(openingLabels, cancellationToken);
        await extractedFixedPlanComponentRepository.AddRangeAsync(fixedComponents, detectedFixedComponents, cancellationToken);
        await extractedProtectedDetailAssemblyRepository.AddRangeAsync(protectedDetails, detectedProtectedDetails, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (run.Id, "Extracted");
    }
}
