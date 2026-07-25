using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class CommissionedHouseAdaptationProfileReadinessTests
{
    private const string CoverageEntityRef = "LABEL:COVERAGE";

    [Fact]
    public void Evaluate_reports_exact_axis_capacities_for_a_complete_profile()
    {
        var profile = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width-1", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth-1", "Height", "Top", 3m)));

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.True(result.IsReady, string.Join("; ", result.Reasons));
        Assert.Equal(1m, result.WidthCapacityInches);
        Assert.Equal(1.5m, result.DepthCapacityInches);
        Assert.Empty(result.Reasons);
    }

    [Fact]
    public void Evaluate_fails_closed_without_a_published_curation_identity()
    {
        var profile = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width-1", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth-1", "Height", "Top", 3m))) with
        {
            PublishedCurationId = Guid.Empty
        };

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.Contains(
            result.Reasons,
            reason => reason.Contains("published curation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_fails_closed_when_auxiliary_coverage_is_empty()
    {
        var profile = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m))) with
        {
            AuxiliaryEntityBindings = []
        };

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.Contains(
            result.Reasons,
            reason => reason.Contains("auxiliary coverage", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_fails_closed_when_an_expected_auxiliary_role_is_missing_or_duplicated()
    {
        var complete = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));
        var width = complete.Variables.Single(variable => variable.Axis == HouseAdaptationAxis.Width);
        var missingRole = complete with
        {
            Variables = complete.Variables
                .Select(variable => variable == width
                    ? variable with
                    {
                        ActionTemplates = variable.ActionTemplates
                            .Select(action => action with
                            {
                                CanonicalEntityRoles = action.CanonicalEntityRoles
                                    .Where(role => role.EntityRef != CoverageEntityRef)
                                    .ToArray()
                            })
                            .ToArray()
                    }
                    : variable)
                .ToArray()
        };
        var duplicateBinding = complete with
        {
            AuxiliaryEntityBindings =
            [
                complete.AuxiliaryEntityBindings[0],
                complete.AuxiliaryEntityBindings[0]
            ]
        };

        var missingResult = CommissionedHouseAdaptationProfileReadiness.Evaluate(missingRole);
        var duplicateResult = CommissionedHouseAdaptationProfileReadiness.Evaluate(duplicateBinding);

        Assert.False(missingResult.IsReady);
        Assert.Contains(
            missingResult.Reasons,
            reason => reason.Contains(CoverageEntityRef, StringComparison.Ordinal) &&
                      reason.Contains("missing", StringComparison.OrdinalIgnoreCase));
        Assert.False(duplicateResult.IsReady);
        Assert.Contains(
            duplicateResult.Reasons,
            reason => reason.Contains(CoverageEntityRef, StringComparison.Ordinal) &&
                      reason.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_fails_closed_when_auxiliary_flags_contradict_the_profile_entity_sets()
    {
        var profile = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m))) with
        {
            ProtectedEntityRefs = []
        };

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.Contains(
            result.Reasons,
            reason => reason.Contains("contradicts", StringComparison.OrdinalIgnoreCase) &&
                      reason.Contains("protected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_fails_closed_for_legacy_missing_or_stale_opening_host_evidence()
    {
        const string openingRef = "OPENING:STALE";
        var openingPath = Guid.NewGuid();
        var staleHostPath = Guid.NewGuid();
        var profile = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));
        var hostRole = profile.Variables
            .Single(variable => variable.Axis == HouseAdaptationAxis.Width)
            .ActionTemplates
            .Single()
            .CanonicalEntityRoles
            .First(role => role.Role == "Stretch");
        var withOpening = profile with
        {
            ImmutableSizeEntityRefs = [openingRef],
            ProtectedEntityRefs = [],
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    openingRef,
                    CommissionExistingCurationAuxiliaryEntityKind.Opening,
                    new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                    openingPath,
                    SegmentSortOrder: 0,
                    IsImmutableSize: true,
                    IsProtected: false)
                {
                    HostGeometryPathId = hostRole.GeometryPathId,
                    HostSegmentSortOrder = hostRole.SegmentSortOrder
                }
            ],
            Variables = profile.Variables
                .Select(variable => variable with
                {
                    ActionTemplates = variable.ActionTemplates
                        .Select(action => action with
                        {
                            CanonicalEntityRoles = action.CanonicalEntityRoles
                                .Where(role => role.EntityRef != CoverageEntityRef)
                                .Append(new AdjustmentRecipeEntityRoleDto(
                                    openingRef,
                                    openingPath,
                                    0,
                                    "RigidMove",
                                    [])
                                {
                                    HostGeometryPathId = hostRole.GeometryPathId,
                                    HostSegmentSortOrder = hostRole.SegmentSortOrder
                                })
                                .ToArray()
                        })
                        .ToArray()
                })
                .ToArray()
        };
        var staleOpening = withOpening with
        {
            Variables = withOpening.Variables
                .Select(variable => variable with
                {
                    ActionTemplates = variable.ActionTemplates
                        .Select(action => action with
                        {
                            CanonicalEntityRoles = action.CanonicalEntityRoles
                                .Select(role => role.EntityRef == openingRef
                                    ? role with { HostGeometryPathId = staleHostPath }
                                    : role)
                                .ToArray()
                        })
                        .ToArray()
                })
                .ToArray()
        };
        var legacyOpening = withOpening with
        {
            AuxiliaryEntityBindings =
            [
                withOpening.AuxiliaryEntityBindings[0] with
                {
                    HostGeometryPathId = null,
                    HostSegmentSortOrder = null
                }
            ],
            Variables = withOpening.Variables
                .Select(variable => variable with
                {
                    ActionTemplates = variable.ActionTemplates
                        .Select(action => action with
                        {
                            CanonicalEntityRoles = action.CanonicalEntityRoles
                                .Select(role => role.EntityRef == openingRef
                                    ? role with
                                    {
                                        HostGeometryPathId = null,
                                        HostSegmentSortOrder = null
                                    }
                                    : role)
                                .ToArray()
                        })
                        .ToArray()
                })
                .ToArray()
        };

        var staleResult = CommissionedHouseAdaptationProfileReadiness.Evaluate(staleOpening);
        var legacyResult = CommissionedHouseAdaptationProfileReadiness.Evaluate(legacyOpening);

        Assert.False(staleResult.IsReady);
        Assert.Contains(
            staleResult.Reasons,
            reason => reason.Contains(openingRef, StringComparison.Ordinal) &&
                      reason.Contains("host", StringComparison.OrdinalIgnoreCase));
        Assert.False(legacyResult.IsReady);
        Assert.Contains(
            legacyResult.Reasons,
            reason => reason.Contains(openingRef, StringComparison.Ordinal) &&
                      reason.Contains("host", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_fails_closed_for_non_wall_or_ambiguous_opening_host_evidence()
    {
        const string openingRef = "OPENING:HOST-PROOF";
        var openingPath = Guid.NewGuid();
        var profile = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));
        var hostRole = profile.Variables
            .Single(variable => variable.Axis == HouseAdaptationAxis.Width)
            .ActionTemplates
            .Single()
            .CanonicalEntityRoles
            .First(role => role.Role == "Stretch");
        var valid = AddOpeningHost(profile, openingRef, openingPath, hostRole.GeometryPathId!.Value, hostRole.SegmentSortOrder!.Value);
        var nonWallPath = Guid.NewGuid();
        var nonWall = RewriteOpeningHost(valid, openingRef, nonWallPath, hostRole.SegmentSortOrder.Value);
        var depth = valid.Variables.Single(variable => variable.Axis == HouseAdaptationAxis.Depth);
        var ambiguous = valid with
        {
            Variables = valid.Variables
                .Select(variable => variable == depth
                    ? variable with
                    {
                        ActionTemplates = variable.ActionTemplates
                            .Select(action => action with
                            {
                                CanonicalEntityRoles = action.CanonicalEntityRoles
                                    .Append(new AdjustmentRecipeEntityRoleDto(
                                        "WALL:HOST:DUPLICATE",
                                        hostRole.GeometryPathId,
                                        hostRole.SegmentSortOrder,
                                        "Fixed",
                                        []))
                                    .ToArray()
                            })
                            .ToArray()
                    }
                    : variable)
                .ToArray()
        };

        var nonWallResult = CommissionedHouseAdaptationProfileReadiness.Evaluate(nonWall);
        var ambiguousResult = CommissionedHouseAdaptationProfileReadiness.Evaluate(ambiguous);

        Assert.False(nonWallResult.IsReady);
        Assert.Contains(
            nonWallResult.Reasons,
            reason => reason.Contains(openingRef, StringComparison.Ordinal) &&
                      reason.Contains("structural wall", StringComparison.OrdinalIgnoreCase));
        Assert.False(ambiguousResult.IsReady);
        Assert.Contains(
            ambiguousResult.Reasons,
            reason => reason.Contains(openingRef, StringComparison.Ordinal) &&
                      (reason.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
                       reason.Contains("ambiguous", StringComparison.OrdinalIgnoreCase)));
    }

    [Theory]
    [InlineData(HouseAdaptationAxis.Width, "Depth")]
    [InlineData(HouseAdaptationAxis.Depth, "Width")]
    public void Evaluate_fails_closed_when_a_required_axis_is_missing(
        HouseAdaptationAxis onlyAxis,
        string missingAxis)
    {
        var axisTag = onlyAxis == HouseAdaptationAxis.Width ? "Width" : "Height";
        var edge = onlyAxis == HouseAdaptationAxis.Width ? "Right" : "Top";
        var profile = Profile(Variable("only", onlyAxis, Template("only-1", axisTag, edge, 2m)));

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.Contains(result.Reasons, reason => reason.Contains(missingAxis, StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_fails_closed_when_an_immutable_entity_would_stretch()
    {
        const string immutableEntity = "OPENING:IMMUTABLE";
        var profile = Profile(
            [immutableEntity],
            Variable("width", HouseAdaptationAxis.Width, Template("unsafe", "Width", "Right", 2m, immutableEntity)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.Contains(result.Reasons, reason => reason.Contains(immutableEntity, StringComparison.Ordinal));
    }

    [Fact]
    public void Evaluate_fails_closed_when_a_stretch_role_disagrees_with_its_target_span()
    {
        var width = Template("width", "Width", "Right", 2m);
        var mismatchedRoles = width.CanonicalEntityRoles
            .Select((role, index) => index == 0
                ? role with { VertexIndices = [0] }
                : role)
            .ToArray();
        var profile = Profile(
            Variable(
                "width",
                HouseAdaptationAxis.Width,
                width with { CanonicalEntityRoles = mismatchedRoles }),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.Contains(result.Reasons, reason => reason.Contains("closing vertex", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("Fixed")]
    [InlineData("RigidMove")]
    public void Evaluate_accepts_a_source_only_annotation_role_without_geometry_identity(string roleName)
    {
        var profile = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m))) with
        {
            AuxiliaryEntityBindings =
            [
                .. ProfileAuxiliaryCoverage(),
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    "LABEL:SOURCE",
                    CommissionExistingCurationAuxiliaryEntityKind.Label,
                    new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                    GeometryPathId: null,
                    SegmentSortOrder: null,
                    IsImmutableSize: false,
                    IsProtected: false)
            ]
        };
        profile = profile with
        {
            Variables = profile.Variables
                .Select(variable => variable with
                {
                    ActionTemplates = variable.ActionTemplates
                        .Select(action => action with
                        {
                            CanonicalEntityRoles = action.CanonicalEntityRoles
                                .Append(new AdjustmentRecipeEntityRoleDto(
                                    "LABEL:SOURCE",
                                    null,
                                    null,
                                    roleName,
                                    []))
                                .ToArray()
                        })
                        .ToArray()
                })
                .ToArray()
        };

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.True(result.IsReady, string.Join("; ", result.Reasons));
    }

    [Fact]
    public void Evaluate_fails_closed_for_an_untracked_source_only_auxiliary_role()
    {
        var profile = Profile(
            Variable("width", HouseAdaptationAxis.Width, Template("width", "Width", "Right", 2m)),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));
        var width = profile.Variables.Single(variable => variable.Axis == HouseAdaptationAxis.Width);
        profile = profile with
        {
            Variables = profile.Variables
                .Select(variable => variable == width
                    ? variable with
                    {
                        ActionTemplates = variable.ActionTemplates
                            .Select(action => action with
                            {
                                CanonicalEntityRoles = action.CanonicalEntityRoles
                                    .Append(new AdjustmentRecipeEntityRoleDto(
                                        "LABEL:UNTRACKED",
                                        null,
                                        null,
                                        "Fixed",
                                        []))
                                    .ToArray()
                            })
                            .ToArray()
                    }
                    : variable)
                .ToArray()
        };

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.Contains(
            result.Reasons,
            reason => reason.Contains("LABEL:UNTRACKED", StringComparison.Ordinal) &&
                      reason.Contains("not declared", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("WrongEntityRef")]
    [InlineData("MissingPath")]
    [InlineData("EmptyPath")]
    [InlineData("MissingSegment")]
    [InlineData("WrongSegment")]
    public void Evaluate_fails_closed_when_a_stretch_role_lacks_full_target_identity(string invalidIdentity)
    {
        var width = Template("width", "Width", "Right", 2m);
        var role = width.CanonicalEntityRoles[0];
        var invalidRole = invalidIdentity switch
        {
            "WrongEntityRef" => role with { EntityRef = "WALL:OTHER" },
            "MissingPath" => role with { GeometryPathId = null },
            "EmptyPath" => role with { GeometryPathId = Guid.Empty },
            "MissingSegment" => role with { SegmentSortOrder = null },
            "WrongSegment" => role with { SegmentSortOrder = 99 },
            _ => throw new ArgumentOutOfRangeException(nameof(invalidIdentity))
        };
        var roles = width.CanonicalEntityRoles
            .Select((candidate, index) => index == 0 ? invalidRole : candidate)
            .ToArray();
        var profile = Profile(
            Variable(
                "width",
                HouseAdaptationAxis.Width,
                width with { CanonicalEntityRoles = roles }),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.NotEmpty(result.Reasons);
    }

    [Theory]
    [InlineData("Segment")]
    [InlineData("Vertex")]
    public void Evaluate_fails_closed_when_a_source_only_annotation_carries_geometric_indices(string indexKind)
    {
        var width = Template("width", "Width", "Right", 2m);
        var annotation = new AdjustmentRecipeEntityRoleDto(
            "LABEL:SOURCE",
            null,
            indexKind == "Segment" ? 0 : null,
            "Fixed",
            indexKind == "Vertex" ? [0] : []);
        var roles = width.CanonicalEntityRoles.Append(annotation).ToArray();
        var profile = Profile(
            Variable(
                "width",
                HouseAdaptationAxis.Width,
                width with { CanonicalEntityRoles = roles }),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public void Evaluate_fails_closed_when_an_action_role_is_not_supported_by_preview_replay()
    {
        var width = Template("width", "Width", "Right", 2m);
        var unsupportedRoles = width.CanonicalEntityRoles
            .Select((role, index) => index == 0
                ? role with { Role = "TranslateMaybe" }
                : role)
            .ToArray();
        var profile = Profile(
            Variable(
                "width",
                HouseAdaptationAxis.Width,
                width with { CanonicalEntityRoles = unsupportedRoles }),
            Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 2m)));

        var result = CommissionedHouseAdaptationProfileReadiness.Evaluate(profile);

        Assert.False(result.IsReady);
        Assert.Contains(result.Reasons, reason => reason.Contains("unsupported", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_is_independent_of_variable_input_order()
    {
        var width = Variable("width", HouseAdaptationAxis.Width, Template("width", "Width", "Right", 2m));
        var depth = Variable("depth", HouseAdaptationAxis.Depth, Template("depth", "Height", "Top", 3m));

        var first = CommissionedHouseAdaptationProfileReadiness.Evaluate(Profile(width, depth));
        var second = CommissionedHouseAdaptationProfileReadiness.Evaluate(Profile(depth, width));

        Assert.Equal(first.IsReady, second.IsReady);
        Assert.Equal(first.WidthCapacityInches, second.WidthCapacityInches);
        Assert.Equal(first.DepthCapacityInches, second.DepthCapacityInches);
        Assert.Equal(first.Reasons, second.Reasons);
    }

    private static CommissionedHouseAdaptationProfile Profile(
        params CommissionedAdaptationVariable[] variables)
        => Profile([], variables);

    private static CommissionedHouseAdaptationProfile Profile(
        IReadOnlyList<string> immutableEntityRefs,
        params CommissionedAdaptationVariable[] variables)
    {
        var coveredVariables = variables
            .Select(variable => variable with
            {
                ActionTemplates = variable.ActionTemplates
                    .Select(action => action with
                    {
                        CanonicalEntityRoles = action.CanonicalEntityRoles
                            .Append(new AdjustmentRecipeEntityRoleDto(
                                CoverageEntityRef,
                                null,
                                null,
                                "Fixed",
                                []))
                            .ToArray()
                    })
                    .ToArray()
            })
            .ToArray();

        return new(Guid.NewGuid(), Guid.NewGuid(), 12.7m, coveredVariables, immutableEntityRefs, [CoverageEntityRef])
        {
            AuxiliaryEntityBindings = ProfileAuxiliaryCoverage()
        };
    }

    private static CommissionExistingCurationAuxiliaryEntityBinding[] ProfileAuxiliaryCoverage()
        =>
        [
            new CommissionExistingCurationAuxiliaryEntityBinding(
                CoverageEntityRef,
                CommissionExistingCurationAuxiliaryEntityKind.Label,
                new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                GeometryPathId: null,
                SegmentSortOrder: null,
                IsImmutableSize: false,
                IsProtected: true)
        ];

    private static CommissionedAdaptationVariable Variable(
        string id,
        HouseAdaptationAxis axis,
        params AdjustmentRecipeStretchActionDto[] actions)
        => new(id, id, axis, 1, actions);

    private static CommissionedHouseAdaptationProfile AddOpeningHost(
        CommissionedHouseAdaptationProfile profile,
        string openingRef,
        Guid openingPathId,
        Guid hostPathId,
        int hostSegmentSortOrder)
    {
        var withoutCoverage = profile.Variables
            .Select(variable => variable with
            {
                ActionTemplates = variable.ActionTemplates
                    .Select(action => action with
                    {
                        CanonicalEntityRoles = action.CanonicalEntityRoles
                            .Where(role => role.EntityRef != CoverageEntityRef)
                            .Append(new AdjustmentRecipeEntityRoleDto(
                                openingRef,
                                openingPathId,
                                0,
                                "RigidMove",
                                [])
                            {
                                HostGeometryPathId = hostPathId,
                                HostSegmentSortOrder = hostSegmentSortOrder
                            })
                            .ToArray()
                    })
                    .ToArray()
            })
            .ToArray();

        return profile with
        {
            Variables = withoutCoverage,
            ImmutableSizeEntityRefs = [openingRef],
            ProtectedEntityRefs = [],
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    openingRef,
                    CommissionExistingCurationAuxiliaryEntityKind.Opening,
                    new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                    openingPathId,
                    0,
                    IsImmutableSize: true,
                    IsProtected: false)
                {
                    HostGeometryPathId = hostPathId,
                    HostSegmentSortOrder = hostSegmentSortOrder
                }
            ]
        };
    }

    private static CommissionedHouseAdaptationProfile RewriteOpeningHost(
        CommissionedHouseAdaptationProfile profile,
        string openingRef,
        Guid hostPathId,
        int hostSegmentSortOrder)
        => profile with
        {
            AuxiliaryEntityBindings = profile.AuxiliaryEntityBindings
                .Select(binding => binding.SourceEntityRef == openingRef
                    ? binding with
                    {
                        HostGeometryPathId = hostPathId,
                        HostSegmentSortOrder = hostSegmentSortOrder
                    }
                    : binding)
                .ToArray(),
            Variables = profile.Variables
                .Select(variable => variable with
                {
                    ActionTemplates = variable.ActionTemplates
                        .Select(action => action with
                        {
                            CanonicalEntityRoles = action.CanonicalEntityRoles
                                .Select(role => role.EntityRef == openingRef
                                    ? role with
                                    {
                                        HostGeometryPathId = hostPathId,
                                        HostSegmentSortOrder = hostSegmentSortOrder
                                    }
                                    : role)
                                .ToArray()
                        })
                        .ToArray()
                })
                .ToArray()
        };

    private static AdjustmentRecipeStretchActionDto Template(
        string id,
        string axisTag,
        string edge,
        decimal capacity,
        string firstEntity = "WALL:A")
    {
        var secondEntity = $"{firstEntity}:PAIR";
        var firstPath = Guid.NewGuid();
        var secondPath = Guid.NewGuid();
        return new AdjustmentRecipeStretchActionDto(
            id,
            axisTag,
            edge,
            10m,
            0m,
            capacity,
            0.001m,
            new AdjustmentRecipeBoundsDto(0m, 0m, 20m, 20m),
            [
                new AdjustmentRecipeTargetSpanDto(firstEntity, firstPath, 0, 0m, 0m, 20m, 0m, 1),
                new AdjustmentRecipeTargetSpanDto(secondEntity, secondPath, 0, 0m, 1m, 20m, 1m, 1)
            ],
            [
                new AdjustmentRecipeEntityRoleDto(firstEntity, firstPath, 0, "Stretch", [1]),
                new AdjustmentRecipeEntityRoleDto(secondEntity, secondPath, 0, "Stretch", [1])
            ]);
    }
}
