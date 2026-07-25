using System.Text.Json;
using System.Security.Cryptography;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.Export;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Tests.PlanSets.Export;

public sealed class ExportProjectedPlanSheetHandlerTests
{
    [Fact]
    public async Task HandleAsync_builds_electrical_composition_recipe_without_compression()
    {
        using var source = new TemporarySource();
        var projection = CreateProjection(SheetAdjustmentProjectionStatus.ReadyForExport);
        var registration = CreateRegistration(projection, source.Sha256);
        var canonicalExportPath = @"C:\exports\plan-set\floor-adjusted.dxf";
        var canonicalAdjustment = new CanonicalFloorPlanAdjustment(
            projection.CanonicalAdjustmentId,
            projection.PlanSetVersionId,
            registration.CanonicalFloorPlanVersionId,
            "site.dxf",
            canonicalExportPath,
            "{}",
            JsonSerializer.Serialize(new AdjustmentRecipeSummaryDto("v1", 1m, 0m, 0m, [])),
            new DateTime(2026, 7, 20, 1, 0, 0, DateTimeKind.Utc));
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            new FakeSheetAdjustmentProjectionRepository(projection),
            exporter,
            new FakeSheetRegistrationRepository(registration),
            new FakeCanonicalFloorPlanAdjustmentRepository(canonicalAdjustment));

        var response = await handler.HandleAsync(
            new ExportProjectedPlanSheetRequest(
                projection.Id,
                source.Path,
                @"C:\exports\plan-set\electrical-adjusted.dxf"),
            CancellationToken.None);

