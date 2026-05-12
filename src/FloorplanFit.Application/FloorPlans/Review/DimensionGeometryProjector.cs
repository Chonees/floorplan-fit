using System.Globalization;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class DimensionGeometryProjector
{
    public static DimensionDto RebuildEditedDimension(
        DimensionDto editedDimension,
        DimensionDto primitiveTemplateDimension,
        bool recalculateMeasurement,
        bool markEdited,
        bool markDirty)
    {
        var startPoint = new Point2(editedDimension.DefPointX, editedDimension.DefPointY);
        var endPoint = new Point2(editedDimension.DefPoint2X, editedDimension.DefPoint2Y);
        var renderState = ResolveRenderState(editedDimension, startPoint, endPoint);
        return BuildDimension(
            editedDimension,
            primitiveTemplateDimension,
            startPoint,
            endPoint,
            renderState,
            recalculateMeasurement,
            markEdited,
            markDirty,
            generatedDisplayTextSource: "ManualDefinitionEdit");
    }

    public static DimensionDto RebuildAssociatedDimension(
        DimensionDto dimension,
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY)
    {
        var startPoint = new Point2(startX, startY);
        var endPoint = new Point2(endX, endY);
        var renderState = ResolveRenderState(dimension, startPoint, endPoint);
        return BuildDimension(
            dimension,
            dimension,
            startPoint,
            endPoint,
            renderState,
            recalculateMeasurement: true,
            markEdited: dimension.IsEdited,
            markDirty: dimension.IsDirty,
            generatedDisplayTextSource: "ReactiveAssociatedMeasurement");
    }

    private static DimensionDto BuildDimension(
        DimensionDto sourceDimension,
        DimensionDto primitiveTemplateDimension,
        Point2 startPoint,
        Point2 endPoint,
        RebuildRenderState renderState,
        bool recalculateMeasurement,
        bool markEdited,
        bool markDirty,
        string generatedDisplayTextSource)
    {
        var measurementSourceUnits = recalculateMeasurement
            ? RoundMeasurement(ComputeDistance(startPoint, endPoint))
            : sourceDimension.MeasurementSourceUnits;
        var measurementMillimeters = recalculateMeasurement
            ? RoundMeasurement(measurementSourceUnits * ResolveMeasurementFactor(sourceDimension))
            : sourceDimension.MeasurementMillimeters;
        var displayText = recalculateMeasurement
            ? ResolveDisplayText(sourceDimension, measurementSourceUnits)
            : sourceDimension.DisplayText;
        var displayTextSource = recalculateMeasurement
            ? ResolveDisplayTextSource(sourceDimension, generatedDisplayTextSource)
            : sourceDimension.DisplayTextSource;

        var updatedDimension = sourceDimension with
        {
            DefPointX = RoundModelValue(startPoint.X),
            DefPointY = RoundModelValue(startPoint.Y),
            DefPoint2X = RoundModelValue(endPoint.X),
            DefPoint2Y = RoundModelValue(endPoint.Y),
            DefPoint3X = RoundModelValue(renderState.DimensionLinePoint.X),
            DefPoint3Y = RoundModelValue(renderState.DimensionLinePoint.Y),
            RenderTextX = RoundModelValue(renderState.TextAnchor.X),
            RenderTextY = RoundModelValue(renderState.TextAnchor.Y),
            DisplayText = displayText,
            MeasurementSourceUnits = measurementSourceUnits,
            MeasurementMillimeters = measurementMillimeters,
            DisplayTextSource = displayTextSource,
            IsEdited = markEdited,
            IsDirty = markDirty
        };

        var lineTemplates = primitiveTemplateDimension.LinePrimitives.Count > 0
            ? primitiveTemplateDimension.LinePrimitives
            : [
                new DimensionLinePrimitiveDto("LINE-1", 1, updatedDimension.DefPointX, updatedDimension.DefPointY, updatedDimension.DefPointX, updatedDimension.DefPointY),
                new DimensionLinePrimitiveDto("LINE-2", 2, updatedDimension.DefPoint2X, updatedDimension.DefPoint2Y, updatedDimension.DefPoint2X, updatedDimension.DefPoint2Y),
                new DimensionLinePrimitiveDto("LINE-3", 3, updatedDimension.DefPointX, updatedDimension.DefPointY, updatedDimension.DefPoint2X, updatedDimension.DefPoint2Y)
            ];
        var updatedLines = lineTemplates
            .Select((template, index) => index switch
            {
                0 => template with
                {
                    StartX = RoundModelValue(renderState.DimensionEndpoint1.X),
                    StartY = RoundModelValue(renderState.DimensionEndpoint1.Y),
                    EndX = updatedDimension.DefPointX,
                    EndY = updatedDimension.DefPointY
                },
                1 => template with
                {
                    StartX = RoundModelValue(renderState.DimensionEndpoint2.X),
                    StartY = RoundModelValue(renderState.DimensionEndpoint2.Y),
                    EndX = updatedDimension.DefPoint2X,
                    EndY = updatedDimension.DefPoint2Y
                },
                2 => template with
                {
                    StartX = RoundModelValue(renderState.DimensionEndpoint1.X),
                    StartY = RoundModelValue(renderState.DimensionEndpoint1.Y),
                    EndX = RoundModelValue(renderState.DimensionEndpoint2.X),
                    EndY = RoundModelValue(renderState.DimensionEndpoint2.Y)
                },
                _ => template
            })
            .ToArray();

        var textTemplates = primitiveTemplateDimension.TextPrimitives.Count > 0
            ? primitiveTemplateDimension.TextPrimitives
            : [new DimensionTextPrimitiveDto("TEXT-1", 1, displayText, updatedDimension.RenderTextX ?? 0m, updatedDimension.RenderTextY ?? 0m, updatedDimension.RenderTextHeight ?? 3.5m, updatedDimension.RenderTextRotationDegrees ?? 0m)];
        var updatedText = textTemplates
            .Select(template => template with
            {
                Text = displayText,
                X = RoundModelValue(renderState.TextAnchor.X),
                Y = RoundModelValue(renderState.TextAnchor.Y),
                Height = updatedDimension.RenderTextHeight ?? template.Height,
                RotationDegrees = updatedDimension.RenderTextRotationDegrees ?? template.RotationDegrees,
                StyleName = updatedDimension.RenderTextStyleName ?? template.StyleName,
                HorizontalAlignment = updatedDimension.RenderTextHorizontalAlignment ?? template.HorizontalAlignment,
                VerticalAlignment = updatedDimension.RenderTextVerticalAlignment ?? template.VerticalAlignment,
                AttachmentPoint = updatedDimension.RenderTextAttachmentPoint ?? template.AttachmentPoint
            })
            .ToArray();

        var updatedInserts = primitiveTemplateDimension.InsertPrimitives
            .Select((template, index) => index switch
            {
                0 => template with
                {
                    X = RoundModelValue(renderState.DimensionEndpoint1.X),
                    Y = RoundModelValue(renderState.DimensionEndpoint1.Y)
                },
                1 => template with
                {
                    X = RoundModelValue(renderState.DimensionEndpoint2.X),
                    Y = RoundModelValue(renderState.DimensionEndpoint2.Y)
                },
                _ => template
            })
            .ToArray();

        return updatedDimension with
        {
            LinePrimitives = updatedLines,
            LineSegments = updatedLines
                .Select(item => new DimensionLineSegmentDto(item.StartX, item.StartY, item.EndX, item.EndY))
                .ToArray(),
            TextPrimitives = updatedText,
            InsertPrimitives = updatedInserts
        };
    }

    private static RebuildRenderState ResolveRenderState(DimensionDto dimension, Point2 newStart, Point2 newEnd)
    {
        var axis = ResolveAxis(dimension);
        var normal = new Vector2(-axis.Y, axis.X);
        var originalStart = new Point2(dimension.DefPointX, dimension.DefPointY);
        var originalEnd = new Point2(dimension.DefPoint2X, dimension.DefPoint2Y);
        var originalDimPoint = new Point2(dimension.DefPoint3X, dimension.DefPoint3Y);
        var originalOffset = ((originalDimPoint.X - originalStart.X) * normal.X) + ((originalDimPoint.Y - originalStart.Y) * normal.Y);

        var originalEndpoint1 = new Point2(originalStart.X + (normal.X * originalOffset), originalStart.Y + (normal.Y * originalOffset));
        var originalEndpoint2 = new Point2(originalEnd.X + (normal.X * originalOffset), originalEnd.Y + (normal.Y * originalOffset));
        var newEndpoint1 = new Point2(newStart.X + (normal.X * originalOffset), newStart.Y + (normal.Y * originalOffset));
        var newEndpoint2 = new Point2(newEnd.X + (normal.X * originalOffset), newEnd.Y + (normal.Y * originalOffset));
        var newDimPoint = new Point2(newStart.X + (normal.X * originalOffset), newStart.Y + (normal.Y * originalOffset));

        var originalMidpoint = new Point2((originalEndpoint1.X + originalEndpoint2.X) / 2m, (originalEndpoint1.Y + originalEndpoint2.Y) / 2m);
        var newMidpoint = new Point2((newEndpoint1.X + newEndpoint2.X) / 2m, (newEndpoint1.Y + newEndpoint2.Y) / 2m);
        var originalTextAnchor = dimension.RenderTextX is not null && dimension.RenderTextY is not null
            ? new Point2(dimension.RenderTextX.Value, dimension.RenderTextY.Value)
            : originalMidpoint;
        var textOffset = new Vector2(originalTextAnchor.X - originalMidpoint.X, originalTextAnchor.Y - originalMidpoint.Y);
        var newTextAnchor = new Point2(newMidpoint.X + textOffset.X, newMidpoint.Y + textOffset.Y);

        return new RebuildRenderState(newDimPoint, newEndpoint1, newEndpoint2, newTextAnchor);
    }

    private static string ResolveDisplayText(DimensionDto dimension, decimal measurementSourceUnits)
    {
        if (!string.IsNullOrWhiteSpace(dimension.RawTextOverride) &&
            !string.Equals(dimension.RawTextOverride.Trim(), "<>", StringComparison.Ordinal))
        {
            return dimension.DisplayText;
        }

        return FormatMeasurementFallback(measurementSourceUnits, dimension.SourceUnit);
    }

    private static string ResolveDisplayTextSource(DimensionDto dimension, string generatedDisplayTextSource)
    {
        return !string.IsNullOrWhiteSpace(dimension.RawTextOverride) &&
               !string.Equals(dimension.RawTextOverride.Trim(), "<>", StringComparison.Ordinal)
            ? dimension.DisplayTextSource
            : generatedDisplayTextSource;
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

    private static Vector2 ResolveAxis(DimensionDto dimension)
    {
        var dx = dimension.DefPoint2X - dimension.DefPointX;
        var dy = dimension.DefPoint2Y - dimension.DefPointY;
        var length = Math.Sqrt((double)((dx * dx) + (dy * dy)));
        if (length > double.Epsilon)
        {
            return new Vector2(decimal.CreateChecked(dx / (decimal)length), decimal.CreateChecked(dy / (decimal)length));
        }

        var angleRadians = (double)dimension.Angle * Math.PI / 180d;
        return new Vector2(decimal.CreateChecked(Math.Cos(angleRadians)), decimal.CreateChecked(Math.Sin(angleRadians)));
    }

    private static decimal ComputeDistance(Point2 start, Point2 end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return decimal.CreateChecked(Math.Sqrt((double)((dx * dx) + (dy * dy))));
    }

    private static string FormatMeasurementFallback(decimal measurement, string sourceUnit)
    {
        return string.Equals(sourceUnit, "Inch", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(sourceUnit, "Foot", StringComparison.OrdinalIgnoreCase)
            ? FormatArchitecturalInches(measurement)
            : measurement.ToString("0.###", CultureInfo.InvariantCulture);
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

    private static decimal RoundMeasurement(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundModelValue(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    private readonly record struct RebuildRenderState(
        Point2 DimensionLinePoint,
        Point2 DimensionEndpoint1,
        Point2 DimensionEndpoint2,
        Point2 TextAnchor);

    private readonly record struct Point2(decimal X, decimal Y);

    private readonly record struct Vector2(decimal X, decimal Y);
}
