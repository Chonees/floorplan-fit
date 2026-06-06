using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class DimensionGeometryProjector
{
    private const string OrdinateYBindingKind = "OrdinateY";

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
        var liveStartPoint = new Point2(startX, startY);
        var liveEndPoint = new Point2(endX, endY);
        return BuildStylePreservingAssociatedDimension(dimension, liveStartPoint, liveEndPoint);
    }

    public static DimensionDto RebuildAssociatedDimensionFromAnchorDeltas(
        DimensionDto dimension,
        decimal authoredStartX,
        decimal authoredStartY,
        decimal liveStartX,
        decimal liveStartY,
        decimal authoredEndX,
        decimal authoredEndY,
        decimal liveEndX,
        decimal liveEndY)
    {
        var authoredStartPoint = new Point2(authoredStartX, authoredStartY);
        var liveStartPoint = new Point2(liveStartX, liveStartY);
        var authoredEndPoint = new Point2(authoredEndX, authoredEndY);
        var liveEndPoint = new Point2(liveEndX, liveEndY);
        var transform = ResolveStylePreservingLinearTransformFromAnchorDeltas(
            dimension,
            authoredStartPoint,
            liveStartPoint,
            authoredEndPoint,
            liveEndPoint);

        return BuildStylePreservingAssociatedDimension(dimension, transform);
    }

    public static DimensionDto TranslateAssociatedDimensionFromAnchorDeltas(
        DimensionDto dimension,
        decimal authoredStartX,
        decimal authoredStartY,
        decimal liveStartX,
        decimal liveStartY,
        decimal authoredEndX,
        decimal authoredEndY,
        decimal liveEndX,
        decimal liveEndY,
        string activeAxisTag)
    {
        var translation = ResolveAverageActiveAxisTranslation(
            authoredStartX,
            authoredStartY,
            liveStartX,
            liveStartY,
            authoredEndX,
            authoredEndY,
            liveEndX,
            liveEndY,
            activeAxisTag);

        return TranslateAssociatedDimension(dimension, translation);
    }

    private static DimensionDto BuildStylePreservingAssociatedDimension(
        DimensionDto dimension,
        Point2 liveStartPoint,
        Point2 liveEndPoint)
    {
        var transform = ResolveStylePreservingLinearTransform(dimension, liveStartPoint, liveEndPoint);
        return BuildStylePreservingAssociatedDimension(dimension, transform);
    }

    private static DimensionDto BuildStylePreservingAssociatedDimension(
        DimensionDto dimension,
        StylePreservingLinearTransform transform)
    {
        var originalStart = new Point2(dimension.DefPointX, dimension.DefPointY);
        var originalEnd = new Point2(dimension.DefPoint2X, dimension.DefPoint2Y);
        var originalDimensionLinePoint = new Point2(dimension.DefPoint3X, dimension.DefPoint3Y);
        var updatedStart = ApplyStylePreservingLinearTransform(originalStart, transform);
        var updatedEnd = ApplyStylePreservingLinearTransform(originalEnd, transform);
        var updatedDimensionLinePoint = ApplyStylePreservingLinearTransform(originalDimensionLinePoint, transform);
        var updatedRenderTextPoint = ResolveStylePreservingRenderTextPoint(dimension, transform);
        var measurementSourceUnits = RoundMeasurement(decimal.Abs(transform.NewEndU - transform.NewStartU));
        var measurementMillimeters = RoundMeasurement(measurementSourceUnits * ResolveMeasurementFactor(dimension));
        var displayText = DimensionDisplayTextFormatter.Resolve(
            dimension,
            measurementSourceUnits,
            "ReactiveAssociatedMeasurement");
        var updatedLines = dimension.LinePrimitives
            .Select(item => TransformLinePrimitive(item, transform))
            .ToArray();
        var updatedLineSegments = updatedLines.Length > 0
            ? updatedLines
                .Select(item => new DimensionLineSegmentDto(item.StartX, item.StartY, item.EndX, item.EndY))
                .ToArray()
            : dimension.LineSegments
                .Select(item => TransformLineSegment(item, transform))
                .ToArray();
        var updatedText = dimension.TextPrimitives
            .Select(item => TransformTextPrimitive(item, transform, displayText.DisplayText, dimension))
            .ToArray();
        var updatedInserts = dimension.InsertPrimitives
            .Select(item => TransformInsertPrimitive(item, transform))
            .ToArray();
        var updatedCircles = dimension.CirclePrimitives
            .Select(item => TransformCirclePrimitive(item, transform))
            .ToArray();
        var updatedArcs = dimension.ArcPrimitives
            .Select(item => TransformArcPrimitive(item, transform))
            .ToArray();
        var updatedSolids = dimension.SolidPrimitives
            .Select(item => TransformSolidPrimitive(item, transform))
            .ToArray();

        return dimension with
        {
            DefPointX = RoundModelValue(updatedStart.X),
            DefPointY = RoundModelValue(updatedStart.Y),
            DefPoint2X = RoundModelValue(updatedEnd.X),
            DefPoint2Y = RoundModelValue(updatedEnd.Y),
            DefPoint3X = RoundModelValue(updatedDimensionLinePoint.X),
            DefPoint3Y = RoundModelValue(updatedDimensionLinePoint.Y),
            RenderTextX = updatedRenderTextPoint?.X is decimal renderTextX ? RoundModelValue(renderTextX) : dimension.RenderTextX,
            RenderTextY = updatedRenderTextPoint?.Y is decimal renderTextY ? RoundModelValue(renderTextY) : dimension.RenderTextY,
            DisplayText = displayText.DisplayText,
            MeasurementSourceUnits = measurementSourceUnits,
            MeasurementMillimeters = measurementMillimeters,
            DisplayTextSource = displayText.DisplayTextSource,
            LinePrimitives = updatedLines,
            LineSegments = updatedLineSegments,
            TextPrimitives = updatedText,
            InsertPrimitives = updatedInserts,
            CirclePrimitives = updatedCircles,
            ArcPrimitives = updatedArcs,
            SolidPrimitives = updatedSolids,
            IsEdited = dimension.IsEdited,
            IsDirty = dimension.IsDirty
        };
    }

    private static DimensionDto TranslateAssociatedDimension(DimensionDto dimension, Vector2 delta)
    {
        if (delta.X == 0m && delta.Y == 0m)
        {
            return dimension;
        }

        var updatedLines = dimension.LinePrimitives
            .Select(item => TranslateLinePrimitive(item, delta))
            .ToArray();
        var updatedLineSegments = updatedLines.Length > 0
            ? updatedLines
                .Select(item => new DimensionLineSegmentDto(item.StartX, item.StartY, item.EndX, item.EndY))
                .ToArray()
            : dimension.LineSegments
                .Select(item => TranslateLineSegment(item, delta))
                .ToArray();

        return dimension with
        {
            DefPointX = RoundModelValue(dimension.DefPointX + delta.X),
            DefPointY = RoundModelValue(dimension.DefPointY + delta.Y),
            DefPoint2X = RoundModelValue(dimension.DefPoint2X + delta.X),
            DefPoint2Y = RoundModelValue(dimension.DefPoint2Y + delta.Y),
            DefPoint3X = RoundModelValue(dimension.DefPoint3X + delta.X),
            DefPoint3Y = RoundModelValue(dimension.DefPoint3Y + delta.Y),
            RenderTextX = dimension.RenderTextX is decimal renderTextX
                ? RoundModelValue(renderTextX + delta.X)
                : null,
            RenderTextY = dimension.RenderTextY is decimal renderTextY
                ? RoundModelValue(renderTextY + delta.Y)
                : null,
            LinePrimitives = updatedLines,
            LineSegments = updatedLineSegments,
            TextPrimitives = dimension.TextPrimitives
                .Select(item => TranslateTextPrimitive(item, delta))
                .ToArray(),
            InsertPrimitives = dimension.InsertPrimitives
                .Select(item => TranslateInsertPrimitive(item, delta))
                .ToArray(),
            CirclePrimitives = dimension.CirclePrimitives
                .Select(item => TranslateCirclePrimitive(item, delta))
                .ToArray(),
            ArcPrimitives = dimension.ArcPrimitives
                .Select(item => TranslateArcPrimitive(item, delta))
                .ToArray(),
            SolidPrimitives = dimension.SolidPrimitives
                .Select(item => TranslateSolidPrimitive(item, delta))
                .ToArray(),
            IsEdited = dimension.IsEdited,
            IsDirty = dimension.IsDirty
        };
    }

    private static StylePreservingLinearTransform ResolveStylePreservingLinearTransform(
        DimensionDto dimension,
        Point2 liveStartPoint,
        Point2 liveEndPoint)
    {
        var origin = new Point2(dimension.DefPointX, dimension.DefPointY);
        var authoredEnd = new Point2(dimension.DefPoint2X, dimension.DefPoint2Y);
        var axis = ResolveAxis(dimension);
        var normal = new Vector2(-axis.Y, axis.X);
        var originalSpan = decimal.Abs(ProjectToAxis(origin, axis, authoredEnd));
        if (originalSpan <= 0.001m)
        {
            originalSpan = 1m;
        }

        var liveStartLocal = ToLocal(origin, axis, normal, liveStartPoint);
        var liveEndLocal = ToLocal(origin, axis, normal, liveEndPoint);
        var mappedStart = liveStartLocal.U <= liveEndLocal.U
            ? liveStartLocal
            : liveEndLocal;
        var mappedEnd = liveStartLocal.U <= liveEndLocal.U
            ? liveEndLocal
            : liveStartLocal;

        return new StylePreservingLinearTransform(
            origin,
            axis,
            normal,
            originalSpan,
            mappedStart.U,
            mappedEnd.U);
    }

    private static StylePreservingLinearTransform ResolveStylePreservingLinearTransformFromAnchorDeltas(
        DimensionDto dimension,
        Point2 authoredStartPoint,
        Point2 liveStartPoint,
        Point2 authoredEndPoint,
        Point2 liveEndPoint)
    {
        var origin = new Point2(dimension.DefPointX, dimension.DefPointY);
        var authoredEnd = new Point2(dimension.DefPoint2X, dimension.DefPoint2Y);
        var axis = ResolveAxis(dimension);
        var normal = new Vector2(-axis.Y, axis.X);
        var originalSpan = decimal.Abs(ProjectToAxis(origin, axis, authoredEnd));
        if (originalSpan <= 0.001m)
        {
            originalSpan = 1m;
        }

        var authoredStartLocal = ToLocal(origin, axis, normal, authoredStartPoint);
        var authoredEndLocal = ToLocal(origin, axis, normal, authoredEndPoint);
        var liveStartLocal = ToLocal(origin, axis, normal, liveStartPoint);
        var liveEndLocal = ToLocal(origin, axis, normal, liveEndPoint);
        var startDeltaU = liveStartLocal.U - authoredStartLocal.U;
        var endDeltaU = liveEndLocal.U - authoredEndLocal.U;
        var lowEndpointDeltaU = authoredStartLocal.U <= authoredEndLocal.U
            ? startDeltaU
            : endDeltaU;
        var highEndpointDeltaU = authoredStartLocal.U <= authoredEndLocal.U
            ? endDeltaU
            : startDeltaU;

        return new StylePreservingLinearTransform(
            origin,
            axis,
            normal,
            originalSpan,
            lowEndpointDeltaU,
            originalSpan + highEndpointDeltaU);
    }

    public static DimensionDto RebuildAssociatedOrdinateDimension(
        DimensionDto dimension,
        decimal datumX,
        decimal datumY,
        decimal featureX,
        decimal featureY,
        string bindingKind)
    {
        var datumPoint = new Point2(datumX, datumY);
        var featurePoint = new Point2(featureX, featureY);
        var originalFeaturePoint = new Point2(dimension.DefPoint2X, dimension.DefPoint2Y);
        var featureDelta = new Vector2(
            featurePoint.X - originalFeaturePoint.X,
            featurePoint.Y - originalFeaturePoint.Y);
        var leaderPoint = TranslatePoint(new Point2(dimension.DefPoint3X, dimension.DefPoint3Y), featureDelta);
        var textAnchor = ResolveOrdinateTextAnchor(dimension, featureDelta);
        var measurementSourceUnits = RoundMeasurement(ComputeOrdinateDistance(datumPoint, featurePoint, bindingKind));
        var measurementMillimeters = RoundMeasurement(measurementSourceUnits * ResolveMeasurementFactor(dimension));
        var displayText = DimensionDisplayTextFormatter.Resolve(
            dimension,
            measurementSourceUnits,
            "ReactiveAssociatedMeasurement");

        var updatedDimension = dimension with
        {
            DefPointX = RoundModelValue(datumPoint.X),
            DefPointY = RoundModelValue(datumPoint.Y),
            DefPoint2X = RoundModelValue(featurePoint.X),
            DefPoint2Y = RoundModelValue(featurePoint.Y),
            DefPoint3X = RoundModelValue(leaderPoint.X),
            DefPoint3Y = RoundModelValue(leaderPoint.Y),
            RenderTextX = textAnchor?.X is decimal renderTextX ? RoundModelValue(renderTextX) : null,
            RenderTextY = textAnchor?.Y is decimal renderTextY ? RoundModelValue(renderTextY) : null,
            DisplayText = displayText.DisplayText,
            MeasurementSourceUnits = measurementSourceUnits,
            MeasurementMillimeters = measurementMillimeters,
            DisplayTextSource = displayText.DisplayTextSource,
            IsEdited = dimension.IsEdited,
            IsDirty = dimension.IsDirty
        };

        var updatedLines = dimension.LinePrimitives
            .Select(item => TranslateLinePrimitive(item, featureDelta))
            .ToArray();
        var updatedLineSegments = updatedLines.Length > 0
            ? updatedLines
                .Select(item => new DimensionLineSegmentDto(item.StartX, item.StartY, item.EndX, item.EndY))
                .ToArray()
            : dimension.LineSegments
                .Select(item => TranslateLineSegment(item, featureDelta))
                .ToArray();
        var updatedText = BuildOrdinateTextPrimitives(dimension, updatedDimension, featureDelta, displayText.DisplayText, textAnchor);
        var updatedInserts = dimension.InsertPrimitives
            .Select(item => TranslateInsertPrimitive(item, featureDelta))
            .ToArray();
        var updatedCircles = dimension.CirclePrimitives
            .Select(item => TranslateCirclePrimitive(item, featureDelta))
            .ToArray();
        var updatedArcs = dimension.ArcPrimitives
            .Select(item => TranslateArcPrimitive(item, featureDelta))
            .ToArray();
        var updatedSolids = dimension.SolidPrimitives
            .Select(item => TranslateSolidPrimitive(item, featureDelta))
            .ToArray();

        return updatedDimension with
        {
            LinePrimitives = updatedLines,
            LineSegments = updatedLineSegments,
            TextPrimitives = updatedText,
            InsertPrimitives = updatedInserts,
            CirclePrimitives = updatedCircles,
            ArcPrimitives = updatedArcs,
            SolidPrimitives = updatedSolids
        };
    }

    public static DimensionDto RebuildAssociatedRadialDimension(
        DimensionDto dimension,
        decimal startX,
        decimal startY,
        decimal endX,
        decimal endY)
    {
        var originalStart = new Point2(dimension.DefPointX, dimension.DefPointY);
        var originalEnd = new Point2(dimension.DefPoint2X, dimension.DefPoint2Y);
        var newStart = new Point2(startX, startY);
        var newEnd = new Point2(endX, endY);
        var transform = ResolveSimilarityTransform(originalStart, originalEnd, newStart, newEnd);
        var measurementSourceUnits = RoundMeasurement(ComputeDistance(newStart, newEnd));
        var measurementMillimeters = RoundMeasurement(measurementSourceUnits * ResolveMeasurementFactor(dimension));
        var displayText = DimensionDisplayTextFormatter.Resolve(
            dimension,
            measurementSourceUnits,
            "ReactiveAssociatedMeasurement");
        var transformedDefPoint3 = ApplySimilarityTransform(new Point2(dimension.DefPoint3X, dimension.DefPoint3Y), transform);
        var transformedTextAnchor = ResolveRadialTextAnchor(dimension, transform);

        var updatedDimension = dimension with
        {
            DefPointX = RoundModelValue(newStart.X),
            DefPointY = RoundModelValue(newStart.Y),
            DefPoint2X = RoundModelValue(newEnd.X),
            DefPoint2Y = RoundModelValue(newEnd.Y),
            DefPoint3X = RoundModelValue(transformedDefPoint3.X),
            DefPoint3Y = RoundModelValue(transformedDefPoint3.Y),
            RenderTextX = transformedTextAnchor?.X is decimal renderTextX ? RoundModelValue(renderTextX) : null,
            RenderTextY = transformedTextAnchor?.Y is decimal renderTextY ? RoundModelValue(renderTextY) : null,
            RenderTextRotationDegrees = dimension.RenderTextRotationDegrees is decimal renderRotation
                ? NormalizeRotationDegrees(renderRotation + transform.RotationDegrees)
                : null,
            DisplayText = displayText.DisplayText,
            MeasurementSourceUnits = measurementSourceUnits,
            MeasurementMillimeters = measurementMillimeters,
            DisplayTextSource = displayText.DisplayTextSource,
            IsEdited = dimension.IsEdited,
            IsDirty = dimension.IsDirty
        };

        var updatedLines = dimension.LinePrimitives
            .Select(item => TransformLinePrimitive(item, transform))
            .ToArray();
        var updatedLineSegments = updatedLines.Length > 0
            ? updatedLines
                .Select(item => new DimensionLineSegmentDto(item.StartX, item.StartY, item.EndX, item.EndY))
                .ToArray()
            : dimension.LineSegments
                .Select(item => TransformLineSegment(item, transform))
                .ToArray();
        var updatedText = BuildRadialTextPrimitives(dimension, updatedDimension, transform, displayText.DisplayText, transformedTextAnchor);
        var updatedInserts = dimension.InsertPrimitives
            .Select(item => TransformInsertPrimitive(item, transform))
            .ToArray();
        var updatedCircles = dimension.CirclePrimitives
            .Select(item => TransformCirclePrimitive(item, transform))
            .ToArray();
        var updatedArcs = dimension.ArcPrimitives
            .Select(item => TransformArcPrimitive(item, transform))
            .ToArray();
        var updatedSolids = dimension.SolidPrimitives
            .Select(item => TransformSolidPrimitive(item, transform))
            .ToArray();

        return updatedDimension with
        {
            LinePrimitives = updatedLines,
            LineSegments = updatedLineSegments,
            TextPrimitives = updatedText,
            InsertPrimitives = updatedInserts,
            CirclePrimitives = updatedCircles,
            ArcPrimitives = updatedArcs,
            SolidPrimitives = updatedSolids
        };
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
            ? DimensionDisplayTextFormatter.Resolve(
                sourceDimension,
                measurementSourceUnits,
                generatedDisplayTextSource)
            : new DimensionDisplayTextResult(sourceDimension.DisplayText, sourceDimension.DisplayTextSource);

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
            DisplayText = displayText.DisplayText,
            MeasurementSourceUnits = measurementSourceUnits,
            MeasurementMillimeters = measurementMillimeters,
            DisplayTextSource = displayText.DisplayTextSource,
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
            : [new DimensionTextPrimitiveDto("TEXT-1", 1, displayText.DisplayText, updatedDimension.RenderTextX ?? 0m, updatedDimension.RenderTextY ?? 0m, updatedDimension.RenderTextHeight ?? 3.5m, updatedDimension.RenderTextRotationDegrees ?? 0m)];
        var updatedText = textTemplates
            .Select(template => template with
            {
                Text = displayText.DisplayText,
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

    private static Point2? ResolveOrdinateTextAnchor(DimensionDto dimension, Vector2 featureDelta)
    {
        if (dimension.RenderTextX is decimal renderTextX && dimension.RenderTextY is decimal renderTextY)
        {
            return TranslatePoint(new Point2(renderTextX, renderTextY), featureDelta);
        }

        if (dimension.TextPrimitives.Count > 0)
        {
            return TranslatePoint(
                new Point2(dimension.TextPrimitives[0].X, dimension.TextPrimitives[0].Y),
                featureDelta);
        }

        return null;
    }

    private static IReadOnlyList<DimensionTextPrimitiveDto> BuildOrdinateTextPrimitives(
        DimensionDto sourceDimension,
        DimensionDto updatedDimension,
        Vector2 featureDelta,
        string displayText,
        Point2? textAnchor)
    {
        if (sourceDimension.TextPrimitives.Count > 0)
        {
            return sourceDimension.TextPrimitives
                .Select(template =>
                {
                    var translatedTextPoint = TranslatePoint(new Point2(template.X, template.Y), featureDelta);
                    return template with
                    {
                        Text = displayText,
                        X = RoundModelValue(translatedTextPoint.X),
                        Y = RoundModelValue(translatedTextPoint.Y),
                        Height = updatedDimension.RenderTextHeight ?? template.Height,
                        RotationDegrees = updatedDimension.RenderTextRotationDegrees ?? template.RotationDegrees,
                        StyleName = updatedDimension.RenderTextStyleName ?? template.StyleName,
                        HorizontalAlignment = updatedDimension.RenderTextHorizontalAlignment ?? template.HorizontalAlignment,
                        VerticalAlignment = updatedDimension.RenderTextVerticalAlignment ?? template.VerticalAlignment,
                        AttachmentPoint = updatedDimension.RenderTextAttachmentPoint ?? template.AttachmentPoint
                    };
                })
                .ToArray();
        }

        if (textAnchor is null)
        {
            return [];
        }

        return
        [
            new DimensionTextPrimitiveDto(
                "TEXT-1",
                1,
                displayText,
                RoundModelValue(textAnchor.Value.X),
                RoundModelValue(textAnchor.Value.Y),
                updatedDimension.RenderTextHeight ?? 3.5m,
                updatedDimension.RenderTextRotationDegrees ?? 0m)
            {
                StyleName = updatedDimension.RenderTextStyleName,
                HorizontalAlignment = updatedDimension.RenderTextHorizontalAlignment,
                VerticalAlignment = updatedDimension.RenderTextVerticalAlignment,
                AttachmentPoint = updatedDimension.RenderTextAttachmentPoint
            }
        ];
    }

    private static Point2? ResolveRadialTextAnchor(DimensionDto dimension, SimilarityTransform transform)
    {
        if (dimension.RenderTextX is decimal renderTextX && dimension.RenderTextY is decimal renderTextY)
        {
            return ApplySimilarityTransform(new Point2(renderTextX, renderTextY), transform);
        }

        if (dimension.TextPrimitives.Count > 0)
        {
            return ApplySimilarityTransform(
                new Point2(dimension.TextPrimitives[0].X, dimension.TextPrimitives[0].Y),
                transform);
        }

        return null;
    }

    private static IReadOnlyList<DimensionTextPrimitiveDto> BuildRadialTextPrimitives(
        DimensionDto sourceDimension,
        DimensionDto updatedDimension,
        SimilarityTransform transform,
        string displayText,
        Point2? textAnchor)
    {
        if (sourceDimension.TextPrimitives.Count > 0)
        {
            return sourceDimension.TextPrimitives
                .Select(template =>
                {
                    var transformedPoint = ApplySimilarityTransform(new Point2(template.X, template.Y), transform);
                    return template with
                    {
                        Text = displayText,
                        X = RoundModelValue(transformedPoint.X),
                        Y = RoundModelValue(transformedPoint.Y),
                        Height = updatedDimension.RenderTextHeight ?? template.Height,
                        RotationDegrees = NormalizeRotationDegrees(template.RotationDegrees + transform.RotationDegrees),
                        StyleName = updatedDimension.RenderTextStyleName ?? template.StyleName,
                        HorizontalAlignment = updatedDimension.RenderTextHorizontalAlignment ?? template.HorizontalAlignment,
                        VerticalAlignment = updatedDimension.RenderTextVerticalAlignment ?? template.VerticalAlignment,
                        AttachmentPoint = updatedDimension.RenderTextAttachmentPoint ?? template.AttachmentPoint
                    };
                })
                .ToArray();
        }

        if (textAnchor is null)
        {
            return [];
        }

        return
        [
            new DimensionTextPrimitiveDto(
                "TEXT-1",
                1,
                displayText,
                RoundModelValue(textAnchor.Value.X),
                RoundModelValue(textAnchor.Value.Y),
                updatedDimension.RenderTextHeight ?? 3.5m,
                updatedDimension.RenderTextRotationDegrees ?? 0m)
            {
                StyleName = updatedDimension.RenderTextStyleName,
                HorizontalAlignment = updatedDimension.RenderTextHorizontalAlignment,
                VerticalAlignment = updatedDimension.RenderTextVerticalAlignment,
                AttachmentPoint = updatedDimension.RenderTextAttachmentPoint
            }
        ];
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
        var authoredAxis = TryNormalize(dimension.DefPoint2X - dimension.DefPointX, dimension.DefPoint2Y - dimension.DefPointY);
        if (TryResolveVisualAxis(dimension, out var visualAxis))
        {
            return AlignAxis(visualAxis, authoredAxis);
        }

        if (authoredAxis is Vector2 resolvedAuthoredAxis)
        {
            return resolvedAuthoredAxis;
        }

        var angleRadians = (double)dimension.Angle * Math.PI / 180d;
        return new Vector2(decimal.CreateChecked(Math.Cos(angleRadians)), decimal.CreateChecked(Math.Sin(angleRadians)));
    }

    private static bool TryResolveVisualAxis(DimensionDto dimension, out Vector2 axis)
    {
        if (TryResolveInsertAxis(dimension, out axis))
        {
            return true;
        }

        return TryResolveLongestPrimitiveAxis(dimension, out axis);
    }

    private static bool TryResolveInsertAxis(DimensionDto dimension, out Vector2 axis)
    {
        axis = default;
        if (dimension.InsertPrimitives.Count < 2)
        {
            return false;
        }

        var points = dimension.InsertPrimitives
            .Select(item => new Point2(item.X, item.Y))
            .DistinctBy(item => (RoundModelValue(item.X), RoundModelValue(item.Y)))
            .ToArray();
        if (points.Length < 2)
        {
            return false;
        }

        var bestStart = points[0];
        var bestEnd = points[1];
        var bestDistanceSquared = DistanceSquared(bestStart, bestEnd);
        for (var i = 0; i < points.Length - 1; i++)
        {
            for (var j = i + 1; j < points.Length; j++)
            {
                var distanceSquared = DistanceSquared(points[i], points[j]);
                if (distanceSquared > bestDistanceSquared)
                {
                    bestStart = points[i];
                    bestEnd = points[j];
                    bestDistanceSquared = distanceSquared;
                }
            }
        }

        return TryNormalize(bestEnd.X - bestStart.X, bestEnd.Y - bestStart.Y) is { } resolvedAxis &&
               AssignAxis(resolvedAxis, out axis);
    }

    private static bool TryResolveLongestPrimitiveAxis(DimensionDto dimension, out Vector2 axis)
    {
        axis = default;
        var bestVector = default(Vector2);
        var bestDistanceSquared = 0m;

        foreach (var line in dimension.LinePrimitives)
        {
            var distanceSquared = DistanceSquared(line.StartX, line.StartY, line.EndX, line.EndY);
            if (distanceSquared > bestDistanceSquared &&
                TryNormalize(line.EndX - line.StartX, line.EndY - line.StartY) is { } candidateAxis)
            {
                bestVector = candidateAxis;
                bestDistanceSquared = distanceSquared;
            }
        }

        foreach (var line in dimension.LineSegments)
        {
            var distanceSquared = DistanceSquared(line.StartX, line.StartY, line.EndX, line.EndY);
            if (distanceSquared > bestDistanceSquared &&
                TryNormalize(line.EndX - line.StartX, line.EndY - line.StartY) is { } candidateAxis)
            {
                bestVector = candidateAxis;
                bestDistanceSquared = distanceSquared;
            }
        }

        return bestDistanceSquared > 0m && AssignAxis(bestVector, out axis);
    }

    private static Vector2 AlignAxis(Vector2 axis, Vector2? preferredDirection)
    {
        if (preferredDirection is not { } direction)
        {
            return axis;
        }

        var dot = (axis.X * direction.X) + (axis.Y * direction.Y);
        return dot < 0m
            ? new Vector2(-axis.X, -axis.Y)
            : axis;
    }

    private static Vector2? TryNormalize(decimal dx, decimal dy)
    {
        var length = Math.Sqrt((double)((dx * dx) + (dy * dy)));
        return length <= double.Epsilon
            ? null
            : new Vector2(decimal.CreateChecked(dx / (decimal)length), decimal.CreateChecked(dy / (decimal)length));
    }

    private static bool AssignAxis(Vector2 value, out Vector2 axis)
    {
        axis = value;
        return true;
    }

    private static decimal ComputeDistance(Point2 start, Point2 end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return decimal.CreateChecked(Math.Sqrt((double)((dx * dx) + (dy * dy))));
    }

    private static decimal DistanceSquared(Point2 start, Point2 end)
        => DistanceSquared(start.X, start.Y, end.X, end.Y);

    private static decimal DistanceSquared(decimal startX, decimal startY, decimal endX, decimal endY)
    {
        var dx = endX - startX;
        var dy = endY - startY;
        return (dx * dx) + (dy * dy);
    }

    private static decimal ComputeOrdinateDistance(Point2 datum, Point2 feature, string bindingKind)
    {
        return string.Equals(bindingKind, OrdinateYBindingKind, StringComparison.Ordinal)
            ? decimal.Abs(feature.Y - datum.Y)
            : decimal.Abs(feature.X - datum.X);
    }

    private static SimilarityTransform ResolveSimilarityTransform(Point2 oldStart, Point2 oldEnd, Point2 newStart, Point2 newEnd)
    {
        var oldDx = (double)(oldEnd.X - oldStart.X);
        var oldDy = (double)(oldEnd.Y - oldStart.Y);
        var newDx = (double)(newEnd.X - newStart.X);
        var newDy = (double)(newEnd.Y - newStart.Y);
        var oldLength = Math.Sqrt((oldDx * oldDx) + (oldDy * oldDy));
        var newLength = Math.Sqrt((newDx * newDx) + (newDy * newDy));

        if (oldLength <= double.Epsilon)
        {
            return new SimilarityTransform(oldStart, newStart, 1d, 1d, 0d, 0m);
        }

        var scale = newLength / oldLength;
        var oldAngle = Math.Atan2(oldDy, oldDx);
        var newAngle = newLength <= double.Epsilon
            ? oldAngle
            : Math.Atan2(newDy, newDx);
        var rotationRadians = newAngle - oldAngle;
        var rotationDegrees = decimal.Round(
            decimal.CreateChecked(rotationRadians * 180d / Math.PI),
            3,
            MidpointRounding.AwayFromZero);

        return new SimilarityTransform(
            oldStart,
            newStart,
            scale,
            Math.Cos(rotationRadians),
            Math.Sin(rotationRadians),
            rotationDegrees == -0m ? 0m : rotationDegrees);
    }

    private static Point2 ApplySimilarityTransform(Point2 point, SimilarityTransform transform)
    {
        var relativeX = (double)(point.X - transform.OldOrigin.X);
        var relativeY = (double)(point.Y - transform.OldOrigin.Y);
        var rotatedX = ((relativeX * transform.Cos) - (relativeY * transform.Sin)) * transform.Scale;
        var rotatedY = ((relativeX * transform.Sin) + (relativeY * transform.Cos)) * transform.Scale;

        return new Point2(
            transform.NewOrigin.X + decimal.CreateChecked(rotatedX),
            transform.NewOrigin.Y + decimal.CreateChecked(rotatedY));
    }

    private static Point2 ApplyStylePreservingLinearTransform(
        Point2 point,
        StylePreservingLinearTransform transform)
    {
        var local = ToLocal(transform.Origin, transform.Axis, transform.Normal, point);
        var ratio = transform.OriginalSpan <= 0.001m
            ? 0m
            : local.U / transform.OriginalSpan;
        var newU = transform.NewStartU + (ratio * (transform.NewEndU - transform.NewStartU));

        return new Point2(
            transform.Origin.X + (transform.Axis.X * newU) + (transform.Normal.X * local.V),
            transform.Origin.Y + (transform.Axis.Y * newU) + (transform.Normal.Y * local.V));
    }

    private static Point2? ResolveStylePreservingRenderTextPoint(
        DimensionDto dimension,
        StylePreservingLinearTransform transform)
    {
        if (dimension.RenderTextX is decimal renderTextX && dimension.RenderTextY is decimal renderTextY)
        {
            return ApplyStylePreservingLinearTransform(new Point2(renderTextX, renderTextY), transform);
        }

        if (dimension.TextPrimitives.Count > 0)
        {
            return ApplyStylePreservingLinearTransform(
                new Point2(dimension.TextPrimitives[0].X, dimension.TextPrimitives[0].Y),
                transform);
        }

        return null;
    }

    private static (decimal U, decimal V) ToLocal(
        Point2 origin,
        Vector2 axis,
        Vector2 normal,
        Point2 point)
    {
        var relative = new Vector2(point.X - origin.X, point.Y - origin.Y);
        return (
            (relative.X * axis.X) + (relative.Y * axis.Y),
            (relative.X * normal.X) + (relative.Y * normal.Y));
    }

    private static decimal ProjectToAxis(Point2 origin, Vector2 axis, Point2 point)
    {
        var relative = new Vector2(point.X - origin.X, point.Y - origin.Y);
        return (relative.X * axis.X) + (relative.Y * axis.Y);
    }

    private static Vector2 ResolveAverageActiveAxisTranslation(
        decimal authoredStartX,
        decimal authoredStartY,
        decimal liveStartX,
        decimal liveStartY,
        decimal authoredEndX,
        decimal authoredEndY,
        decimal liveEndX,
        decimal liveEndY,
        string activeAxisTag)
    {
        var averageDeltaX = ((liveStartX - authoredStartX) + (liveEndX - authoredEndX)) / 2m;
        var averageDeltaY = ((liveStartY - authoredStartY) + (liveEndY - authoredEndY)) / 2m;

        if (string.Equals(activeAxisTag, "Width", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(averageDeltaX, 0m);
        }

        if (string.Equals(activeAxisTag, "Height", StringComparison.OrdinalIgnoreCase))
        {
            return new Vector2(0m, averageDeltaY);
        }

        return new Vector2(averageDeltaX, averageDeltaY);
    }

    private static Point2 TranslatePoint(Point2 point, Vector2 delta)
        => new(point.X + delta.X, point.Y + delta.Y);

    private static DimensionLinePrimitiveDto TranslateLinePrimitive(DimensionLinePrimitiveDto primitive, Vector2 delta)
        => primitive with
        {
            StartX = RoundModelValue(primitive.StartX + delta.X),
            StartY = RoundModelValue(primitive.StartY + delta.Y),
            EndX = RoundModelValue(primitive.EndX + delta.X),
            EndY = RoundModelValue(primitive.EndY + delta.Y)
        };

    private static DimensionLineSegmentDto TranslateLineSegment(DimensionLineSegmentDto segment, Vector2 delta)
        => new(
            RoundModelValue(segment.StartX + delta.X),
            RoundModelValue(segment.StartY + delta.Y),
            RoundModelValue(segment.EndX + delta.X),
            RoundModelValue(segment.EndY + delta.Y));

    private static DimensionTextPrimitiveDto TranslateTextPrimitive(DimensionTextPrimitiveDto primitive, Vector2 delta)
        => primitive with
        {
            X = RoundModelValue(primitive.X + delta.X),
            Y = RoundModelValue(primitive.Y + delta.Y)
        };

    private static DimensionInsertPrimitiveDto TranslateInsertPrimitive(DimensionInsertPrimitiveDto primitive, Vector2 delta)
        => primitive with
        {
            X = RoundModelValue(primitive.X + delta.X),
            Y = RoundModelValue(primitive.Y + delta.Y)
        };

    private static DimensionCirclePrimitiveDto TranslateCirclePrimitive(DimensionCirclePrimitiveDto primitive, Vector2 delta)
        => primitive with
        {
            CenterX = RoundModelValue(primitive.CenterX + delta.X),
            CenterY = RoundModelValue(primitive.CenterY + delta.Y)
        };

    private static DimensionArcPrimitiveDto TranslateArcPrimitive(DimensionArcPrimitiveDto primitive, Vector2 delta)
        => primitive with
        {
            CenterX = RoundModelValue(primitive.CenterX + delta.X),
            CenterY = RoundModelValue(primitive.CenterY + delta.Y)
        };

    private static DimensionSolidPrimitiveDto TranslateSolidPrimitive(DimensionSolidPrimitiveDto primitive, Vector2 delta)
        => primitive with
        {
            Point1X = RoundModelValue(primitive.Point1X + delta.X),
            Point1Y = RoundModelValue(primitive.Point1Y + delta.Y),
            Point2X = RoundModelValue(primitive.Point2X + delta.X),
            Point2Y = RoundModelValue(primitive.Point2Y + delta.Y),
            Point3X = RoundModelValue(primitive.Point3X + delta.X),
            Point3Y = RoundModelValue(primitive.Point3Y + delta.Y),
            Point4X = RoundModelValue(primitive.Point4X + delta.X),
            Point4Y = RoundModelValue(primitive.Point4Y + delta.Y)
        };

    private static DimensionLinePrimitiveDto TransformLinePrimitive(DimensionLinePrimitiveDto primitive, SimilarityTransform transform)
    {
        var start = ApplySimilarityTransform(new Point2(primitive.StartX, primitive.StartY), transform);
        var end = ApplySimilarityTransform(new Point2(primitive.EndX, primitive.EndY), transform);
        return primitive with
        {
            StartX = RoundModelValue(start.X),
            StartY = RoundModelValue(start.Y),
            EndX = RoundModelValue(end.X),
            EndY = RoundModelValue(end.Y)
        };
    }

    private static DimensionLinePrimitiveDto TransformLinePrimitive(
        DimensionLinePrimitiveDto primitive,
        StylePreservingLinearTransform transform)
    {
        var start = ApplyStylePreservingLinearTransform(new Point2(primitive.StartX, primitive.StartY), transform);
        var end = ApplyStylePreservingLinearTransform(new Point2(primitive.EndX, primitive.EndY), transform);
        return primitive with
        {
            StartX = RoundModelValue(start.X),
            StartY = RoundModelValue(start.Y),
            EndX = RoundModelValue(end.X),
            EndY = RoundModelValue(end.Y)
        };
    }

    private static DimensionLineSegmentDto TransformLineSegment(DimensionLineSegmentDto segment, SimilarityTransform transform)
    {
        var start = ApplySimilarityTransform(new Point2(segment.StartX, segment.StartY), transform);
        var end = ApplySimilarityTransform(new Point2(segment.EndX, segment.EndY), transform);
        return new(
            RoundModelValue(start.X),
            RoundModelValue(start.Y),
            RoundModelValue(end.X),
            RoundModelValue(end.Y));
    }

    private static DimensionLineSegmentDto TransformLineSegment(
        DimensionLineSegmentDto segment,
        StylePreservingLinearTransform transform)
    {
        var start = ApplyStylePreservingLinearTransform(new Point2(segment.StartX, segment.StartY), transform);
        var end = ApplyStylePreservingLinearTransform(new Point2(segment.EndX, segment.EndY), transform);
        return new(
            RoundModelValue(start.X),
            RoundModelValue(start.Y),
            RoundModelValue(end.X),
            RoundModelValue(end.Y));
    }

    private static DimensionTextPrimitiveDto TransformTextPrimitive(
        DimensionTextPrimitiveDto primitive,
        StylePreservingLinearTransform transform,
        string displayText,
        DimensionDto dimension)
    {
        var point = ApplyStylePreservingLinearTransform(new Point2(primitive.X, primitive.Y), transform);
        return primitive with
        {
            Text = displayText,
            X = RoundModelValue(point.X),
            Y = RoundModelValue(point.Y),
            Height = dimension.RenderTextHeight ?? primitive.Height,
            RotationDegrees = dimension.RenderTextRotationDegrees ?? primitive.RotationDegrees,
            StyleName = dimension.RenderTextStyleName ?? primitive.StyleName,
            HorizontalAlignment = dimension.RenderTextHorizontalAlignment ?? primitive.HorizontalAlignment,
            VerticalAlignment = dimension.RenderTextVerticalAlignment ?? primitive.VerticalAlignment,
            AttachmentPoint = dimension.RenderTextAttachmentPoint ?? primitive.AttachmentPoint
        };
    }

    private static DimensionInsertPrimitiveDto TransformInsertPrimitive(DimensionInsertPrimitiveDto primitive, SimilarityTransform transform)
    {
        var point = ApplySimilarityTransform(new Point2(primitive.X, primitive.Y), transform);
        return primitive with
        {
            X = RoundModelValue(point.X),
            Y = RoundModelValue(point.Y),
            RotationDegrees = NormalizeRotationDegrees(primitive.RotationDegrees + transform.RotationDegrees),
            ScaleX = RoundModelValue(primitive.ScaleX * decimal.CreateChecked(transform.Scale)),
            ScaleY = RoundModelValue(primitive.ScaleY * decimal.CreateChecked(transform.Scale)),
            ScaleZ = RoundModelValue(primitive.ScaleZ * decimal.CreateChecked(transform.Scale))
        };
    }

    private static DimensionInsertPrimitiveDto TransformInsertPrimitive(
        DimensionInsertPrimitiveDto primitive,
        StylePreservingLinearTransform transform)
    {
        var point = ApplyStylePreservingLinearTransform(new Point2(primitive.X, primitive.Y), transform);
        return primitive with
        {
            X = RoundModelValue(point.X),
            Y = RoundModelValue(point.Y)
        };
    }

    private static DimensionCirclePrimitiveDto TransformCirclePrimitive(DimensionCirclePrimitiveDto primitive, SimilarityTransform transform)
    {
        var center = ApplySimilarityTransform(new Point2(primitive.CenterX, primitive.CenterY), transform);
        return primitive with
        {
            CenterX = RoundModelValue(center.X),
            CenterY = RoundModelValue(center.Y),
            Radius = RoundModelValue(primitive.Radius * decimal.CreateChecked(transform.Scale))
        };
    }

    private static DimensionCirclePrimitiveDto TransformCirclePrimitive(
        DimensionCirclePrimitiveDto primitive,
        StylePreservingLinearTransform transform)
    {
        var center = ApplyStylePreservingLinearTransform(new Point2(primitive.CenterX, primitive.CenterY), transform);
        return primitive with
        {
            CenterX = RoundModelValue(center.X),
            CenterY = RoundModelValue(center.Y)
        };
    }

    private static DimensionArcPrimitiveDto TransformArcPrimitive(DimensionArcPrimitiveDto primitive, SimilarityTransform transform)
    {
        var center = ApplySimilarityTransform(new Point2(primitive.CenterX, primitive.CenterY), transform);
        return primitive with
        {
            CenterX = RoundModelValue(center.X),
            CenterY = RoundModelValue(center.Y),
            Radius = RoundModelValue(primitive.Radius * decimal.CreateChecked(transform.Scale)),
            StartAngleDegrees = NormalizeRotationDegrees(primitive.StartAngleDegrees + transform.RotationDegrees),
            EndAngleDegrees = NormalizeRotationDegrees(primitive.EndAngleDegrees + transform.RotationDegrees)
        };
    }

    private static DimensionArcPrimitiveDto TransformArcPrimitive(
        DimensionArcPrimitiveDto primitive,
        StylePreservingLinearTransform transform)
    {
        var center = ApplyStylePreservingLinearTransform(new Point2(primitive.CenterX, primitive.CenterY), transform);
        return primitive with
        {
            CenterX = RoundModelValue(center.X),
            CenterY = RoundModelValue(center.Y)
        };
    }

    private static DimensionSolidPrimitiveDto TransformSolidPrimitive(DimensionSolidPrimitiveDto primitive, SimilarityTransform transform)
    {
        var point1 = ApplySimilarityTransform(new Point2(primitive.Point1X, primitive.Point1Y), transform);
        var point2 = ApplySimilarityTransform(new Point2(primitive.Point2X, primitive.Point2Y), transform);
        var point3 = ApplySimilarityTransform(new Point2(primitive.Point3X, primitive.Point3Y), transform);
        var point4 = ApplySimilarityTransform(new Point2(primitive.Point4X, primitive.Point4Y), transform);
        return primitive with
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
    }

    private static DimensionSolidPrimitiveDto TransformSolidPrimitive(
        DimensionSolidPrimitiveDto primitive,
        StylePreservingLinearTransform transform)
    {
        var point1 = ApplyStylePreservingLinearTransform(new Point2(primitive.Point1X, primitive.Point1Y), transform);
        var point2 = ApplyStylePreservingLinearTransform(new Point2(primitive.Point2X, primitive.Point2Y), transform);
        var point3 = ApplyStylePreservingLinearTransform(new Point2(primitive.Point3X, primitive.Point3Y), transform);
        var point4 = ApplyStylePreservingLinearTransform(new Point2(primitive.Point4X, primitive.Point4Y), transform);
        return primitive with
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
    }

    private static decimal RoundMeasurement(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundModelValue(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }

    private static decimal NormalizeRotationDegrees(decimal value)
    {
        var normalized = value % 360m;
        return normalized < 0m
            ? normalized + 360m
            : normalized;
    }

    private readonly record struct RebuildRenderState(
        Point2 DimensionLinePoint,
        Point2 DimensionEndpoint1,
        Point2 DimensionEndpoint2,
        Point2 TextAnchor);

    private readonly record struct StylePreservingLinearTransform(
        Point2 Origin,
        Vector2 Axis,
        Vector2 Normal,
        decimal OriginalSpan,
        decimal NewStartU,
        decimal NewEndU);

    private readonly record struct SimilarityTransform(
        Point2 OldOrigin,
        Point2 NewOrigin,
        double Scale,
        double Cos,
        double Sin,
        decimal RotationDegrees);

    private readonly record struct Point2(decimal X, decimal Y);

    private readonly record struct Vector2(decimal X, decimal Y);
}
