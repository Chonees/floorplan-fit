using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.Curation;

public sealed class FloorPlanLabelOverrideTests
{
    [Fact]
    public void CreateResolvedTextHeight_accepts_room_labels_and_stores_manual_height()
    {
        var updatedAt = new DateTime(2026, 5, 11, 23, 30, 0, DateTimeKind.Utc);

        var overrideRow = FloorPlanLabelOverride.CreateResolvedTextHeight(
            Guid.NewGuid(),
            FloorPlanLabelOverrideSourceKinds.RoomLabel,
            Guid.NewGuid(),
            6.5m,
            updatedAt);

        Assert.Equal(6.5m, overrideRow.ResolvedTextHeight);
        Assert.Equal(updatedAt, overrideRow.UpdatedAtUtc);
    }

    [Fact]
    public void CreateDetectedDefault_accepts_opening_labels_and_stores_null_height()
    {
        var overrideRow = FloorPlanLabelOverride.CreateDetectedDefault(
            Guid.NewGuid(),
            FloorPlanLabelOverrideSourceKinds.OpeningLabel,
            Guid.NewGuid(),
            new DateTime(2026, 5, 11, 23, 31, 0, DateTimeKind.Utc));

        Assert.Null(overrideRow.ResolvedTextHeight);
    }

    [Fact]
    public void CreateResolvedTextHeight_rejects_non_label_source_kinds()
    {
        Assert.Throws<ArgumentException>(() =>
            FloorPlanLabelOverride.CreateResolvedTextHeight(
                Guid.NewGuid(),
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                Guid.NewGuid(),
                5m,
                DateTime.UtcNow));
    }

    [Fact]
    public void CreateResolvedTextHeight_rejects_non_positive_heights()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FloorPlanLabelOverride.CreateResolvedTextHeight(
                Guid.NewGuid(),
                FloorPlanLabelOverrideSourceKinds.RoomLabel,
                Guid.NewGuid(),
                0m,
                DateTime.UtcNow));
    }
}
