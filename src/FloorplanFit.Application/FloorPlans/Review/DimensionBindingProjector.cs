using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.FloorPlans.Review;

public static class DimensionBindingProjector
{
    private const string LinearSpanBindingKind = "LinearSpan";
    private const string OrdinateXBindingKind = "OrdinateX";
    private const string OrdinateYBindingKind = "OrdinateY";
    private const string RadiusBindingKind = "Radius";
    private const string DiameterBindingKind = "Diameter";

    public static IReadOnlyList<DimensionBindingDto> Build(
        IReadOnlyList<DimensionDto> dimensions,
        IReadOnlyList<DimensionAssociationDto> associations)
    {
        if (dimensions.Count == 0)
        {
            return [];
        }

        var associationLookup = associations.ToDictionary(item => item.DimensionId);
        return dimensions
            .Select(dimension => BuildBinding(
                dimension,
                associationLookup.TryGetValue(dimension.DimensionId, out var association)
                    ? association
                    : null))
            .ToArray();
    }

    private static DimensionBindingDto BuildBinding(
        DimensionDto dimension,
        DimensionAssociationDto? association)
    {
        var bindingKind = ResolveBindingKind(dimension);
        var anchors = CollectAnchors(association);

        if (string.Equals(bindingKind, RadiusBindingKind, StringComparison.Ordinal) ||
            string.Equals(bindingKind, DiameterBindingKind, StringComparison.Ordinal))
        {
            return BuildRadialBinding(dimension, bindingKind, association, anchors);
        }

        if (string.Equals(bindingKind, OrdinateXBindingKind, StringComparison.Ordinal) ||
            string.Equals(bindingKind, OrdinateYBindingKind, StringComparison.Ordinal))
        {
            return BuildOrdinateBinding(dimension, bindingKind, association, anchors);
        }

        if (association is null)
        {
            return new DimensionBindingDto(
                dimension.DimensionId,
                bindingKind,
                false,
                0m,
                "No dimension association available.",
                false);
        }

        if (!association.IsFullyResolved ||
            association.StartAnchor is null ||
            association.EndAnchor is null)
        {
            return new DimensionBindingDto(
                dimension.DimensionId,
                bindingKind,
                false,
                association.Confidence,
                association.Notes,
                false)
            {
                Anchors = anchors
            };
        }

        return new DimensionBindingDto(
            dimension.DimensionId,
            bindingKind,
            true,
            association.Confidence,
            association.Notes,
            false)
        {
            Anchors = anchors,
            MeasuredSpan = BuildMeasuredSpan(association.StartAnchor, association.EndAnchor)
        };
    }

    private static DimensionBindingDto BuildOrdinateBinding(
        DimensionDto dimension,
        string bindingKind,
        DimensionAssociationDto? association,
        IReadOnlyList<DimensionAnchorReferenceDto> anchors)
    {
        if (association is null)
        {
            return new DimensionBindingDto(
                dimension.DimensionId,
                bindingKind,
                false,
                0m,
                "No dimension association available.",
                false);
        }

        if (!association.IsFullyResolved ||
            association.StartAnchor is null ||
            association.EndAnchor is null)
        {
            return new DimensionBindingDto(
                dimension.DimensionId,
                bindingKind,
                false,
                association.Confidence,
                association.Notes,
                false)
            {
                Anchors = anchors
            };
        }

        return new DimensionBindingDto(
            dimension.DimensionId,
            bindingKind,
            true,
            association.Confidence,
            association.Notes,
            false)
        {
            Anchors = anchors,
            MeasuredSpan = BuildOrdinateMeasuredSpan(bindingKind, association.StartAnchor, association.EndAnchor)
        };
    }

    private static DimensionBindingDto BuildRadialBinding(
        DimensionDto dimension,
        string bindingKind,
        DimensionAssociationDto? association,
        IReadOnlyList<DimensionAnchorReferenceDto> anchors)
    {
        if (association is null)
        {
            return new DimensionBindingDto(
                dimension.DimensionId,
                bindingKind,
                false,
                0m,
                "No dimension association available.",
                false);
        }

        if (!association.IsFullyResolved ||
            association.StartAnchor is null ||
            association.EndAnchor is null)
        {
            return new DimensionBindingDto(
                dimension.DimensionId,
                bindingKind,
                false,
                association.Confidence,
                association.Notes,
                false)
            {
                Anchors = anchors
            };
        }

        return new DimensionBindingDto(
            dimension.DimensionId,
            bindingKind,
            true,
            association.Confidence,
            association.Notes,
            false)
        {
            Anchors = anchors,
            MeasuredSpan = BuildRadialMeasuredSpan(association.StartAnchor, association.EndAnchor)
        };
    }

