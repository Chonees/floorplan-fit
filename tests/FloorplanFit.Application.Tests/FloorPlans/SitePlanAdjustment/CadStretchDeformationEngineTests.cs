using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class CadStretchDeformationEngineTests
{
    [Fact]
    public void Apply_uses_one_total_delta_for_paired_faces_and_preserves_rigid_and_fixed_geometry()
    {
        var action = new CadStretchAction(
            "paired-wall",
            "Width",
            "Right",
            DeltaSourceUnits: 2m,
            MaxDeltaSourceUnits: 4m,
            TargetEntityIds: ["face-a", "face-b"],
            Tolerance: 0.001m);
        CadStretchEntity[] entities =
        [
            Entity("face-a", (0m, 0m), (10m, 0m)),
            Entity("face-b", (0m, 4m), (10m, 4m)),
            Entity("rigid", (12m, 1m), (14m, 1m)),
            Entity("fixed", (-4m, 8m), (-2m, 8m))
        ];
        CadStretchEntityRole[] roles =
        [
            new("face-a", CadStretchRole.Stretch, [1]),
            new("face-b", CadStretchRole.Stretch, [1]),
            new("rigid", CadStretchRole.RigidMove, [])
        ];

        var result = CadStretchDeformationEngine.Apply(action, entities, roles);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.Equal(2m, result.Audit.RequestedDeltaSourceUnits);
        Assert.Equal(2m, result.Audit.MeasuredDeltaSourceUnits);
        Assert.Equal(4m, result.Audit.PairSpacingBeforeSourceUnits);
        Assert.Equal(4m, result.Audit.PairSpacingAfterSourceUnits);
        Assert.Equal(2, result.Audit.StretchedEntityCount);
        Assert.Equal(1, result.Audit.RigidMovedEntityCount);
        Assert.Equal(1, result.Audit.FixedEntityCount);

        var faceA = Assert.Single(result.Edits, edit => edit.EntityId == "face-a");
        Assert.Equal(CadStretchRole.Stretch, faceA.Role);
        Assert.Equal(-2m, Assert.Single(faceA.Vertices).DeltaX);
        Assert.Equal(0m, Assert.Single(faceA.Vertices).DeltaY);

        var faceB = Assert.Single(result.Edits, edit => edit.EntityId == "face-b");
        Assert.Equal(-2m, Assert.Single(faceB.Vertices).DeltaX);

        var rigid = Assert.Single(result.Edits, edit => edit.EntityId == "rigid");
        Assert.Equal(CadStretchRole.RigidMove, rigid.Role);
        Assert.All(rigid.Vertices, vertex => Assert.Equal(-2m, vertex.DeltaX));
        Assert.DoesNotContain(result.Edits, edit => edit.EntityId == "fixed");
    }

    [Fact]
    public void Apply_rejects_an_explicit_crossing_and_returns_no_partial_edits()
    {
        var result = CadStretchDeformationEngine.Apply(
            Action(),
            [Entity("face-a", (0m, 0m), (10m, 0m)), Entity("face-b", (0m, 4m), (10m, 4m)), Entity("wire", (4m, 2m), (8m, 2m))],
            [
                new("face-a", CadStretchRole.Stretch, [1]),
                new("face-b", CadStretchRole.Stretch, [1]),
                new("wire", CadStretchRole.Rejected, [], "Unselected entity crosses the cut.")
            ]);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Edits);
        Assert.Contains("crosses", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_rejects_duplicate_roles_and_returns_no_partial_edits()
    {
        var result = CadStretchDeformationEngine.Apply(
            Action(),
            [Entity("face-a", (0m, 0m), (10m, 0m)), Entity("face-b", (0m, 4m), (10m, 4m))],
            [
                new("face-a", CadStretchRole.Stretch, [1]),
                new("face-a", CadStretchRole.RigidMove, []),
                new("face-b", CadStretchRole.Stretch, [1])
            ]);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Edits);
        Assert.Contains("role", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_rejects_missing_or_unsupported_target_span_and_returns_no_partial_edits()
    {
        var result = CadStretchDeformationEngine.Apply(
            Action(),
            [
                Entity("face-a", (0m, 0m), (10m, 0m), supportsVertexStretch: false),
                Entity("face-b", (0m, 4m), (10m, 4m))
            ],
            [new("face-a", CadStretchRole.Stretch, [1]), new("face-b", CadStretchRole.Stretch, [1])]);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Edits);
        Assert.Contains("support", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    private static CadStretchAction Action() => new(
        "paired-wall",
        "Width",
        "Right",
        DeltaSourceUnits: 2m,
        MaxDeltaSourceUnits: 4m,
        TargetEntityIds: ["face-a", "face-b"],
        Tolerance: 0.001m);

    private static CadStretchEntity Entity(
        string id,
        (decimal X, decimal Y) first,
        (decimal X, decimal Y) second,
        bool supportsVertexStretch = true) =>
        new(
            id,
            "LINE",
            [new CadStretchPoint(first.X, first.Y), new CadStretchPoint(second.X, second.Y)],
            supportsVertexStretch);
}
