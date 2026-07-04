using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

public static class ChangedDimensionDetector
{
    public static IReadOnlyList<Guid> ResolveAffectedDimensionIds(
        IReadOnlyList<DimensionDto> baselineDimensions,
        IReadOnlyList<DimensionDto> adjustedDimensions)
    {
        ArgumentNullException.ThrowIfNull(baselineDimensions);
        ArgumentNullException.ThrowIfNull(adjustedDimensions);

        if (baselineDimensions.Count == 0 || adjustedDimensions.Count == 0)
        {
            return [];
        }

        var baselineById = baselineDimensions.ToDictionary(dimension => dimension.DimensionId);
        return adjustedDimensions
            .Where(dimension =>
                baselineById.TryGetValue(dimension.DimensionId, out var baseline) &&
                IsAffected(baseline, dimension))
            .Select(dimension => dimension.DimensionId)
            .ToArray();
    }

    private static bool IsAffected(DimensionDto baseline, DimensionDto adjusted)
        => !string.Equals(NormalizeVisibleText(baseline.DisplayText), NormalizeVisibleText(adjusted.DisplayText), StringComparison.Ordinal) ||
           !Equal(baseline.MeasurementSourceUnits, adjusted.MeasurementSourceUnits) ||
           !Equal(baseline.MeasurementMillimeters, adjusted.MeasurementMillimeters) ||
           !Equal(baseline.DefPointX, adjusted.DefPointX) ||
           !Equal(baseline.DefPointY, adjusted.DefPointY) ||
           !Equal(baseline.DefPoint2X, adjusted.DefPoint2X) ||
           !Equal(baseline.DefPoint2Y, adjusted.DefPoint2Y) ||
           !Equal(baseline.DefPoint3X, adjusted.DefPoint3X) ||
           !Equal(baseline.DefPoint3Y, adjusted.DefPoint3Y) ||
           !Equal(baseline.RenderTextX, adjusted.RenderTextX) ||
           !Equal(baseline.RenderTextY, adjusted.RenderTextY) ||
           !Equal(baseline.RenderTextHeight, adjusted.RenderTextHeight) ||
           !DimensionLineSegmentsEqual(baseline.LineSegments, adjusted.LineSegments) ||
           !DimensionLinePrimitivesEqual(baseline.LinePrimitives, adjusted.LinePrimitives) ||
           !DimensionTextPrimitivesEqual(baseline.TextPrimitives, adjusted.TextPrimitives) ||
           !DimensionInsertPrimitivesEqual(baseline.InsertPrimitives, adjusted.InsertPrimitives) ||
           !DimensionCirclePrimitivesEqual(baseline.CirclePrimitives, adjusted.CirclePrimitives) ||
           !DimensionArcPrimitivesEqual(baseline.ArcPrimitives, adjusted.ArcPrimitives) ||
           !DimensionSolidPrimitivesEqual(baseline.SolidPrimitives, adjusted.SolidPrimitives);

    private static bool DimensionLineSegmentsEqual(
        IReadOnlyList<DimensionLineSegmentDto> baseline,
        IReadOnlyList<DimensionLineSegmentDto> adjusted)
        => baseline.Count == adjusted.Count &&
           baseline.Zip(adjusted).All(pair =>
               Equal(pair.First.StartX, pair.Second.StartX) &&
               Equal(pair.First.StartY, pair.Second.StartY) &&
               Equal(pair.First.EndX, pair.Second.EndX) &&
               Equal(pair.First.EndY, pair.Second.EndY));

    private static bool DimensionLinePrimitivesEqual(
        IReadOnlyList<DimensionLinePrimitiveDto> baseline,
        IReadOnlyList<DimensionLinePrimitiveDto> adjusted)
        => baseline.Count == adjusted.Count &&
           baseline.Zip(adjusted).All(pair =>
               string.Equals(pair.First.PrimitiveKey, pair.Second.PrimitiveKey, StringComparison.Ordinal) &&
               pair.First.SortOrder == pair.Second.SortOrder &&
               Equal(pair.First.StartX, pair.Second.StartX) &&
               Equal(pair.First.StartY, pair.Second.StartY) &&
               Equal(pair.First.EndX, pair.Second.EndX) &&
               Equal(pair.First.EndY, pair.Second.EndY));

