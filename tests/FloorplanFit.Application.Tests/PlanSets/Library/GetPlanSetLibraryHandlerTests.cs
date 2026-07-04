using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Library;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Library;

public sealed class GetPlanSetLibraryHandlerTests
{
    [Fact]
    public async Task HandleAsync_projects_current_floor_plan_as_canonical_plan_set_sheet()
    {
        var templateId = Guid.NewGuid();
        var currentVersionId = Guid.NewGuid();
        var activePublishedCurationId = Guid.NewGuid();
        var importedAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            1,
            currentVersionId,
            1,
            [
                new FloorPlanLibraryVersionDto(
                    currentVersionId,
                    1,
                    "Published",
                    importedAtUtc,
                    "inch",
                    IsCurrent: true,
                    activePublishedCurationId)
            ]);

        var handler = new GetPlanSetLibraryHandler(
            new FakeFloorPlanLibraryReader([item]),
            new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>()));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(templateId, planSet.HousePlanSetId);
        Assert.Equal("seminole2000", planSet.Code);
        Assert.Equal("SEMINOLE2000", planSet.Name);
        Assert.Equal(currentVersionId, planSet.ActivePlanSetVersionId);
        Assert.Equal(currentVersionId, planSet.CanonicalFloorPlanVersionId);
        Assert.Equal(activePublishedCurationId, planSet.ActivePublishedCurationId);
        Assert.True(planSet.HasCanonicalFloorPlan);
        Assert.True(planSet.CanProduceCanonicalAdjustment);

        var sheet = Assert.Single(planSet.Sheets);
        Assert.Equal(currentVersionId, sheet.SheetId);
        Assert.Equal("FloorPlan", sheet.SheetType);
        Assert.Equal("SEMINOLE2000 Floor Plan", sheet.Name);
        Assert.Equal(currentVersionId, sheet.SourceFloorPlanVersionId);
        Assert.True(sheet.IsCanonical);
        Assert.Equal("Canonical", sheet.RegistrationStatus);
        Assert.Equal("CanonicalSource", sheet.ProjectionStatus);
    }

    [Fact]
    public async Task HandleAsync_keeps_plan_set_visible_when_floor_plan_has_no_current_version()
    {
        var templateId = Guid.NewGuid();
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "empty-template",
            "Empty Template",
            0,
            null,
            null,
            []);

        var handler = new GetPlanSetLibraryHandler(
            new FakeFloorPlanLibraryReader([item]),
            new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>()));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(templateId, planSet.HousePlanSetId);
        Assert.Null(planSet.ActivePlanSetVersionId);
        Assert.Null(planSet.CanonicalFloorPlanVersionId);
        Assert.False(planSet.HasCanonicalFloorPlan);
        Assert.False(planSet.CanProduceCanonicalAdjustment);
        Assert.Empty(planSet.Sheets);
    }

    [Fact]
    public async Task HandleAsync_includes_dependent_sheets_for_current_plan_set_version()
    {
        var templateId = Guid.NewGuid();
        var currentVersionId = Guid.NewGuid();
        var importedAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var electricalSheet = new PlanSetSheetDto(
            Guid.NewGuid(),
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            null,
            IsCanonical: false,
            "Unregistered",
            "NotProjected");
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            1,
            currentVersionId,
            1,
            [
                new FloorPlanLibraryVersionDto(
                    currentVersionId,
                    1,
                    "Published",
                    importedAtUtc,
                    "inch",
                    IsCurrent: true)
            ]);

        var handler = new GetPlanSetLibraryHandler(
            new FakeFloorPlanLibraryReader([item]),
            new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
            {
                [currentVersionId] = [electricalSheet]
            }));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(2, planSet.Sheets.Count);
        Assert.Contains(planSet.Sheets, sheet => sheet.IsCanonical && sheet.SheetType == "FloorPlan");
        Assert.Contains(planSet.Sheets, sheet => !sheet.IsCanonical && sheet.SheetType == "ElectricalPlan");
    }

    [Fact]
    public async Task HandleAsync_uses_explicit_plan_set_version_when_available()
    {
        var templateId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var importedAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var electricalSheet = new PlanSetSheetDto(
            Guid.NewGuid(),
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            null,
            IsCanonical: false,
            "Unregistered",
            "NotProjected");
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            1,
            canonicalFloorPlanVersionId,
            1,
            [
                new FloorPlanLibraryVersionDto(
                    canonicalFloorPlanVersionId,
                    1,
                    "Published",
                    importedAtUtc,
                    "inch",
                    IsCurrent: true)
            ]);

        var handler = new GetPlanSetLibraryHandler(
            new FakeFloorPlanLibraryReader([item]),
            new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
            {
                [planSetVersionId] = [electricalSheet]
            }),
            new FakePlanSetVersionRepository(
                new PlanSetVersion(
                    planSetVersionId,
                    templateId,
                    canonicalFloorPlanVersionId,
                    versionNumber: 1,
                    createdAtUtc: importedAtUtc)));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(planSetVersionId, planSet.ActivePlanSetVersionId);
        Assert.Equal(canonicalFloorPlanVersionId, planSet.CanonicalFloorPlanVersionId);
        Assert.Equal(2, planSet.Sheets.Count);
        Assert.Contains(planSet.Sheets, sheet => sheet.SheetId == electricalSheet.SheetId);
    }

    [Fact]
    public async Task HandleAsync_uses_existing_house_plan_set_identity_when_available()
    {
        var sourceTemplateId = Guid.NewGuid();
        var housePlanSetId = Guid.NewGuid();
        var currentVersionId = Guid.NewGuid();
        var importedAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        var item = new FloorPlanLibraryItemDto(
            sourceTemplateId,
            "seminole2000",
            "SEMINOLE2000",
            1,
            currentVersionId,
            1,
            [
                new FloorPlanLibraryVersionDto(
                    currentVersionId,
                    1,
                    "Published",
                    importedAtUtc,
                    "inch",
                    IsCurrent: true)
            ]);

        var handler = new GetPlanSetLibraryHandler(
            new FakeFloorPlanLibraryReader([item]),
            new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>()),
            new FakePlanSetVersionRepository(),
            new FakeHousePlanSetRepository(
                new HousePlanSet(
                    housePlanSetId,
                    sourceTemplateId,
                    "seminole2000",
                    "SEMINOLE2000",
                    importedAtUtc)));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        Assert.Equal(housePlanSetId, planSet.HousePlanSetId);
        Assert.NotEqual(sourceTemplateId, planSet.HousePlanSetId);
        Assert.Equal(currentVersionId, planSet.ActivePlanSetVersionId);
    }

    [Fact]
    public async Task HandleAsync_surfaces_latest_registration_status_for_dependent_sheets()
    {
        var templateId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var importedAtUtc = new DateTime(2026, 7, 1, 13, 0, 0, DateTimeKind.Utc);
        var electricalSheet = new PlanSetSheetDto(
            sheetId,
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            null,
            IsCanonical: false,
            "Unregistered",
            "NotProjected");
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            1,
            canonicalFloorPlanVersionId,
            1,
            [
                new FloorPlanLibraryVersionDto(
                    canonicalFloorPlanVersionId,
                    1,
                    "Published",
                    importedAtUtc,
                    "inch",
                    IsCurrent: true)
            ]);
        var registration = new SheetRegistration(
            Guid.NewGuid(),
            planSetVersionId,
            sheetId,
            canonicalFloorPlanVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.25m,
            SheetRegistrationStatus.PendingConfirmation,
            importedAtUtc.AddMinutes(5),
            confirmedAtUtc: null,
            warning: "Needs manual review");

        var handler = new GetPlanSetLibraryHandler(
            new FakeFloorPlanLibraryReader([item]),
            new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
            {
                [planSetVersionId] = [electricalSheet]
            }),
            new FakePlanSetVersionRepository(
                new PlanSetVersion(
                    planSetVersionId,
                    templateId,
                    canonicalFloorPlanVersionId,
                    versionNumber: 1,
                    createdAtUtc: importedAtUtc)),
            housePlanSetRepository: null,
            sheetRegistrationRepository: new FakeSheetRegistrationRepository([registration]));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        var sheet = Assert.Single(planSet.Sheets.Where(item => item.SheetId == sheetId));
        Assert.Equal("PendingConfirmation", sheet.RegistrationStatus);
        Assert.Equal("NotProjected", sheet.ProjectionStatus);
        Assert.Equal(registration.Id, sheet.SheetRegistrationId);
        Assert.Equal("WholeSheetSimilarity", sheet.RegistrationMethod);
        Assert.Equal(0.25m, sheet.RegistrationConfidence);
        Assert.Equal("Needs manual review", sheet.RegistrationWarning);
        Assert.Contains("WholeSheetSimilarity", sheet.RegistrationQualityLabel, StringComparison.Ordinal);
        Assert.Contains("0.25", sheet.RegistrationQualityLabel, StringComparison.Ordinal);
        Assert.True(sheet.CanConfirmRegistration);
        Assert.False(sheet.CanUnlink);
    }

    [Fact]
    public async Task HandleAsync_surfaces_latest_projection_status_for_dependent_sheets()
    {
        var templateId = Guid.NewGuid();
        var canonicalFloorPlanVersionId = Guid.NewGuid();
        var planSetVersionId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var registrationId = Guid.NewGuid();
        var importedAtUtc = new DateTime(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc);
        var electricalSheet = new PlanSetSheetDto(
            sheetId,
            "ElectricalPlan",
            "Electrical",
            Guid.NewGuid(),
            null,
            IsCanonical: false,
            "Confirmed",
            "NotProjected",
            SheetRegistrationId: registrationId);
        var item = new FloorPlanLibraryItemDto(
            templateId,
            "seminole2000",
            "SEMINOLE2000",
            1,
            canonicalFloorPlanVersionId,
            1,
            [
                new FloorPlanLibraryVersionDto(
                    canonicalFloorPlanVersionId,
                    1,
                    "Published",
                    importedAtUtc,
                    "inch",
                    IsCurrent: true)
            ]);
        var projection = new SheetAdjustmentProjection(
            Guid.NewGuid(),
            planSetVersionId,
            sheetId,
            registrationId,
            Guid.NewGuid(),
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(1m, 0m, 0m, 0m),
            0.25m,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            "Low confidence projection requires review.",
            canonicalCompressionStepCount: 0,
            importedAtUtc.AddMinutes(5));

        var handler = new GetPlanSetLibraryHandler(
            new FakeFloorPlanLibraryReader([item]),
            new FakePlanSheetReader(new Dictionary<Guid, IReadOnlyList<PlanSetSheetDto>>
            {
                [planSetVersionId] = [electricalSheet]
            }),
            new FakePlanSetVersionRepository(
                new PlanSetVersion(
                    planSetVersionId,
                    templateId,
                    canonicalFloorPlanVersionId,
                    versionNumber: 1,
                    createdAtUtc: importedAtUtc)),
            housePlanSetRepository: null,
            sheetRegistrationRepository: null,
            sheetAdjustmentProjectionRepository: new FakeSheetAdjustmentProjectionRepository([projection]));

        var result = await handler.HandleAsync(CancellationToken.None);

        var planSet = Assert.Single(result);
        var sheet = Assert.Single(planSet.Sheets.Where(item => item.SheetId == sheetId));
        Assert.Equal("RequiresManualConfirmation", sheet.ProjectionStatus);
        Assert.Equal(projection.Id, sheet.SheetProjectionId);
        Assert.Equal("ElectricalWholeSheetSimilarity", sheet.ProjectionMethod);
        Assert.Equal(0.25m, sheet.ProjectionConfidence);
        Assert.Equal("Low confidence projection requires review.", sheet.ProjectionWarning);
        Assert.Contains("ElectricalWholeSheetSimilarity", sheet.ProjectionQualityLabel, StringComparison.Ordinal);
        Assert.Contains("0.25", sheet.ProjectionQualityLabel, StringComparison.Ordinal);
        Assert.True(sheet.CanConfirmProjection);
    }

    private sealed class FakeFloorPlanLibraryReader : IFloorPlanLibraryReader
    {
        private readonly IReadOnlyList<FloorPlanLibraryItemDto> items;

        public FakeFloorPlanLibraryReader(IReadOnlyList<FloorPlanLibraryItemDto> items)
        {
            this.items = items;
        }

        public Task<IReadOnlyList<FloorPlanLibraryItemDto>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(items);
        }
    }

    private sealed class FakePlanSheetReader : IPlanSheetReader
    {
        private readonly IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> sheets;

        public FakePlanSheetReader(IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>> sheets)
        {
            this.sheets = sheets;
        }

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<PlanSetSheetDto>>> ListByPlanSetVersionIdsAsync(
            IReadOnlyCollection<Guid> planSetVersionIds,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(sheets);
        }
    }

    private sealed class FakePlanSetVersionRepository : IPlanSetVersionRepository
    {
        private readonly IReadOnlyList<PlanSetVersion> versions;

        public FakePlanSetVersionRepository(params PlanSetVersion[] versions)
        {
            this.versions = versions;
        }

        public Task AddAsync(PlanSetVersion version, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<PlanSetVersion?> GetByCanonicalFloorPlanVersionAsync(
            Guid canonicalFloorPlanVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult(versions.FirstOrDefault(
                item => item.CanonicalFloorPlanVersionId == canonicalFloorPlanVersionId));
    }

    private sealed class FakeHousePlanSetRepository : IHousePlanSetRepository
    {
        private readonly IReadOnlyList<HousePlanSet> sets;

        public FakeHousePlanSetRepository(params HousePlanSet[] sets)
        {
            this.sets = sets;
        }

        public Task AddAsync(HousePlanSet housePlanSet, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<HousePlanSet?> GetBySourceFloorPlanTemplateAsync(
            Guid sourceFloorPlanTemplateId,
            CancellationToken cancellationToken)
            => Task.FromResult(sets.FirstOrDefault(
                item => item.SourceFloorPlanTemplateId == sourceFloorPlanTemplateId));
    }

    private sealed class FakeSheetRegistrationRepository : ISheetRegistrationRepository
    {
        private readonly IReadOnlyList<SheetRegistration> registrations;

        public FakeSheetRegistrationRepository(IReadOnlyList<SheetRegistration> registrations)
        {
            this.registrations = registrations;
        }

        public Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SheetRegistration>> ListByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetRegistration>>(
                registrations.Where(item => item.PlanSetVersionId == planSetVersionId).ToArray());
    }

    private sealed class FakeSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        private readonly IReadOnlyList<SheetAdjustmentProjection> projections;

        public FakeSheetAdjustmentProjectionRepository(IReadOnlyList<SheetAdjustmentProjection> projections)
        {
            this.projections = projections;
        }

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAsync(
            Guid planSetVersionId,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SheetAdjustmentProjection>>(
                projections.Where(item => item.PlanSetVersionId == planSetVersionId).ToArray());
    }
}
