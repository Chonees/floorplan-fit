using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Registration;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Registration;

public sealed class RegisterElectricalSheetHandlerTests
{
    [Fact]
    public void RegisterElectricalSheetRequest_exposes_only_registration_identity()
    {
        var request = new RegisterElectricalSheetRequest(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(
            new[] { "ElectricalSheetId", "PlanSetVersionId" },
            typeof(RegisterElectricalSheetRequest)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());
        Assert.NotEqual(Guid.Empty, request.PlanSetVersionId);
        Assert.NotEqual(Guid.Empty, request.ElectricalSheetId);
    }

    [Fact]
    public async Task HandleAsync_resolves_exact_sources_and_passes_their_paths_to_the_estimator()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var floorSource = CreateFloorSource(canonicalFloorPlanVersionId);
        var electricalSource = CreateElectricalSource(electricalSheet.Id);
        var floorSourceReader = new CapturingFloorPlanExtractionSourceReader(floorSource);
        var electricalSourceReader = new CapturingPlanSheetSourceReader(electricalSource);
        var estimator = new CapturingElectricalFloorRegistrationEstimator(CreateConclusiveEstimate());
        var handler = CreateHandler(
            electricalSheet,
            CreatePlanSetVersion(planSetVersionId, canonicalFloorPlanVersionId),
            floorSourceReader,
            electricalSourceReader,
            estimator,
            new CapturingSheetRegistrationRepository(),
            new CapturingUnitOfWork());

        await handler.HandleAsync(
            new RegisterElectricalSheetRequest(planSetVersionId, electricalSheet.Id),
            CancellationToken.None);

        Assert.Equal(new[] { canonicalFloorPlanVersionId }, floorSourceReader.RequestedVersionIds);
        Assert.Equal(new[] { electricalSheet.Id }, electricalSourceReader.RequestedSheetIds);
        var call = Assert.Single(estimator.Calls);
        Assert.Equal(floorSource.ManagedFilePath, call.CanonicalFloorSourcePath);
        Assert.Equal(electricalSource.SourceFilePath, call.ElectricalSourcePath);
    }

