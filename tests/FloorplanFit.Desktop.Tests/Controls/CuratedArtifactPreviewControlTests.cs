using Avalonia.Media;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Desktop.Controls;
using FloorplanFit.Desktop.Controls.Preview;
using FloorplanFit.Domain.FloorPlans;
using Xunit;

namespace FloorplanFit.Desktop.Tests.Controls;

public sealed class CuratedArtifactPreviewControlTests
{
    [Fact]
    public void Preview_control_exposes_curated_plan_artifacts_for_canvas_overlay()
    {
        CuratedPlanArtifactDto[] artifacts =
        [
            new(
                Guid.NewGuid(),
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                "LINE:1",
                "DOORS",
                "LINE",
                null,
                [Guid.NewGuid()],
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
        ];

        var control = new FloorPlanPreviewControl
        {
            CuratedPlanArtifacts = artifacts
        };

        Assert.Same(artifacts, control.CuratedPlanArtifacts);
    }

    [Fact]
    public void BuildHitTestGeometry_prioritizes_curated_artifact_family_after_reclassification()
    {
        var wallPathId = Guid.NewGuid();
        var reclassifiedPathId = Guid.NewGuid();
        GeometryPathDto[] geometryPaths =
        [
            new(wallPathId, false, [new GeometrySegmentDto(wallPathId, 1, 0m, 0m, 120m, 0m)]),
            new(reclassifiedPathId, false, [new GeometrySegmentDto(reclassifiedPathId, 1, 40m, 0m, 76m, 0m)])
        ];
        CuratedPlanArtifactDto[] curatedArtifacts =
        [
            new(
                Guid.NewGuid(),
                FloorPlanArtifactSourceKinds.OpeningCandidate,
                "LINE:1",
                "DOORS",
                "LINE",
                null,
                [reclassifiedPathId],
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
        ];

        var hitTestGeometry = FloorPlanPreviewControl.BuildHitTestGeometry(
            geometryPaths,
            curatedArtifacts: curatedArtifacts);

        Assert.Equal([reclassifiedPathId, wallPathId], hitTestGeometry.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void CuratedArtifactPreviewLayerRenderer_uses_resolved_semantic_color_and_selection_highlight()
    {
        var artifact = new CuratedPlanArtifactDto(
            Guid.NewGuid(),
            FloorPlanArtifactSourceKinds.FixedPlanComponent,
            "INSERT:1",
            "FIXTURES",
            "INSERT",
            "TOILET1",
            [Guid.NewGuid()],
            0.95m,
            null,
            1,
            FloorPlanArtifactTaxonomy.FixedFamily,
            FloorPlanArtifactTaxonomy.GenericFixedCategory,
            FloorPlanArtifactTaxonomy.GenericFixtureType,
            FloorPlanArtifactTaxonomy.FixedFamily,
            FloorPlanArtifactTaxonomy.WetFixtureCategory,
            FloorPlanArtifactTaxonomy.TubType,
            FloorPlanArtifactDecisionState.Reclassified.ToString(),
            "#FFDC2626");

        var normalPen = CuratedArtifactPreviewLayerRenderer.CreatePen(artifact, isHighlighted: false);
        var highlightedPen = CuratedArtifactPreviewLayerRenderer.CreatePen(artifact, isHighlighted: true);

        Assert.Equal(Color.Parse("#FFDC2626"), Assert.IsType<SolidColorBrush>(normalPen.Brush).Color);
        Assert.Equal(Colors.SeaGreen, Assert.IsType<SolidColorBrush>(highlightedPen.Brush).Color);
        Assert.True(highlightedPen.Thickness > normalPen.Thickness);
    }
}
