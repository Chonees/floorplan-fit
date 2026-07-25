using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Infrastructure.Dxf;

namespace FloorplanFit.Infrastructure.Tests.SitePlanAdjustment;

/// <summary>
/// Phase 3 real-fixture contracts: the commissioned Floor fit is proven against the
/// authoritative SEMINOLE 2000 DXF while every assertion flows through the same generic
/// production path (wall extraction, structural footprint, request factory, planner,
/// readiness). The fixture is referenced only from this harness; production stays
/// house-agnostic. Tests skip cleanly when the fixture DXF is absent on this machine.
/// </summary>
public sealed class SeminoleCommissionedHouseFitContractTests
{
    // SEMINOLE source units are inches, so one source unit is 25.4 millimeters and the
    // factory/planner inch conversions cancel exactly for rounded coordinates.
    private const decimal SourceToMillimetersFactor = 25.4m;
    private const decimal WidthCapacitySourceUnits = 6m;
    private const decimal DepthCapacitySourceUnits = 6m;

    [SeminoleFixtureFact]
    public async Task Width_only_case_allocates_the_exact_deficit_and_keeps_depth_untouched()
    {
        var scenario = await LoadScenarioAsync();
        var request = BuildRequest(scenario, widthTrimInches: 2m, depthTrimInches: -5m);

        var result = new CommissionedHouseFitPlanner(scenario.Profile).Plan(request);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.False(result.IsRigidPlacement);
        Assert.Equal(2m, result.WidthReductionInches);
        Assert.Equal(0m, result.DepthReductionInches);
        var action = Assert.Single(result.Actions);
        AssertReplays(scenario.WidthTemplate, action, expectedDeltaSourceUnits: 2m);
        Assert.DoesNotContain(result.Actions, emitted =>
            string.Equals(emitted.AxisTag, "Height", StringComparison.OrdinalIgnoreCase));
        // Final width stated against the SITE, never restated from the house: the planner
        // must land the house exactly on the buildable width the request factory computed
        // from the trimmed buildable area, so a planner that under- or over-allocates the
        // reported deficit fails here.
        // TODO(3.3): anchor the final width to an absolute literal once the SEMINOLE
        // structural footprint width in inches has been measured externally (run this
        // contract on a machine holding the fixture DXF and record footprint.Width). Until
        // that measurement exists the absolute original width is not independently known
        // and must not be invented here.
        Assert.Equal(
            request.BuildableWidthInches,
            request.OriginalWidthInches - result.WidthReductionInches);
        // Depth is genuinely untouched: the depth variable's action was never emitted, so
        // no depth-owned segment identity can have moved.
        Assert.DoesNotContain(
            result.Actions,
            emitted => emitted.ActionId == scenario.DepthTemplate.ActionId);
        AssertPreservationInvariants(scenario, result);
    }

    [SeminoleFixtureFact]
    public async Task Depth_only_case_allocates_the_exact_deficit_and_keeps_width_untouched()
    {
        var scenario = await LoadScenarioAsync();
        var request = BuildRequest(scenario, widthTrimInches: -5m, depthTrimInches: 1.5m);

        var result = new CommissionedHouseFitPlanner(scenario.Profile).Plan(request);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.False(result.IsRigidPlacement);
        Assert.Equal(0m, result.WidthReductionInches);
        Assert.Equal(1.5m, result.DepthReductionInches);
        var action = Assert.Single(result.Actions);
        AssertReplays(scenario.DepthTemplate, action, expectedDeltaSourceUnits: 1.5m);
        Assert.DoesNotContain(result.Actions, emitted =>
            string.Equals(emitted.AxisTag, "Width", StringComparison.OrdinalIgnoreCase));
        // Final depth stated against the SITE, never restated from the house: the planner
        // must land the house exactly on the buildable depth the request factory computed
        // from the trimmed buildable area.
        // TODO(3.4): anchor the final depth to an absolute literal once the SEMINOLE
        // structural footprint depth in inches has been measured externally (run this
        // contract on a machine holding the fixture DXF and record footprint.Height). Until
        // that measurement exists the absolute original depth is not independently known
        // and must not be invented here.
        Assert.Equal(
            request.BuildableDepthInches,
            request.OriginalDepthInches - result.DepthReductionInches);
        // Width is genuinely untouched: the width variable's action was never emitted, so
        // no width-owned segment identity can have moved.
        Assert.DoesNotContain(
            result.Actions,
            emitted => emitted.ActionId == scenario.WidthTemplate.ActionId);
        AssertPreservationInvariants(scenario, result);
    }

