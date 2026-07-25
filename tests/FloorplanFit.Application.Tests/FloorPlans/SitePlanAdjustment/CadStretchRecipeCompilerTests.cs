using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class CadStretchRecipeCompilerTests
{
    [Fact]
    public void Compile_allows_two_markers_at_different_cut_coordinates()
    {
        var fixture = Fixture();

        var result = CadStretchRecipeCompiler.Compile(new CadStretchRecipeCompilationRequest(
            fixture.GroupId,
            "Width",
            "Right",
            DeltaSourceUnits: 2m,
            SourceToMillimetersFactor: 1m,
            CoordinateTolerance: 0.001m,
            fixture.Markers,
            fixture.Candidates,
            fixture.Paths));

        Assert.True(result.Succeeded, result.RejectionReason);
        var action = Assert.IsType<AdjustmentRecipeStretchActionDto>(result.Action);
        Assert.Equal(fixture.GroupId.ToString("D"), action.ActionId);
        Assert.Equal(2m, action.DeltaSourceUnits);
        Assert.Equal(4m, action.MaxDeltaSourceUnits);
        Assert.Equal(5m, action.CutCoordinate);
        Assert.Equal(2, action.TargetSpans.Count);
        Assert.All(action.TargetSpans, span => Assert.Equal(1, span.ClosingVertexIndex));
        Assert.Equal(2, action.CanonicalEntityRoles.Count(role => role.Role == "Stretch"));
        Assert.Contains(action.CanonicalEntityRoles, role => role.EntityRef == "BRIDGE:PRIMARY" && role.Role == "RigidMove");
        Assert.DoesNotContain(action.CanonicalEntityRoles, role => role.EntityRef == "FIXED:LEFT");
    }

    [Fact]
    public void Compile_rejects_a_group_that_does_not_have_exactly_two_distinct_faces()
    {
        var fixture = Fixture();

        var result = CadStretchRecipeCompiler.Compile(new CadStretchRecipeCompilationRequest(
            fixture.GroupId,
            "Width",
            "Right",
            2m,
            1m,
            0.001m,
            fixture.Markers.Take(1).ToArray(),
            fixture.Candidates,
            fixture.Paths));

        Assert.False(result.Succeeded);
        Assert.Null(result.Action);
        Assert.Contains("exactly two", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_leaves_an_unrelated_span_crossing_the_cut_implicitly_fixed()
    {
        var fixture = Fixture(includeCrossing: true);

        var result = CadStretchRecipeCompiler.Compile(new CadStretchRecipeCompilationRequest(
            fixture.GroupId,
            "Width",
            "Right",
            2m,
            1m,
            0.001m,
            fixture.Markers,
            fixture.Candidates,
            fixture.Paths));

        Assert.True(result.Succeeded, result.RejectionReason);
        var action = Assert.IsType<AdjustmentRecipeStretchActionDto>(result.Action);
        Assert.DoesNotContain(action.CanonicalEntityRoles, role => role.EntityRef == "CROSSING:UNRELATED");
    }

    [Fact]
    public void Compile_moves_the_connected_closing_side_component_but_not_disconnected_geometry()
    {
        var fixture = Fixture();
        var duplicateBridgePathId = Guid.NewGuid();
        var connectedBranchPathId = Guid.NewGuid();
        var unrelatedClosingPathId = Guid.NewGuid();
        var paths = fixture.Paths.Concat(
        [
            Path(duplicateBridgePathId, 10m, 0m, 10m, 4m),
            new GeometryPathDto(
                connectedBranchPathId,
                false,
                [
                    new GeometrySegmentDto(connectedBranchPathId, 0, 10m, 2m, 14m, 2m),
                    new GeometrySegmentDto(connectedBranchPathId, 1, 14m, 2m, 14m, 6m)
                ]),
            Path(unrelatedClosingPathId, 12m, 8m, 16m, 8m)
        ]).ToArray();
        var candidates = fixture.Candidates.Concat(
        [
            Candidate(Guid.NewGuid(), "BRIDGE:DUPLICATE", duplicateBridgePathId),
            Candidate(Guid.NewGuid(), "COMPONENT:BRANCH", connectedBranchPathId),
            Candidate(Guid.NewGuid(), "CLOSING:UNRELATED", unrelatedClosingPathId)
        ]).ToArray();

        var result = CadStretchRecipeCompiler.Compile(new CadStretchRecipeCompilationRequest(
            fixture.GroupId,
            "Width",
            "Right",
            2m,
            1m,
            0.001m,
            fixture.Markers,
            candidates,
            paths));

        Assert.True(result.Succeeded, result.RejectionReason);
        var action = Assert.IsType<AdjustmentRecipeStretchActionDto>(result.Action);
        Assert.Contains(action.CanonicalEntityRoles, role => role.EntityRef == "BRIDGE:PRIMARY" && role.Role == "RigidMove");
        Assert.Contains(action.CanonicalEntityRoles, role => role.EntityRef == "BRIDGE:DUPLICATE" && role.Role == "RigidMove");
        Assert.Contains(action.CanonicalEntityRoles, role => role.EntityRef == "COMPONENT:BRANCH" && role.Role == "RigidMove");
        Assert.DoesNotContain(action.CanonicalEntityRoles, role => role.EntityRef == "CLOSING:UNRELATED");
    }

    [Fact]
    public void CompileGroup_carries_disconnected_later_stations_and_keeps_physical_pair_water_fill()
    {
        var fixture = GroupFixture();

        var result = CadStretchRecipeCompiler.CompileGroup(GroupRequest(fixture, 3.6m));

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.Collection(
            result.Actions,
            first =>
            {
                Assert.Equal($"{fixture.GroupId:D}:station:0", first.ActionId);
                Assert.Equal(5m, first.CutCoordinate);
                Assert.Equal(2.6m, first.DeltaSourceUnits);
                Assert.Equal(4m, first.MaxDeltaSourceUnits);
                Assert.Equal(2, first.TargetSpans.Count);
                var carriedTarget = Assert.Single(first.CanonicalEntityRoles.Where(
                    role => role.EntityRef == "STATION:1:A" && role.Role == "RigidMove"));
                Assert.Null(carriedTarget.SegmentSortOrder);
                Assert.Empty(carriedTarget.VertexIndices);
                Assert.Contains(
                    first.CanonicalEntityRoles,
                    role => role.EntityRef == "STATION:1:B" && role.Role == "RigidMove");
                Assert.Contains(
                    first.CanonicalEntityRoles,
                    role => role.EntityRef == "STATION:1:CLOSING" && role.Role == "RigidMove");
                Assert.DoesNotContain(
                    first.CanonicalEntityRoles,
                    role => role.EntityRef == "STATION:0:A" && role.Role == "RigidMove");
            },
            second =>
            {
                Assert.Equal($"{fixture.GroupId:D}:station:1", second.ActionId);
                Assert.Equal(15m, second.CutCoordinate);
                Assert.Equal(1m, second.DeltaSourceUnits);
                Assert.Equal(1m, second.MaxDeltaSourceUnits);
                Assert.Equal(2, second.TargetSpans.Count);
                Assert.Contains(
                    second.CanonicalEntityRoles,
                    role => role.EntityRef == "STATION:1:CLOSING" && role.Role == "RigidMove");
            });
        Assert.Equal(3.6m, result.Actions.Sum(action => action.DeltaSourceUnits));
    }

    [Fact]
    public void CompileGroup_orders_Left_stations_by_descending_cut_and_reassigns_action_ids()
    {
        var fixture = GroupFixture();

        var result = CadStretchRecipeCompiler.CompileGroup(GroupRequest(fixture, 3.6m, "Left"));

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.Collection(
            result.Actions,
            first =>
            {
                Assert.Equal($"{fixture.GroupId:D}:station:0", first.ActionId);
                Assert.Equal(15m, first.CutCoordinate);
                Assert.Equal(1m, first.DeltaSourceUnits);
                Assert.Contains(first.TargetSpans, span => span.SourceEntityRef == "STATION:1:A");
            },
            second =>
            {
                Assert.Equal($"{fixture.GroupId:D}:station:1", second.ActionId);
                Assert.Equal(5m, second.CutCoordinate);
                Assert.Equal(2.6m, second.DeltaSourceUnits);
                Assert.Contains(second.TargetSpans, span => span.SourceEntityRef == "STATION:0:A");
            });
    }

    [Fact]
    public void CompileGroup_rejects_an_odd_marker_count_atomically()
    {
        var fixture = GroupFixture();
        var request = GroupRequest(fixture, 3m) with
        {
            Markers = fixture.Markers.Take(3).ToArray()
        };

        var result = CadStretchRecipeCompiler.CompileGroup(request);

        Assert.False(result.Succeeded);
        Assert.Empty(result.Actions);
        Assert.Contains("even", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompileGroup_rejects_a_total_over_combined_capacity_atomically()
    {
        var fixture = GroupFixture();

        var result = CadStretchRecipeCompiler.CompileGroup(GroupRequest(fixture, 5.1m));

        Assert.False(result.Succeeded);
        Assert.Empty(result.Actions);
        Assert.Contains("capacity", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_rejects_duplicate_geometry_ids_instead_of_throwing()
    {
        var fixture = Fixture();

        var result = CadStretchRecipeCompiler.Compile(new CadStretchRecipeCompilationRequest(
            fixture.GroupId,
            "Width",
            "Right",
            2m,
            1m,
            0.001m,
            fixture.Markers,
            fixture.Candidates,
            fixture.Paths.Concat([fixture.Paths[0]]).ToArray()));

        Assert.False(result.Succeeded);
        Assert.Null(result.Action);
        Assert.Contains("duplicate geometry", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_rejects_duplicate_candidate_ids_instead_of_throwing()
    {
        var fixture = Fixture();

        var result = CadStretchRecipeCompiler.Compile(new CadStretchRecipeCompilationRequest(
            fixture.GroupId,
            "Width",
            "Right",
            2m,
            1m,
            0.001m,
            fixture.Markers,
            fixture.Candidates.Concat([fixture.Candidates[0]]).ToArray(),
            fixture.Paths));

        Assert.False(result.Succeeded);
        Assert.Null(result.Action);
        Assert.Contains("duplicate wall candidate", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_rejects_a_multi_segment_target_before_preview_can_disconnect_it()
    {
        var fixture = Fixture();
        var original = fixture.Paths[0];
        var multiSegmentTarget = new GeometryPathDto(
            original.Id,
            false,
            [
                new GeometrySegmentDto(original.Id, 0, 0m, 0m, 5m, 0m),
                new GeometrySegmentDto(original.Id, 1, 5m, 0m, 10m, 0m)
            ]);
        var paths = fixture.Paths
            .Select(path => path.Id == original.Id ? multiSegmentTarget : path)
            .ToArray();

        var result = CadStretchRecipeCompiler.Compile(new CadStretchRecipeCompilationRequest(
            fixture.GroupId,
            "Width",
            "Right",
            2m,
            1m,
            0.001m,
            fixture.Markers,
            fixture.Candidates,
            paths));

        Assert.False(result.Succeeded);
        Assert.Null(result.Action);
        Assert.Contains("single segment", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    private static CompilerFixture Fixture(bool includeCrossing = false)
    {
        var groupId = Guid.NewGuid();
        var faceAPathId = Guid.NewGuid();
        var faceBPathId = Guid.NewGuid();
        var rigidPathId = Guid.NewGuid();
        var fixedPathId = Guid.NewGuid();
        var crossingPathId = Guid.NewGuid();
        var faceACandidateId = Guid.NewGuid();
        var faceBCandidateId = Guid.NewGuid();
        var rigidCandidateId = Guid.NewGuid();
        var fixedCandidateId = Guid.NewGuid();
        var crossingCandidateId = Guid.NewGuid();

        var paths = new List<GeometryPathDto>
        {
            Path(faceAPathId, 0m, 0m, 10m, 0m),
            Path(faceBPathId, 0m, 4m, 10m, 4m),
            Path(rigidPathId, 10m, 0m, 10m, 4m),
            Path(fixedPathId, -4m, 0m, -4m, 4m)
        };
        var candidates = new List<WallCandidateDto>
        {
            Candidate(faceACandidateId, "FACE:A", faceAPathId),
            Candidate(faceBCandidateId, "FACE:B", faceBPathId),
            Candidate(rigidCandidateId, "BRIDGE:PRIMARY", rigidPathId),
            Candidate(fixedCandidateId, "FIXED:LEFT", fixedPathId)
        };
        if (includeCrossing)
        {
            paths.Add(Path(crossingPathId, 2m, 8m, 8m, 8m));
            candidates.Add(Candidate(crossingCandidateId, "CROSSING:UNRELATED", crossingPathId));
        }

        PinchMarkerDto[] markers =
        [
            new(Guid.NewGuid(), groupId, "Room", faceACandidateId, faceAPathId, "Width", 0.4m, 6m, 1),
            new(Guid.NewGuid(), groupId, "Room", faceBCandidateId, faceBPathId, "Width", 0.6m, 4m, 2)
        ];

        return new CompilerFixture(groupId, markers, candidates, paths);
    }

    private static CompilerFixture GroupFixture()
    {
        var groupId = Guid.NewGuid();
        var pathIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        var candidateIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray();
        GeometryPathDto[] paths =
        [
            Path(pathIds[0], 0m, 0m, 10m, 0m),
            Path(pathIds[1], 0m, 4m, 10m, 4m),
            Path(pathIds[2], 10m, 10m, 20m, 10m),
            Path(pathIds[3], 10m, 14m, 20m, 14m),
            Path(pathIds[4], 10m, 0m, 10m, 4m),
            Path(pathIds[5], 20m, 10m, 20m, 14m)
        ];
        WallCandidateDto[] candidates =
        [
            Candidate(candidateIds[0], "STATION:0:A", pathIds[0]),
            Candidate(candidateIds[1], "STATION:0:B", pathIds[1]),
            Candidate(candidateIds[2], "STATION:1:A", pathIds[2]),
            Candidate(candidateIds[3], "STATION:1:B", pathIds[3]),
            Candidate(candidateIds[4], "STATION:0:CLOSING", pathIds[4]),
            Candidate(candidateIds[5], "STATION:1:CLOSING", pathIds[5])
        ];
        PinchMarkerDto[] markers =
        [
            new(Guid.NewGuid(), groupId, "Room", candidateIds[0], pathIds[0], "Width", 0.4m, 4m, 1),
            new(Guid.NewGuid(), groupId, "Room", candidateIds[1], pathIds[1], "Width", 0.6m, 6m, 2),
            new(Guid.NewGuid(), groupId, "Room", candidateIds[2], pathIds[2], "Width", 0.4m, 1m, 3),
            new(Guid.NewGuid(), groupId, "Room", candidateIds[3], pathIds[3], "Width", 0.6m, 3m, 4)
        ];

        return new CompilerFixture(groupId, markers, candidates, paths);
    }

    private static CadStretchRecipeCompilationRequest GroupRequest(
        CompilerFixture fixture,
        decimal deltaSourceUnits,
        string edge = "Right")
        => new(
            fixture.GroupId,
            "Width",
            edge,
            deltaSourceUnits,
            1m,
            0.001m,
            fixture.Markers,
            fixture.Candidates,
            fixture.Paths);

    private static GeometryPathDto Path(Guid id, decimal x1, decimal y1, decimal x2, decimal y2)
        => new(id, false, [new GeometrySegmentDto(id, 0, x1, y1, x2, y2)]);

    private static WallCandidateDto Candidate(Guid id, string sourceRef, Guid pathId)
        => new(id, sourceRef, "WALL", "Accepted", 1m, 4m, null, pathId, 0);

    private sealed record CompilerFixture(
        Guid GroupId,
        IReadOnlyList<PinchMarkerDto> Markers,
        IReadOnlyList<WallCandidateDto> Candidates,
        IReadOnlyList<GeometryPathDto> Paths);
}
