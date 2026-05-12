using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanArtifactPositionTests
{
    [Fact]
    public void CreateAbsolutePoint_accepts_room_labels_and_stores_resolved_coordinates()
    {
        var updatedAt = new DateTime(2026, 5, 11, 19, 0, 0, DateTimeKind.Utc);

        var position = FloorPlanArtifactPosition.CreateAbsolutePoint(
            Guid.NewGuid(),
            FloorPlanArtifactPositionSourceKinds.RoomLabel,
            Guid.NewGuid(),
            220m,
            410m,
            updatedAt);

        Assert.Equal(FloorPlanArtifactPositionMode.AbsolutePoint, position.PositionMode);
        Assert.Equal(220m, position.ResolvedX);
        Assert.Equal(410m, position.ResolvedY);
        Assert.Null(position.TranslationDx);
        Assert.Null(position.TranslationDy);
        Assert.Equal(updatedAt, position.UpdatedAtUtc);
    }

    [Fact]
    public void CreateTranslation_accepts_curated_geometry_kinds_and_stores_offsets()
    {
        var position = FloorPlanArtifactPosition.CreateTranslation(
            Guid.NewGuid(),
            FloorPlanArtifactPositionSourceKinds.OpeningCandidate,
            Guid.NewGuid(),
            24m,
            -12m,
            new DateTime(2026, 5, 11, 19, 1, 0, DateTimeKind.Utc));

        Assert.Equal(FloorPlanArtifactPositionMode.Translation, position.PositionMode);
        Assert.Equal(24m, position.TranslationDx);
        Assert.Equal(-12m, position.TranslationDy);
        Assert.Null(position.ResolvedX);
        Assert.Null(position.ResolvedY);
    }

    [Fact]
    public void CreateAbsolutePoint_rejects_geometry_only_source_kinds()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            FloorPlanArtifactPosition.CreateAbsolutePoint(
                Guid.NewGuid(),
                FloorPlanArtifactPositionSourceKinds.FixedPlanComponent,
                Guid.NewGuid(),
                10m,
                20m,
                DateTime.UtcNow));

        Assert.Contains("AbsolutePoint", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateTranslation_rejects_unsupported_source_kinds_like_walls_and_pinches()
    {
        Assert.Throws<ArgumentException>(() =>
            FloorPlanArtifactPosition.CreateTranslation(
                Guid.NewGuid(),
                "WallCandidate",
                Guid.NewGuid(),
                4m,
                0m,
                DateTime.UtcNow));

        Assert.Throws<ArgumentException>(() =>
            FloorPlanArtifactPosition.CreateTranslation(
                Guid.NewGuid(),
                "PinchMarker",
                Guid.NewGuid(),
                4m,
                0m,
                DateTime.UtcNow));
    }
}
