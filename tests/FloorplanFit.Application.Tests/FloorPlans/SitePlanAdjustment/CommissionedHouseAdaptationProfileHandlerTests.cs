using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class CommissionedHouseAdaptationProfileHandlerTests
{
    private const string CoverageEntityRef = "LABEL:COVERAGE";

    [Fact]
    public async Task Save_rejects_an_incomplete_profile_without_writing_or_committing()
    {
        var profile = Profile(
            Variable(HouseAdaptationAxis.Width, Template("width", "Width", "Right")));
        var expectedReason = Assert.Single(
            CommissionedHouseAdaptationProfileReadiness.Evaluate(profile).Reasons);
        var repository = new ProfileRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = new SaveCommissionedHouseAdaptationProfileHandler(repository, unitOfWork);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(profile, CancellationToken.None));

        Assert.Contains(expectedReason, exception.Message, StringComparison.Ordinal);
        Assert.Empty(repository.Upserts);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task Save_upserts_and_commits_a_ready_profile_once_and_returns_its_readiness()
    {
        var profile = ReadyProfile();
        var repository = new ProfileRepositorySpy();
        var unitOfWork = new UnitOfWorkSpy();
        var handler = new SaveCommissionedHouseAdaptationProfileHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(profile, CancellationToken.None);

        Assert.Same(profile, Assert.Single(repository.Upserts));
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.True(result.IsReady, string.Join("; ", result.Reasons));
        Assert.Equal(1m, result.WidthCapacityInches);
        Assert.Equal(1m, result.DepthCapacityInches);
        Assert.Empty(result.Reasons);
    }

    [Fact]
    public async Task Get_loads_the_exact_floor_plan_version_and_returns_evaluated_readiness()
    {
        var profile = ReadyProfile();
        var repository = new ProfileRepositorySpy(profile);
        var handler = new GetCommissionedHouseAdaptationReadinessHandler(repository);

        var result = await handler.HandleAsync(
            profile.FloorPlanVersionId,
            profile.PublishedCurationId,
            CancellationToken.None);

        Assert.Equal(profile.FloorPlanVersionId, Assert.Single(repository.RequestedIds));
        Assert.Equal(profile.PublishedCurationId, Assert.Single(repository.RequestedPublishedCurationIds));
        Assert.True(result.IsReady, string.Join("; ", result.Reasons));
        Assert.Equal(1m, result.WidthCapacityInches);
        Assert.Equal(1m, result.DepthCapacityInches);
        Assert.Empty(result.Reasons);
    }

    [Fact]
    public async Task Get_reports_a_missing_profile_as_not_ready_with_a_clear_reason()
    {
        var floorPlanVersionId = Guid.NewGuid();
        var publishedCurationId = Guid.NewGuid();
        var repository = new ProfileRepositorySpy();
        var handler = new GetCommissionedHouseAdaptationReadinessHandler(repository);

        var result = await handler.HandleAsync(
            floorPlanVersionId,
            publishedCurationId,
            CancellationToken.None);

        Assert.Equal(floorPlanVersionId, Assert.Single(repository.RequestedIds));
        Assert.Equal(publishedCurationId, Assert.Single(repository.RequestedPublishedCurationIds));
        Assert.False(result.IsReady);
        Assert.Contains(
            result.Reasons,
            reason => reason.Contains("No commissioned house adaptation profile", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Get_rejects_an_empty_floor_plan_version_id_before_repository_access()
    {
        var repository = new ProfileRepositorySpy();
        var handler = new GetCommissionedHouseAdaptationReadinessHandler(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(Guid.Empty, Guid.NewGuid(), CancellationToken.None));

        Assert.Empty(repository.RequestedIds);
        Assert.Empty(repository.RequestedPublishedCurationIds);
    }

    [Fact]
    public async Task Get_profile_returns_the_exact_ready_profile_and_its_readiness()
    {
        var profile = ReadyProfile();
        var repository = new ProfileRepositorySpy(profile);
        var handler = new GetCommissionedHouseAdaptationProfileHandler(repository);

        var result = await handler.HandleAsync(
            profile.FloorPlanVersionId,
            profile.PublishedCurationId,
            CancellationToken.None);

        Assert.Same(profile, result.Profile);
        Assert.True(result.Readiness.IsReady, string.Join("; ", result.Readiness.Reasons));
        Assert.Equal(profile.FloorPlanVersionId, Assert.Single(repository.RequestedIds));
        Assert.Equal(profile.PublishedCurationId, Assert.Single(repository.RequestedPublishedCurationIds));
    }

    [Fact]
    public async Task Get_profile_returns_null_and_not_ready_when_the_house_is_not_commissioned()
    {
        var floorPlanVersionId = Guid.NewGuid();
        var publishedCurationId = Guid.NewGuid();
        var repository = new ProfileRepositorySpy();
        var handler = new GetCommissionedHouseAdaptationProfileHandler(repository);

        var result = await handler.HandleAsync(
            floorPlanVersionId,
            publishedCurationId,
            CancellationToken.None);

        Assert.Null(result.Profile);
        Assert.False(result.Readiness.IsReady);
        Assert.Contains(
            result.Readiness.Reasons,
            reason => reason.Contains("No commissioned house adaptation profile", StringComparison.Ordinal));
        Assert.Equal(floorPlanVersionId, Assert.Single(repository.RequestedIds));
        Assert.Equal(publishedCurationId, Assert.Single(repository.RequestedPublishedCurationIds));
    }

    [Fact]
    public async Task Get_profile_returns_null_and_not_ready_when_the_profile_targets_a_stale_published_curation()
    {
        var profile = ReadyProfile();
        var expectedPublishedCurationId = Guid.NewGuid();
        var repository = new ProfileRepositorySpy(profile);
        var handler = new GetCommissionedHouseAdaptationProfileHandler(repository);

        var result = await handler.HandleAsync(
            profile.FloorPlanVersionId,
            expectedPublishedCurationId,
            CancellationToken.None);

        Assert.Null(result.Profile);
        Assert.False(result.Readiness.IsReady);
        Assert.Equal(profile.FloorPlanVersionId, Assert.Single(repository.RequestedIds));
        Assert.Equal(expectedPublishedCurationId, Assert.Single(repository.RequestedPublishedCurationIds));
    }

    private static CommissionedHouseAdaptationProfile ReadyProfile()
        => Profile(
            Variable(HouseAdaptationAxis.Width, Template("width", "Width", "Right")),
            Variable(HouseAdaptationAxis.Depth, Template("depth", "Height", "Top")));

    private static CommissionedHouseAdaptationProfile Profile(
        params CommissionedAdaptationVariable[] variables)
        => new(
            FloorPlanVersionId: Guid.NewGuid(),
            PublishedCurationId: Guid.NewGuid(),
            SourceToMillimetersFactor: 12.7m,
            Variables: variables
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
                .ToArray(),
            ImmutableSizeEntityRefs: [],
            ProtectedEntityRefs: [CoverageEntityRef])
        {
            AuxiliaryEntityBindings =
            [
                new CommissionExistingCurationAuxiliaryEntityBinding(
                    CoverageEntityRef,
                    CommissionExistingCurationAuxiliaryEntityKind.Label,
                    new AdjustmentRecipeBoundsDto(1m, 1m, 2m, 2m),
                    GeometryPathId: null,
                    SegmentSortOrder: null,
                    IsImmutableSize: false,
                    IsProtected: true)
            ]
        };

    private static CommissionedAdaptationVariable Variable(
        HouseAdaptationAxis axis,
        AdjustmentRecipeStretchActionDto action)
        => new(action.ActionId, action.ActionId, axis, 1, [action]);

    private static AdjustmentRecipeStretchActionDto Template(
        string id,
        string axisTag,
        string edge)
    {
        var firstEntity = $"{id}:A";
        var secondEntity = $"{id}:B";
        var firstPath = Guid.NewGuid();
        var secondPath = Guid.NewGuid();

        return new AdjustmentRecipeStretchActionDto(
            id,
            axisTag,
            edge,
            10m,
            0m,
            2m,
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

    private sealed class ProfileRepositorySpy : ICommissionedHouseAdaptationProfileRepository
    {
        private readonly CommissionedHouseAdaptationProfile? profile;

        public ProfileRepositorySpy(CommissionedHouseAdaptationProfile? profile = null)
        {
            this.profile = profile;
        }

        public List<CommissionedHouseAdaptationProfile> Upserts { get; } = [];

        public List<Guid> RequestedIds { get; } = [];

        public List<Guid> RequestedPublishedCurationIds { get; } = [];

        public Task UpsertAsync(
            CommissionedHouseAdaptationProfile profile,
            CancellationToken cancellationToken)
        {
            Upserts.Add(profile);
            return Task.CompletedTask;
        }

        public Task<CommissionedHouseAdaptationProfile?> GetByFloorPlanVersionIdAsync(
            Guid floorPlanVersionId,
            Guid expectedPublishedCurationId,
            CancellationToken cancellationToken)
        {
            RequestedIds.Add(floorPlanVersionId);
            RequestedPublishedCurationIds.Add(expectedPublishedCurationId);
            return Task.FromResult(
                profile?.FloorPlanVersionId == floorPlanVersionId ? profile : null);
        }

        public Task RemoveByFloorPlanVersionIdAsync(
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class UnitOfWorkSpy : IUnitOfWork
    {
        public int CommitCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            CommitCount++;
            return Task.CompletedTask;
        }
    }
}
