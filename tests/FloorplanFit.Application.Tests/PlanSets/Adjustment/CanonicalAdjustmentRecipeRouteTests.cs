using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Adjustment;
using FloorplanFit.Application.PlanSets.ExportAudit;
using FloorplanFit.Application.PlanSets.Projection;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Adjustment;

public sealed class CanonicalAdjustmentRecipeRouteTests
{
    [Fact]
    public async Task Route_records_recipe_projects_dependent_sheet_and_reports_review()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var dependentSheetId = Guid.NewGuid();
        var clock = new FakeClock(new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        var placement = new AdjustedSitePlanPlacementDto(
            2m,
            100m,
            200m,
            [new AdjustedCompressionStepDto("Width", "Right", [new AdjustedCompressionMarkerDto(50m, 2m)])]);
        var adjustmentRepository = new CapturingCanonicalFloorPlanAdjustmentRepository();
        var recordHandler = new RecordCanonicalFloorPlanAdjustmentHandler(
            adjustmentRepository,
            new CapturingUnitOfWork(),
            clock);

        var canonicalAdjustment = await recordHandler.HandleAsync(
            new RecordCanonicalFloorPlanAdjustmentRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                "site.dxf",
                "exports/floor.dxf",
                placement),
            CancellationToken.None);

        var savedAdjustment = Assert.Single(adjustmentRepository.Items);
        Assert.Contains("HorizontalCompression", savedAdjustment.AdjustmentRecipeJson, StringComparison.Ordinal);

        var registration = new SheetRegistration(
            Guid.NewGuid(),
            planSetVersionId,
            dependentSheetId,
            canonicalFloorPlanVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1.5m, 10m, 5m, -3m),
            0.92m,
            SheetRegistrationStatus.Confirmed,
            clock.UtcNow,
            clock.UtcNow,
            warning: null);
        var registrationRepository = new InMemorySheetRegistrationRepository(registration);
        var projectionRepository = new InMemorySheetAdjustmentProjectionRepository();
        var registeredProjector = new ProjectRegisteredPlanSetSheetsHandler(
            registrationRepository,
            new ProjectElectricalSheetAdjustmentHandler(registrationRepository, projectionRepository, new CapturingUnitOfWork(), clock),
            new ProjectRoofSheetAdjustmentHandler(registrationRepository, projectionRepository, new CapturingUnitOfWork(), clock),
            new ProjectFacadeElevationSheetAdjustmentHandler(registrationRepository, projectionRepository, new CapturingUnitOfWork(), clock));

        var projected = await registeredProjector.HandleAsync(
            new ProjectRegisteredPlanSetSheetsRequest(
                planSetVersionId,
                canonicalAdjustment.AdjustmentId,
                placement,
                canonicalAdjustment.AdjustmentRecipe),
            CancellationToken.None);

        var projection = Assert.Single(projected.Projections);
        Assert.Equal("RequiresManualConfirmation", projection.Status);
        Assert.Equal(3m, projection.Transform.Scale);
        Assert.Equal(110m, projection.Transform.TranslateX);
        Assert.Equal(194m, projection.Transform.TranslateY);
        var projectionRecipe = Assert.IsType<string>(projection.RecipeHandlingSummary);
        Assert.Contains("HorizontalCompression", projectionRecipe, StringComparison.Ordinal);

        var manifestWriter = new CapturingPlanSetExportManifestWriter();
        var auditHandler = new CreateMultiSheetExportAuditHandler(
            projectionRepository,
            new EmptyPlanSheetReader(),
            new CapturingPlanSetExportRepository(),
            new CapturingPlanSetAuditEventRepository(),
            manifestWriter,
            new CapturingUnitOfWork(),
            clock);

        var audit = await auditHandler.HandleAsync(
            new CreateMultiSheetExportAuditRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustment.AdjustmentId,
                canonicalAdjustment.CanonicalFloorPlanExportPath,
                [new MultiSheetExportProjectionRequestDto(projection.ProjectionId)]),
            CancellationToken.None);

        var electricalSheet = Assert.Single(audit.Sheets, sheet => sheet.ProjectionId == projection.ProjectionId);
        Assert.Equal("RequiresManualConfirmation", electricalSheet.Status);
        var auditRecipe = Assert.IsType<string>(electricalSheet.RecipeHandlingSummary);
        Assert.Contains("HorizontalCompression", auditRecipe, StringComparison.Ordinal);
        var manifestRecipe = Assert.IsType<string>(
            manifestWriter.Audit!.Sheets.Single(sheet => sheet.ProjectionId == projection.ProjectionId).RecipeHandlingSummary);
        Assert.Contains("HorizontalCompression", manifestRecipe, StringComparison.Ordinal);
    }

    private sealed class CapturingCanonicalFloorPlanAdjustmentRepository : ICanonicalFloorPlanAdjustmentRepository
    {
        public List<CanonicalFloorPlanAdjustment> Items { get; } = [];

        public Task AddAsync(CanonicalFloorPlanAdjustment adjustment, CancellationToken cancellationToken)
        {
            Items.Add(adjustment);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly SheetRegistration registration;

        public InMemorySheetRegistrationRepository(SheetRegistration registration)
        {
            this.registration = registration;
        }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => Task.FromResult(registration.Id == registrationId ? registration : null);

        public Task<IReadOnlyList<SheetRegistration>> ListByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetRegistration>>(
                registration.PlanSetVersionId == planSetVersionId ? [registration] : []);
    }

    private sealed class InMemorySheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        private readonly List<SheetAdjustmentProjection> items = [];

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            items.Add(projection);
            return Task.CompletedTask;
        }

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
            => Task.FromResult(items.FirstOrDefault(item => item.Id == projectionId));

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(
                items.Where(item => item.PlanSetVersionId == planSetVersionId &&
                                    item.CanonicalAdjustmentId == canonicalAdjustmentId)
                    .ToArray());
    }

    private sealed class EmptyPlanSheetReader : IPlanSheetReader
    {
        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
            IReadOnlyCollection<Guid> planSetVersionIds,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>>(
                new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>());
    }

    private sealed class CapturingPlanSetExportRepository : IPlanSetExportRepository
    {
        public Task AddAsync(PlanSetExport export, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class CapturingPlanSetAuditEventRepository : IPlanSetAuditEventRepository
    {
        public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class CapturingPlanSetExportManifestWriter : IPlanSetExportManifestWriter
    {
        public MultiSheetExportAuditDto? Audit { get; private set; }

        public Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken)
        {
            Audit = audit;
            return Task.FromResult("exports/package/manifest.json");
        }
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
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
