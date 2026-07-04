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
    private readonly IDimensionExtractor dimensionExtractor;
    private readonly IWallExtractionRunRepository wallExtractionRunRepository;
    private readonly IExtractedWallCandidateRepository extractedWallCandidateRepository;
    private readonly IExtractedRoomLabelRepository extractedRoomLabelRepository;
    private readonly IExtractedOpeningCandidateRepository extractedOpeningCandidateRepository;
    private readonly IExtractedOpeningLabelRepository extractedOpeningLabelRepository;
    private readonly IExtractedFixedPlanComponentRepository extractedFixedPlanComponentRepository;
    private readonly IExtractedProtectedDetailAssemblyRepository extractedProtectedDetailAssemblyRepository;
    private readonly IExtractedDimensionRepository extractedDimensionRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public ExtractWallCandidatesHandler(
        IWallExtractor wallExtractor,
        IRoomLabelExtractor roomLabelExtractor,
        IOpeningExtractor openingExtractor,
        IFixedPlanComponentExtractor fixedPlanComponentExtractor,
        IProtectedDetailAssemblyExtractor protectedDetailAssemblyExtractor,
        IDimensionExtractor dimensionExtractor,
        IWallExtractionRunRepository wallExtractionRunRepository,
        IExtractedWallCandidateRepository extractedWallCandidateRepository,
        IExtractedRoomLabelRepository extractedRoomLabelRepository,
        IExtractedOpeningCandidateRepository extractedOpeningCandidateRepository,
        IExtractedOpeningLabelRepository extractedOpeningLabelRepository,
        IExtractedFixedPlanComponentRepository extractedFixedPlanComponentRepository,
        IExtractedProtectedDetailAssemblyRepository extractedProtectedDetailAssemblyRepository,
        IExtractedDimensionRepository extractedDimensionRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.wallExtractor = wallExtractor;
        this.roomLabelExtractor = roomLabelExtractor;
        this.openingExtractor = openingExtractor;
        this.fixedPlanComponentExtractor = fixedPlanComponentExtractor;
        this.protectedDetailAssemblyExtractor = protectedDetailAssemblyExtractor;
        this.dimensionExtractor = dimensionExtractor;
        this.wallExtractionRunRepository = wallExtractionRunRepository;
        this.extractedWallCandidateRepository = extractedWallCandidateRepository;
        this.extractedRoomLabelRepository = extractedRoomLabelRepository;
        this.extractedOpeningCandidateRepository = extractedOpeningCandidateRepository;
        this.extractedOpeningLabelRepository = extractedOpeningLabelRepository;
        this.extractedFixedPlanComponentRepository = extractedFixedPlanComponentRepository;
        this.extractedProtectedDetailAssemblyRepository = extractedProtectedDetailAssemblyRepository;
        this.extractedDimensionRepository = extractedDimensionRepository;
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
        var detectedDimensions = await dimensionExtractor.ExtractAsync(managedFilePath, cancellationToken);
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
                ExtractedWallCandidateStatus.Accepted,
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
        var dimensions = detectedDimensions
            .Select((item, index) => new ExtractedDimension(
                Guid.NewGuid(),
                run.Id,
                item.SourceEntityRef,
                item.SourceLayer,
                item.SourceEntityKind,
                item.GeometryBlockName,
                item.DisplayText,
                item.DisplayTextSource,
                item.RawTextOverride,
                item.MeasurementSourceUnits,
                item.MeasurementMillimeters,
                item.SourceUnit,
                item.DimType,
                item.Angle,
                item.ObliqueAngle,
                item.DefPointX,
                item.DefPointY,
                item.DefPointZ,
                item.DefPoint2X,
                item.DefPoint2Y,
                item.DefPoint2Z,
                item.DefPoint3X,
                item.DefPoint3Y,
                item.DefPoint3Z,
                item.Confidence,
                item.DetectionNotes,
                sortOrder: index + 1,
                renderTextX: item.RenderTextX,
                renderTextY: item.RenderTextY,
                renderTextHeight: item.RenderTextHeight,
                renderTextRotationDegrees: item.RenderTextRotationDegrees,
                renderTextStyleName: item.RenderTextStyleName,
                renderTextHorizontalAlignment: item.RenderTextHorizontalAlignment,
                renderTextVerticalAlignment: item.RenderTextVerticalAlignment,
                renderTextAttachmentPoint: item.RenderTextAttachmentPoint,
                sourceHandle: item.SourceHandle,
                lineSegments: item.LineSegments
                    .Select(segment => new ExtractedDimensionLineSegment(segment.StartX, segment.StartY, segment.EndX, segment.EndY))
                    .ToArray(),
                linePrimitives: item.LinePrimitives
                    .Select(primitive => new ExtractedDimensionLinePrimitive(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.StartX,
                        primitive.StartY,
                        primitive.EndX,
                        primitive.EndY)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer
                    })
                    .ToArray(),
                textPrimitives: item.TextPrimitives
                    .Select(primitive => new ExtractedDimensionTextPrimitive(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.Text,
                        primitive.X,
                        primitive.Y,
                        primitive.Height,
                        primitive.RotationDegrees)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer,
                        StyleName = primitive.StyleName,
                        HorizontalAlignment = primitive.HorizontalAlignment,
                        VerticalAlignment = primitive.VerticalAlignment,
                        AttachmentPoint = primitive.AttachmentPoint
                    })
                    .ToArray(),
                insertPrimitives: item.InsertPrimitives
                    .Select(primitive => new ExtractedDimensionInsertPrimitive(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.Name,
                        primitive.X,
                        primitive.Y,
                        primitive.Z)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer,
                        RotationDegrees = primitive.RotationDegrees,
                        ScaleX = primitive.ScaleX,
                        ScaleY = primitive.ScaleY,
                        ScaleZ = primitive.ScaleZ
                    })
                    .ToArray(),
                circlePrimitives: item.CirclePrimitives
                    .Select(primitive => new ExtractedDimensionCirclePrimitive(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.CenterX,
                        primitive.CenterY,
                        primitive.Radius)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer
                    })
                    .ToArray(),
                arcPrimitives: item.ArcPrimitives
                    .Select(primitive => new ExtractedDimensionArcPrimitive(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.CenterX,
                        primitive.CenterY,
                        primitive.Radius,
                        primitive.StartAngleDegrees,
                        primitive.EndAngleDegrees)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer
                    })
                    .ToArray(),
                solidPrimitives: item.SolidPrimitives
                    .Select(primitive => new ExtractedDimensionSolidPrimitive(
                        primitive.PrimitiveKey,
                        primitive.SortOrder,
                        primitive.Point1X,
                        primitive.Point1Y,
                        primitive.Point2X,
                        primitive.Point2Y,
                        primitive.Point3X,
                        primitive.Point3Y,
                        primitive.Point4X,
                        primitive.Point4Y)
                    {
                        SourceHandle = primitive.SourceHandle,
                        SourceLayer = primitive.SourceLayer
                    })
                    .ToArray()))
            .ToArray();

        await wallExtractionRunRepository.AddAsync(run, cancellationToken);
        await extractedWallCandidateRepository.AddRangeAsync(domainCandidates, detectedCandidates, cancellationToken);
        await extractedRoomLabelRepository.AddRangeAsync(roomLabels, cancellationToken);
        await extractedOpeningCandidateRepository.AddRangeAsync(openingCandidates, detectedOpenings.Candidates, cancellationToken);
        await extractedOpeningLabelRepository.AddRangeAsync(openingLabels, cancellationToken);
        await extractedFixedPlanComponentRepository.AddRangeAsync(fixedComponents, detectedFixedComponents, cancellationToken);
        await extractedProtectedDetailAssemblyRepository.AddRangeAsync(protectedDetails, detectedProtectedDetails, cancellationToken);
        await extractedDimensionRepository.AddRangeAsync(dimensions, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return (run.Id, "Extracted");
    }
}
