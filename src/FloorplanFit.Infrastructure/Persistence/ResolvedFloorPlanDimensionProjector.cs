using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Infrastructure.Persistence;

internal static class ResolvedFloorPlanDimensionProjector
{
    public static DimensionDto Resolve(DimensionDto dimension, FloorPlanDimensionOverride? dimensionOverride)
    {
        if (dimensionOverride is null)
        {
            return dimension with
            {
                IsEdited = false,
                IsDirty = false,
                LastExportedAtUtc = null
            };
        }

        var linePrimitives = dimensionOverride.LinePrimitives.Select(item => new DimensionLinePrimitiveDto(
            item.PrimitiveKey,
            item.SortOrder,
            item.StartX,
            item.StartY,
            item.EndX,
            item.EndY)
        {
            SourceHandle = item.SourceHandle,
            SourceLayer = item.SourceLayer
        }).ToArray();
        var textPrimitives = dimensionOverride.TextPrimitives.Select(item => new DimensionTextPrimitiveDto(
            item.PrimitiveKey,
            item.SortOrder,
            item.Text,
            item.X,
            item.Y,
            item.Height,
            item.RotationDegrees)
        {
            SourceHandle = item.SourceHandle,
            SourceLayer = item.SourceLayer,
            StyleName = item.StyleName,
            HorizontalAlignment = item.HorizontalAlignment,
            VerticalAlignment = item.VerticalAlignment,
            AttachmentPoint = item.AttachmentPoint
        }).ToArray();
        var insertPrimitives = dimensionOverride.InsertPrimitives.Select(item => new DimensionInsertPrimitiveDto(
            item.PrimitiveKey,
            item.SortOrder,
            item.Name,
            item.X,
            item.Y,
            item.Z)
        {
            SourceHandle = item.SourceHandle,
            SourceLayer = item.SourceLayer,
            RotationDegrees = item.RotationDegrees,
            ScaleX = item.ScaleX,
            ScaleY = item.ScaleY,
            ScaleZ = item.ScaleZ
        }).ToArray();
        var circlePrimitives = dimensionOverride.CirclePrimitives.Select(item => new DimensionCirclePrimitiveDto(
            item.PrimitiveKey,
            item.SortOrder,
            item.CenterX,
            item.CenterY,
            item.Radius)
        {
            SourceHandle = item.SourceHandle,
            SourceLayer = item.SourceLayer
        }).ToArray();
        var arcPrimitives = dimensionOverride.ArcPrimitives.Select(item => new DimensionArcPrimitiveDto(
            item.PrimitiveKey,
            item.SortOrder,
            item.CenterX,
            item.CenterY,
            item.Radius,
            item.StartAngleDegrees,
            item.EndAngleDegrees)
        {
            SourceHandle = item.SourceHandle,
            SourceLayer = item.SourceLayer
        }).ToArray();
        var solidPrimitives = dimensionOverride.SolidPrimitives.Select(item => new DimensionSolidPrimitiveDto(
            item.PrimitiveKey,
            item.SortOrder,
            item.Point1X,
            item.Point1Y,
            item.Point2X,
            item.Point2Y,
            item.Point3X,
            item.Point3Y,
            item.Point4X,
            item.Point4Y)
        {
            SourceHandle = item.SourceHandle,
            SourceLayer = item.SourceLayer
        }).ToArray();

        return dimension with
        {
            DisplayText = dimensionOverride.DisplayText,
            DefPointX = dimensionOverride.DefPointX,
            DefPointY = dimensionOverride.DefPointY,
            DefPointZ = dimensionOverride.DefPointZ,
            DefPoint2X = dimensionOverride.DefPoint2X,
            DefPoint2Y = dimensionOverride.DefPoint2Y,
            DefPoint2Z = dimensionOverride.DefPoint2Z,
            DefPoint3X = dimensionOverride.DefPoint3X,
            DefPoint3Y = dimensionOverride.DefPoint3Y,
            DefPoint3Z = dimensionOverride.DefPoint3Z,
            RenderTextX = dimensionOverride.RenderTextX,
            RenderTextY = dimensionOverride.RenderTextY,
            RenderTextHeight = dimensionOverride.RenderTextHeight,
            RenderTextRotationDegrees = dimensionOverride.RenderTextRotationDegrees,
            RenderTextStyleName = dimensionOverride.RenderTextStyleName,
            RenderTextHorizontalAlignment = dimensionOverride.RenderTextHorizontalAlignment,
            RenderTextVerticalAlignment = dimensionOverride.RenderTextVerticalAlignment,
            RenderTextAttachmentPoint = dimensionOverride.RenderTextAttachmentPoint,
            LinePrimitives = linePrimitives,
            TextPrimitives = textPrimitives,
            InsertPrimitives = insertPrimitives,
            CirclePrimitives = circlePrimitives,
            ArcPrimitives = arcPrimitives,
            SolidPrimitives = solidPrimitives,
            LineSegments = linePrimitives
                .Select(item => new DimensionLineSegmentDto(item.StartX, item.StartY, item.EndX, item.EndY))
                .ToArray(),
            IsEdited = true,
            IsDirty = dimensionOverride.IsDirty,
            LastExportedAtUtc = dimensionOverride.LastExportedAtUtc
        };
    }
}
