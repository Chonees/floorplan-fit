using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Review;

public sealed class DimensionBindingProjectorTests
{
    [Fact]
    public void Build_creates_resolved_linear_binding_with_measured_span_from_association()
    {
        var dimension = CreateDimension(dimType: 0, startX: 100m, startY: 120m, endX: 260m, endY: 120m);
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                0.98m,
                "Resolved.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Projected", 100m, 120m, 0m)
                {
                    SegmentRatio = 0.25m
                },
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Projected", 260m, 120m, 0m)
                {
                    SegmentRatio = 0.75m
                }
            }
        ];

        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var binding = Assert.Single(bindings);
        Assert.Equal(dimension.DimensionId, binding.DimensionId);
        Assert.Equal("LinearSpan", binding.BindingKind);
        Assert.True(binding.IsResolved);
        Assert.Equal(0.98m, binding.Confidence);
        Assert.False(binding.HasManualBindingOverride);
        Assert.Equal(2, binding.Anchors.Count);
        Assert.Equal("Width", binding.MeasuredSpan?.AxisTag);
        Assert.Equal(100m, binding.MeasuredSpan?.StartCoordinate);
        Assert.Equal(260m, binding.MeasuredSpan?.EndCoordinate);
        Assert.Equal(0m, binding.MeasuredSpan?.OrientationDegrees);
    }

    [Fact]
    public void Build_keeps_partial_linear_binding_unresolved_without_silent_fallback()
    {
        var dimension = CreateDimension(dimType: 1, startX: 100m, startY: 100m, endX: 224m, endY: 100m);
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                false,
                0.42m,
                "start ambiguous across 2 anchors; end unresolved within 0.5 source units.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m)
            }
        ];

        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var binding = Assert.Single(bindings);
        Assert.Equal("LinearSpan", binding.BindingKind);
        Assert.False(binding.IsResolved);
        Assert.Equal(0.42m, binding.Confidence);
        Assert.Single(binding.Anchors);
        Assert.Null(binding.MeasuredSpan);
        Assert.Contains("end unresolved", binding.Notes, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_creates_resolved_radius_binding_with_radial_measured_span()
    {
        var dimension = CreateDimension(dimType: 4, startX: 100m, startY: 100m, endX: 150m, endY: 100m);
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                0.96m,
                "Resolved radius anchors.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-center", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-feature", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Projected", 150m, 100m, 0m)
                {
                    SegmentRatio = 0.5m
                }
            }
        ];

        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var binding = Assert.Single(bindings);
        Assert.Equal("Radius", binding.BindingKind);
        Assert.True(binding.IsResolved);
        Assert.Equal(2, binding.Anchors.Count);
        Assert.Equal("Radial", binding.MeasuredSpan?.AxisTag);
        Assert.Equal(0m, binding.MeasuredSpan?.StartCoordinate);
        Assert.Equal(50m, binding.MeasuredSpan?.EndCoordinate);
    }

    [Fact]
    public void Build_creates_resolved_diameter_binding_with_radial_measured_span()
    {
        var dimension = CreateDimension(dimType: 3, startX: 80m, startY: 100m, endX: 120m, endY: 100m);
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                0.96m,
                "Resolved diameter anchors.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 80m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Projected", 120m, 100m, 0m)
                {
                    SegmentRatio = 0.5m
                }
            }
        ];

        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var binding = Assert.Single(bindings);
        Assert.Equal("Diameter", binding.BindingKind);
        Assert.True(binding.IsResolved);
        Assert.Equal(2, binding.Anchors.Count);
        Assert.Equal("Radial", binding.MeasuredSpan?.AxisTag);
        Assert.Equal(0m, binding.MeasuredSpan?.StartCoordinate);
        Assert.Equal(40m, binding.MeasuredSpan?.EndCoordinate);
    }

    [Fact]
    public void Build_creates_resolved_ordinate_x_binding_with_axis_projected_span()
    {
        var dimension = CreateDimension(dimType: 6, startX: 100m, startY: 100m, endX: 260m, endY: 140m);
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                0.94m,
                "Resolved ordinate X anchors.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Projected", 260m, 140m, 0m)
                {
                    SegmentRatio = 0.5m
                }
            }
        ];

        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var binding = Assert.Single(bindings);
        Assert.Equal("OrdinateX", binding.BindingKind);
        Assert.True(binding.IsResolved);
        Assert.Equal("OrdinateX", binding.MeasuredSpan?.AxisTag);
        Assert.Equal(100m, binding.MeasuredSpan?.StartCoordinate);
        Assert.Equal(260m, binding.MeasuredSpan?.EndCoordinate);
        Assert.Equal(0m, binding.MeasuredSpan?.OrientationDegrees);
        Assert.Equal(2, binding.Anchors.Count);
    }

    [Fact]
    public void Build_creates_resolved_ordinate_y_binding_with_axis_projected_span()
    {
        var dimension = CreateDimension(dimType: 6, startX: 100m, startY: 100m, endX: 120m, endY: 260m);
        IReadOnlyList<DimensionAssociationDto> associations =
        [
            new DimensionAssociationDto(
                dimension.DimensionId,
                "MeasuredEndpointAnchors",
                true,
                0.94m,
                "Resolved ordinate Y anchors.")
            {
                StartAnchor = new DimensionAnchorReferenceDto("edge-a", "WallCandidate", Guid.NewGuid(), Guid.NewGuid(), "Start", 100m, 100m, 0m),
                EndAnchor = new DimensionAnchorReferenceDto("edge-b", "OpeningCandidate", Guid.NewGuid(), Guid.NewGuid(), "Projected", 120m, 260m, 0m)
                {
                    SegmentRatio = 0.5m
                }
            }
        ];

        var bindings = DimensionBindingProjector.Build([dimension], associations);

        var binding = Assert.Single(bindings);
        Assert.Equal("OrdinateY", binding.BindingKind);
        Assert.True(binding.IsResolved);
        Assert.Equal("OrdinateY", binding.MeasuredSpan?.AxisTag);
        Assert.Equal(100m, binding.MeasuredSpan?.StartCoordinate);
        Assert.Equal(260m, binding.MeasuredSpan?.EndCoordinate);
        Assert.Equal(90m, binding.MeasuredSpan?.OrientationDegrees);
        Assert.Equal(2, binding.Anchors.Count);
    }

    private static DimensionDto CreateDimension(int dimType, decimal startX, decimal startY, decimal endX, decimal endY)
    {
        return new DimensionDto(
            Guid.NewGuid(),
            "DIMENSION:AB12",
            "DIMS",
            "DIMENSION",
            "*D169",
            "10'-4\"",
            "GeometryBlock",
            string.Empty,
            124m,
            3149.6m,
            "Inch",
            dimType,
            0m,
            0m,
            startX,
            startY,
            0m,
            endX,
            endY,
            0m,
            startX,
            startY + 40m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "AB12"
        };
    }
}