        Assert.Equal(projection.Id, response.ProjectionId);
        Assert.Equal(@"C:\exports\plan-set\electrical-adjusted.dxf", response.OutputFilePath);
        var call = Assert.Single(exporter.Calls);
        Assert.Equal(source.Path, call.SourceFilePath);
        Assert.Equal(@"C:\exports\plan-set\electrical-adjusted.dxf", call.OutputFilePath);
        Assert.Same(projection.Transform, call.Transform);
        Assert.NotNull(call.Recipe);
        Assert.Empty(call.Recipe.CanonicalRecipe.Operations);
        Assert.Equal(canonicalExportPath, call.Recipe.CanonicalFloorPlanExportPath);
    }

    [Fact]
    public async Task HandleAsync_passes_canonical_recipe_for_ready_electrical_projection_with_compression()
    {
        using var source = new TemporarySource();
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.ReadyForExport,
            canonicalCompressionStepCount: 1);
        var registrationCanonicalFloorPlanVersionId = Guid.NewGuid();
        var registration = new SheetRegistration(
            projection.SheetRegistrationId,
            projection.PlanSetVersionId,
            projection.DependentSheetId,
            registrationCanonicalFloorPlanVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(2m, 0m, 10m, 20m),
            confidence: 0.9m,
            SheetRegistrationStatus.Confirmed,
            new DateTime(2026, 7, 4, 1, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 4, 1, 5, 0, DateTimeKind.Utc),
            warning: null,
            wholePlanRegistrationProof: new WholePlanRegistrationProof(
                WholePlanRegistrationProof.CurrentVersion,
                Passed: true,
                registrationCanonicalFloorPlanVersionId,
                projection.DependentSheetId,
                new string('a', 64),
                source.Sha256,
                HorizontalCoverage: 0.94m,
                VerticalCoverage: 0.91m,
                RootMeanSquareResidual: 0.01m,
                MaximumResidual: 0.02m));
        var recipe = new AdjustmentRecipeSummaryDto(
            "v1",
            FloorToSiteScale: 3m,
            SiteOffsetX: 100m,
            SiteOffsetY: 200m,
            Operations:
            [
                new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
            ]);
        var canonicalAdjustment = new CanonicalFloorPlanAdjustment(
            projection.CanonicalAdjustmentId,
            projection.PlanSetVersionId,
            registration.CanonicalFloorPlanVersionId,
            "site.dxf",
            "floor.dxf",
            "{}",
            JsonSerializer.Serialize(recipe),
            new DateTime(2026, 7, 4, 2, 0, 0, DateTimeKind.Utc));
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            new FakeSheetAdjustmentProjectionRepository(projection),
            exporter,
            new FakeSheetRegistrationRepository(registration),
            new FakeCanonicalFloorPlanAdjustmentRepository(canonicalAdjustment));

        await handler.HandleAsync(
            new ExportProjectedPlanSheetRequest(
                projection.Id,
                source.Path,
                @"C:\exports\plan-set\electrical-adjusted.dxf"),
            CancellationToken.None);

        var call = Assert.Single(exporter.Calls);
        Assert.NotNull(call.Recipe);
        Assert.Same(registration.Transform, call.Recipe.RegistrationTransform);
        Assert.Equal(SheetRegistrationStatus.Confirmed, call.Recipe.RegistrationStatus);
        Assert.Same(registration.WholePlanRegistrationProof, call.Recipe.WholePlanRegistrationProof);
        Assert.Equal(3m, call.Recipe.CanonicalRecipe.FloorToSiteScale);
        Assert.Single(call.Recipe.CanonicalRecipe.Operations);
        Assert.Equal("floor.dxf", call.Recipe.CanonicalFloorPlanExportPath);
    }

    [Fact]
    public async Task HandleAsync_passes_v2_stretch_recipe_and_marks_it_applied_after_export()
    {
        using var source = new TemporarySource();
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.ReadyForExport,
            canonicalCompressionStepCount: 1,
            recipeHandlingSummary: "ElectricalPlan: affine placement applied; recipe-aware DXF export will apply CAD stretch actions: paired-wall.");
        var registration = CreateRegistration(projection, source.Sha256);
        var recipe = CreateV2Recipe();
        var canonicalAdjustment = new CanonicalFloorPlanAdjustment(
            projection.CanonicalAdjustmentId,
            projection.PlanSetVersionId,
            registration.CanonicalFloorPlanVersionId,
            "site.dxf",
            "floor.dxf",
            "{}",
            JsonSerializer.Serialize(recipe),
            new DateTime(2026, 7, 19, 2, 0, 0, DateTimeKind.Utc));
        var repository = new FakeSheetAdjustmentProjectionRepository(projection);
        var unitOfWork = new CapturingUnitOfWork();
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            repository,
            exporter,
            new FakeSheetRegistrationRepository(registration),
            new FakeCanonicalFloorPlanAdjustmentRepository(canonicalAdjustment),
            unitOfWork);

        await handler.HandleAsync(
            new ExportProjectedPlanSheetRequest(
                projection.Id,
                source.Path,
                @"C:\exports\plan-set\electrical-adjusted.dxf"),
            CancellationToken.None);

        var call = Assert.Single(exporter.Calls);
        Assert.NotNull(call.Recipe);
        Assert.Empty(call.Recipe.CanonicalRecipe.Operations);
        Assert.Single(call.Recipe.CanonicalRecipe.StretchActions);
        Assert.True(unitOfWork.Saved);
        Assert.NotNull(repository.Updated);
        Assert.Contains("applied CAD stretch actions", repository.Updated!.RecipeHandlingSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("will apply CAD stretch actions", repository.Updated.RecipeHandlingSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_rewrites_stale_recipe_review_summary_after_successful_recipe_export()
    {
        using var source = new TemporarySource();
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.ReadyForExport,
            canonicalCompressionStepCount: 1,
            recipeHandlingSummary: "ElectricalPlan: affine placement applied; local recipe requires review before DXF deformation: HorizontalCompression Right @50 delta 2.");
        var repository = new FakeSheetAdjustmentProjectionRepository(projection);
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ExportProjectedPlanSheetHandler(
            repository,
            new CapturingProjectedPlanSheetExporter(),
            new FakeSheetRegistrationRepository(CreateRegistration(projection, source.Sha256)),
            new FakeCanonicalFloorPlanAdjustmentRepository(CreateCanonicalAdjustment(projection)),
            unitOfWork);

        await handler.HandleAsync(
            new ExportProjectedPlanSheetRequest(
                projection.Id,
                source.Path,
                @"C:\exports\plan-set\electrical-adjusted.dxf"),
            CancellationToken.None);

        Assert.True(unitOfWork.Saved);
        Assert.NotNull(repository.Updated);
        Assert.DoesNotContain(
            "requires review before DXF deformation",
            repository.Updated!.RecipeHandlingSummary,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "recipe-aware DXF export applied canonical operations",
            repository.Updated.RecipeHandlingSummary,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_rejects_manual_projection_before_writing_artifact()
    {
        var projection = CreateProjection(SheetAdjustmentProjectionStatus.RequiresManualConfirmation);
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            new FakeSheetAdjustmentProjectionRepository(projection),
            exporter);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(
                new ExportProjectedPlanSheetRequest(
                    projection.Id,
                    @"C:\library\raw-dxf\roof.dxf",
                    @"C:\exports\plan-set\roof-adjusted.dxf"),
                CancellationToken.None));

        Assert.Empty(exporter.Calls);
    }

    [Fact]
    public async Task HandleAsync_marks_projection_manual_when_exporter_requires_manual_review()
    {
        using var source = new TemporarySource();
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.ReadyForExport,
            canonicalCompressionStepCount: 1);
        var exporter = new CapturingProjectedPlanSheetExporter
        {
            ManualReviewError = new ProjectedPlanSheetManualReviewRequiredException(
                "CIRCLE crosses a canonical recipe pinch line.")
        };
        var repository = new FakeSheetAdjustmentProjectionRepository(projection);
        var unitOfWork = new CapturingUnitOfWork();
        var handler = new ExportProjectedPlanSheetHandler(
            repository,
            exporter,
            new FakeSheetRegistrationRepository(CreateRegistration(projection, source.Sha256)),
            new FakeCanonicalFloorPlanAdjustmentRepository(CreateCanonicalAdjustment(projection)),
            unitOfWork);

        var error = await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() =>
            handler.HandleAsync(
                new ExportProjectedPlanSheetRequest(
                    projection.Id,
                    source.Path,
                    @"C:\exports\plan-set\electrical-adjusted.dxf"),
                CancellationToken.None));

        Assert.Contains("pinch line", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(unitOfWork.Saved);
        Assert.NotNull(repository.Updated);
        Assert.Equal(SheetAdjustmentProjectionStatus.RequiresManualConfirmation, repository.Updated!.Status);
        Assert.Contains("pinch line", repository.Updated.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("manual review required", repository.Updated.RecipeHandlingSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_marks_projection_manual_when_registration_identity_chain_mismatches()
    {
        using var source = new TemporarySource();
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.ReadyForExport,
            canonicalCompressionStepCount: 1);
        var registration = CreateRegistration(projection, source.Sha256, dependentSheetId: Guid.NewGuid());
        var repository = new FakeSheetAdjustmentProjectionRepository(projection);
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            repository,
            exporter,
            new FakeSheetRegistrationRepository(registration),
            new FakeCanonicalFloorPlanAdjustmentRepository(CreateCanonicalAdjustment(projection)),
            new CapturingUnitOfWork());

        await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() => handler.HandleAsync(
            new ExportProjectedPlanSheetRequest(
                projection.Id,
                source.Path,
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf")),
            CancellationToken.None));

        Assert.Empty(exporter.Calls);
        Assert.Equal(SheetAdjustmentProjectionStatus.RequiresManualConfirmation, repository.Updated!.Status);
    }

    [Fact]
    public async Task HandleAsync_marks_projection_manual_when_dependent_source_hash_mismatches()
    {
        using var source = new TemporarySource();
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.ReadyForExport,
            canonicalCompressionStepCount: 1);
        var repository = new FakeSheetAdjustmentProjectionRepository(projection);
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            repository,
            exporter,
            new FakeSheetRegistrationRepository(CreateRegistration(projection, new string('f', 64))),
            new FakeCanonicalFloorPlanAdjustmentRepository(CreateCanonicalAdjustment(projection)),
            new CapturingUnitOfWork());

        await Assert.ThrowsAsync<ProjectedPlanSheetManualReviewRequiredException>(() => handler.HandleAsync(
            new ExportProjectedPlanSheetRequest(
                projection.Id,
                source.Path,
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf")),
            CancellationToken.None));

        Assert.Empty(exporter.Calls);
        Assert.Equal(SheetAdjustmentProjectionStatus.RequiresManualConfirmation, repository.Updated!.Status);
    }

    [Theory]
    [InlineData(SheetAdjustmentProjectionMethod.RoofOverhangPreserving)]
    [InlineData(SheetAdjustmentProjectionMethod.FacadeHorizontalPreservingVerticals)]
    public async Task HandleAsync_rejects_legacy_ready_compressed_affine_only_projection_before_writing_artifact(
        SheetAdjustmentProjectionMethod method)
    {
        var projection = CreateProjection(
            SheetAdjustmentProjectionStatus.ReadyForExport,
            canonicalCompressionStepCount: 1,
            method: method);
        var exporter = new CapturingProjectedPlanSheetExporter();
        var handler = new ExportProjectedPlanSheetHandler(
            new FakeSheetAdjustmentProjectionRepository(projection),
            exporter);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(
            new ExportProjectedPlanSheetRequest(
                projection.Id,
                @"C:\library\raw-dxf\dependent.dxf",
                @"C:\exports\plan-set\dependent-adjusted.dxf"),
            CancellationToken.None));

        Assert.Contains("canonical compression", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(exporter.Calls);
    }

    private static SheetAdjustmentProjection CreateProjection(
        SheetAdjustmentProjectionStatus status,
        int canonicalCompressionStepCount = 0,
        string? recipeHandlingSummary = null,
        SheetAdjustmentProjectionMethod method = SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity)
    {
        return new SheetAdjustmentProjection(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            method,
            new SheetAdjustmentProjectionTransform(
                scale: 1.15m,
                rotationDegrees: 0m,
                translateX: 12m,
                translateY: 24m),
            confidence: status is SheetAdjustmentProjectionStatus.ReadyForExport ? 0.92m : 0.65m,
            status,
            warning: null,
            canonicalCompressionStepCount,
            createdAtUtc: new DateTime(2026, 6, 30, 23, 59, 0, DateTimeKind.Utc),
            recipeHandlingSummary: recipeHandlingSummary ?? (canonicalCompressionStepCount > 0
                ? "ElectricalPlan: local recipe manually confirmed; recipe-aware DXF export will apply canonical operations."
                : null));
    }

    private static SheetRegistration CreateRegistration(
        SheetAdjustmentProjection projection,
        string dependentSourceSha256,
        Guid? dependentSheetId = null)
    {
        var resolvedDependentSheetId = dependentSheetId ?? projection.DependentSheetId;
        var canonicalFloorPlanVersionId = projection.PlanSetVersionId;
        return new(
            projection.SheetRegistrationId,
            projection.PlanSetVersionId,
            resolvedDependentSheetId,
            canonicalFloorPlanVersionId,
            SheetRegistrationMethod.WholeSheetSimilarity,
            new SheetRegistrationTransform(1m, 0m, 0m, 0m),
            0.9m,
            SheetRegistrationStatus.Confirmed,
            new DateTime(2026, 7, 4, 1, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 4, 1, 5, 0, DateTimeKind.Utc),
            warning: null,
            wholePlanRegistrationProof: new WholePlanRegistrationProof(
                WholePlanRegistrationProof.CurrentVersion,
                Passed: true,
                canonicalFloorPlanVersionId,
                resolvedDependentSheetId,
                new string('a', 64),
                dependentSourceSha256,
                HorizontalCoverage: 0.94m,
                VerticalCoverage: 0.91m,
                RootMeanSquareResidual: 0.01m,
                MaximumResidual: 0.02m));
    }

    private static CanonicalFloorPlanAdjustment CreateCanonicalAdjustment(SheetAdjustmentProjection projection)
    {
        var recipe = new AdjustmentRecipeSummaryDto(
            "v1",
            FloorToSiteScale: 1m,
            SiteOffsetX: 0m,
            SiteOffsetY: 0m,
            Operations:
            [
                new AdjustmentRecipeOperationDto("HorizontalCompression", "Width", "Right", 50m, 2m)
            ]);

        return new CanonicalFloorPlanAdjustment(
            projection.CanonicalAdjustmentId,
            projection.PlanSetVersionId,
            projection.PlanSetVersionId,
            "site.dxf",
            "floor.dxf",
            "{}",
            JsonSerializer.Serialize(recipe),
            new DateTime(2026, 7, 4, 2, 0, 0, DateTimeKind.Utc));
    }

    private static AdjustmentRecipeSummaryDto CreateV2Recipe()
        => new("v2", 1m, 0m, 0m, [])
        {
            StretchActions =
            [
                new AdjustmentRecipeStretchActionDto(
                    "paired-wall",
                    "Width",
                    "Right",
                    5m,
                    2m,
                    4m,
                    0.05m,
                    new AdjustmentRecipeBoundsDto(0m, 0m, 12m, 4m),
                    [
                        new AdjustmentRecipeTargetSpanDto("FLOOR:1", Guid.NewGuid(), 0, 0m, 0m, 10m, 0m, 1),
                        new AdjustmentRecipeTargetSpanDto("FLOOR:2", Guid.NewGuid(), 0, 0m, 4m, 10m, 4m, 1)
                    ],
                    [])
            ]
        };

    private sealed class FakeSheetAdjustmentProjectionRepository : ISheetAdjustmentProjectionRepository
    {
        private readonly SheetAdjustmentProjection projection;

        public FakeSheetAdjustmentProjectionRepository(SheetAdjustmentProjection projection)
        {
            this.projection = projection;
        }

        public SheetAdjustmentProjection? Updated { get; private set; }

        public Task AddAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<SheetAdjustmentProjection?> GetByIdAsync(Guid projectionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<SheetAdjustmentProjection?>(projection.Id == projectionId ? projection : null);
        }

        public Task<IReadOnlyList<SheetAdjustmentProjection>> ListByPlanSetVersionAndCanonicalAdjustmentAsync(
            Guid planSetVersionId,
            Guid canonicalAdjustmentId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(SheetAdjustmentProjection projection, CancellationToken cancellationToken)
        {
            Updated = projection;
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingProjectedPlanSheetExporter : IProjectedPlanSheetExporter
    {
        public List<Call> Calls { get; } = [];

        public ProjectedPlanSheetManualReviewRequiredException? ManualReviewError { get; init; }

        public Task ExportAsync(
            string sourceFilePath,
            string outputFilePath,
            SheetAdjustmentProjectionTransform transform,
            CancellationToken cancellationToken)
            => ExportAsync(sourceFilePath, outputFilePath, transform, recipe: null, cancellationToken);

        public Task ExportAsync(
            string sourceFilePath,
            string outputFilePath,
            SheetAdjustmentProjectionTransform transform,
            ProjectedPlanSheetExportRecipe? recipe,
            CancellationToken cancellationToken)
        {
            if (ManualReviewError is not null)
            {
                throw ManualReviewError;
            }

            Calls.Add(new Call(sourceFilePath, outputFilePath, transform, recipe));
            return Task.CompletedTask;
        }

        public sealed record Call(
            string SourceFilePath,
            string OutputFilePath,
            SheetAdjustmentProjectionTransform Transform,
            ProjectedPlanSheetExportRecipe? Recipe);
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
            return Task.FromResult<SheetRegistration?>(registration.Id == registrationId ? registration : null);
        }
    }

    private sealed class FakeCanonicalFloorPlanAdjustmentRepository : ICanonicalFloorPlanAdjustmentRepository
    {
        private readonly CanonicalFloorPlanAdjustment adjustment;

        public FakeCanonicalFloorPlanAdjustmentRepository(CanonicalFloorPlanAdjustment adjustment)
        {
            this.adjustment = adjustment;
        }

        public Task AddAsync(CanonicalFloorPlanAdjustment adjustment, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CanonicalFloorPlanAdjustment?> GetByIdAsync(Guid adjustmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<CanonicalFloorPlanAdjustment?>(adjustment.Id == adjustmentId ? adjustment : null);
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

    private sealed class TemporarySource : IDisposable
    {
        public TemporarySource()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.dxf");
            File.WriteAllText(Path, "generic dependent source");
            Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path))).ToLowerInvariant();
        }

        public string Path { get; }

        public string Sha256 { get; }

        public void Dispose() => File.Delete(Path);
    }
}
