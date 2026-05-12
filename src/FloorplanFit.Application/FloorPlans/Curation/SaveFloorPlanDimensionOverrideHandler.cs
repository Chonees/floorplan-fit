using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Curation;

public sealed class SaveFloorPlanDimensionOverrideHandler
{
    private readonly IFloorPlanCurationRepository curationRepository;
    private readonly IFloorPlanDimensionOverrideRepository dimensionOverrideRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IClock clock;

    public SaveFloorPlanDimensionOverrideHandler(
        IFloorPlanCurationRepository curationRepository,
        IFloorPlanDimensionOverrideRepository dimensionOverrideRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        this.curationRepository = curationRepository;
        this.dimensionOverrideRepository = dimensionOverrideRepository;
        this.unitOfWork = unitOfWork;
        this.clock = clock;
    }

    public async Task HandleAsync(Guid curationId, DimensionDto dimension, CancellationToken cancellationToken)
    {
        var curation = await curationRepository.GetByIdAsync(curationId, cancellationToken)
            ?? throw new InvalidOperationException("Floor plan curation was not found.");
        if (curation.Status != FloorPlanCurationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft curations can be edited.");
        }

        var sourceDimensionKey = ResolveSourceDimensionKey(dimension);
        await dimensionOverrideRepository.UpsertAsync(
            FloorPlanDimensionOverride.CreateManualSnapshot(
                curationId,
                sourceDimensionKey,
                dimension.SourceEntityRef,
                dimension.SourceHandle,
                dimension.DisplayText,
                dimension.DefPointX,
                dimension.DefPointY,
                dimension.DefPointZ,
                dimension.DefPoint2X,
                dimension.DefPoint2Y,
                dimension.DefPoint2Z,
                dimension.DefPoint3X,
                dimension.DefPoint3Y,
                dimension.DefPoint3Z,
                dimension.RenderTextX,
                dimension.RenderTextY,
                dimension.RenderTextHeight,
                dimension.RenderTextRotationDegrees,
                dimension.RenderTextStyleName,
                dimension.RenderTextHorizontalAlignment,
                dimension.RenderTextVerticalAlignment,
                dimension.RenderTextAttachmentPoint,
                dimension.LinePrimitives.Select(Map).ToArray(),
                dimension.TextPrimitives.Select(Map).ToArray(),
                dimension.InsertPrimitives.Select(Map).ToArray(),
                dimension.CirclePrimitives.Select(Map).ToArray(),
                dimension.ArcPrimitives.Select(Map).ToArray(),
                dimension.SolidPrimitives.Select(Map).ToArray(),
                clock.UtcNow,
                dimension.LastExportedAtUtc),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public static string ResolveSourceDimensionKey(DimensionDto dimension)
    {
        return !string.IsNullOrWhiteSpace(dimension.SourceHandle)
            ? dimension.SourceHandle
            : dimension.SourceEntityRef;
    }

    private static ExtractedDimensionLinePrimitive Map(DimensionLinePrimitiveDto primitive)
        => new(primitive.PrimitiveKey, primitive.SortOrder, primitive.StartX, primitive.StartY, primitive.EndX, primitive.EndY)
        {
            SourceHandle = primitive.SourceHandle,
            SourceLayer = primitive.SourceLayer
        };

    private static ExtractedDimensionTextPrimitive Map(DimensionTextPrimitiveDto primitive)
        => new(primitive.PrimitiveKey, primitive.SortOrder, primitive.Text, primitive.X, primitive.Y, primitive.Height, primitive.RotationDegrees)
        {
            SourceHandle = primitive.SourceHandle,
            SourceLayer = primitive.SourceLayer,
            StyleName = primitive.StyleName,
            HorizontalAlignment = primitive.HorizontalAlignment,
            VerticalAlignment = primitive.VerticalAlignment,
            AttachmentPoint = primitive.AttachmentPoint
        };

    private static ExtractedDimensionInsertPrimitive Map(DimensionInsertPrimitiveDto primitive)
        => new(primitive.PrimitiveKey, primitive.SortOrder, primitive.Name, primitive.X, primitive.Y, primitive.Z)
        {
            SourceHandle = primitive.SourceHandle,
            SourceLayer = primitive.SourceLayer,
            RotationDegrees = primitive.RotationDegrees,
            ScaleX = primitive.ScaleX,
            ScaleY = primitive.ScaleY,
            ScaleZ = primitive.ScaleZ
        };

    private static ExtractedDimensionCirclePrimitive Map(DimensionCirclePrimitiveDto primitive)
        => new(primitive.PrimitiveKey, primitive.SortOrder, primitive.CenterX, primitive.CenterY, primitive.Radius)
        {
            SourceHandle = primitive.SourceHandle,
            SourceLayer = primitive.SourceLayer
        };

    private static ExtractedDimensionArcPrimitive Map(DimensionArcPrimitiveDto primitive)
        => new(primitive.PrimitiveKey, primitive.SortOrder, primitive.CenterX, primitive.CenterY, primitive.Radius, primitive.StartAngleDegrees, primitive.EndAngleDegrees)
        {
            SourceHandle = primitive.SourceHandle,
            SourceLayer = primitive.SourceLayer
        };

    private static ExtractedDimensionSolidPrimitive Map(DimensionSolidPrimitiveDto primitive)
        => new(primitive.PrimitiveKey, primitive.SortOrder, primitive.Point1X, primitive.Point1Y, primitive.Point2X, primitive.Point2Y, primitive.Point3X, primitive.Point3Y, primitive.Point4X, primitive.Point4Y)
        {
            SourceHandle = primitive.SourceHandle,
            SourceLayer = primitive.SourceLayer
        };
}
