using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Application.FloorPlans.Review;

namespace FloorplanFit.Application.Tests.FloorPlans.Review;

public sealed class ArticulationBandProjectorTests
{
    [Fact]
    public void Build_projects_single_min_max_band_per_group_from_marker_geometry()
    {
        var leftPathId = Guid.NewGuid();
        var rightPathId = Guid.NewGuid();
        var pinchGroupId = Guid.NewGuid();
        IReadOnlyList<GeometryPathDto> geometryPaths =
        [
            new(leftPathId, false, [new GeometrySegmentDto(leftPathId, 1, 100m, 0m, 100m, 100m)]),
            new(rightPathId, false, [new GeometrySegmentDto(rightPathId, 1, 260m, 0m, 260m, 100m)])
        ];
        IReadOnlyList<PinchGroupDto> pinchGroups =
        [
            new(pinchGroupId, "Patio", "Width", 1)
        ];
        IReadOnlyList<PinchMarkerDto> pinchMarkers =
        [
            new(Guid.NewGuid(), pinchGroupId, "Patio", Guid.NewGuid(), leftPathId, "Width", 0.5m, 80m, 1),
            new(Guid.NewGuid(), pinchGroupId, "Patio", Guid.NewGuid(), rightPathId, "Width", 0.5m, 120m, 2)
        ];

        var bands = ArticulationBandProjector.Build(pinchGroups, pinchMarkers, geometryPaths);

        var band = Assert.Single(bands);
        Assert.Equal(pinchGroupId, band.PinchGroupId);
        Assert.Equal("Patio", band.PinchGroupName);
        Assert.Equal("Width", band.AxisTag);
        Assert.Equal(100m, band.BandStartCoordinate);
        Assert.Equal(260m, band.BandEndCoordinate);
        Assert.Equal(200m, band.MaxTrimMm);
        Assert.Equal("Suggested", band.Status);
    }
}
