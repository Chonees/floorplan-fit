using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Domain.PlanSets;
using FloorplanFit.Infrastructure.Persistence;
using FloorplanFit.Infrastructure.Runtime;
using Microsoft.Data.Sqlite;

namespace FloorplanFit.Infrastructure.Tests.PlanSets;

public sealed class SheetConfirmationPersistenceTests
{
    [Fact]
    public async Task SheetRegistration_list_by_plan_set_version_reads_only_that_plan_set()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-sheet-registration-list-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var planSetVersionId = Guid.NewGuid();
            var first = CreateRegistration(
                SheetRegistrationStatus.Confirmed,
                new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
                planSetVersionId: planSetVersionId);
            var second = CreateRegistration(
                SheetRegistrationStatus.PendingConfirmation,
                confirmedAtUtc: null,
                planSetVersionId: planSetVersionId);
            var other = CreateRegistration(
                SheetRegistrationStatus.Confirmed,
                new DateTime(2026, 7, 1, 8, 30, 0, DateTimeKind.Utc),
                planSetVersionId: Guid.NewGuid());

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                await SeedRegistrationOwnershipAsync(session, first, second, other);

                var repository = new SqliteSheetRegistrationRepository(session);
                await repository.AddAsync(first, CancellationToken.None);
                await repository.AddAsync(second, CancellationToken.None);
                await repository.AddAsync(other, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var registrations = await new SqliteSheetRegistrationRepository(readSession).ListByPlanSetVersionAsync(
                planSetVersionId,
                CancellationToken.None);

            Assert.Equal(2, registrations.Count);
            Assert.Contains(registrations, registration => registration.Id == first.Id);
            Assert.Contains(registrations, registration => registration.Id == second.Id);
            Assert.DoesNotContain(registrations, registration => registration.Id == other.Id);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SheetRegistration_update_persists_confirmed_status()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-sheet-registration-confirm-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var registration = CreateRegistration(SheetRegistrationStatus.PendingConfirmation, confirmedAtUtc: null);
            var confirmed = CreateRegistration(
                SheetRegistrationStatus.Confirmed,
                new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
                registration.Id,
                registration.PlanSetVersionId,
                registration.DependentSheetId,
                registration.CanonicalFloorPlanVersionId);

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                await SeedRegistrationOwnershipAsync(session, registration);

                var repository = new SqliteSheetRegistrationRepository(session);
                await repository.AddAsync(registration, CancellationToken.None);
                await repository.UpdateAsync(confirmed, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqliteSheetRegistrationRepository(readSession).GetByIdAsync(registration.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(SheetRegistrationStatus.Confirmed, loaded!.Status);
            Assert.Equal(confirmed.ConfirmedAtUtc, loaded.ConfirmedAtUtc);
            Assert.Equal(registration.Transform.Scale, loaded.Transform.Scale);
            Assert.Equal(registration.WholePlanRegistrationProof, loaded.WholePlanRegistrationProof);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SheetAdjustmentProjection_update_persists_ready_for_export_status()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-sheet-projection-confirm-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var projection = CreateProjection(SheetAdjustmentProjectionStatus.RequiresManualConfirmation);
            var confirmed = CreateProjection(SheetAdjustmentProjectionStatus.ReadyForExport, projection.Id);

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteSheetAdjustmentProjectionRepository(session);
                await repository.AddAsync(projection, CancellationToken.None);
                await repository.UpdateAsync(confirmed, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqliteSheetAdjustmentProjectionRepository(readSession).GetByIdAsync(projection.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(SheetAdjustmentProjectionStatus.ReadyForExport, loaded!.Status);
            Assert.Equal(projection.Transform.Scale, loaded.Transform.Scale);
            Assert.Equal(projection.Warning, loaded.Warning);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SheetAdjustmentProjection_update_persists_manual_warning_and_recipe_summary()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-sheet-projection-manual-{Guid.NewGuid():N}");

        try
        {
            var workspace = new AppWorkspace(tempRoot);
            workspace.EnsureCreated();
            await SqliteSchemaInitializer.InitializeAsync(workspace.DatabasePath, CancellationToken.None);

            var projection = CreateProjection(
                SheetAdjustmentProjectionStatus.ReadyForExport,
                warning: null,
                recipeHandlingSummary: "ElectricalPlan: recipe-aware DXF export will apply canonical operations.");
            var manual = CreateProjection(
                SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
                projection.Id,
                "CIRCLE crosses a canonical recipe pinch line.",
                "ElectricalPlan: manual review required before recipe-aware DXF export.");

            await using (var session = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None))
            {
                var repository = new SqliteSheetAdjustmentProjectionRepository(session);
                await repository.AddAsync(projection, CancellationToken.None);
                await repository.UpdateAsync(manual, CancellationToken.None);
                await new SqliteUnitOfWork(session).SaveChangesAsync(CancellationToken.None);
            }

            await using var readSession = await SqliteSession.OpenAsync(workspace.DatabasePath, CancellationToken.None);
            var loaded = await new SqliteSheetAdjustmentProjectionRepository(readSession).GetByIdAsync(projection.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(SheetAdjustmentProjectionStatus.RequiresManualConfirmation, loaded!.Status);
            Assert.Equal("CIRCLE crosses a canonical recipe pinch line.", loaded.Warning);
            Assert.Equal("ElectricalPlan: manual review required before recipe-aware DXF export.", loaded.RecipeHandlingSummary);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static SheetRegistration CreateRegistration(
        SheetRegistrationStatus status,
        DateTime? confirmedAtUtc,
        Guid? id = null,
        Guid? planSetVersionId = null,
        Guid? dependentSheetId = null,
        Guid? canonicalFloorPlanVersionId = null)
    {
        var resolvedPlanSetVersionId = planSetVersionId ?? Guid.NewGuid();
        var resolvedDependentSheetId = dependentSheetId ?? Guid.NewGuid();
        var resolvedCanonicalFloorPlanVersionId = canonicalFloorPlanVersionId ?? resolvedPlanSetVersionId;
        return new SheetRegistration(
            id ?? Guid.NewGuid(),
            resolvedPlanSetVersionId,
            resolvedDependentSheetId,
            resolvedCanonicalFloorPlanVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1.2m, 0.5m, 10m, -2m),
            0.74m,
            status,
            new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
            confirmedAtUtc,
            "Needs visual review",
            "Whole sheet anchors",
            new WholePlanRegistrationProof(
                WholePlanRegistrationProof.CurrentVersion,
                Passed: true,
                resolvedCanonicalFloorPlanVersionId,
                resolvedDependentSheetId,
                new string('a', 64),
                new string('b', 64),
                HorizontalCoverage: 0.94m,
                VerticalCoverage: 0.91m,
                RootMeanSquareResidual: 0.01m,
                MaximumResidual: 0.02m));
    }

    private static async Task SeedRegistrationOwnershipAsync(
        SqliteSession session,
        params SheetRegistration[] registrations)
    {
        var createdAtUtc = new DateTime(2026, 7, 1, 7, 30, 0, DateTimeKind.Utc);
        var floorPlanVersionRepository = new SqliteFloorPlanVersionRepository(session);
        var planSetVersionRepository = new SqlitePlanSetVersionRepository(session);
        var planSheetRepository = new SqlitePlanSheetRepository(session);

        foreach (var registration in registrations.DistinctBy(item => item.CanonicalFloorPlanVersionId))
        {
            await floorPlanVersionRepository.AddAsync(
                new FloorPlanVersion(
                    registration.CanonicalFloorPlanVersionId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    $"fixture-{registration.CanonicalFloorPlanVersionId:N}",
                    versionNumber: 1,
                    createdAtUtc: createdAtUtc),
                CancellationToken.None);
        }

        foreach (var registration in registrations.DistinctBy(item => item.PlanSetVersionId))
        {
            await planSetVersionRepository.AddAsync(
                new PlanSetVersion(
                    registration.PlanSetVersionId,
                    Guid.NewGuid(),
                    registration.CanonicalFloorPlanVersionId,
                    versionNumber: 1,
                    createdAtUtc: createdAtUtc),
                CancellationToken.None);
        }

        foreach (var registration in registrations.DistinctBy(item => item.DependentSheetId))
        {
            await planSheetRepository.AddAsync(
                new PlanSheet(
                    registration.DependentSheetId,
                    registration.PlanSetVersionId,
                    PlanSheetType.ElectricalPlan,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Fixture electrical plan",
                    PlanSheetStatus.Imported,
                    createdAtUtc),
                CancellationToken.None);
        }
    }

    private static SheetAdjustmentProjection CreateProjection(
        SheetAdjustmentProjectionStatus status,
        Guid? id = null,
        string? warning = "Needs visual review",
        string? recipeHandlingSummary = null)
        => new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity,
            new SheetAdjustmentProjectionTransform(1.1m, 0m, 12m, -3m),
            0.76m,
            status,
            warning,
            1,
            new DateTime(2026, 7, 1, 8, 30, 0, DateTimeKind.Utc),
            "electrical follows floor plan",
            recipeHandlingSummary);
}
