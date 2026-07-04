using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Application.PlanSets.ExportAudit;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Export;

public sealed class ExportMultiSheetPlanSetPackageHandlerTests
{
    [Fact]
    public async Task HandleAsync_exports_projected_sheets_then_audits_package_paths()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(planSetVersionId, canonicalAdjustmentId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(projection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var auditRepository = new CapturingPlanSetExportRepository();
        var expectedOutputPath = Path.Combine(
            "exports",
            "plan-sets",
            "package",
            $"electrical-{projection.Id:N}.dxf");
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(
                    projection.DependentSheetId,
                    "ElectricalPlan",
                    "Electrical",
                    Guid.NewGuid(),
                    "library/raw-dxf/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(),
                auditRepository,
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan-adjusted.dxf",
                Path.Combine("exports", "plan-sets", "package"),
                [projection.Id]),
            CancellationToken.None);

        var exportCall = Assert.Single(projectedSheetExporter.Calls);
        Assert.Equal("library/raw-dxf/electrical.dxf", exportCall.SourceFilePath);
        Assert.Equal(expectedOutputPath, exportCall.OutputFilePath);

        var dependentSheet = Assert.Single(response.Sheets, sheet => sheet.ProjectionId == projection.Id);
        Assert.Equal("ProjectedAutomatically", dependentSheet.Status);
        Assert.Equal(expectedOutputPath, dependentSheet.StoragePath);
        Assert.Equal("exports/plan-sets/package/manifest.json", response.PackageManifestPath);

        var savedAudit = Assert.Single(auditRepository.Items);
        Assert.Contains(
            savedAudit.Sheets,
            sheet => sheet.SheetProjectionId == projection.Id &&
                     sheet.StoragePath == expectedOutputPath);
    }