    private static bool DimensionTextPrimitivesEqual(
        IReadOnlyList<DimensionTextPrimitiveDto> baseline,
        IReadOnlyList<DimensionTextPrimitiveDto> adjusted)
        => baseline.Count == adjusted.Count &&
           baseline.Zip(adjusted).All(pair =>
               string.Equals(pair.First.PrimitiveKey, pair.Second.PrimitiveKey, StringComparison.Ordinal) &&
               pair.First.SortOrder == pair.Second.SortOrder &&
               string.Equals(pair.First.Text, pair.Second.Text, StringComparison.Ordinal) &&
               Equal(pair.First.X, pair.Second.X) &&
               Equal(pair.First.Y, pair.Second.Y) &&
               Equal(pair.First.Height, pair.Second.Height) &&
               Equal(pair.First.RotationDegrees, pair.Second.RotationDegrees));

    private static bool DimensionInsertPrimitivesEqual(
        IReadOnlyList<DimensionInsertPrimitiveDto> baseline,
        IReadOnlyList<DimensionInsertPrimitiveDto> adjusted)
        => baseline.Count == adjusted.Count &&
           baseline.Zip(adjusted).All(pair =>
               string.Equals(pair.First.PrimitiveKey, pair.Second.PrimitiveKey, StringComparison.Ordinal) &&
               pair.First.SortOrder == pair.Second.SortOrder &&
               string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) &&
               Equal(pair.First.X, pair.Second.X) &&
               Equal(pair.First.Y, pair.Second.Y) &&
               Equal(pair.First.RotationDegrees, pair.Second.RotationDegrees) &&
               Equal(pair.First.ScaleX, pair.Second.ScaleX) &&
               Equal(pair.First.ScaleY, pair.Second.ScaleY));

    private static bool DimensionCirclePrimitivesEqual(
        IReadOnlyList<DimensionCirclePrimitiveDto> baseline,
        IReadOnlyList<DimensionCirclePrimitiveDto> adjusted)
        => baseline.Count == adjusted.Count &&
           baseline.Zip(adjusted).All(pair =>
               string.Equals(pair.First.PrimitiveKey, pair.Second.PrimitiveKey, StringComparison.Ordinal) &&
               pair.First.SortOrder == pair.Second.SortOrder &&
               Equal(pair.First.CenterX, pair.Second.CenterX) &&
               Equal(pair.First.CenterY, pair.Second.CenterY) &&
               Equal(pair.First.Radius, pair.Second.Radius));

    private static bool DimensionArcPrimitivesEqual(
        IReadOnlyList<DimensionArcPrimitiveDto> baseline,
        IReadOnlyList<DimensionArcPrimitiveDto> adjusted)
        => baseline.Count == adjusted.Count &&
           baseline.Zip(adjusted).All(pair =>
               string.Equals(pair.First.PrimitiveKey, pair.Second.PrimitiveKey, StringComparison.Ordinal) &&
               pair.First.SortOrder == pair.Second.SortOrder &&
               Equal(pair.First.CenterX, pair.Second.CenterX) &&
               Equal(pair.First.CenterY, pair.Second.CenterY) &&
               Equal(pair.First.Radius, pair.Second.Radius) &&
               Equal(pair.First.StartAngleDegrees, pair.Second.StartAngleDegrees) &&
               Equal(pair.First.EndAngleDegrees, pair.Second.EndAngleDegrees));

    private static bool DimensionSolidPrimitivesEqual(
        IReadOnlyList<DimensionSolidPrimitiveDto> baseline,
        IReadOnlyList<DimensionSolidPrimitiveDto> adjusted)
        => baseline.Count == adjusted.Count &&
           baseline.Zip(adjusted).All(pair =>
               string.Equals(pair.First.PrimitiveKey, pair.Second.PrimitiveKey, StringComparison.Ordinal) &&
               pair.First.SortOrder == pair.Second.SortOrder &&
               Equal(pair.First.Point1X, pair.Second.Point1X) &&
               Equal(pair.First.Point1Y, pair.Second.Point1Y) &&
               Equal(pair.First.Point2X, pair.Second.Point2X) &&
               Equal(pair.First.Point2Y, pair.Second.Point2Y) &&
               Equal(pair.First.Point3X, pair.Second.Point3X) &&
               Equal(pair.First.Point3Y, pair.Second.Point3Y) &&
               Equal(pair.First.Point4X, pair.Second.Point4X) &&
               Equal(pair.First.Point4Y, pair.Second.Point4Y));

    private static bool Equal(decimal first, decimal second)
        => decimal.Round(first, 6, MidpointRounding.AwayFromZero) == decimal.Round(second, 6, MidpointRounding.AwayFromZero);

    private static bool Equal(decimal? first, decimal? second)
        => first.HasValue == second.HasValue &&
           (!first.HasValue || Equal(first.Value, second!.Value));

    private static string NormalizeVisibleText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim();
}
