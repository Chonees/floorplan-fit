using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Projection;

public sealed class ProjectFacadeElevationSheetAdjustmentHandlerTests
{
    [Fact]
    public async Task HandleAsync_projects_facade_horizontal_reference_and_preserves_vertical_values()
    {
        var clock = new FakeClock(new DateTime(2026, 6, 30, 23, 30, 0, DateTimeKind.Utc));
        var registration = CreateFacadeRegistration("PreserveVertical=true;HorizontalReference=FrontWallBaseline");
        var projectionRepository = new CapturingSheetAdjustmentProjectionRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ProjectFacadeElevationSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            projectionRepository,
            unitOfWork,
            clock);
        var canonicalAdjustmentId = Guid.NewGuid();

        var response = await handler.HandleAsync(
            new ProjectFacadeElevationSheetAdjustmentRequest(
                registration.Id,
                canonicalAdjustmentId,
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 2m,
                    SiteOffsetX: 10m,
                    SiteOffsetY: 99m,
                    CompressionSteps: [])),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal(registration.PlanSetVersionId, response.PlanSetVersionId);
        Assert.Equal(registration.DependentSheetId, response.DependentSheetId);
        Assert.Equal("FacadeHorizontalPreservingVerticals", response.Method);
        Assert.Equal("ReadyForExport", response.Status);
        Assert.Equal("PreserveVertical=true;HorizontalReference=FrontWallBaseline", response.RuleSummary);
        Assert.Equal(2.4m, response.Transform.Scale);
        Assert.Equal(0m, response.Transform.RotationDegrees);
        Assert.Equal(24m, response.Transform.TranslateX);
        Assert.Equal(0m, response.Transform.TranslateY);

        var saved = Assert.Single(projectionRepository.Items);
        Assert.Equal(SheetAdjustmentProjectionMethod.FacadeHorizontalPreservingVerticals, saved.Method);
        Assert.Equal("PreserveVertical=true;HorizontalReference=FrontWallBaseline", saved.RuleSummary);
    }

    [Fact]
    public async Task HandleAsync_requires_manual_confirmation_when_vertical_rule_is_missing()
    {
        var registration = CreateFacadeRegistration(ruleSummary: null);
        var handler = new ProjectFacadeElevationSheetAdjustmentHandler(
            new FakeSheetRegistrationRepository(registration),
            new CapturingSheetAdjustmentProjectionRepository(),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 30, 23, 30, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new ProjectFacadeElevationSheetAdjustmentRequest(
                registration.Id,
                Guid.NewGuid(),
                new AdjustedSitePlanPlacementDto(
                    FloorToSiteScale: 1m,
                    SiteOffsetX: 0m,
                    SiteOffsetY: 0m,
                    CompressionSteps: [])),
            CancellationToken.None);

        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Contains("vertical", response.Warning, StringComparison.OrdinalIgnoreCase);
    }

    private static SheetRegistration CreateFacadeRegistration(string? ruleSummary)
    {
        return new SheetRegistration(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SheetRegistrationMethod.FacadeHorizontalReference,
            new SheetRegistrationTransform(
                scale: 1.2m,
                rotationDegrees: 0m,
                translateX: 7m,
                translateY: 0m),
            0.91m,
            SheetRegistrationStatus.Confirmed,
            new DateTime(2026, 6, 30, 23, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 23, 5, 0, DateTimeKind.Utc),
            warning: null,
            ruleSummary: ruleSummary);
    }

    private sealed class FakeSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly SheetRegistration registration;

        public FakeSheetRegistrationRepository(SheetRegistration registration)
        {
            this.registration = registration;
        }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(registration.Id == registrationId ? registration : null);
        }
    }

    private sealed class CapturingSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        public List<SheetAdjustmentProjection> Items { get; } = [];

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            Items.Add(projection);
            return Task.CompletedTask;
        }

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Items.FirstOrDefault(item => item.Id == projectionId));
        }
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public bool Saved { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saved = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
