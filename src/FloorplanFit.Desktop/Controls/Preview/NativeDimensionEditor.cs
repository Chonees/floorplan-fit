using System.Globalization;
using Avalonia;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class NativeDimensionEditor
{
    private const decimal SpanEpsilon = 0.001m;
    private const decimal EndpointTolerance = 8m;

    public static DimensionDto ApplyHandleDelta(
        DimensionDto dimension,
        FloorPlanPreviewControl.DimensionHandleKind handleKind,
        decimal deltaX,
        decimal deltaY)
    {
        var layout = NativeDimensionShape.TryResolve(dimension);
        if (layout is null)
        {
            return ApplyFallbackHandleDelta(dimension, handleKind, deltaX, deltaY);
        }

        return handleKind switch
        {
            FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint =>
                ApplyEndpointDelta(dimension, layout.Value, moveStartExtent: true, deltaX, deltaY),
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint =>
                ApplyEndpointDelta(dimension, layout.Value, moveStartExtent: false, deltaX, deltaY),
            FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint =>
                ApplyBodyDelta(dimension, layout.Value, deltaX, deltaY),
            _ => dimension
        };
    }

    public static Point ResolveSnappedWorldPoint(
        Point worldPoint,
        CadViewportContext viewportContext,
        IReadOnlyList<Point> anchorPoints,
        bool enableGridSnap)
    {
        var snapTolerance = viewportContext.SnappingToleranceWorld;
        var gridSnapped = enableGridSnap
            ? new Point(
                SnapCoordinate(worldPoint.X, viewportContext.MajorGridSpacingWorld, snapTolerance),
                SnapCoordinate(worldPoint.Y, viewportContext.MajorGridSpacingWorld, snapTolerance))
            : worldPoint;
        if (Distance(gridSnapped, worldPoint) > double.Epsilon)
        {
            return gridSnapped;
        }

        var nearestAnchor = anchorPoints
            .Select(anchor => (Anchor: anchor, Distance: Distance(anchor, worldPoint)))
            .Where(item => item.Distance <= snapTolerance)
            .OrderBy(item => item.Distance)
            .Cast<(Point Anchor, double Distance)?>()
            .FirstOrDefault();
        return nearestAnchor is { } anchorMatch
            ? anchorMatch.Anchor
            : worldPoint;
    }

    public static IReadOnlyList<Point> ResolveSnapAnchors(
        DimensionDto dimension,
        FloorPlanPreviewControl.DimensionHandleKind activeHandleKind)
    {
        return ResolveHandles(dimension)
            .Where(item => item.HandleKind != activeHandleKind)
            .Select(item => item.WorldPoint)
            .ToArray();
    }

    public static IReadOnlyList<(FloorPlanPreviewControl.DimensionHandleKind HandleKind, Point WorldPoint)> ResolveHandles(DimensionDto dimension)
    {
        if (NativeDimensionShape.TryResolve(dimension) is { } layout)
        {
            return
            [
                (FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint, ToPoint(layout.StartExtent)),
                (FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint, ToPoint(layout.EndExtent))
            ];
        }

        return
        [
            (FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint, new Point((double)dimension.DefPointX, (double)dimension.DefPointY)),
            (FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint, new Point((double)dimension.DefPoint2X, (double)dimension.DefPoint2Y))
        ];
    }

    public static Point ResolveHandlePoint(
        DimensionDto dimension,
        FloorPlanPreviewControl.DimensionHandleKind handleKind)
    {
        if (NativeDimensionShape.TryResolve(dimension) is { } layout)
        {
            return handleKind switch
            {
                FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint => ToPoint(layout.StartExtent),
                FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint => ToPoint(layout.EndExtent),
                FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint => ResolveImplicitBodyControl(dimension),
                _ => ResolveImplicitBodyControl(dimension)
            };
        }

        return handleKind switch
        {
            FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint => new Point((double)dimension.DefPointX, (double)dimension.DefPointY),
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint => new Point((double)dimension.DefPoint2X, (double)dimension.DefPoint2Y),
            FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint => ResolveImplicitBodyControl(dimension),
            _ => ResolveImplicitBodyControl(dimension)
        };
    }

    public static Point ResolveImplicitBodyControl(DimensionDto dimension)
    {
        if (dimension.TextPrimitives.Count > 0)
        {
            var primaryText = dimension.TextPrimitives[0];
            return new Point((double)primaryText.X, (double)primaryText.Y);
        }

        if (dimension.RenderTextX is not null && dimension.RenderTextY is not null)
        {
            return new Point((double)dimension.RenderTextX.Value, (double)dimension.RenderTextY.Value);
        }

        if (NativeDimensionShape.TryResolve(dimension) is { } layout)
        {
            var midpoint = NativeDimensionShape.Add(
                layout.StartExtent,
                NativeDimensionShape.Scale(NativeDimensionShape.Subtract(layout.EndExtent, layout.StartExtent), 0.5m));
            return ToPoint(midpoint);
        }

        return CadTextPreviewLayerRenderer.ResolveDimensionTextAnchor(dimension);
    }

    internal static (decimal X, decimal Y) ResolveMeasureAxis(DimensionDto dimension)
    {
        if (NativeDimensionShape.TryResolve(dimension) is { } layout)
        {
            return (layout.Axis.X, layout.Axis.Y);
        }

        return ResolveFallbackMeasureAxis(dimension);
    }

    private static DimensionDto ApplyEndpointDelta(
        DimensionDto dimension,
        NativeDimensionShape.ResolvedLayout layout,
        bool moveStartExtent,
        decimal deltaX,
        decimal deltaY)
    {
        var axisScalar = Project(deltaX, deltaY, layout.Axis);
        var clampedScalar = ClampEndpointDelta(layout.SpanLength, moveStartExtent, axisScalar);
        return RebuildDimension(
            dimension,
            layout,
            point => TransformPointForEndpointDrag(layout, point, moveStartExtent, clampedScalar),
            recalculateMeasurement: true);
    }

    private static DimensionDto ApplyBodyDelta(
        DimensionDto dimension,
        NativeDimensionShape.ResolvedLayout layout,
        decimal deltaX,
        decimal deltaY)
    {
        var normalScalar = Project(deltaX, deltaY, layout.Normal);
        return RebuildDimension(
            dimension,
            layout,
            point => TransformPointForBodyDrag(layout, point, normalScalar),
            recalculateMeasurement: false);
    }

    private static DimensionDto ApplyFallbackHandleDelta(
        DimensionDto dimension,
        FloorPlanPreviewControl.DimensionHandleKind handleKind,
        decimal deltaX,
        decimal deltaY)
    {
        var axis = ResolveFallbackMeasureAxis(dimension);
        var normal = (X: -axis.Y, Y: axis.X);
        var axisScalar = Project(deltaX, deltaY, axis);
        var normalScalar = Project(deltaX, deltaY, normal);

        return handleKind switch
        {
            FloorPlanPreviewControl.DimensionHandleKind.FirstDefinitionPoint => dimension with
            {
                DefPointX = RoundModelValue(dimension.DefPointX + (axisScalar * axis.X)),
                DefPointY = RoundModelValue(dimension.DefPointY + (axisScalar * axis.Y)),
                IsEdited = true,
                IsDirty = true
            },
            FloorPlanPreviewControl.DimensionHandleKind.SecondDefinitionPoint => dimension with
            {
                DefPoint2X = RoundModelValue(dimension.DefPoint2X + (axisScalar * axis.X)),
                DefPoint2Y = RoundModelValue(dimension.DefPoint2Y + (axisScalar * axis.Y)),
                IsEdited = true,
                IsDirty = true
            },
            FloorPlanPreviewControl.DimensionHandleKind.DimensionLinePoint => dimension with
            {
                DefPoint3X = RoundModelValue(dimension.DefPoint3X + (normalScalar * normal.X)),
                DefPoint3Y = RoundModelValue(dimension.DefPoint3Y + (normalScalar * normal.Y)),
                RenderTextX = dimension.RenderTextX is null ? null : RoundModelValue(dimension.RenderTextX.Value + (normalScalar * normal.X)),
                RenderTextY = dimension.RenderTextY is null ? null : RoundModelValue(dimension.RenderTextY.Value + (normalScalar * normal.Y)),
                IsEdited = true,
                IsDirty = true
            },
            _ => dimension
        };
    }

    private static DimensionDto RebuildDimension(
        DimensionDto dimension,
        NativeDimensionShape.ResolvedLayout layout,
        Func<NativeDimensionShape.Vec2, NativeDimensionShape.Vec2> transform,
        bool recalculateMeasurement)
    {
        var measurementStart = transform(layout.RemoteStart ?? layout.StartExtent);
        var measurementEnd = transform(layout.RemoteEnd ?? layout.EndExtent);
        var dimensionLinePoint = transform(layout.StartExtent);
        var resolvedRenderText = transform(ResolveRenderTextPoint(dimension));

        var measurementSourceUnits = recalculateMeasurement
            ? RoundMeasurement(decimal.Abs(NativeDimensionShape.Dot(NativeDimensionShape.Subtract(measurementEnd, measurementStart), layout.Axis)))
            : dimension.MeasurementSourceUnits;
        var measurementMillimeters = recalculateMeasurement
            ? RoundMeasurement(measurementSourceUnits * ResolveMeasurementFactor(dimension))
            : dimension.MeasurementMillimeters;
        var displayText = recalculateMeasurement
            ? ResolveDisplayText(dimension, measurementSourceUnits)
            : dimension.DisplayText;
        var displayTextSource = recalculateMeasurement
            ? ResolveDisplayTextSource(dimension)
            : dimension.DisplayTextSource;

        var updatedLinePrimitives = TransformLinePrimitives(dimension.LinePrimitives, transform);
        var updatedTextPrimitives = TransformTextPrimitives(dimension.TextPrimitives, transform, displayText);
        var updatedInsertPrimitives = TransformInsertPrimitives(dimension.InsertPrimitives, transform);
        var updatedCirclePrimitives = TransformCirclePrimitives(dimension.CirclePrimitives, transform);
        var updatedArcPrimitives = TransformArcPrimitives(dimension.ArcPrimitives, transform);
        var updatedSolidPrimitives = TransformSolidPrimitives(dimension.SolidPrimitives, transform);

        return dimension with
        {
            DefPointX = RoundModelValue(measurementStart.X),
            DefPointY = RoundModelValue(measurementStart.Y),
            DefPoint2X = RoundModelValue(measurementEnd.X),
            DefPoint2Y = RoundModelValue(measurementEnd.Y),
            DefPoint3X = RoundModelValue(dimensionLinePoint.X),
            DefPoint3Y = RoundModelValue(dimensionLinePoint.Y),
            RenderTextX = RoundModelValue(resolvedRenderText.X),
            RenderTextY = RoundModelValue(resolvedRenderText.Y),
            DisplayText = displayText,
            DisplayTextSource = displayTextSource,
            MeasurementSourceUnits = measurementSourceUnits,
            MeasurementMillimeters = measurementMillimeters,
            LinePrimitives = updatedLinePrimitives,
            LineSegments = updatedLinePrimitives
                .Select(line => new DimensionLineSegmentDto(line.StartX, line.StartY, line.EndX, line.EndY))
                .ToArray(),
            TextPrimitives = updatedTextPrimitives,
            InsertPrimitives = updatedInsertPrimitives,
            CirclePrimitives = updatedCirclePrimitives,
            ArcPrimitives = updatedArcPrimitives,
            SolidPrimitives = updatedSolidPrimitives,
            IsEdited = true,
            IsDirty = true
        };
    }

    private static IReadOnlyList<DimensionLinePrimitiveDto> TransformLinePrimitives(
        IReadOnlyList<DimensionLinePrimitiveDto> primitives,
        Func<NativeDimensionShape.Vec2, NativeDimensionShape.Vec2> transform)
    {
        return primitives
            .Select(line =>
            {
                var start = transform(new NativeDimensionShape.Vec2(line.StartX, line.StartY));
                var end = transform(new NativeDimensionShape.Vec2(line.EndX, line.EndY));
                return line with
                {
                    StartX = RoundModelValue(start.X),
                    StartY = RoundModelValue(start.Y),
                    EndX = RoundModelValue(end.X),
                    EndY = RoundModelValue(end.Y)
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<DimensionTextPrimitiveDto> TransformTextPrimitives(
        IReadOnlyList<DimensionTextPrimitiveDto> primitives,
        Func<NativeDimensionShape.Vec2, NativeDimensionShape.Vec2> transform,
        string displayText)
    {
        return primitives
            .Select(text =>
            {
                var point = transform(new NativeDimensionShape.Vec2(text.X, text.Y));
                return text with
                {
                    Text = displayText,
                    X = RoundModelValue(point.X),
                    Y = RoundModelValue(point.Y)
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<DimensionInsertPrimitiveDto> TransformInsertPrimitives(
        IReadOnlyList<DimensionInsertPrimitiveDto> primitives,
        Func<NativeDimensionShape.Vec2, NativeDimensionShape.Vec2> transform)
    {
        return primitives
            .Select(insert =>
            {
                var point = transform(new NativeDimensionShape.Vec2(insert.X, insert.Y));
                return insert with
                {
                    X = RoundModelValue(point.X),
                    Y = RoundModelValue(point.Y)
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<DimensionCirclePrimitiveDto> TransformCirclePrimitives(
        IReadOnlyList<DimensionCirclePrimitiveDto> primitives,
        Func<NativeDimensionShape.Vec2, NativeDimensionShape.Vec2> transform)
    {
        return primitives
            .Select(circle =>
            {
                var center = transform(new NativeDimensionShape.Vec2(circle.CenterX, circle.CenterY));
                return circle with
                {
                    CenterX = RoundModelValue(center.X),
                    CenterY = RoundModelValue(center.Y)
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<DimensionArcPrimitiveDto> TransformArcPrimitives(
        IReadOnlyList<DimensionArcPrimitiveDto> primitives,
        Func<NativeDimensionShape.Vec2, NativeDimensionShape.Vec2> transform)
    {
        return primitives
            .Select(arc =>
            {
                var center = transform(new NativeDimensionShape.Vec2(arc.CenterX, arc.CenterY));
                return arc with
                {
                    CenterX = RoundModelValue(center.X),
                    CenterY = RoundModelValue(center.Y)
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<DimensionSolidPrimitiveDto> TransformSolidPrimitives(
        IReadOnlyList<DimensionSolidPrimitiveDto> primitives,
        Func<NativeDimensionShape.Vec2, NativeDimensionShape.Vec2> transform)
    {
        return primitives
            .Select(solid =>
            {
                var point1 = transform(new NativeDimensionShape.Vec2(solid.Point1X, solid.Point1Y));
                var point2 = transform(new NativeDimensionShape.Vec2(solid.Point2X, solid.Point2Y));
                var point3 = transform(new NativeDimensionShape.Vec2(solid.Point3X, solid.Point3Y));
                var point4 = transform(new NativeDimensionShape.Vec2(solid.Point4X, solid.Point4Y));
                return solid with
                {
                    Point1X = RoundModelValue(point1.X),
                    Point1Y = RoundModelValue(point1.Y),
                    Point2X = RoundModelValue(point2.X),
                    Point2Y = RoundModelValue(point2.Y),
                    Point3X = RoundModelValue(point3.X),
                    Point3Y = RoundModelValue(point3.Y),
                    Point4X = RoundModelValue(point4.X),
                    Point4Y = RoundModelValue(point4.Y)
                };
            })
            .ToArray();
    }

    private static NativeDimensionShape.Vec2 TransformPointForEndpointDrag(
        NativeDimensionShape.ResolvedLayout layout,
        NativeDimensionShape.Vec2 point,
        bool moveStartExtent,
        decimal deltaAlongAxis)
    {
        var local = ToLocal(layout, point);
        var newStart = moveStartExtent ? deltaAlongAxis : 0m;
        var newEnd = moveStartExtent ? layout.SpanLength : layout.SpanLength + deltaAlongAxis;
        var newSpan = Math.Max(SpanEpsilon, newEnd - newStart);

        decimal newU;
        if (local.U <= EndpointTolerance)
        {
            newU = moveStartExtent ? local.U + deltaAlongAxis : local.U;
        }
        else if (local.U >= layout.SpanLength - EndpointTolerance)
        {
            newU = moveStartExtent ? local.U : local.U + deltaAlongAxis;
        }
        else
        {
            var ratio = layout.SpanLength <= SpanEpsilon
                ? 0m
                : local.U / layout.SpanLength;
            newU = newStart + (ratio * newSpan);
        }

        return FromLocal(layout, newU, local.V);
    }

    private static NativeDimensionShape.Vec2 TransformPointForBodyDrag(
        NativeDimensionShape.ResolvedLayout layout,
        NativeDimensionShape.Vec2 point,
        decimal deltaAlongNormal)
    {
        if (IsRemoteAnchor(layout.RemoteStart, point) || IsRemoteAnchor(layout.RemoteEnd, point))
        {
            return point;
        }

        var local = ToLocal(layout, point);
        return FromLocal(layout, local.U, local.V + deltaAlongNormal);
    }

    private static bool IsRemoteAnchor(NativeDimensionShape.Vec2? remoteAnchor, NativeDimensionShape.Vec2 point)
    {
        return remoteAnchor is not null && NativeDimensionShape.ApproxEqual(remoteAnchor.Value, point);
    }

    private static decimal ClampEndpointDelta(decimal spanLength, bool moveStartExtent, decimal requestedDelta)
    {
        return moveStartExtent
            ? Math.Min(requestedDelta, spanLength - SpanEpsilon)
            : Math.Max(requestedDelta, SpanEpsilon - spanLength);
    }

    private static (decimal U, decimal V) ToLocal(
        NativeDimensionShape.ResolvedLayout layout,
        NativeDimensionShape.Vec2 point)
    {
        var relative = NativeDimensionShape.Subtract(point, layout.StartExtent);
        return (
            NativeDimensionShape.Dot(relative, layout.Axis),
            NativeDimensionShape.Dot(relative, layout.Normal));
    }

    private static NativeDimensionShape.Vec2 FromLocal(
        NativeDimensionShape.ResolvedLayout layout,
        decimal u,
        decimal v)
    {
        var axisOffset = NativeDimensionShape.Scale(layout.Axis, u);
        var normalOffset = NativeDimensionShape.Scale(layout.Normal, v);
        return NativeDimensionShape.Add(layout.StartExtent, NativeDimensionShape.Add(axisOffset, normalOffset));
    }

    private static NativeDimensionShape.Vec2 ResolveRenderTextPoint(DimensionDto dimension)
    {
        if (dimension.TextPrimitives.Count > 0)
        {
            var primaryText = dimension.TextPrimitives[0];
            return new NativeDimensionShape.Vec2(primaryText.X, primaryText.Y);
        }

        var anchor = CadTextPreviewLayerRenderer.ResolveDimensionTextAnchor(dimension);
        return new NativeDimensionShape.Vec2((decimal)anchor.X, (decimal)anchor.Y);
    }

    private static string ResolveDisplayText(DimensionDto dimension, decimal measurementSourceUnits)
    {
        if (!string.IsNullOrWhiteSpace(dimension.RawTextOverride) &&
            !string.Equals(dimension.RawTextOverride.Trim(), "<>", StringComparison.Ordinal))
        {
            return dimension.DisplayText;
        }

        return string.Equals(dimension.SourceUnit, "Inch", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(dimension.SourceUnit, "Foot", StringComparison.OrdinalIgnoreCase)
            ? FormatArchitecturalInches(measurementSourceUnits)
            : measurementSourceUnits.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string ResolveDisplayTextSource(DimensionDto dimension)
    {
        return !string.IsNullOrWhiteSpace(dimension.RawTextOverride) &&
               !string.Equals(dimension.RawTextOverride.Trim(), "<>", StringComparison.Ordinal)
            ? dimension.DisplayTextSource
            : "ManualDefinitionEdit";
    }

    private static decimal ResolveMeasurementFactor(DimensionDto dimension)
    {
        if (dimension.MeasurementSourceUnits > 0m)
        {
            return decimal.Round(dimension.MeasurementMillimeters / dimension.MeasurementSourceUnits, 6, MidpointRounding.AwayFromZero);
        }

        return string.Equals(dimension.SourceUnit, "Millimeter", StringComparison.OrdinalIgnoreCase)
            ? 1m
            : string.Equals(dimension.SourceUnit, "Centimeter", StringComparison.OrdinalIgnoreCase)
                ? 10m
                : string.Equals(dimension.SourceUnit, "Meter", StringComparison.OrdinalIgnoreCase)
                    ? 1000m
                    : string.Equals(dimension.SourceUnit, "Foot", StringComparison.OrdinalIgnoreCase)
                        ? 304.8m
                        : 25.4m;
    }

    private static string FormatArchitecturalInches(decimal totalInches)
    {
        var rounded = decimal.Round(totalInches, 0, MidpointRounding.AwayFromZero);
        var feet = decimal.ToInt32(decimal.Truncate(rounded / 12m));
        var inches = decimal.ToInt32(rounded % 12m);
        return feet > 0
            ? $"{feet}'-{inches}\""
            : $"{inches}\"";
    }

    private static (decimal X, decimal Y) ResolveFallbackMeasureAxis(DimensionDto dimension)
    {
        var dx = (double)(dimension.DefPoint2X - dimension.DefPointX);
        var dy = (double)(dimension.DefPoint2Y - dimension.DefPointY);
        var length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length > double.Epsilon)
        {
            return ((decimal)(dx / length), (decimal)(dy / length));
        }

        var angleRadians = (double)dimension.Angle * Math.PI / 180d;
        return ((decimal)Math.Cos(angleRadians), (decimal)Math.Sin(angleRadians));
    }

    private static decimal Project(decimal deltaX, decimal deltaY, (decimal X, decimal Y) axis)
    {
        return RoundModelValue((deltaX * axis.X) + (deltaY * axis.Y));
    }

    private static decimal Project(decimal deltaX, decimal deltaY, NativeDimensionShape.Vec2 axis)
    {
        return RoundModelValue((deltaX * axis.X) + (deltaY * axis.Y));
    }

    private static Point ToPoint(NativeDimensionShape.Vec2 point)
    {
        return new Point((double)point.X, (double)point.Y);
    }

    private static double SnapCoordinate(double coordinate, double spacing, double tolerance)
    {
        if (spacing <= double.Epsilon)
        {
            return coordinate;
        }

        var snapped = Math.Round(coordinate / spacing) * spacing;
        return Math.Abs(snapped - coordinate) <= tolerance
            ? snapped
            : coordinate;
    }

    private static double Distance(Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static decimal RoundMeasurement(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundModelValue(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