    [SeminoleFixtureFact]
    public async Task Combined_case_allocates_width_before_depth_with_exact_final_dimensions()
    {
        var scenario = await LoadScenarioAsync();
        var request = BuildRequest(scenario, widthTrimInches: 2m, depthTrimInches: 1.5m);

        var result = new CommissionedHouseFitPlanner(scenario.Profile).Plan(request);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.False(result.IsRigidPlacement);
        Assert.Equal(2m, result.WidthReductionInches);
        Assert.Equal(1.5m, result.DepthReductionInches);
        Assert.Collection(
            result.Actions,
            action => AssertReplays(scenario.WidthTemplate, action, expectedDeltaSourceUnits: 2m),
            action => AssertReplays(scenario.DepthTemplate, action, expectedDeltaSourceUnits: 1.5m));
        // Both final dimensions stated against the SITE, never restated from the house: the
        // combined case must land the house exactly on the buildable rectangle the request
        // factory computed on both axes at once.
        // TODO(3.5): anchor both final dimensions to absolute literals once the SEMINOLE
        // structural footprint width/depth in inches have been measured externally (run this
        // contract on a machine holding the fixture DXF and record footprint.Width and
        // footprint.Height). Until that measurement exists the absolute original dimensions
        // are not independently known and must not be invented here.
        Assert.Equal(
            request.BuildableWidthInches,
            request.OriginalWidthInches - result.WidthReductionInches);
        Assert.Equal(
            request.BuildableDepthInches,
            request.OriginalDepthInches - result.DepthReductionInches);
        AssertPreservationInvariants(scenario, result);
    }

    [SeminoleFixtureFact]
    public async Task Exact_fit_uses_rigid_placement_with_zero_actions()
    {
        var scenario = await LoadScenarioAsync();
        var request = BuildRequest(scenario, widthTrimInches: 0m, depthTrimInches: 0m);

        var result = new CommissionedHouseFitPlanner(scenario.Profile).Plan(request);

        Assert.True(result.Succeeded, result.RejectionReason);
        Assert.True(result.IsRigidPlacement);
        Assert.Equal(0m, result.WidthReductionInches);
        Assert.Equal(0m, result.DepthReductionInches);
        Assert.Empty(result.Actions);
    }

