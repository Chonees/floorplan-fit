using Avalonia;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class MovableArtifactPreviewControlTests
{
    [Fact]
    public void GetRoomLabelBounds_returns_a_non_empty_hit_target_around_the_rendered_text()
    {
        var pathId = Guid.NewGuid();
        var viewport = FloorPlanPreviewGeometry.CalculateViewport(
            [new GeometryPathDto(pathId, false, [new GeometrySegmentDto(pathId, 1, 0m, 0m, 100m, 100m)])],
            new Rect(0, 0, 500, 500),
            48d)!.Value;
        var roomLabel = new RoomLabelDto(Guid.NewGuid(), "TEXT:1", "ROOM LBLS", "KITCHEN", 25m, 75m, 0.95m, null, 1, TextHeight: 8m);

        var bounds = CadTextPreviewLayerRenderer.GetRoomLabelBounds(roomLabel, viewport);

        Assert.True(bounds.Width > 0d);
        Assert.True(bounds.Height > 0d);
        Assert.True(bounds.Contains(viewport.Project(roomLabel.X, roomLabel.Y)));
    }

    [Fact]
    public void ApplyAbsolutePointDelta_uses_model_space_delta_not_pixels()
    {
        var moved = FloorPlanPreviewControl.ApplyAbsolutePointDelta(240m, 180m, 40m, -24m);

        Assert.Equal(280m, moved.X);
        Assert.Equal(156m, moved.Y);
    }

    [Fact]
    public void ApplyTranslationDelta_preserves_existing_manual_offset_and_adds_new_world_delta()
    {
        var moved = FloorPlanPreviewControl.ApplyTranslationDelta(12m, -6m, 8m, 4m);

        Assert.Equal(20m, moved.Dx);
        Assert.Equal(-2m, moved.Dy);
    }

    [Fact]
    public void BuildHitTestGeometry_still_prioritizes_curated_geometry_before_walls_while_labels_use_their_own_hit_targets()
    {
        var wallPathId = Guid.NewGuid();
        var curatedPathId = Guid.NewGuid();
        var geometry = FloorPlanPreviewControl.BuildHitTestGeometry(
            [
                new GeometryPathDto(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 100m, 0m)]),
                new GeometryPathDto(curatedPathId, false, [new GeometrySegmentDto(curatedPathId, 1, 20m, 0m, 40m, 0m)])
            ],
            curatedArtifacts:
            [
                new CuratedPlanArtifactDto(
                    Guid.NewGuid(),
                    FloorPlanArtifactSourceKinds.OpeningCandidate,
                    "LINE:DOOR:1",
                    "DOORS",
                    "LINE",
                    null,
                    [curatedPathId],
                    0.95m,
                    null,
                    1,
                    FloorPlanArtifactTaxonomy.OpeningFamily,
                    FloorPlanArtifactTaxonomy.OpeningCategory,
                    FloorPlanArtifactTaxonomy.DoorType,
                    FloorPlanArtifactTaxonomy.FixedFamily,
                    FloorPlanArtifactTaxonomy.WetFixtureCategory,
                    FloorPlanArtifactTaxonomy.TubType,
                    FloorPlanArtifactDecisionState.Reclassified.ToString(),
                    "#FFDC2626")
            ]);

        Assert.Equal([curatedPathId, wallPathId], geometry.Select(item => item.Id).ToArray());
    }
}
