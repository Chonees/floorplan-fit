using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.DataCollection;
using FloorplanFit.Application.PlanSets.ExportAudit;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.ExportAudit;

public sealed class CreateMultiSheetExportAuditHandlerTests
{
    [Fact]
    public async Task HandleAsync_persists_export_audit_with_automatic_and_manual_sheet_statuses()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var readyProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            confidence: 0.91m,
            warning: null);
        var manualProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            confidence: 0.72m,
            warning: "Needs review",
            recipeHandlingSummary: "ElectricalPlan: affine placement applied; local recipe requires review before DXF deformation: HorizontalCompression Right @50 delta 2.");
        var exportRepository = new CapturingPlanSetExportRepository();
        var auditEventRepository = new CapturingPlanSetAuditEventRepository();
        var manifestWriter = new CapturingPlanSetExportManifestWriter(
            "exports/package/manifest.json",
            CreateBlockedVerification(PlanSetVerificationReasonCode.MissingExpectedOutput));
        var unitOfWork = new CapturingUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc));
        var qualityReportHandler = new GetPlanSetQualityReportHandler(
            new FakePlanSetAuditEventReader(
                new PlanSetAuditEvent(
                    Guid.NewGuid(),
                    "SheetRegistration",
                    Guid.NewGuid(),
                    "SheetRegistrationQualityMeasured",
                    $$"""
                    {"planSetVersionId":"{{planSetVersionId}}","method":"WholeSheetSimilarity","confidence":0.82,"status":"PendingConfirmation","warning":"Needs visual review","ruleSummary":null}
                    """,
                    new DateTime(2026, 6, 30, 23, 40, 0, DateTimeKind.Utc)),
                new PlanSetAuditEvent(
                    Guid.NewGuid(),
                    "SheetAdjustmentProjection",
                    readyProjection.Id,
                    "SheetAdjustmentProjectionQualityMeasured",
                    $$"""
                    {"planSetVersionId":"{{planSetVersionId}}","method":"ElectricalWholeSheetSimilarity","confidence":0.91,"status":"ReadyForExport","warning":null,"ruleSummary":"WholeSheetSimilarity"}
                    """,
                    new DateTime(2026, 6, 30, 23, 50, 0, DateTimeKind.Utc))));
        var handler = new CreateMultiSheetExportAuditHandler(
            new FakeSheetAdjustmentProjectionRepository(readyProjection, manualProjection),
            new FakePlanSheetReader(),
            exportRepository,
            auditEventRepository,
            manifestWriter,
            unitOfWork,
            clock,
            qualityReportHandler);

        var response = await handler.HandleAsync(
            new CreateMultiSheetExportAuditRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan.dxf",
                [
                    new MultiSheetExportProjectionRequestDto(readyProjection.Id, "exports/electrical.dxf"),
                    new MultiSheetExportProjectionRequestDto(manualProjection.Id)
                ]),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal("exports/package/manifest.json", response.PackageManifestPath);
        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Equal(3, response.Summary.TotalSheetCount);
        Assert.Equal(1, response.Summary.AutomaticallyProjectedSheetCount);
        Assert.Equal(1, response.Summary.ManualConfirmationRequiredSheetCount);
        Assert.Equal(0.72m, response.Summary.LowestConfidence);
        Assert.False(response.Summary.CanExportPackageAutomatically);
        Assert.NotNull(response.QualityReport);
        Assert.Equal(1, response.QualityReport!.RegistrationEventCount);
        Assert.Equal(1, response.QualityReport.ProjectionEventCount);
        Assert.Equal(0.82m, response.QualityReport.LowestRegistrationConfidence);
        Assert.Equal(0.91m, response.QualityReport.LowestProjectionConfidence);
        Assert.Equal(1, response.QualityReport.ManualRegistrationCount);

        var canonicalSheet = Assert.Single(response.Sheets, sheet => sheet.SheetKind == "CanonicalFloorPlan");
        Assert.Equal(canonicalFloorPlanVersionId, canonicalSheet.SheetId);
        Assert.Equal("Exported", canonicalSheet.Status);
        Assert.Equal("exports/floor-plan.dxf", canonicalSheet.StoragePath);
        Assert.Equal(1m, canonicalSheet.Confidence);

        var automaticSheet = Assert.Single(response.Sheets, sheet => sheet.ProjectionId == readyProjection.Id);
        Assert.Equal("ProjectedAutomatically", automaticSheet.Status);
        Assert.Equal("ElectricalWholeSheetSimilarity", automaticSheet.ProjectionMethod);
        Assert.Equal(0.91m, automaticSheet.Confidence);

        var manualSheet = Assert.Single(response.Sheets, sheet => sheet.ProjectionId == manualProjection.Id);
        Assert.Equal("RequiresManualConfirmation", manualSheet.Status);
        Assert.Equal("Needs review", manualSheet.Warning);
        var manualRecipeHandling = Assert.IsType<string>(manualSheet.RecipeHandlingSummary);
        Assert.Contains("HorizontalCompression", manualRecipeHandling);

        var savedExport = Assert.Single(exportRepository.Items);
        Assert.Equal(response.ExportId, savedExport.Id);
        Assert.Equal("exports/package/manifest.json", savedExport.PackageManifestPath);
        Assert.Equal(3, savedExport.Sheets.Count);
        Assert.Contains(savedExport.Sheets, sheet =>
            sheet.SheetProjectionId == manualProjection.Id &&
            sheet.RecipeHandlingSummary?.Contains("HorizontalCompression", StringComparison.Ordinal) == true);

        var manifestAudit = Assert.Single(manifestWriter.Items);
        Assert.Equal(response.ExportId, manifestAudit.ExportId);
        Assert.Equal(3, manifestAudit.Sheets.Count);
        Assert.NotNull(manifestAudit.QualityReport);
        Assert.Equal(1, manifestAudit.QualityReport!.ManualRegistrationCount);

        var auditEvent = Assert.Single(auditEventRepository.Items);
        Assert.Equal("PlanSetExport", auditEvent.AggregateType);
        Assert.Equal(response.ExportId, auditEvent.AggregateId);
        Assert.Equal("PlanSetExportAuditCreated", auditEvent.EventType);
    }

    [Fact]
    public async Task HandleAsync_saves_export_even_when_quality_event_fails()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var projection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            confidence: 0.95m,
            warning: null);
        var exportRepository = new CapturingPlanSetExportRepository();
        var manifestWriter = new CapturingPlanSetExportManifestWriter("exports/package/manifest.json");
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new CreateMultiSheetExportAuditHandler(
            new FakeSheetAdjustmentProjectionRepository(projection),
            new FakePlanSheetReader(),
            exportRepository,
            new ThrowingPlanSetAuditEventRepository(),
            manifestWriter,
            unitOfWork,
            new FakeClock(new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new CreateMultiSheetExportAuditRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan.dxf",
                [new MultiSheetExportProjectionRequestDto(projection.Id, "exports/electrical.dxf")]),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.Equal("exports/package/manifest.json", response.PackageManifestPath);
        Assert.Equal("ReadyForExport", response.Status);
        Assert.Single(exportRepository.Items);
    }

    [Fact]
    public async Task HandleAsync_with_discovery_audits_all_dependent_sheets_and_marks_missing_projection()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var canonicalAdjustmentId = Guid.NewGuid();
        var electricalSheetId = Guid.NewGuid();
        var roofSheetId = Guid.NewGuid();
        var electricalProjection = CreateProjection(
            planSetVersionId,
            canonicalAdjustmentId,
            SheetAdjustmentProjectionStatus.ReadyForExport,
            confidence: 0.93m,
            warning: null,
            dependentSheetId: electricalSheetId);
        var exportRepository = new CapturingPlanSetExportRepository();
        var handler = new CreateMultiSheetExportAuditHandler(
            new FakeSheetAdjustmentProjectionRepository(electricalProjection),
            new FakePlanSheetReader(
                planSetVersionId,
                [
                    CreateSheet(electricalSheetId, "ElectricalPlan"),
                    CreateSheet(roofSheetId, "RoofPlan")
                ]),
            exportRepository,
            new CapturingPlanSetAuditEventRepository(),
            new CapturingPlanSetExportManifestWriter(
                "exports/package/manifest.json",
                CreateBlockedVerification(PlanSetVerificationReasonCode.MissingExpectedOutput)),
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 6, 30, 23, 55, 0, DateTimeKind.Utc)));

        var response = await handler.HandleAsync(
            new CreateMultiSheetExportAuditRequest(
                planSetVersionId,
                canonicalFloorPlanVersionId,
                canonicalAdjustmentId,
                "exports/floor-plan.dxf",
                [new MultiSheetExportProjectionRequestDto(electricalProjection.Id, "exports/electrical.dxf")])
            {
                DiscoverAllDependentSheets = true
            },
            CancellationToken.None);

        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.Equal(3, response.Summary.TotalSheetCount);
        Assert.Equal(1, response.Summary.AutomaticallyProjectedSheetCount);
        Assert.Equal(1, response.Summary.ManualConfirmationRequiredSheetCount);

        var projectedSheet = Assert.Single(response.Sheets, sheet => sheet.SheetId == electricalSheetId);
        Assert.Equal("ProjectedAutomatically", projectedSheet.Status);
        Assert.Equal(electricalProjection.Id, projectedSheet.ProjectionId);
        Assert.Equal("exports/electrical.dxf", projectedSheet.StoragePath);

        var missingSheet = Assert.Single(response.Sheets, sheet => sheet.SheetId == roofSheetId);
        Assert.Equal("MissingProjection", missingSheet.Status);
        Assert.Null(missingSheet.ProjectionId);
        Assert.Contains("No projection", missingSheet.Warning, StringComparison.OrdinalIgnoreCase);

        var saved = Assert.Single(exportRepository.Items);
        Assert.Contains(saved.Sheets, sheet => sheet.PlanSheetId == roofSheetId && sheet.Status == PlanSetExportedSheetStatus.MissingProjection);
    }

    [Fact]
    public async Task HandleAsync_green_verification_report_is_the_only_path_to_ready_for_export()
    {
        var repository = new CapturingPlanSetExportRepository();
        var reportBuiltBeforePersistence = false;
        var writer = new CapturingPlanSetExportManifestWriter(
            "exports/package/manifest.json",
            CreateGreenVerification(),
            () => reportBuiltBeforePersistence = repository.Items.Count == 0);
        var projection = CreateReadyProjection();

        var response = await CreateHandler(repository, writer, projection).HandleAsync(
            CreateRequest(projection),
            CancellationToken.None);

        Assert.True(reportBuiltBeforePersistence);
        Assert.True(response.Verification?.IsGreen);
        Assert.Equal("ReadyForExport", response.Status);
        Assert.True(response.Summary.CanExportPackageAutomatically);
        Assert.Equal(PlanSetExportStatus.ReadyForExport, Assert.Single(repository.Items).Status);
    }

    [Fact]
    public async Task HandleAsync_mismatch_verification_report_cannot_be_ready_for_export()
    {
        var repository = new CapturingPlanSetExportRepository();
        var writer = new CapturingPlanSetExportManifestWriter(
            "exports/package/manifest.json",
            CreateBlockedVerification(PlanSetVerificationReasonCode.FinalOutputCongruenceMismatch));
        var projection = CreateReadyProjection();

        var response = await CreateHandler(repository, writer, projection).HandleAsync(
            CreateRequest(projection),
            CancellationToken.None);

        Assert.False(response.Verification?.IsGreen);
        Assert.Equal("RequiresManualConfirmation", response.Status);
        Assert.False(response.Summary.CanExportPackageAutomatically);
        Assert.Equal(PlanSetExportStatus.RequiresManualConfirmation, Assert.Single(repository.Items).Status);
    }

    [Fact]
    public async Task HandleAsync_missing_required_evidence_cannot_be_ready_for_export()
    {
        var repository = new CapturingPlanSetExportRepository();
        var writer = new CapturingPlanSetExportManifestWriter(
            "exports/package/manifest.json",
            CreateBlockedVerification(PlanSetVerificationReasonCode.MissingRequiredEvidence));
        var projection = CreateReadyProjection();

        var response = await CreateHandler(repository, writer, projection).HandleAsync(
            CreateRequest(projection),
            CancellationToken.None);

        Assert.Equal(PlanSetVerificationDecision.Blocked, response.Verification?.Decision);
        Assert.Contains(
            response.Verification!.Reasons,
            reason => reason.Code == PlanSetVerificationReasonCode.MissingRequiredEvidence);
        Assert.Equal("RequiresManualConfirmation", response.Status);
    }

    [Fact]
    public async Task HandleAsync_unsupported_capability_cannot_be_ready_for_export()
    {
        var repository = new CapturingPlanSetExportRepository();
        var writer = new CapturingPlanSetExportManifestWriter(
            "exports/package/manifest.json",
            CreateBlockedVerification(PlanSetVerificationReasonCode.UnsupportedCapability));
        var projection = CreateReadyProjection();

        var response = await CreateHandler(repository, writer, projection).HandleAsync(
            CreateRequest(projection),
            CancellationToken.None);

        Assert.Equal(PlanSetVerificationCheckStatus.Unsupported, response.Verification?.Capabilities.Status);
        Assert.Equal("RequiresManualConfirmation", response.Status);
    }

    private static CreateMultiSheetExportAuditHandler CreateHandler(
        CapturingPlanSetExportRepository repository,
        CapturingPlanSetExportManifestWriter writer,
        SheetAdjustmentProjection projection)
        => new(
            new FakeSheetAdjustmentProjectionRepository(projection),
            new FakePlanSheetReader(),
            repository,
            new CapturingPlanSetAuditEventRepository(),
            writer,
            new CapturingUnitOfWork(),
            new FakeClock(new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc)));

    private static SheetAdjustmentProjection CreateReadyProjection()
        => CreateProjection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SheetAdjustmentProjectionStatus.ReadyForExport,
            confidence: 0.95m,
            warning: null);

    private static CreateMultiSheetExportAuditRequest CreateRequest(SheetAdjustmentProjection projection)
        => new(
            projection.PlanSetVersionId,
            Guid.NewGuid(),
            projection.CanonicalAdjustmentId,
            "exports/floor-plan.dxf",
            [new MultiSheetExportProjectionRequestDto(projection.Id, "exports/electrical.dxf")]);

    private static PlanSetVerificationReportDto CreateGreenVerification()
        => new(
            SchemaVersion: PlanSetVerificationReportDto.CurrentSchemaVersion,
            new PlanSetVerificationOutputDto(2, 2, []),
            PassedCheck(1),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            PassedCheck(1),
            PassedCheck(1),
            PassedCheck(1),
            PassedCheck(1),
            Reasons: []);

    private static PlanSetVerificationReportDto CreateBlockedVerification(PlanSetVerificationReasonCode code)
    {
        var failedCheck = code == PlanSetVerificationReasonCode.UnsupportedCapability
            ? new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Unsupported, 1, 0, 1, 0)
            : new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Failed, 1, 0, 1, 0);
        return new PlanSetVerificationReportDto(
            PlanSetVerificationReportDto.CurrentSchemaVersion,
            code == PlanSetVerificationReasonCode.MissingExpectedOutput
                ? new PlanSetVerificationOutputDto(2, 1, [Guid.NewGuid()])
                : new PlanSetVerificationOutputDto(2, 2, []),
            code == PlanSetVerificationReasonCode.MissingRequiredEvidence
                ? new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.InsufficientData, 1, 0, 0, 1)
                : PassedCheck(1),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            new PlanSetVerificationOperationDto(0, 0, 0, 0),
            PassedCheck(1),
            PassedCheck(1),
            code == PlanSetVerificationReasonCode.FinalOutputCongruenceMismatch ? failedCheck : PassedCheck(1),
            code == PlanSetVerificationReasonCode.UnsupportedCapability ? failedCheck : PassedCheck(1),
            [new PlanSetVerificationReasonDto(code, "test", null, "Focused gate regression.")]);
    }

    private static PlanSetVerificationCheckDto PassedCheck(int count)
        => new(PlanSetVerificationCheckStatus.Passed, count, count, 0, 0);

    private static SheetAdjustmentProjection CreateProjection(
        Guid planSetVersionId,
        Guid canonicalAdjustmentId,
        SheetAdjustmentProjectionStatus status,
        decimal confidence,
        string? warning,
        Guid? dependentSheetId = null,
        string? recipeHandlingSummary = null)
    {
        return new SheetAdjustmentProjection(
            Guid.NewGuid(),
            planSetVersionId,
            dependentSheetId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            canonicalAdjustmentId,
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(
                scale: 1.1m,
                rotationDegrees: 0m,
                translateX: 10m,
                translateY: 20m),
            confidence,
            status,
            warning,
            canonicalCompressionStepCount: 0,
            new DateTime(2026, 6, 30, 23, 50, 0, DateTimeKind.Utc),
            ruleSummary: "WholeSheetSimilarity",
            recipeHandlingSummary: recipeHandlingSummary);
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
            IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> result =
                planSetVersionIds.Contains(planSetVersionId)
                    ? new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
                    {
                        [planSetVersionId] = sheets
                    }
                    : new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>();

            return Task.FromResult(result);
        }
    }

    private sealed class FakePlanSetAuditEventReader : IPlanSetAuditEventReader
    {
        private readonly IReadOnlyList<PlanSetAuditEvent> events;

        public FakePlanSetAuditEventReader(params PlanSetAuditEvent[] events)
        {
            this.events = events;
        }

        public Task<IReadOnlyList<PlanSetAuditEvent>> ListQualityEventsByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<PlanSetAuditEvent> result = events
                .Where(auditEvent => auditEvent.PayloadJson.Contains(planSetVersionId.ToString(), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return Task.FromResult(result);
        }
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
        public List<PlanSetAuditEvent> Items { get; } = [];

        public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Items.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingPlanSetAuditEventRepository : IPlanSetAuditEventRepository
    {
        public Task AddAsync(PlanSetAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Telemetry sink unavailable.");
        }
    }

    private sealed class CapturingPlanSetExportManifestWriter : IPlanSetExportManifestWriter
    {
        private readonly string manifestPath;
        private readonly PlanSetVerificationReportDto verificationReport;
        private readonly Action? onBuildVerification;

        public CapturingPlanSetExportManifestWriter(
            string manifestPath,
            PlanSetVerificationReportDto? verificationReport = null,
            Action? onBuildVerification = null)
        {
            this.manifestPath = manifestPath;
            this.verificationReport = verificationReport ?? CreateGreenVerification();
            this.onBuildVerification = onBuildVerification;
        }

        public List<MultiSheetExportAuditDto> Items { get; } = [];

        public PlanSetVerificationReportDto BuildVerificationReport(MultiSheetExportAuditDto audit)
        {
            onBuildVerification?.Invoke();
            return verificationReport;
        }

        public Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken)
        {
            Items.Add(audit);
            return Task.FromResult(manifestPath);
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
