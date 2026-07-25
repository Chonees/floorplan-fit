using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class CommissionedHouseFitPlannerTests
{
    [Fact]
    public void Plan_uses_rigid_placement_when_the_house_already_fits()
    {
        var planner = new CommissionedHouseFitPlanner(Profile());

        var result = planner.Plan(
            Request(originalWidth: 100m, originalDepth: 80m, buildableWidth: 100m, buildableDepth: 81m));

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.True(result.IsRigidPlacement);
        Assert.Equal(0m, result.WidthReductionInches);
        Assert.Equal(0m, result.DepthReductionInches);
        Assert.Empty(result.Actions);
        Assert.True(string.IsNullOrEmpty(result.RejectionReason));
    }

    [Fact]
    public void Plan_fails_closed_before_rigid_output_when_an_opening_has_only_its_own_preview_identity()
    {
        const string openingRef = "OPENING:LEGACY-NO-HOST";
        var openingPathId = Guid.NewGuid();
        var action = Template(
            "width-opening-host-check",
            HouseAdaptationAxis.Width,
            2m,
            "WALL:WIDTH",
            new AdjustmentRecipeEntityRoleDto(
                openingRef,
                openingPathId,
                SegmentSortOrder: 0,
                Role: "RigidMove",
                VertexIndices: []));
        var profile = Profile(
            variables:
            [
                Variable("width", HouseAdaptationAxis.Width, priority: 10, action)
            ],
            immutableSizeEntityRefs: [openingRef]) with
        {
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    openingRef,
                    CommissionExistingCurationAuxiliaryEntityKind.Opening,
                    new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                    openingPathId,
                    SegmentSortOrder: 0,
                    IsImmutableSize: true,
                    IsProtected: false)
            ]
        };

        var result = new CommissionedHouseFitPlanner(profile).Plan(
            Request(originalWidth: 100m, originalDepth: 80m, buildableWidth: 100m, buildableDepth: 81m));

        Assert.False(result.Succeeded);
        Assert.False(result.IsRigidPlacement);
        Assert.Empty(result.Actions);
        Assert.Contains(openingRef, result.RejectionReason, StringComparison.Ordinal);
        Assert.Contains("host", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plan_fails_closed_when_the_profile_has_no_published_curation_identity()
    {
        var profile = Profile(
            variables:
            [
                Variable(
                    "width",
                    HouseAdaptationAxis.Width,
                    priority: 10,
                    Template("width-1", HouseAdaptationAxis.Width, 2m, "WALL:WIDTH"))
            ]) with
        {
            PublishedCurationId = Guid.Empty
        };

        var result = new CommissionedHouseFitPlanner(profile).Plan(
            Request(originalWidth: 101m, originalDepth: 80m, buildableWidth: 100m, buildableDepth: 80m));

        Assert.False(result.Succeeded);
        Assert.Empty(result.Actions);
        Assert.Contains("published curation", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plan_allocates_width_and_depth_deficits_by_priority_and_template_order()
    {
        var widthFirst = Template("width-primary-1", HouseAdaptationAxis.Width, 2m, "WALL:WIDTH:A");
        var widthSecond = Template("width-primary-2", HouseAdaptationAxis.Width, 3m, "WALL:WIDTH:B");
        var widthLater = Template("width-secondary-1", HouseAdaptationAxis.Width, 10m, "WALL:WIDTH:C");
        var depth = Template("depth-primary-1", HouseAdaptationAxis.Depth, 3m, "WALL:DEPTH:A");
        var profile = Profile(
            sourceToMillimetersFactor: 12.7m,
            variables:
            [
                Variable("width-secondary", HouseAdaptationAxis.Width, priority: 20, widthLater),
                Variable("depth-primary", HouseAdaptationAxis.Depth, priority: 10, depth),
                Variable("width-primary", HouseAdaptationAxis.Width, priority: 10, widthFirst, widthSecond)
            ]);

        var result = new CommissionedHouseFitPlanner(profile).Plan(
            Request(originalWidth: 104m, originalDepth: 81.5m, buildableWidth: 100m, buildableDepth: 80m));

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.False(result.IsRigidPlacement);
        Assert.Equal(4m, result.WidthReductionInches);
        Assert.Equal(1.5m, result.DepthReductionInches);
        Assert.Collection(
            result.Actions,
            action => AssertReplays(widthFirst, action, expectedDeltaSourceUnits: 2m),
            action => AssertReplays(widthSecond, action, expectedDeltaSourceUnits: 3m),
            action => AssertReplays(widthLater, action, expectedDeltaSourceUnits: 3m),
            action => AssertReplays(depth, action, expectedDeltaSourceUnits: 3m));
    }

    [Fact]
    public void Plan_fails_closed_when_an_axis_has_insufficient_capacity()
    {
        var profile = Profile(
            variables:
            [
                Variable(
                    "width-limited",
                    HouseAdaptationAxis.Width,
                    priority: 10,
                    Template("width-limited-1", HouseAdaptationAxis.Width, 2m, "WALL:WIDTH:LIMITED"))
            ]);

        var result = new CommissionedHouseFitPlanner(profile).Plan(
            Request(originalWidth: 103m, originalDepth: 80m, buildableWidth: 100m, buildableDepth: 80m));

        Assert.False(result.Succeeded);
        Assert.False(result.IsRigidPlacement);
        Assert.Empty(result.Actions);
        Assert.False(string.IsNullOrWhiteSpace(result.RejectionReason));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Plan_fails_closed_when_a_template_stretches_an_immutable_or_protected_entity(
        bool immutableSize)
    {
        const string rigidOpeningRef = "OPENING:RIGID";
        const string forbiddenStretchRef = "OPENING:FORBIDDEN-STRETCH";
        var action = Template(
            "width-opening-conflict",
            HouseAdaptationAxis.Width,
            capacity: 2m,
            forbiddenStretchRef,
            Role(rigidOpeningRef, "RigidMove"));
        var profile = Profile(
            variables:
            [
                Variable("width-opening-conflict", HouseAdaptationAxis.Width, priority: 10, action)
            ],
            immutableSizeEntityRefs: immutableSize
                ? [rigidOpeningRef, forbiddenStretchRef]
                : [rigidOpeningRef],
            protectedEntityRefs: immutableSize ? [] : [forbiddenStretchRef]);

        var result = new CommissionedHouseFitPlanner(profile).Plan(
            Request(originalWidth: 101m, originalDepth: 80m, buildableWidth: 100m, buildableDepth: 80m));

        Assert.False(result.Succeeded);
        Assert.False(result.IsRigidPlacement);
        Assert.Empty(result.Actions);
        Assert.Contains(forbiddenStretchRef, result.RejectionReason, StringComparison.Ordinal);
    }

    [Fact]
    public void Plan_fails_closed_when_no_variable_exists_for_a_required_axis()
    {
        var profile = Profile(
            variables:
            [
                Variable(
                    "depth-only",
                    HouseAdaptationAxis.Depth,
                    priority: 10,
                    Template("depth-only-1", HouseAdaptationAxis.Depth, 2m, "WALL:DEPTH:ONLY"))
            ]);

        var result = new CommissionedHouseFitPlanner(profile).Plan(
            Request(originalWidth: 101m, originalDepth: 80m, buildableWidth: 100m, buildableDepth: 80m));

        Assert.False(result.Succeeded);
        Assert.False(result.IsRigidPlacement);
        Assert.Empty(result.Actions);
        Assert.False(string.IsNullOrWhiteSpace(result.RejectionReason));
    }

    private static CommissionedHouseAdaptationProfile Profile(
        decimal sourceToMillimetersFactor = 25.4m,
        IReadOnlyList<CommissionedAdaptationVariable>? variables = null,
        IReadOnlyList<string>? immutableSizeEntityRefs = null,
        IReadOnlyList<string>? protectedEntityRefs = null)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            sourceToMillimetersFactor,
            variables ?? [],
            immutableSizeEntityRefs ?? [],
            protectedEntityRefs ?? []);

    private static CommissionedAdaptationVariable Variable(
        string id,
        HouseAdaptationAxis axis,
        int priority,
        params AdjustmentRecipeStretchActionDto[] templates)
        => new(id, id, axis, priority, templates);

    private static CommissionedHouseFitRequest Request(
        decimal originalWidth,
        decimal originalDepth,
        decimal buildableWidth,
        decimal buildableDepth)
        => new(originalWidth, originalDepth, buildableWidth, buildableDepth);

    private static AdjustmentRecipeStretchActionDto Template(
        string actionId,
        HouseAdaptationAxis axis,
        decimal capacity,
        string stretchedEntityRef,
        params AdjustmentRecipeEntityRoleDto[] additionalRoles)
    {
        var pairedEntityRef = $"{stretchedEntityRef}:PAIR";
        var firstPathId = Guid.NewGuid();
        var secondPathId = Guid.NewGuid();
        var firstStretchRole = new AdjustmentRecipeEntityRoleDto(
            stretchedEntityRef,
            firstPathId,
            SegmentSortOrder: 0,
            Role: "Stretch",
            VertexIndices: [1]);
        var secondStretchRole = new AdjustmentRecipeEntityRoleDto(
            pairedEntityRef,
            secondPathId,
            SegmentSortOrder: 0,
            Role: "Stretch",
            VertexIndices: [1]);

        return new AdjustmentRecipeStretchActionDto(
            actionId,
            axis == HouseAdaptationAxis.Width ? "Width" : "Height",
            axis == HouseAdaptationAxis.Width ? "Right" : "Top",
            CutCoordinate: 10m,
            DeltaSourceUnits: 0m,
            MaxDeltaSourceUnits: capacity,
            CoordinateTolerance: 0.001m,
            new AdjustmentRecipeBoundsDto(0m, 0m, 20m, 20m),
            [
                new AdjustmentRecipeTargetSpanDto(stretchedEntityRef, firstPathId, 0, 0m, 0m, 20m, 0m, 1),
                new AdjustmentRecipeTargetSpanDto(pairedEntityRef, secondPathId, 0, 0m, 1m, 20m, 1m, 1)
            ],
            [firstStretchRole, secondStretchRole, .. additionalRoles]);
    }

    private static AdjustmentRecipeEntityRoleDto Role(string entityRef, string role)
        => new(entityRef, Guid.NewGuid(), null, role, []);

    private static void AssertReplays(
        AdjustmentRecipeStretchActionDto template,
        AdjustmentRecipeStretchActionDto action,
        decimal expectedDeltaSourceUnits)
    {
        Assert.Equal(template.ActionId, action.ActionId);
        Assert.Equal(template.AxisTag, action.AxisTag);
        Assert.Equal(template.Edge, action.Edge);
        Assert.Equal(template.CutCoordinate, action.CutCoordinate);
        Assert.Equal(expectedDeltaSourceUnits, action.DeltaSourceUnits);
        Assert.Equal(template.MaxDeltaSourceUnits, action.MaxDeltaSourceUnits);
        Assert.Equal(template.CoordinateTolerance, action.CoordinateTolerance);
        Assert.Equal(template.CanonicalSourceBounds, action.CanonicalSourceBounds);
        Assert.Equal(template.TargetSpans.ToArray(), action.TargetSpans.ToArray());
        Assert.Equal(template.CanonicalEntityRoles.ToArray(), action.CanonicalEntityRoles.ToArray());
    }

}