    [Fact]
    public async Task HandleAsync_without_explicit_projection_ids_exports_ready_projections_and_audits_manual_or_missing_sheets()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var electricalSheetId = Guid.NewGuid();
        var roofSheetId = Guid.NewGuid();
        var facadeSheetId = Guid.NewGuid();
        var readyProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            electricalSheetId);
        var manualProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            roofSheetId);
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(readyProjection, manualProjection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var packageDirectory = Path.Combine("exports", "plan-sets", "package");
        var expectedOutputPath = Path.Combine(packageDirectory, $"electrical-{readyProjection.Id:N}.dxf");
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(electricalSheetId, "ElectricalPlan", "Electrical", Guid.NewGuid(), "library/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(
                    planSetVersionId,
                    [
                        CreateSheet(electricalSheetId, "ElectricalPlan"),
                        CreateSheet(roofSheetId, "RoofPlan"),
                        CreateSheet(facadeSheetId, "FacadeElevation")
                    ]),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan-adjusted.dxf",
                packageDirectory,
                []),
            CancellationToken.None);

        var exportCall = Assert.Single(projectedSheetExporter.Calls);
        Assert.Equal("library/electrical.dxf", exportCall.SourceFilePath);
        Assert.Equal(expectedOutputPath, exportCall.OutputFilePath);

        Assert.Equal(4, response.Summary.TotalSheetCount);
        Assert.Equal(1, response.Summary.AutomaticallyProjectedSheetCount);
        Assert.Equal(2, response.Summary.ManualConfirmationRequiredSheetCount);

        var electrical = Assert.Single(response.Sheets, sheet => sheet.SheetId == electricalSheetId);
        Assert.Equal("ProjectedAutomatically", electrical.Status);
        Assert.Equal(expectedOutputPath, electrical.StoragePath);

        var roof = Assert.Single(response.Sheets, sheet => sheet.SheetId == roofSheetId);
        Assert.Equal("RequiresManualConfirmation", roof.Status);
        Assert.Null(roof.StoragePath);

        var facade = Assert.Single(response.Sheets, sheet => sheet.SheetId == facadeSheetId);
        Assert.Equal("MissingProjection", facade.Status);
    }

    [Fact]
    public async Task HandleAsync_without_explicit_projection_ids_uses_latest_projection_per_sheet()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var electricalSheetId = Guid.NewGuid();
        var oldReadyProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            electricalSheetId,
            new DateTime(2026, 6, 30, 23, 50, 0, DateTimeKind.Utc));
        var latestManualProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            electricalSheetId,
            new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc));
        var projectionRepository = new FakeSheetAdjustmentProjectionRepository(oldReadyProjection, latestManualProjection);
        var projectedSheetExporter = new CapturingProjectedPlanSheetExporter();
        var packageHandler = new ExportMultiSheetPlanSetPackageHandler(
            projectionRepository,
            new FakePlanSheetSourceReader(
                new PlanSheetSourceDto(electricalSheetId, "ElectricalPlan", "Electrical", Guid.NewGuid(), "library/electrical.dxf")),
            new ExportProjectedPlanSheetHandler(projectionRepository, projectedSheetExporter),
            new CreateMultiSheetExportAuditHandler(
                projectionRepository,
                new FakePlanSheetReader(planSetVersionId, [CreateSheet(electricalSheetId, "ElectricalPlan")]),
                new CapturingPlanSetExportRepository(),
                new CapturingPlanSetAuditEventRepository(),
                new CapturingPlanSetExportManifestWriter("exports/plan-sets/package/manifest.json"),
                new CapturingUnitOfWork(),
                new FakeClock(new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc))));

        var response = await packageHandler.HandleAsync(
            new ExportMultiSheetPlanSetPackageRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan-adjusted.dxf",
                Path.Combine("exports", "plan-sets", "package"),
                []),
            CancellationToken.None);

        Assert.Empty(projectedSheetExporter.Calls);
        var electrical = Assert.Single(response.Sheets, sheet => sheet.SheetId == electricalSheetId);
        Assert.Equal(latestManualProjection.Id, electrical.ProjectionId);
        Assert.Equal("RequiresManualConfirmation", electrical.Status);
        Assert.Null(electrical.StoragePath);
    }

    private static SheetAdjustmentProjection CreateProjection(Guid planSetVersionId, Guid canonicalAdjustmentId)
    {
        return CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            Guid.NewGuid());
    }

    private static SheetAdjustmentProjection CreateProjection(
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        SheetAdjustmentProjectionStatus status,
        Guid dependentSheetId,
        DateTime? createdAtUtc = null)
    {
        return new SheetAdjustmentProjection(
            Guid.NewGuid(),
            planSetVersionId,
            dependentSheetId,
            Guid.NewGuid(),
            canonicalAdjustmentId,
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(1m, 0m, 10m, 20m),
            confidence: status is SheetAdjustmentProjectionStatus.ReadyForExport ? 0.9m : 0.6m,
            status,
            warning: status is SheetAdjustmentProjectionStatus.ReadyForExport ? null : "Needs review",
            canonicalCompressionStepCount: 0,
            createdAtUtc: createdAtUtc ?? new DateTime(2026, 6, 30, 23, 58, 0, DateTimeKind.Utc));
    }

    private static PlanSetSheetDto CreateSheet(Guid sheetId, string sheetType)
    {
        return new PlanSetSheetDto(
            sheetId,
            sheetType,
            sheetType,
            Guid.NewGuid(),
            null,
            IsCanonical: false,
            "Registered",
            "NotProjected");
    }

    private sealed class FakeSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        private readonly IReadOnlyDictionary<Guid, SheetAdjustmentProjection> projections;

        public FakeSheetAdjustmentProjectionRepository(params SheetAdjustmentProjection[] projections)
        {
            this.projections = projections.ToDictionary(item => item.Id);
        }

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
        {
            projections.TryGetValue(projectionId, out var projection);
            return Task.FromResult(projection);
        }

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(
                projections.Values
                    .Where(item => item.PlanSetVersionId == planSetVersionId &&
                                   item.CanonicalAdjustmentId == canonicalAdjustmentId)
                    .ToArray());
        }
    }

    private sealed class FakePlanSheetReader : IPlanSheetReader
    {
        private readonly Guid planSetVersionId;
        private readonly IReadOnlyList<PlanSetSheetDto> sheets;

        public FakePlanSheetReader()
            : this(Guid.NewGuid(), [])
        {
        }

        public FakePlanSheetReader(Guid planSetVersionId, IReadOnlyList<PlanSetSheetDto> sheets)
        {
            this.planSetVersionId = planSetVersionId;
            this.sheets = sheets;
        }

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
            IReadOnlyCollection<Guid> planSetVersionIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>>(
                planSetVersionIds.Contains(planSetVersionId)
                    ? new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
                    {
                        [planSetVersionId] = sheets
                    }
                    : new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>());
        }
    }

    private sealed class FakePlanSheetSourceReader : IPlanSheetSourceReader
    {
        private readonly IReadOnlyDictionary<Guid, PlanSheetSourceDto> sources;

        public FakePlanSheetSourceReader(params PlanSheetSourceDto[] sources)
        {
            this.sources = sources.ToDictionary(source => source.SheetId);
        }

        public Task<PlanSheetSourceDto?> GetBySheetIdAsync(Guid sheetId, CancellationToken cancellationToken)
        {
            sources.TryGetValue(sheetId, out var source);
            return Task.FromResult<PlanSheetSourceDto?>(source);
        }
    }

    private sealed class CapturingProjectedPlanSheetExporter : IProjectedPlanSheetExporter
    {
        public List<Call> Calls { get; } = [];

        public Task ExportAsync(
            string sourceFilePath,
            string outputFilePath,
            SheetAdjustmentProjectionTransform transform,
            CancellationToken cancellationToken)
        {
            Calls.Add(new Call(sourceFilePath, outputFilePath));
            return Task.CompletedTask;
        }

        public sealed record Call(string SourceFilePath, string OutputFilePath);
    }

    private sealed class CapturingPlanSetExportRepository : IPlanSetExportRepository
    {
        public List<PlanSetExport> Items { get; } = [];

        public Task AddAsync(PlanSetExport export, CancellationToken cancellationToken)
        {
            Items.Add(export);
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingPlanSetAuditEventRepository : IPlanSetAuditEventRepository
    {
        public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingPlanSetExportManifestWriter : IPlanSetExportManifestWriter
    {
        private readonly string manifestPath;

        public CapturingPlanSetExportManifestWriter(string manifestPath)
        {
            this.manifestPath = manifestPath;
        }

        public Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken)
        {
            return Task.FromResult(manifestPath);
        }
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
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
