using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class CommissionExistingCurationProfileCompilerTests
{
    [Fact]
    public void Compile_emits_the_exact_curation_identity_and_max_capacity_width_depth_profile()
    {
        var fixture = Fixture();

        var result = CommissionExistingCurationProfileCompiler.Compile(fixture.Request);

        Assert.True(result.Succeeded, result.RejectionReason);
        var profile = Assert.IsType<CommissionedHouseAdaptationProfile>(result.Profile);
        Assert.Equal(fixture.FloorPlanVersionId, profile.FloorPlanVersionId);
        Assert.Equal(fixture.PublishedCurationId, profile.PublishedCurationId);
        Assert.Equal(1m, profile.SourceToMillimetersFactor);
        Assert.Collection(
            profile.Variables,
            width =>
            {
                Assert.Equal("width", width.Id);
                Assert.Equal(HouseAdaptationAxis.Width, width.Axis);
                var action = Assert.Single(width.ActionTemplates);
                Assert.Equal($"{fixture.WidthGroupId:D}:station:0", action.ActionId);
                Assert.Equal(4m, action.MaxDeltaSourceUnits);
                Assert.Equal(action.MaxDeltaSourceUnits, action.DeltaSourceUnits);
            },
            depth =>
            {
                Assert.Equal("depth", depth.Id);
                Assert.Equal(HouseAdaptationAxis.Depth, depth.Axis);
                var action = Assert.Single(depth.ActionTemplates);
                Assert.Equal($"{fixture.DepthGroupId:D}:station:0", action.ActionId);
                Assert.Equal(3m, action.MaxDeltaSourceUnits);
                Assert.Equal(action.MaxDeltaSourceUnits, action.DeltaSourceUnits);
            });
        Assert.Empty(profile.ImmutableSizeEntityRefs);
        Assert.Equal(["LABEL:COVERAGE"], profile.ProtectedEntityRefs);
        var auxiliary = Assert.Single(profile.AuxiliaryEntityBindings);
        Assert.Equal("LABEL:COVERAGE", auxiliary.SourceEntityRef);
        Assert.Equal(CommissionExistingCurationAuxiliaryEntityKind.Label, auxiliary.Kind);
        Assert.True(auxiliary.IsProtected);
        Assert.True(CommissionedHouseAdaptationProfileReadiness.Evaluate(profile).IsReady);
    }

    [Fact]
    public void Compile_is_deterministic_when_current_curation_inputs_arrive_in_reverse_order()
    {
        var fixture = Fixture();
        var reversed = fixture.Request with
        {
            VariableSelections = fixture.Request.VariableSelections!.Reverse().ToArray(),
            PinchGroups = fixture.Request.PinchGroups!.Reverse().ToArray(),
            Markers = fixture.Request.Markers!.Reverse().ToArray(),
            WallCandidates = fixture.Request.WallCandidates!.Reverse().ToArray(),
            GeometryPaths = fixture.Request.GeometryPaths!.Reverse().ToArray()
        };

        var first = CommissionExistingCurationProfileCompiler.Compile(fixture.Request);
        var second = CommissionExistingCurationProfileCompiler.Compile(reversed);

        Assert.True(first.Succeeded, first.RejectionReason);
        Assert.True(second.Succeeded, second.RejectionReason);
        Assert.Equal(ProfileSignature(first.Profile!), ProfileSignature(second.Profile!));
    }

    [Fact]
    public void Compile_classifies_a_moving_immutable_opening_as_rigid_move()
    {
        var fixture = Fixture();
        var openingPathId = Guid.NewGuid();
        var opening = new CommissionExistingCurationAuxiliaryEntityBinding(
            "OPENING:RIGHT",
            CommissionExistingCurationAuxiliaryEntityKind.Opening,
            new AdjustmentRecipeBoundsDto(10m, 1m, 10m, 2m),
            openingPathId,
            SegmentSortOrder: 0,
            IsImmutableSize: true,
            IsProtected: false)
        {
            HostGeometryPathId = fixture.WidthClosingPathId,
            HostSegmentSortOrder = 0
        };
        var request = fixture.Request with
        {
            GeometryPaths = fixture.Request.GeometryPaths!
                .Append(Path(openingPathId, 10m, 1m, 10m, 2m))
                .ToArray(),
            AuxiliaryEntityBindings =
            [
                .. fixture.Request.AuxiliaryEntityBindings!,
                opening
            ]
        };

        var result = CommissionExistingCurationProfileCompiler.Compile(request);

        Assert.True(result.Succeeded, result.RejectionReason);
        var profile = Assert.IsType<CommissionedHouseAdaptationProfile>(result.Profile);
        Assert.Equal(["OPENING:RIGHT"], profile.ImmutableSizeEntityRefs);
        var width = Assert.Single(profile.Variables.Where(variable => variable.Axis == HouseAdaptationAxis.Width));
        var role = Assert.Single(width.ActionTemplates[0].CanonicalEntityRoles.Where(
            candidate => candidate.EntityRef == "OPENING:RIGHT"));
        Assert.Equal("RigidMove", role.Role);
        Assert.Equal(openingPathId, role.GeometryPathId);
        Assert.Equal(0, role.SegmentSortOrder);
        Assert.Equal(fixture.WidthClosingPathId, role.HostGeometryPathId);
        Assert.Equal(0, role.HostSegmentSortOrder);
        Assert.Empty(role.VertexIndices);
        var depth = Assert.Single(profile.Variables.Where(variable => variable.Axis == HouseAdaptationAxis.Depth));
        var depthRole = Assert.Single(depth.ActionTemplates[0].CanonicalEntityRoles.Where(
            candidate => candidate.EntityRef == "OPENING:RIGHT"));
        Assert.Equal("Fixed", depthRole.Role);
        Assert.Equal(openingPathId, depthRole.GeometryPathId);
        Assert.Equal(fixture.WidthClosingPathId, depthRole.HostGeometryPathId);
        Assert.DoesNotContain(
            profile.Variables.SelectMany(variable => variable.ActionTemplates).SelectMany(action => action.CanonicalEntityRoles),
            candidate => candidate.EntityRef == "OPENING:RIGHT" && candidate.Role == "Stretch");

        var hostWallRole = Assert.Single(width.ActionTemplates[0].CanonicalEntityRoles.Where(
            candidate => candidate.GeometryPathId == fixture.WidthClosingPathId));
        Assert.Equal("RigidMove", hostWallRole.Role);
        Assert.Null(hostWallRole.SegmentSortOrder);
        Assert.Null(hostWallRole.HostGeometryPathId);
        Assert.Null(hostWallRole.HostSegmentSortOrder);
        Assert.Equal(
            4m,
            fixture.Request.WallCandidates!.Single(candidate => candidate.GeometryPathId == fixture.WidthClosingPathId).ThicknessMm);

        var protectedRole = Assert.Single(width.ActionTemplates[0].CanonicalEntityRoles.Where(
            candidate => candidate.EntityRef == "LABEL:COVERAGE"));
        Assert.Equal("Fixed", protectedRole.Role);
        Assert.Null(protectedRole.HostGeometryPathId);
        Assert.Null(protectedRole.HostSegmentSortOrder);

        var auxiliary = Assert.Single(
            profile.AuxiliaryEntityBindings,
            candidate => candidate.SourceEntityRef == "OPENING:RIGHT");
        Assert.Equal("OPENING:RIGHT", auxiliary.SourceEntityRef);
        Assert.True(auxiliary.IsImmutableSize);
        Assert.Equal(openingPathId, auxiliary.GeometryPathId);
        Assert.Equal(fixture.WidthClosingPathId, auxiliary.HostGeometryPathId);
        Assert.NotEqual(auxiliary.GeometryPathId, auxiliary.HostGeometryPathId);
    }

    [Fact]
    public void Compile_rejects_empty_auxiliary_coverage_instead_of_emitting_a_falsely_ready_profile()
    {
        var fixture = Fixture();

        var result = CommissionExistingCurationProfileCompiler.Compile(
            fixture.Request with { AuxiliaryEntityBindings = [] });

        AssertRejected(result, "auxiliary coverage");
    }

    [Fact]
    public void Compile_rejects_an_opening_without_immutable_size_or_complete_wall_host_binding()
    {
        var fixture = Fixture();
        var openingPathId = Guid.NewGuid();
        var paths = fixture.Request.GeometryPaths!
            .Append(Path(openingPathId, 8m, 1m, 9m, 2m))
            .ToArray();
        var opening = new CommissionExistingCurationAuxiliaryEntityBinding(
            "OPENING:INCOMPLETE",
            CommissionExistingCurationAuxiliaryEntityKind.Opening,
            new AdjustmentRecipeBoundsDto(8m, 1m, 9m, 2m),
            openingPathId,
            SegmentSortOrder: 0,
            IsImmutableSize: false,
            IsProtected: false)
        {
            HostGeometryPathId = fixture.WidthClosingPathId,
            HostSegmentSortOrder = 0
        };

        AssertRejected(
            CommissionExistingCurationProfileCompiler.Compile(
                fixture.Request with
                {
                    GeometryPaths = paths,
                    AuxiliaryEntityBindings = [opening]
                }),
            "immutable");
        AssertRejected(
            CommissionExistingCurationProfileCompiler.Compile(
                fixture.Request with
                {
                    GeometryPaths = paths,
                    AuxiliaryEntityBindings = [opening with
                    {
                        HostSegmentSortOrder = null,
                        IsImmutableSize = true
                    }]
                }),
            "host");
    }

    [Fact]
    public void Compile_rejects_a_non_wall_or_duplicate_accepted_wall_host_with_the_opening_reference()
    {
        var fixture = Fixture();
        var openingPathId = Guid.NewGuid();
        var nonWallPathId = Guid.NewGuid();
        var paths = fixture.Request.GeometryPaths!
            .Append(Path(openingPathId, 10m, 1m, 10m, 2m))
            .Append(Path(nonWallPathId, 10m, 1m, 10m, 2m))
            .ToArray();
        var opening = new CommissionExistingCurationAuxiliaryEntityBinding(
            "OPENING:HOST-CHECK",
            CommissionExistingCurationAuxiliaryEntityKind.Opening,
            new AdjustmentRecipeBoundsDto(10m, 1m, 10m, 2m),
            openingPathId,
            SegmentSortOrder: 0,
            IsImmutableSize: true,
            IsProtected: false)
        {
            HostGeometryPathId = nonWallPathId,
            HostSegmentSortOrder = 0
        };

        var nonWall = CommissionExistingCurationProfileCompiler.Compile(
            fixture.Request with
            {
                GeometryPaths = paths,
                AuxiliaryEntityBindings = [opening]
            });
        var duplicateWall = CommissionExistingCurationProfileCompiler.Compile(
            fixture.Request with
            {
                GeometryPaths = paths,
                WallCandidates =
                [
                    .. fixture.Request.WallCandidates!,
                    Candidate(Guid.NewGuid(), "WIDTH:CLOSING:DUPLICATE", fixture.WidthClosingPathId, 99)
                ],
                AuxiliaryEntityBindings =
                [
                    opening with { HostGeometryPathId = fixture.WidthClosingPathId }
                ]
            });

        AssertRejected(nonWall, "OPENING:HOST-CHECK");
        AssertRejected(nonWall, "accepted structural wall");
        AssertRejected(duplicateWall, "OPENING:HOST-CHECK");
        AssertRejected(duplicateWall, "duplicate");
    }

    [Fact]
    public void Compile_rejects_a_protected_auxiliary_without_protected_semantics()
    {
        var fixture = Fixture();
        var contradictory = new CommissionExistingCurationAuxiliaryEntityBinding(
            "PROTECTED:CONTRADICTORY",
            CommissionExistingCurationAuxiliaryEntityKind.ProtectedEntity,
            new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
            GeometryPathId: null,
            SegmentSortOrder: null,
            IsImmutableSize: false,
            IsProtected: false);

        var result = CommissionExistingCurationProfileCompiler.Compile(
            fixture.Request with { AuxiliaryEntityBindings = [contradictory] });

        AssertRejected(result, "protected semantics");
    }

    [Fact]
    public void Compile_classifies_a_fixed_source_only_label_with_null_geometry_path_as_fixed()
    {
        var fixture = Fixture();
        var request = fixture.Request with
        {
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    "LABEL:LEFT",
                    CommissionExistingCurationAuxiliaryEntityKind.Label,
                    new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                    GeometryPathId: null,
                    SegmentSortOrder: null,
                    IsImmutableSize: false,
                    IsProtected: true)
            ]
        };

        var result = CommissionExistingCurationProfileCompiler.Compile(request);

        Assert.True(result.Succeeded, result.RejectionReason);
        var profile = Assert.IsType<CommissionedHouseAdaptationProfile>(result.Profile);
        Assert.Equal(["LABEL:LEFT"], profile.ProtectedEntityRefs);
        var width = Assert.Single(profile.Variables.Where(variable => variable.Axis == HouseAdaptationAxis.Width));
        var role = Assert.Single(width.ActionTemplates[0].CanonicalEntityRoles.Where(
            candidate => candidate.EntityRef == "LABEL:LEFT"));
        Assert.Equal("Fixed", role.Role);
        Assert.Null(role.GeometryPathId);
        Assert.Null(role.SegmentSortOrder);
    }

    [Fact]
    public void Compile_rejects_a_crossing_auxiliary_atomically()
    {
        var fixture = Fixture();
        var request = fixture.Request with
        {
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    "LABEL:CROSSING",
                    CommissionExistingCurationAuxiliaryEntityKind.Label,
                    new AdjustmentRecipeBoundsDto(4m, 1m, 6m, 2m),
                    GeometryPathId: null,
                    SegmentSortOrder: null,
                    IsImmutableSize: false,
                    IsProtected: false)
            ]
        };

        var result = CommissionExistingCurationProfileCompiler.Compile(request);

        Assert.False(result.Succeeded);
        Assert.Null(result.Profile);
        Assert.Contains("cut", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compile_rejects_a_missing_axis_variable_without_a_partial_profile()
    {
        var fixture = Fixture();
        var request = fixture.Request with
        {
            VariableSelections = fixture.Request.VariableSelections!
                .Where(variable => variable.Axis == HouseAdaptationAxis.Width)
                .ToArray()
        };

        var result = CommissionExistingCurationProfileCompiler.Compile(request);

        AssertRejected(result, "exactly one");
    }

    [Fact]
    public void Compile_rejects_a_group_selected_for_the_wrong_axis()
    {
        var fixture = Fixture();
        var width = fixture.Request.VariableSelections!
            .Single(variable => variable.Axis == HouseAdaptationAxis.Width) with
        {
            PinchGroups = [new(fixture.DepthGroupId, "Right")]
        };
        var depth = fixture.Request.VariableSelections!
            .Single(variable => variable.Axis == HouseAdaptationAxis.Depth);
        var request = fixture.Request with { VariableSelections = [width, depth] };

        var result = CommissionExistingCurationProfileCompiler.Compile(request);

        AssertRejected(result, "axis");
    }

    [Fact]
    public void Compile_rejects_a_duplicate_explicit_group_selection()
    {
        var fixture = Fixture();
        var width = fixture.Request.VariableSelections!
            .Single(variable => variable.Axis == HouseAdaptationAxis.Width) with
        {
            PinchGroups =
            [
                new(fixture.WidthGroupId, "Right"),
                new(fixture.WidthGroupId, "Right")
            ]
        };
        var depth = fixture.Request.VariableSelections!
            .Single(variable => variable.Axis == HouseAdaptationAxis.Depth);
        var request = fixture.Request with { VariableSelections = [width, depth] };

        var result = CommissionExistingCurationProfileCompiler.Compile(request);

        AssertRejected(result, "duplicate");
    }

    [Fact]
    public void Compile_rejects_invalid_identity_factor_and_tolerance_envelopes()
    {
        var fixture = Fixture();

        AssertRejected(
            CommissionExistingCurationProfileCompiler.Compile(
                fixture.Request with { FloorPlanVersionId = Guid.Empty }),
            "identities");
        AssertRejected(
            CommissionExistingCurationProfileCompiler.Compile(
                fixture.Request with { SourceToMillimetersFactor = 0m }),
            "factor");
        AssertRejected(
            CommissionExistingCurationProfileCompiler.Compile(
                fixture.Request with { CoordinateTolerance = -0.001m }),
            "tolerance");
    }

    [Fact]
    public void Compile_rejects_odd_marker_pairing_before_emitting_any_profile()
    {
        var fixture = Fixture();
        var request = fixture.Request with
        {
            Markers = fixture.Request.Markers!
                .Where(marker => marker.PinchGroupId != fixture.WidthGroupId)
                .Append(fixture.Request.Markers!.First(marker => marker.PinchGroupId == fixture.WidthGroupId))
                .ToArray()
        };

        var result = CommissionExistingCurationProfileCompiler.Compile(request);

        AssertRejected(result, "even");
    }

    [Fact]
    public void Compile_rejects_duplicate_auxiliary_refs_and_unsupported_edges()
    {
        var fixture = Fixture();
        CommissionExistingCurationAuxiliaryEntityBinding auxiliary = new(
            "LABEL:DUPLICATE",
            CommissionExistingCurationAuxiliaryEntityKind.Label,
            new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
            GeometryPathId: null,
            SegmentSortOrder: null,
            IsImmutableSize: false,
            IsProtected: false);
        var duplicateAuxiliaryRequest = fixture.Request with
        {
            AuxiliaryEntityBindings = [auxiliary, auxiliary]
        };
        var width = fixture.Request.VariableSelections!
            .Single(variable => variable.Axis == HouseAdaptationAxis.Width) with
        {
            PinchGroups = [new(fixture.WidthGroupId, "Center")]
        };
        var depth = fixture.Request.VariableSelections!
            .Single(variable => variable.Axis == HouseAdaptationAxis.Depth);

        AssertRejected(
            CommissionExistingCurationProfileCompiler.Compile(duplicateAuxiliaryRequest),
            "duplicate");
        AssertRejected(
            CommissionExistingCurationProfileCompiler.Compile(
                fixture.Request with { VariableSelections = [width, depth] }),
            "edge");
    }

    private static void AssertRejected(
        CommissionExistingCurationProfileCompilationResult result,
        string reasonFragment)
    {
        Assert.False(result.Succeeded);
        Assert.Null(result.Profile);
        Assert.Contains(reasonFragment, result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    private static string[] ProfileSignature(CommissionedHouseAdaptationProfile profile)
        => profile.AuxiliaryEntityBindings
            .Select(binding =>
                $"auxiliary:{binding.SourceEntityRef}:{binding.Kind}:{binding.GeometryPathId}:{binding.SegmentSortOrder}:{binding.HostGeometryPathId}:{binding.HostSegmentSortOrder}:{binding.IsImmutableSize}:{binding.IsProtected}")
            .Concat(profile.Variables
            .SelectMany(variable => new[]
                {
                    $"variable:{variable.Axis}:{variable.Priority}:{variable.Id}:{variable.Name}"
                }
                .Concat(variable.ActionTemplates.Select(action =>
                    $"action:{action.ActionId}:{action.AxisTag}:{action.Edge}:{action.CutCoordinate}:{action.DeltaSourceUnits}:{action.MaxDeltaSourceUnits}"))
                .Concat(variable.ActionTemplates.SelectMany(action => action.CanonicalEntityRoles.Select(role =>
                    $"role:{action.ActionId}:{role.EntityRef}:{role.GeometryPathId}:{role.SegmentSortOrder}:{role.HostGeometryPathId}:{role.HostSegmentSortOrder}:{role.Role}:{string.Join(",", role.VertexIndices)}"))))
            .ToArray())
            .ToArray();

    private static CompilerFixture Fixture()
    {
        var floorPlanVersionId = Guid.NewGuid();
        var publishedCurationId = Guid.NewGuid();
        var widthGroupId = Guid.NewGuid();
        var depthGroupId = Guid.NewGuid();
        var widthAPathId = Guid.NewGuid();
        var widthBPathId = Guid.NewGuid();
        var widthClosingPathId = Guid.NewGuid();
        var depthAPathId = Guid.NewGuid();
        var depthBPathId = Guid.NewGuid();
        var depthClosingPathId = Guid.NewGuid();
        var widthACandidateId = Guid.NewGuid();
        var widthBCandidateId = Guid.NewGuid();
        var widthClosingCandidateId = Guid.NewGuid();
        var depthACandidateId = Guid.NewGuid();
        var depthBCandidateId = Guid.NewGuid();
        var depthClosingCandidateId = Guid.NewGuid();

        PinchGroupDto[] groups =
        [
            new(widthGroupId, "Width room", "Width", 20),
            new(depthGroupId, "Depth room", "Height", 10)
        ];
        PinchMarkerDto[] markers =
        [
            new(Guid.NewGuid(), widthGroupId, "Width room", widthACandidateId, widthAPathId, "Width", 0.5m, 4m, 1),
            new(Guid.NewGuid(), widthGroupId, "Width room", widthBCandidateId, widthBPathId, "Width", 0.5m, 6m, 2),
            new(Guid.NewGuid(), depthGroupId, "Depth room", depthACandidateId, depthAPathId, "Height", 0.5m, 3m, 1),
            new(Guid.NewGuid(), depthGroupId, "Depth room", depthBCandidateId, depthBPathId, "Height", 0.5m, 5m, 2)
        ];
        WallCandidateDto[] candidates =
        [
            Candidate(widthACandidateId, "WIDTH:A", widthAPathId, 1),
            Candidate(widthBCandidateId, "WIDTH:B", widthBPathId, 2),
            Candidate(widthClosingCandidateId, "WIDTH:CLOSING", widthClosingPathId, 3),
            Candidate(depthACandidateId, "DEPTH:A", depthAPathId, 4),
            Candidate(depthBCandidateId, "DEPTH:B", depthBPathId, 5),
            Candidate(depthClosingCandidateId, "DEPTH:CLOSING", depthClosingPathId, 6)
        ];
        GeometryPathDto[] paths =
        [
            Path(widthAPathId, 0m, 0m, 10m, 0m),
            Path(widthBPathId, 0m, 4m, 10m, 4m),
            Path(widthClosingPathId, 10m, 0m, 10m, 4m),
            Path(depthAPathId, 20m, 0m, 20m, 10m),
            Path(depthBPathId, 24m, 0m, 24m, 10m),
            Path(depthClosingPathId, 20m, 10m, 24m, 10m)
        ];
        CommissionExistingCurationVariableSelection[] variableSelections =
        [
            new(
                "depth",
                "Commissioned depth",
                HouseAdaptationAxis.Depth,
                Priority: 1,
                PinchGroups: [new(depthGroupId, "Top")]),
            new(
                "width",
                "Commissioned width",
                HouseAdaptationAxis.Width,
                Priority: 1,
                PinchGroups: [new(widthGroupId, "Right")])
        ];
        var request = new CommissionExistingCurationProfileCompilationRequest(
            floorPlanVersionId,
            publishedCurationId,
            SourceToMillimetersFactor: 1m,
            CoordinateTolerance: 0.001m,
            variableSelections,
            groups,
            markers,
            candidates,
            paths,
            AuxiliaryEntityBindings:
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    "LABEL:COVERAGE",
                    CommissionExistingCurationAuxiliaryEntityKind.Label,
                    new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                    GeometryPathId: null,
                    SegmentSortOrder: null,
                    IsImmutableSize: false,
                    IsProtected: true)
            ]);

        return new CompilerFixture(
            floorPlanVersionId,
            publishedCurationId,
            widthGroupId,
            depthGroupId,
            widthClosingPathId,
            request);
    }

    private static GeometryPathDto Path(Guid id, decimal x1, decimal y1, decimal x2, decimal y2)
        => new(id, false, [new GeometrySegmentDto(id, 0, x1, y1, x2, y2)]);

    private static WallCandidateDto Candidate(Guid id, string sourceRef, Guid pathId, int sortOrder)
        => new(id, sourceRef, "WALL", "Accepted", 1m, 4m, null, pathId, sortOrder);

    private sealed record CompilerFixture(
        Guid FloorPlanVersionId,
        Guid PublishedCurationId,
        Guid WidthGroupId,
        Guid DepthGroupId,
        Guid WidthClosingPathId,
        CommissionExistingCurationProfileCompilationRequest Request);
}