    private static IReadOnlyList<DimensionAnchorReferenceDto> CollectAnchors(DimensionAssociationDto? association)
    {
        if (association is null)
        {
            return [];
        }

        var anchors = new List<DimensionAnchorReferenceDto>(2);
        if (association.StartAnchor is not null)
        {
            anchors.Add(association.StartAnchor);
        }

        if (association.EndAnchor is not null)
        {
            anchors.Add(association.EndAnchor);
        }

        return anchors;
    }

    private static DimensionMeasuredSpanDto BuildMeasuredSpan(
        DimensionAnchorReferenceDto startAnchor,
        DimensionAnchorReferenceDto endAnchor)
    {
        var dx = endAnchor.AnchorX - startAnchor.AnchorX;
        var dy = endAnchor.AnchorY - startAnchor.AnchorY;
        var length = Math.Sqrt((double)((dx * dx) + (dy * dy)));

        decimal axisX;
        decimal axisY;
        decimal orientationDegrees;

        if (length <= double.Epsilon)
        {
            axisX = 1m;
            axisY = 0m;
            orientationDegrees = 0m;
        }
        else
        {
            axisX = dx / decimal.CreateChecked(length);
            axisY = dy / decimal.CreateChecked(length);
            orientationDegrees = decimal.Round(
                decimal.CreateChecked(Math.Atan2((double)dy, (double)dx) * 180d / Math.PI),
                3,
                MidpointRounding.AwayFromZero);
        }

        var startCoordinate = decimal.Round(
            (startAnchor.AnchorX * axisX) + (startAnchor.AnchorY * axisY),
            3,
            MidpointRounding.AwayFromZero);
        var endCoordinate = decimal.Round(
            (endAnchor.AnchorX * axisX) + (endAnchor.AnchorY * axisY),
            3,
            MidpointRounding.AwayFromZero);

        return new DimensionMeasuredSpanDto(
            DimensionAxisClassifier.ResolveAxisTagFromDelta(dx, dy),
            startCoordinate,
            endCoordinate,
            orientationDegrees == -0m ? 0m : orientationDegrees);
    }

    private static DimensionMeasuredSpanDto BuildOrdinateMeasuredSpan(
        string bindingKind,
        DimensionAnchorReferenceDto startAnchor,
        DimensionAnchorReferenceDto endAnchor)
    {
        return string.Equals(bindingKind, OrdinateYBindingKind, StringComparison.Ordinal)
            ? new DimensionMeasuredSpanDto(
                OrdinateYBindingKind,
                decimal.Round(startAnchor.AnchorY, 3, MidpointRounding.AwayFromZero),
                decimal.Round(endAnchor.AnchorY, 3, MidpointRounding.AwayFromZero),
                90m)
            : new DimensionMeasuredSpanDto(
                OrdinateXBindingKind,
                decimal.Round(startAnchor.AnchorX, 3, MidpointRounding.AwayFromZero),
                decimal.Round(endAnchor.AnchorX, 3, MidpointRounding.AwayFromZero),
                0m);
    }

    private static DimensionMeasuredSpanDto BuildRadialMeasuredSpan(
        DimensionAnchorReferenceDto startAnchor,
        DimensionAnchorReferenceDto endAnchor)
    {
        var dx = endAnchor.AnchorX - startAnchor.AnchorX;
        var dy = endAnchor.AnchorY - startAnchor.AnchorY;
        var distance = decimal.Round(
            decimal.CreateChecked(Math.Sqrt((double)((dx * dx) + (dy * dy)))),
            3,
            MidpointRounding.AwayFromZero);
        var orientationDegrees = decimal.Round(
            decimal.CreateChecked(Math.Atan2((double)dy, (double)dx) * 180d / Math.PI),
            3,
            MidpointRounding.AwayFromZero);

        return new DimensionMeasuredSpanDto(
            "Radial",
            0m,
            distance,
            orientationDegrees == -0m ? 0m : orientationDegrees);
    }

    private static string ResolveBindingKind(DimensionDto dimension)
    {
        var baseType = dimension.DimType & 0x7;
        return baseType switch
        {
            3 => DiameterBindingKind,
            4 => RadiusBindingKind,
            6 => ResolveOrdinateBindingKind(dimension),
            _ => LinearSpanBindingKind
        };
    }

    private static string ResolveOrdinateBindingKind(DimensionDto dimension)
    {
        var dx = Math.Abs((double)(dimension.DefPoint2X - dimension.DefPointX));
        var dy = Math.Abs((double)(dimension.DefPoint2Y - dimension.DefPointY));
        return dx >= dy
            ? OrdinateXBindingKind
            : OrdinateYBindingKind;
    }

}
