using FloorplanFit.Application.FloorPlans.Review;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Infrastructure.Tests.Review;

public sealed class DimensionAssociationProjectorTests
{
    [Fact]
    public void Build_resolves_dimension_endpoints_to_measurable_edge_anchors()
    {
        var dimension = CreateDimension(100m, 100m, 224m, 100m);
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        IReadOnlyList<MeasurableEdgeDto> edges =
        [
            new MeasurableEdgeDto(
                "WallCandidate:wall-a:path-a:1",
                FloorPlanArtifactSourceKinds.WallCandidate,
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                40m,
                1016m,
                90m,
                100m,
                100m,
                100m,
                140m),
            new MeasurableEdgeDto(
                "OpeningCandidate:opening-b:path-b:1",
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                40m,
                1016m,
                90m,
                224m,
                100m,
                224m,
                140m)
        ];

        var associations = DimensionAssociationProjector.Build([dimension], edges, measurementContext);

        var association = Assert.Single(associations);
        Assert.True(association.IsFullyResolved);
        Assert.Equal("MeasuredEndpointAnchors", association.AssociationKind);
        Assert.Equal(1m, association.Confidence);
        Assert.Equal("WallCandidate:wall-a:path-a:1", association.StartAnchor?.EdgeKey);
        Assert.Equal("OpeningCandidate:opening-b:path-b:1", association.EndAnchor?.EdgeKey);
        Assert.Contains("Resolved both dimension endpoints", association.Notes, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_marks_ambiguous_or_unresolved_anchor_matches()
    {
        var dimension = CreateDimension(100m, 100m, 224m, 100m);
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        IReadOnlyList<MeasurableEdgeDto> edges =
        [
            new MeasurableEdgeDto(
                "WallCandidate:wall-a:path-a:1",
                FloorPlanArtifactSourceKinds.WallCandidate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                40m,
                1016m,
                90m,
                100m,
                100m,
                100m,
                140m),
            new MeasurableEdgeDto(
                "WallCandidate:wall-b:path-b:1",
                FloorPlanArtifactSourceKinds.WallCandidate,
                Guid.NewGuid(),
                Guid.NewGuid(),
                40m,
                1016m,
                90m,
                100.2m,
                100m,
                100.2m,
                140m)
        ];

        var associations = DimensionAssociationProjector.Build([dimension], edges, measurementContext);

        var association = Assert.Single(associations);
        Assert.False(association.IsFullyResolved);
        Assert.NotNull(association.StartAnchor);
        Assert.Null(association.EndAnchor);
        Assert.True(association.Confidence < 0.5m);
        Assert.Contains("start ambiguous", association.Notes, StringComparison.Ordinal);
        Assert.Contains("end unresolved", association.Notes, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_resolves_dimension_endpoints_projected_onto_edge_segments()
    {
        var dimension = CreateDimension(100m, 120m, 224m, 120m);
        var measurementContext = new MeasurementContextDto("Inch", 25.4m, 1m, 1m);
        IReadOnlyList<MeasurableEdgeDto> edges =
        [
            new MeasurableEdgeDto(
                "WallCandidate:wall-a:path-a:1",
                FloorPlanArtifactSourceKinds.WallCandidate,
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                40m,
                1016m,
                90m,
                100m,
                100m,
                100m,
                140m),
            new MeasurableEdgeDto(
                "OpeningCandidate:opening-b:path-b:1",
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                40m,
                1016m,
                90m,
                224m,
                100m,
                224m,
                140m)
        ];

        var associations = DimensionAssociationProjector.Build([dimension], edges, measurementContext);

        var association = Assert.Single(associations);
        Assert.True(association.IsFullyResolved);
        Assert.Equal("WallCandidate:wall-a:path-a:1", association.StartAnchor?.EdgeKey);
        Assert.Equal("OpeningCandidate:opening-b:path-b:1", association.EndAnchor?.EdgeKey);
        Assert.Contains("Resolved both dimension endpoints", association.Notes, StringComparison.Ordinal);
    }

    private static DimensionDto CreateDimension(decimal defPointX, decimal defPointY, decimal defPoint2X, decimal defPoint2Y)
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
            0,
            0m,
            0m,
            defPointX,
            defPointY,
            0m,
            defPoint2X,
            defPoint2Y,
            0m,
            defPointX,
            140m,
            0m,
            0.99m,
            null,
            1)
        {
            SourceHandle = "AB12"
        };
    }
}