    [Fact]
    public async Task HandleAsync_persists_the_owning_canonical_floor_plan_version()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var transform = new SheetRegistrationTransform(1.02m, 90m, 3m, -2m);
        var estimate = CreateConclusiveEstimate(transform, 0.91m, "Unique structural evidence selected one candidate.");
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var clock = new FakeClock(new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc));
        var handler = CreateHandler(
            electricalSheet,
            CreatePlanSetVersion(planSetVersionId, canonicalFloorPlanVersionId),
            new CapturingFloorPlanExtractionSourceReader(CreateFloorSource(canonicalFloorPlanVersionId)),
            new CapturingPlanSheetSourceReader(CreateElectricalSource(electricalSheet.Id)),
            new CapturingElectricalFloorRegistrationEstimator(estimate),
            registrationRepository,
            unitOfWork,
            clock);

        var response = await handler.HandleAsync(
            new RegisterElectricalSheetRequest(planSetVersionId, electricalSheet.Id),
            CancellationToken.None);

        Assert.True(estimate.IsConclusive);
        Assert.Equal(1, unitOfWork.SaveCallCount);
        Assert.Equal(canonicalFloorPlanVersionId, response.CanonicalFloorPlanVersionId);
        Assert.Equal("WholeSheetSimilarity", response.Method);
        AssertTransform(transform, response.Transform);
        Assert.Equal(estimate.Confidence, response.Confidence);
        Assert.Equal("PendingConfirmation", response.Status);
        Assert.Null(response.ConfirmedAtUtc);
        Assert.Equal(estimate.EvidenceSummary, response.RuleSummary);

        var saved = Assert.Single(registrationRepository.Items);
        Assert.Equal(canonicalFloorPlanVersionId, saved.CanonicalFloorPlanVersionId);
        AssertTransform(transform, saved.Transform);
        Assert.Equal(estimate.Confidence, saved.Confidence);
        Assert.Equal(SheetRegistrationStatus.PendingConfirmation, saved.Status);
        Assert.Null(saved.ConfirmedAtUtc);
        Assert.Equal(estimate.EvidenceSummary, saved.RuleSummary);
        Assert.Equal(clock.UtcNow, saved.CreatedAtUtc);
        Assert.Equal(
            new WholePlanRegistrationProof(
                WholePlanRegistrationProof.CurrentVersion,
                Passed: true,
                canonicalFloorPlanVersionId,
                electricalSheet.Id,
                estimate.CanonicalSourceSha256!,
                estimate.DependentSourceSha256!,
                estimate.HorizontalCoverage!.Value,
                estimate.VerticalCoverage!.Value,
                estimate.RootMeanSquareResidual!.Value,
                estimate.MaximumResidual!.Value),
            saved.WholePlanRegistrationProof);
    }

    [Fact]
    public async Task HandleAsync_audits_estimate_metrics_and_candidate_decisions()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var estimate = CreateConclusiveEstimate();
        var auditRepository = new CapturingPlanSetAuditEventRepository();
        var handler = CreateHandler(
            electricalSheet,
            CreatePlanSetVersion(planSetVersionId, canonicalFloorPlanVersionId),
            new CapturingFloorPlanExtractionSourceReader(CreateFloorSource(canonicalFloorPlanVersionId)),
            new CapturingPlanSheetSourceReader(CreateElectricalSource(electricalSheet.Id)),
            new CapturingElectricalFloorRegistrationEstimator(estimate),
            new CapturingSheetRegistrationRepository(),
            new CapturingUnitOfWork(),
            auditEventRepository: auditRepository);

        await handler.HandleAsync(
            new RegisterElectricalSheetRequest(planSetVersionId, electricalSheet.Id),
            CancellationToken.None);

        var auditEvent = Assert.Single(auditRepository.Items);
        using var payload = JsonDocument.Parse(auditEvent.PayloadJson);
        var root = payload.RootElement;
        var selectedTransform = root.GetProperty("transform");
        Assert.Equal(estimate.Transform!.Scale, selectedTransform.GetProperty("scale").GetDecimal());
        Assert.Equal(estimate.Transform.RotationDegrees, selectedTransform.GetProperty("rotationDegrees").GetDecimal());
        Assert.Equal(estimate.Transform.TranslateX, selectedTransform.GetProperty("translateX").GetDecimal());
        Assert.Equal(estimate.Transform.TranslateY, selectedTransform.GetProperty("translateY").GetDecimal());
        Assert.Equal(estimate.ObservedScaleX, root.GetProperty("observedScaleX").GetDecimal());
        Assert.Equal(estimate.ObservedScaleY, root.GetProperty("observedScaleY").GetDecimal());
        Assert.Equal(estimate.HorizontalCoverage, root.GetProperty("horizontalCoverage").GetDecimal());
        Assert.Equal(estimate.VerticalCoverage, root.GetProperty("verticalCoverage").GetDecimal());
        Assert.Equal(estimate.RootMeanSquareResidual, root.GetProperty("rootMeanSquareResidual").GetDecimal());
        Assert.Equal(estimate.MaximumResidual, root.GetProperty("maximumResidual").GetDecimal());

        var candidates = root.GetProperty("candidates").EnumerateArray().ToArray();
        var accepted = Assert.Single(candidates.Where(candidate => candidate.GetProperty("accepted").GetBoolean()));
        var rejected = Assert.Single(candidates.Where(candidate => !candidate.GetProperty("accepted").GetBoolean()));
        Assert.Equal("Accepted unique generic evidence.", accepted.GetProperty("reason").GetString());
        Assert.Equal("Rejected weaker generic evidence.", rejected.GetProperty("reason").GetString());
        Assert.Equal(0.03m, accepted.GetProperty("leftEdgeResidual").GetDecimal());
        Assert.Equal(0.04m, accepted.GetProperty("rightEdgeResidual").GetDecimal());
        Assert.Equal(0.05m, accepted.GetProperty("bottomEdgeResidual").GetDecimal());
        Assert.Equal(0.06m, accepted.GetProperty("topEdgeResidual").GetDecimal());
    }

    [Theory]
    [InlineData(ElectricalFloorRegistrationEstimateStatus.Ambiguous)]
    [InlineData(ElectricalFloorRegistrationEstimateStatus.InsufficientEvidence)]
    public async Task HandleAsync_requires_manual_review_before_writes(
        ElectricalFloorRegistrationEstimateStatus status)
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var estimate = CreateInconclusiveEstimate(status);
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = CreateHandler(
            electricalSheet,
            CreatePlanSetVersion(planSetVersionId, canonicalFloorPlanVersionId),
            new CapturingFloorPlanExtractionSourceReader(CreateFloorSource(canonicalFloorPlanVersionId)),
            new CapturingPlanSheetSourceReader(CreateElectricalSource(electricalSheet.Id)),
            new CapturingElectricalFloorRegistrationEstimator(estimate),
            registrationRepository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<ElectricalFloorRegistrationManualReviewRequiredException>(
            () => handler.HandleAsync(
                new RegisterElectricalSheetRequest(planSetVersionId, electricalSheet.Id),
                CancellationToken.None));

        Assert.Same(estimate, exception.Estimate);
        Assert.False(estimate.IsConclusive);
        Assert.Equal(0, registrationRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_fails_before_estimation_when_exact_floor_source_is_missing()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var estimator = new CapturingElectricalFloorRegistrationEstimator(CreateConclusiveEstimate());
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = CreateHandler(
            electricalSheet,
            CreatePlanSetVersion(planSetVersionId, canonicalFloorPlanVersionId),
            new CapturingFloorPlanExtractionSourceReader(),
            new CapturingPlanSheetSourceReader(CreateElectricalSource(electricalSheet.Id)),
            estimator,
            registrationRepository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new RegisterElectricalSheetRequest(planSetVersionId, electricalSheet.Id),
            CancellationToken.None));

        Assert.Contains("floor", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("source", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(estimator.Calls);
        Assert.Equal(0, registrationRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_fails_before_estimation_when_electrical_source_is_missing()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var electricalSheet = CreateSheet(planSetVersionId, PlanSheetType.ElectricalPlan);
        var estimator = new CapturingElectricalFloorRegistrationEstimator(CreateConclusiveEstimate());
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = CreateHandler(
            electricalSheet,
            CreatePlanSetVersion(planSetVersionId, canonicalFloorPlanVersionId),
            new CapturingFloorPlanExtractionSourceReader(CreateFloorSource(canonicalFloorPlanVersionId)),
            new CapturingPlanSheetSourceReader(),
            estimator,
            registrationRepository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new RegisterElectricalSheetRequest(planSetVersionId, electricalSheet.Id),
            CancellationToken.None));

        Assert.Contains("electrical", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("source", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(estimator.Calls);
        Assert.Equal(0, registrationRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_rejects_non_electrical_sheet()
    {
        var planSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var roofSheet = CreateSheet(planSetVersionId, PlanSheetType.RoofPlan);
        var estimator = new CapturingElectricalFloorRegistrationEstimator(CreateConclusiveEstimate());
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = CreateHandler(
            roofSheet,
            CreatePlanSetVersion(planSetVersionId, canonicalFloorPlanVersionId),
            new CapturingFloorPlanExtractionSourceReader(CreateFloorSource(canonicalFloorPlanVersionId)),
            new CapturingPlanSheetSourceReader(),
            estimator,
            registrationRepository,
            unitOfWork);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new RegisterElectricalSheetRequest(planSetVersionId, roofSheet.Id),
            CancellationToken.None));

        Assert.Empty(estimator.Calls);
        Assert.Equal(0, registrationRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_rejects_a_sheet_from_another_plan_set_version()
    {
        var requestedPlanSetVersionId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var sheet = CreateSheet(Guid.NewGuid(), PlanSheetType.ElectricalPlan);
        var estimator = new CapturingElectricalFloorRegistrationEstimator(CreateConclusiveEstimate());
        var registrationRepository = new CapturingSheetRegistrationRepository();
        var unitOfWork = new CapturingUnitOfWork();
        var handler = CreateHandler(
            sheet,
            CreatePlanSetVersion(requestedPlanSetVersionId, canonicalFloorPlanVersionId),
            new CapturingFloorPlanExtractionSourceReader(CreateFloorSource(canonicalFloorPlanVersionId)),
            new CapturingPlanSheetSourceReader(),
            estimator,
            registrationRepository,
            unitOfWork);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(
            new RegisterElectricalSheetRequest(requestedPlanSetVersionId, sheet.Id),
            CancellationToken.None));

        Assert.Contains("does not belong", exception.Message, StringComparison.Ordinal);
        Assert.Empty(estimator.Calls);
        Assert.Equal(0, registrationRepository.AddCallCount);
        Assert.Equal(0, unitOfWork.SaveCallCount);
    }

    private static RegisterElectricalSheetHandler CreateHandler(
        PlanSheet sheet,
        PlanSetVersion planSetVersion,
        IFloorPlanExtractionSourceReader floorSourceReader,
        IPlanSheetSourceReader electricalSourceReader,
        IElectricalFloorRegistrationEstimator estimator,
        ISheetRegistrationRepository registrationRepository,
        IUnitOfWork unitOfWork,
        IClock? clock = null,
        IPlanSetAuditEventRepository? auditEventRepository = null)
        => new(
            new FakePlanSheetRepository(sheet),
            new FakePlanSetVersionRepository(planSetVersion),
            floorSourceReader,
            electricalSourceReader,
            estimator,
            registrationRepository,
            unitOfWork,
            clock ?? new FakeClock(new DateTime(2026, 6, 30, 18, 0, 0, DateTimeKind.Utc)),
            auditEventRepository);

    private static PlanSheet CreateSheet(Guid planSetVersionId, PlanSheetType sheetType)
        => new(
            Guid.NewGuid(),
            planSetVersionId,
            sheetType,
            Guid.NewGuid(),
            Guid.NewGuid(),
            sheetType.ToString(),
            PlanSheetStatus.Imported,
            new DateTime(2026, 6, 30, 17, 0, 0, DateTimeKind.Utc));

    private static PlanSetVersion CreatePlanSetVersion(Guid id, Guid canonicalFloorPlanVersionId)
        => new(
            id,
            Guid.NewGuid(),
            canonicalFloorPlanVersionId,
            versionNumber: 1,
            createdAtUtc: new DateTime(2026, 6, 30, 16, 0, 0, DateTimeKind.Utc));

    private static FloorPlanExtractionSource CreateFloorSource(Guid floorPlanVersionId)
        => new(Guid.NewGuid(), floorPlanVersionId, "managed/canonical-floor-source.dxf");

    private static PlanSheetSourceDto CreateElectricalSource(Guid sheetId)
        => new(
            sheetId,
            "ElectricalPlan",
            "Generic electrical sheet",
            Guid.NewGuid(),
            "source/electrical-sheet-source.dxf");

    private static ElectricalFloorRegistrationEstimate CreateConclusiveEstimate(
        SheetRegistrationTransform? transform = null,
        decimal confidence = 0.91m,
        string evidenceSummary = "Unique generic structural evidence.")
        => new(
            Status: ElectricalFloorRegistrationEstimateStatus.Estimated,
            Transform: transform ?? new SheetRegistrationTransform(1.02m, 90m, 3m, -2m),
            Confidence: confidence,
            ObservedScaleX: 1.02m,
            ObservedScaleY: 1.01m,
            HorizontalCoverage: 0.94m,
            VerticalCoverage: 0.91m,
            RootMeanSquareResidual: 0.01m,
            MaximumResidual: 0.02m,
            EvidenceSummary: evidenceSummary,
            Candidates:
            [
                new ElectricalFloorRegistrationCandidateEvidence(
                    RotationDegrees: 90m,
                    Accepted: true,
                    Scale: 1.02m,
                    TranslateX: 3m,
                    TranslateY: -2m,
                    HorizontalCoverage: 0.94m,
                    VerticalCoverage: 0.91m,
                    RootMeanSquareResidual: 0.01m,
                    MaximumResidual: 0.02m,
                    LeftEdgeResidual: 0.03m,
                    RightEdgeResidual: 0.04m,
                    BottomEdgeResidual: 0.05m,
                    TopEdgeResidual: 0.06m,
                    Reason: "Accepted unique generic evidence."),
                new ElectricalFloorRegistrationCandidateEvidence(
                    RotationDegrees: 0m,
                    Accepted: false,
                    Scale: 0.98m,
                    TranslateX: 1m,
                    TranslateY: 2m,
                    HorizontalCoverage: 0.70m,
                    VerticalCoverage: 0.68m,
                    RootMeanSquareResidual: 0.42m,
                    MaximumResidual: 0.80m,
                    LeftEdgeResidual: 0.20m,
                    RightEdgeResidual: 0.30m,
                    BottomEdgeResidual: 0.40m,
                    TopEdgeResidual: 0.50m,
                    Reason: "Rejected weaker generic evidence.")
            ],
            CanonicalSourceSha256: new string('a', 64),
            DependentSourceSha256: new string('b', 64));

    private static ElectricalFloorRegistrationEstimate CreateInconclusiveEstimate(
        ElectricalFloorRegistrationEstimateStatus status)
        => new(
            Status: status,
            Transform: null,
            Confidence: 0.25m,
            ObservedScaleX: null,
            ObservedScaleY: null,
            HorizontalCoverage: null,
            VerticalCoverage: null,
            RootMeanSquareResidual: null,
            MaximumResidual: null,
            EvidenceSummary: "Generic evidence did not select one candidate.",
            Candidates:
            [
                new ElectricalFloorRegistrationCandidateEvidence(
                    RotationDegrees: 0m,
                    Accepted: false,
                    Scale: null,
                    TranslateX: null,
                    TranslateY: null,
                    HorizontalCoverage: null,
                    VerticalCoverage: null,
                    RootMeanSquareResidual: null,
                    MaximumResidual: null,
                    LeftEdgeResidual: null,
                    RightEdgeResidual: null,
                    BottomEdgeResidual: null,
                    TopEdgeResidual: null,
                    Reason: "Manual review required.")
            ]);

    private static void AssertTransform(
        SheetRegistrationTransform expected,
        SheetRegistrationTransform actual)
    {
        Assert.Equal(expected.Scale, actual.Scale);
        Assert.Equal(expected.RotationDegrees, actual.RotationDegrees);
        Assert.Equal(expected.TranslateX, actual.TranslateX);
        Assert.Equal(expected.TranslateY, actual.TranslateY);
    }

    private static void AssertTransform(
        SheetRegistrationTransform expected,
        SheetRegistrationTransformDto actual)
    {
        Assert.Equal(expected.Scale, actual.Scale);
        Assert.Equal(expected.RotationDegrees, actual.RotationDegrees);
        Assert.Equal(expected.TranslateX, actual.TranslateX);
        Assert.Equal(expected.TranslateY, actual.TranslateY);
    }

    private sealed class FakePlanSetVersionRepository : IPlanSetVersionRepository
    {
        private readonly PlanSetVersion version;

        public FakePlanSetVersionRepository(PlanSetVersion version)
        {
            this.version = version;
        }

        public Task<PlanSetVersion?> GetByIdAsync(Guid planSetVersionId, CancellationToken cancellationToken)
            => Task.FromResult(version.Id == planSetVersionId ? version : null);

        public Task<PlanSetVersion?> GetByCanonicalFloorPlanVersionAsync(
            Guid canonicalFloorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult(
                version.CanonicalFloorPlanVersionId == canonicalFloorPlanVersionId ? version : null);

        public Task AddAsync(PlanSetVersion version, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class FakePlanSheetRepository : IPlanSheetRepository
    {
        private readonly PlanSheet sheet;

        public FakePlanSheetRepository(PlanSheet sheet)
        {
            this.sheet = sheet;
        }

        public Task AddAsync(PlanSheet sheet, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PlanSheet?> GetByIdAsync(Guid sheetId, CancellationToken cancellationToken)
            => Task.FromResult(sheet.Id == sheetId ? sheet : null);

        public Task RemoveAsync(Guid sheetId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class CapturingFloorPlanExtractionSourceReader : IFloorPlanExtractionSourceReader
    {
        private readonly IReadOnlyDictionary<Guid, FloorPlanExtractionSource> sources;

        public CapturingFloorPlanExtractionSourceReader(params FloorPlanExtractionSource[] sources)
        {
            this.sources = sources.ToDictionary(source => source.FloorPlanVersionId);
        }

        public List<Guid> RequestedVersionIds { get; } = [];

        public Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(
            Guid templateId,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Registration must resolve the exact canonical version.");

        public Task<FloorPlanExtractionSource?> GetByVersionAsync(
            Guid floorPlanVersionId,
            CancellationToken cancellationToken)
        {
            RequestedVersionIds.Add(floorPlanVersionId);
            sources.TryGetValue(floorPlanVersionId, out var source);
            return Task.FromResult<FloorPlanExtractionSource?>(source);
        }
    }

    private sealed class CapturingPlanSheetSourceReader : IPlanSheetSourceReader
    {
        private readonly IReadOnlyDictionary<Guid, PlanSheetSourceDto> sources;

        public CapturingPlanSheetSourceReader(params PlanSheetSourceDto[] sources)
        {
            this.sources = sources.ToDictionary(source => source.SheetId);
        }

        public List<Guid> RequestedSheetIds { get; } = [];

        public Task<PlanSheetSourceDto?> GetBySheetIdAsync(Guid sheetId, CancellationToken cancellationToken)
        {
            RequestedSheetIds.Add(sheetId);
            sources.TryGetValue(sheetId, out var source);
            return Task.FromResult<PlanSheetSourceDto?>(source);
        }
    }

    private sealed class CapturingElectricalFloorRegistrationEstimator : IElectricalFloorRegistrationEstimator
    {
        private readonly ElectricalFloorRegistrationEstimate estimate;

        public CapturingElectricalFloorRegistrationEstimator(ElectricalFloorRegistrationEstimate estimate)
        {
            this.estimate = estimate;
        }

        public List<(string CanonicalFloorSourcePath, string ElectricalSourcePath)> Calls { get; } = [];

        public Task<ElectricalFloorRegistrationEstimate> EstimateAsync(
            string canonicalFloorSourcePath,
            string electricalSourcePath,
            CancellationToken cancellationToken)
        {
            Calls.Add((canonicalFloorSourcePath, electricalSourcePath));
            return Task.FromResult(estimate);
        }
    }

    private sealed class CapturingSheetRegistrationRepository : ISheetRegistrationRepository
    {
        public List<SheetRegistration> Items { get; } = [];

        public int AddCallCount { get; private set; }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
        {
            AddCallCount++;
            Items.Add(registration);
            return Task.CompletedTask;
        }

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == registrationId));
    }

    private sealed class CapturingUnitOfWork : IUnitOfWork
    {
        public int SaveCallCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCallCount++;
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

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTime utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTime UtcNow { get; }
    }
}
