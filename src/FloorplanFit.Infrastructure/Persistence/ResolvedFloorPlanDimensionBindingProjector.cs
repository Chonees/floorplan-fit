using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Infrastructure.Persistence;

internal static class ResolvedFloorPlanDimensionBindingProjector
{
    private const string CompatibilityAssociationKind = "DimensionBindingCompatibility";

    public static DimensionBindingDto Resolve(
        Guid dimensionId,
        DimensionBindingDto? detectedBinding,
        FloorPlanDimensionBindingOverride? bindingOverride)
    {
        if (bindingOverride is null)
        {
            return detectedBinding
                ?? new DimensionBindingDto(
                    dimensionId,
                    "LinearSpan",
                    false,
                    0m,
                    "No detected binding was available.",
                    false);
        }

        return new DimensionBindingDto(
            dimensionId,
            bindingOverride.BindingKind,
            bindingOverride.IsResolved,
            bindingOverride.Confidence,
            bindingOverride.Notes,
            true)
        {
            Anchors = bindingOverride.Anchors
                .OrderBy(item => item.SortOrder)
                .Select(item => new DimensionAnchorReferenceDto(
                    item.EdgeKey,
                    item.SourceArtifactKind,
                    item.SourceArtifactId,
                    item.GeometryPathId,
                    item.EdgeAnchorKind,
                    item.AnchorX,
                    item.AnchorY,
                    item.DistanceSourceUnits)
                {
                    SegmentRatio = item.SegmentRatio
                })
                .ToArray(),
            MeasuredSpan = bindingOverride.MeasuredSpan is null
                ? null
                : new DimensionMeasuredSpanDto(
                    bindingOverride.MeasuredSpan.AxisTag,
                    bindingOverride.MeasuredSpan.StartCoordinate,
                    bindingOverride.MeasuredSpan.EndCoordinate,
                    bindingOverride.MeasuredSpan.OrientationDegrees)
        };
    }

    public static DimensionAssociationDto ToCompatibilityAssociation(DimensionBindingDto binding)
    {
        var orderedAnchors = binding.Anchors
            .Take(2)
            .ToArray();

        return new DimensionAssociationDto(
            binding.DimensionId,
            CompatibilityAssociationKind,
            binding.IsResolved && orderedAnchors.Length == 2,
            binding.Confidence,
            binding.Notes)
        {
            StartAnchor = orderedAnchors.ElementAtOrDefault(0),
            EndAnchor = orderedAnchors.ElementAtOrDefault(1)
        };
    }
}