    [SeminoleFixtureFact]
    public async Task Insufficient_width_capacity_fails_closed_with_an_actionable_blocker()
    {
        var scenario = await LoadScenarioAsync();
        var request = BuildRequest(
            scenario,
            widthTrimInches: WidthCapacitySourceUnits + 1m,
            depthTrimInches: 0m);

        var result = new CommissionedHouseFitPlanner(scenario.Profile).Plan(request);

        Assert.False(result.Succeeded);
        Assert.False(result.IsRigidPlacement);
        Assert.Empty(result.Actions);
        // The failed plan reports the full requested deficit, never a partial allocation
        // clamped to the commissioned capacity: 7" requested against 6" of Width capacity.
        Assert.Equal(WidthCapacitySourceUnits + 1m, result.WidthReductionInches);
        Assert.Equal(0m, result.DepthReductionInches);
        Assert.Contains("Width", result.RejectionReason, StringComparison.Ordinal);
        Assert.Contains("capacity", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [SeminoleFixtureFact]
    public async Task Protected_stretch_target_fails_closed_with_zero_partial_output()
    {
        var scenario = await LoadScenarioAsync();
        var protectedStretchRef = scenario.WidthTemplate.TargetSpans[0].SourceEntityRef;
        var violatingProfile = scenario.Profile with
        {
            ProtectedEntityRefs = [.. scenario.Profile.ProtectedEntityRefs, protectedStretchRef]
        };
        var request = BuildRequest(scenario, widthTrimInches: 2m, depthTrimInches: 0m);

        var result = new CommissionedHouseFitPlanner(violatingProfile).Plan(request);

        Assert.False(result.Succeeded);
        Assert.False(result.IsRigidPlacement);
        Assert.Empty(result.Actions);
        // Zero partial output, and the reported deficit is still the exact 2" the site asked
        // for: the invariant violation blocks the plan without rewriting the request.
        Assert.Equal(2m, result.WidthReductionInches);
        Assert.Equal(0m, result.DepthReductionInches);
        Assert.Contains(protectedStretchRef, result.RejectionReason, StringComparison.Ordinal);
        Assert.Contains("stretch", result.RejectionReason, StringComparison.OrdinalIgnoreCase);
    }

    [SeminoleFixtureFact]
    public async Task Real_profile_is_auto_fit_ready_with_exact_commissioned_capacities()
    {
        var scenario = await LoadScenarioAsync();

        var readiness = CommissionedHouseAdaptationProfileReadiness.Evaluate(scenario.Profile);

        Assert.True(readiness.IsReady, string.Join("; ", readiness.Reasons));
        Assert.Equal(WidthCapacitySourceUnits, readiness.WidthCapacityInches);
        Assert.Equal(DepthCapacitySourceUnits, readiness.DepthCapacityInches);
    }

    private static void AssertPreservationInvariants(
        SeminoleScenario scenario,
        CommissionedHouseFitResult result)
    {
        foreach (var action in result.Actions)
        {
            // Openings/protected entities can never deform: no Stretch role may reference
            // a protected or immutable-size commissioned entity.
            Assert.DoesNotContain(action.CanonicalEntityRoles, role =>
                string.Equals(role.Role, "Stretch", StringComparison.OrdinalIgnoreCase) &&
                (scenario.Profile.ProtectedEntityRefs.Contains(role.EntityRef) ||
                 scenario.Profile.ImmutableSizeEntityRefs.Contains(role.EntityRef)));

            // The protected label survives as a Fixed source-only role in every action.
            var labelRole = Assert.Single(
                action.CanonicalEntityRoles,
                role => role.EntityRef == scenario.ProtectedLabelRef);
            Assert.Equal("Fixed", labelRole.Role);
            Assert.Empty(labelRole.VertexIndices);
        }

        // Unrelated geometry stays unreferenced: the emitted actions name only the
        // commissioned faces/components, never the rest of the real wall inventory.
        var referencedRefs = result.Actions
            .SelectMany(action => action.CanonicalEntityRoles)
            .Select(role => role.EntityRef)
            .ToHashSet(StringComparer.Ordinal);
        var unrelatedWallRefs = scenario.WallEntityRefs
            .Where(entityRef => !referencedRefs.Contains(entityRef))
            .ToArray();
        Assert.NotEmpty(unrelatedWallRefs);
    }

    private static CommissionedHouseFitRequest BuildRequest(
        SeminoleScenario scenario,
        decimal widthTrimInches,
        decimal depthTrimInches)
    {
        // Source units are inches, so trimming the buildable area in source units yields
        // an exact inch deficit after the factory's cancelling unit conversion.
        var buildableArea = new SitePlanBuildableAreaDto(
            scenario.Footprint.MinX,
            scenario.Footprint.MinY,
            scenario.Footprint.MaxX - widthTrimInches,
            scenario.Footprint.MaxY - depthTrimInches);

        var built = CommissionedHouseFitRequestFactory.Create(
            scenario.GeometryPaths,
            buildableArea,
            SourceToMillimetersFactor);

        Assert.True(built.Succeeded, built.RejectionReason);
        Assert.Equal(scenario.OriginalWidthInches, built.Request!.OriginalWidthInches);
        Assert.Equal(scenario.OriginalDepthInches, built.Request.OriginalDepthInches);

        // The REQUESTED deficit is a known constant of the scenario, not a restatement of
        // the factory output: the site keeps MinX/MinY and source units are inches, so
        // trimming the buildable area by N source units must surface as exactly N inches of
        // shortfall on that axis. A negative trim is a surplus and must stay negative, which
        // is what makes the "other axis untouched" cases real surplus cases rather than
        // accidental zero-deficit cases.
        Assert.Equal(
            widthTrimInches,
            built.Request.OriginalWidthInches - built.Request.BuildableWidthInches);
        Assert.Equal(
            depthTrimInches,
            built.Request.OriginalDepthInches - built.Request.BuildableDepthInches);
        return built.Request;
    }

    private static async Task<SeminoleScenario> LoadScenarioAsync()
    {
        var fixturePath = SeminoleFixture.ResolvePath();
        var wallCandidates = await new IxMiliaWallExtractor()
            .ExtractAsync(fixturePath, CancellationToken.None);
        Assert.NotEmpty(wallCandidates);

        var geometryPaths = new List<GeometryPathDto>();
        var segmentOwners = new List<WallSegmentOwner>();
        foreach (var candidate in wallCandidates)
        {
            var pathId = Guid.NewGuid();
            var segments = new List<GeometrySegmentDto>();
            for (var index = 1; index < candidate.Points.Count; index++)
            {
                var start = candidate.Points[index - 1];
                var end = candidate.Points[index];
                segments.Add(new GeometrySegmentDto(
                    pathId,
                    index,
                    Round(start.X),
                    Round(start.Y),
                    Round(end.X),
                    Round(end.Y)));
            }

            if (segments.Count == 0)
            {
                continue;
            }

            var path = new GeometryPathDto(pathId, IsClosed: false, segments);
            geometryPaths.Add(path);
            foreach (var segment in segments)
            {
                segmentOwners.Add(new WallSegmentOwner(candidate.SourceEntityRef, path, segment));
            }
        }

        Assert.NotEmpty(geometryPaths);
        var footprint = StructuralFootprint.Resolve(geometryPaths);
        Assert.NotNull(footprint);
        Assert.True(footprint.Value.Width > 0m);
        Assert.True(footprint.Value.Height > 0m);

        var roomLabels = await new IxMiliaRoomLabelExtractor()
            .ExtractAsync(fixturePath, CancellationToken.None);
        Assert.NotEmpty(roomLabels);
        var protectedLabel = roomLabels
            .OrderBy(label => label.SourceEntityRef, StringComparer.Ordinal)
            .First();

        // Width variable: the two dominant horizontal exterior faces shorten toward the
        // right closing side; the right wall moves rigidly and the left wall stays fixed.
        var widthUsedRefs = new HashSet<string>(StringComparer.Ordinal);
        var topFace = PickFace(segmentOwners, horizontal: true, highSide: true, widthUsedRefs);
        var bottomFace = PickFace(segmentOwners, horizontal: true, highSide: false, widthUsedRefs);
        var rightWall = PickFace(segmentOwners, horizontal: false, highSide: true, widthUsedRefs);
        var leftWall = PickFace(segmentOwners, horizontal: false, highSide: false, widthUsedRefs);

        var protectedLabelRole = new AdjustmentRecipeEntityRoleDto(
            protectedLabel.SourceEntityRef,
            GeometryPathId: null,
            SegmentSortOrder: null,
            Role: "Fixed",
            VertexIndices: []);
        var bounds = new AdjustmentRecipeBoundsDto(
            footprint.Value.MinX,
            footprint.Value.MinY,
            footprint.Value.MaxX,
            footprint.Value.MaxY);

        var widthTemplate = new AdjustmentRecipeStretchActionDto(
            "seminole-width",
            "Width",
            "Right",
            CutCoordinate: footprint.Value.MaxX,
            DeltaSourceUnits: 0m,
            MaxDeltaSourceUnits: WidthCapacitySourceUnits,
            CoordinateTolerance: 0.01m,
            bounds,
            [
                TargetSpan(topFace, closingTowardMaxX: true),
                TargetSpan(bottomFace, closingTowardMaxX: true)
            ],
            [
                StretchRole(topFace, closingTowardMaxX: true),
                StretchRole(bottomFace, closingTowardMaxX: true),
                ComponentRole(rightWall, "RigidMove"),
                ComponentRole(leftWall, "Fixed"),
                protectedLabelRole
            ]);

        // Depth variable: the two dominant vertical exterior faces shorten toward the top
        // closing side; the top wall moves rigidly and the bottom wall stays fixed.
        var depthUsedRefs = new HashSet<string>(StringComparer.Ordinal);
        var leftFace = PickFace(segmentOwners, horizontal: false, highSide: false, depthUsedRefs);
        var rightFace = PickFace(segmentOwners, horizontal: false, highSide: true, depthUsedRefs);
        var topWall = PickFace(segmentOwners, horizontal: true, highSide: true, depthUsedRefs);
        var bottomWall = PickFace(segmentOwners, horizontal: true, highSide: false, depthUsedRefs);

        var depthTemplate = new AdjustmentRecipeStretchActionDto(
            "seminole-depth",
            "Height",
            "Top",
            CutCoordinate: footprint.Value.MaxY,
            DeltaSourceUnits: 0m,
            MaxDeltaSourceUnits: DepthCapacitySourceUnits,
            CoordinateTolerance: 0.01m,
            bounds,
            [
                TargetSpan(leftFace, closingTowardMaxX: false),
                TargetSpan(rightFace, closingTowardMaxX: false)
            ],
            [
                StretchRole(leftFace, closingTowardMaxX: false),
                StretchRole(rightFace, closingTowardMaxX: false),
                ComponentRole(topWall, "RigidMove"),
                ComponentRole(bottomWall, "Fixed"),
                protectedLabelRole
            ]);

        // Pin the exact commissioned affected-role list of both axis templates before the
        // planner ever runs. The per-case replay assertions compare production output to
        // these templates, so pinning the shape here is what turns those comparisons into an
        // exact affected-role contract instead of "whatever the harness happened to build".
        AssertCommissionedRoleShape(widthTemplate, protectedLabel.SourceEntityRef);
        AssertCommissionedRoleShape(depthTemplate, protectedLabel.SourceEntityRef);

        var profile = new CommissionedHouseAdaptationProfile(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SourceToMillimetersFactor,
            [
                new CommissionedAdaptationVariable(
                    "seminole-width-variable",
                    "Width",
                    HouseAdaptationAxis.Width,
                    1,
                    [widthTemplate]),
                new CommissionedAdaptationVariable(
                    "seminole-depth-variable",
                    "Depth",
                    HouseAdaptationAxis.Depth,
                    1,
                    [depthTemplate])
            ],
            ImmutableSizeEntityRefs: [],
            ProtectedEntityRefs: [protectedLabel.SourceEntityRef])
        {
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    protectedLabel.SourceEntityRef,
                    CommissionExistingCurationAuxiliaryEntityKind.Label,
                    new AdjustmentRecipeBoundsDto(
                        protectedLabel.X,
                        protectedLabel.Y,
                        protectedLabel.X + 1m,
                        protectedLabel.Y + 1m),
                    GeometryPathId: null,
                    SegmentSortOrder: null,
                    IsImmutableSize: false,
                    IsProtected: true)
            ]
        };

        return new SeminoleScenario(
            geometryPaths,
            footprint.Value,
            footprint.Value.Width,
            footprint.Value.Height,
            profile,
            widthTemplate,
            depthTemplate,
            protectedLabel.SourceEntityRef,
            segmentOwners.Select(owner => owner.EntityRef).Distinct(StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// Pins the exact affected-role list of one commissioned axis template: five distinct
    /// real entities, of which exactly two deforming exterior faces, one rigidly moving
    /// closing wall, one fixed opposite wall, and the protected label as a source-only
    /// Fixed role, with every target span backed by its own Stretch role and closing vertex.
    /// </summary>
    private static void AssertCommissionedRoleShape(
        AdjustmentRecipeStretchActionDto template,
        string protectedLabelRef)
    {
        Assert.Equal(5, template.CanonicalEntityRoles.Count);
        Assert.Equal(
            5,
            template.CanonicalEntityRoles
                .Select(role => role.EntityRef)
                .Distinct(StringComparer.Ordinal)
                .Count());
        Assert.Equal(2, template.CanonicalEntityRoles.Count(role => role.Role == "Stretch"));
        Assert.Equal(1, template.CanonicalEntityRoles.Count(role => role.Role == "RigidMove"));
        Assert.Equal(2, template.CanonicalEntityRoles.Count(role => role.Role == "Fixed"));
        Assert.Equal(2, template.TargetSpans.Count);

        foreach (var target in template.TargetSpans)
        {
            var stretchRole = Assert.Single(
                template.CanonicalEntityRoles,
                role => role.Role == "Stretch" &&
                        role.EntityRef == target.SourceEntityRef &&
                        role.GeometryPathId == target.GeometryPathId &&
                        role.SegmentSortOrder == target.SegmentSortOrder);
            var vertexIndex = Assert.Single(stretchRole.VertexIndices);
            Assert.Equal(target.ClosingVertexIndex, vertexIndex);
        }

        var labelRole = Assert.Single(
            template.CanonicalEntityRoles,
            role => role.EntityRef == protectedLabelRef);
        Assert.Equal("Fixed", labelRole.Role);
        Assert.Empty(labelRole.VertexIndices);
        Assert.Null(labelRole.GeometryPathId);
        Assert.Null(labelRole.SegmentSortOrder);
    }

    private static WallSegmentOwner PickFace(
        IReadOnlyList<WallSegmentOwner> owners,
        bool horizontal,
        bool highSide,
        ISet<string> usedRefs)
    {
        const decimal axisTolerance = 0.01m;
        var aligned = owners
            .Where(owner => !usedRefs.Contains(owner.EntityRef))
            .Where(owner => horizontal
                ? Math.Abs(owner.Segment.EndY - owner.Segment.StartY) <= axisTolerance
                : Math.Abs(owner.Segment.EndX - owner.Segment.StartX) <= axisTolerance)
            .Select(owner => new
            {
                Owner = owner,
                Length = horizontal
                    ? Math.Abs(owner.Segment.EndX - owner.Segment.StartX)
                    : Math.Abs(owner.Segment.EndY - owner.Segment.StartY),
                Position = horizontal
                    ? (owner.Segment.StartY + owner.Segment.EndY) / 2m
                    : (owner.Segment.StartX + owner.Segment.EndX) / 2m
            })
            .Where(face => face.Length > 0m)
            .ToArray();
        Assert.NotEmpty(aligned);

        var longest = aligned.Max(face => face.Length);
        var dominant = aligned
            .Where(face => face.Length >= longest * 0.5m)
            .ToArray();
        var ordered = highSide
            ? dominant.OrderByDescending(face => face.Position)
            : dominant.OrderBy(face => face.Position);
        var chosen = ordered
            .ThenByDescending(face => face.Length)
            .ThenBy(face => face.Owner.EntityRef, StringComparer.Ordinal)
            .First()
            .Owner;
        usedRefs.Add(chosen.EntityRef);
        return chosen;
    }

    private static AdjustmentRecipeTargetSpanDto TargetSpan(
        WallSegmentOwner face,
        bool closingTowardMaxX)
        => new(
            face.EntityRef,
            face.Path.Id,
            face.Segment.SortOrder,
            face.Segment.StartX,
            face.Segment.StartY,
            face.Segment.EndX,
            face.Segment.EndY,
            ClosingVertexIndex(face.Segment, closingTowardMaxX));

    private static AdjustmentRecipeEntityRoleDto StretchRole(
        WallSegmentOwner face,
        bool closingTowardMaxX)
        => new(
            face.EntityRef,
            face.Path.Id,
            face.Segment.SortOrder,
            "Stretch",
            [ClosingVertexIndex(face.Segment, closingTowardMaxX)]);

    private static AdjustmentRecipeEntityRoleDto ComponentRole(
        WallSegmentOwner component,
        string role)
        => new(
            component.EntityRef,
            component.Path.Id,
            component.Segment.SortOrder,
            role,
            []);

    private static int ClosingVertexIndex(GeometrySegmentDto segment, bool closingTowardMaxX)
        => closingTowardMaxX
            ? segment.StartX >= segment.EndX ? 0 : 1
            : segment.StartY >= segment.EndY ? 0 : 1;

    private static decimal Round(decimal value)
        => decimal.Round(value, 6, MidpointRounding.AwayFromZero);

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

    private sealed record WallSegmentOwner(
        string EntityRef,
        GeometryPathDto Path,
        GeometrySegmentDto Segment);

    private sealed record SeminoleScenario(
        IReadOnlyList<GeometryPathDto> GeometryPaths,
        StructuralFootprintBounds Footprint,
        decimal OriginalWidthInches,
        decimal OriginalDepthInches,
        CommissionedHouseAdaptationProfile Profile,
        AdjustmentRecipeStretchActionDto WidthTemplate,
        AdjustmentRecipeStretchActionDto DepthTemplate,
        string ProtectedLabelRef,
        IReadOnlyList<string> WallEntityRefs);
}

/// <summary>
/// Discovery-time conditional fact: the SEMINOLE real-fixture contracts skip with an
/// actionable message on machines without the authoritative DXF instead of failing.
/// </summary>
public sealed class SeminoleFixtureFactAttribute : FactAttribute
{
    public SeminoleFixtureFactAttribute()
    {
        var fixturePath = SeminoleFixture.ResolvePath();
        if (!File.Exists(fixturePath))
        {
            Skip = $"SEMINOLE fixture DXF was not found at '{fixturePath}'. " +
                   $"Set the {SeminoleFixture.EnvironmentVariable} environment variable " +
                   "to the SEMINOLE2000.dxf path to run the real-fixture contracts.";
        }
    }
}

/// <summary>
/// Resolves the authoritative SEMINOLE fixture path for tests only; production code
/// must never reference this file or any house-specific path.
/// </summary>
internal static class SeminoleFixture
{
    public const string EnvironmentVariable = "FLOORPLANFIT_SEMINOLE_FIXTURE";

    public const string DefaultPath = @"D:\PointAIData\PLANS\originalFloorPlans\SEMINOLE2000.dxf";

    public static string ResolvePath()
    {
        var overridePath = Environment.GetEnvironmentVariable(EnvironmentVariable);
        return string.IsNullOrWhiteSpace(overridePath) ? DefaultPath : overridePath;
    }
}
