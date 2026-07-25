using System.Text.Json;
using System.Security.Cryptography;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Export;

public sealed class ExportProjectedPlanSheetHandler
{
    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly IProjectedPlanSheetExporter projectedPlanSheetExporter;
    private readonly ISheetRegistrationRepository? sheetRegistrationRepository;
    private readonly ICanonicalFloorPlanAdjustmentRepository? canonicalFloorPlanAdjustmentRepository;
    private readonly IUnitOfWork? unitOfWork;

    public ExportProjectedPlanSheetHandler(
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        IProjectedPlanSheetExporter projectedPlanSheetExporter,
        ISheetRegistrationRepository? sheetRegistrationRepository = null,
        ICanonicalFloorPlanAdjustmentRepository? canonicalFloorPlanAdjustmentRepository = null,
        IUnitOfWork? unitOfWork = null)
    {
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.projectedPlanSheetExporter = projectedPlanSheetExporter;
        this.sheetRegistrationRepository = sheetRegistrationRepository;
        this.canonicalFloorPlanAdjustmentRepository = canonicalFloorPlanAdjustmentRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<ExportProjectedPlanSheetResponse> HandleAsync(
        ExportProjectedPlanSheetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ProjectionId == Guid.Empty)
        {
            throw new ArgumentException("Projection id is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.SourceFilePath))
        {
            throw new ArgumentException("Source sheet path is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OutputFilePath))
        {
            throw new ArgumentException("Output sheet path is required.", nameof(request));
        }

        var projection = await sheetAdjustmentProjectionRepository.GetByIdAsync(
            request.ProjectionId,
            cancellationToken);
        if (projection is null)
        {
            throw new InvalidOperationException("Sheet adjustment projection was not found.");
        }

        SheetAdjustmentProjectionCapabilities.EnsureSupported(
            projection.Method,
            projection.CanonicalCompressionStepCount);

        if (projection.Status is not SheetAdjustmentProjectionStatus.ReadyForExport)
        {
            throw new InvalidOperationException("Sheet projection requires manual confirmation before export.");
        }

        ProjectedPlanSheetExportRecipe? recipe = null;
        ProjectedPlanSheetExportAuditDto? exportAudit = null;
        try
        {
            recipe = await BuildExportRecipeAsync(
                projection,
                request.SourceFilePath,
                cancellationToken);
            exportAudit = await projectedPlanSheetExporter.ExportWithAuditAsync(
                request.SourceFilePath,
                request.OutputFilePath,
                projection.Transform,
                recipe,
                cancellationToken);
        }
        catch (ProjectedPlanSheetManualReviewRequiredException exception)
        {
            await MarkRequiresManualConfirmationAsync(projection, exception.Message, cancellationToken);
            throw;
        }

        await MarkRecipeAwareExportAppliedAsync(projection, recipe, cancellationToken);

        return new ExportProjectedPlanSheetResponse(projection.Id, request.OutputFilePath, exportAudit);
    }

    private async Task MarkRecipeAwareExportAppliedAsync(
        SheetAdjustmentProjection projection,
        ProjectedPlanSheetExportRecipe? recipe,
        CancellationToken cancellationToken)
    {
        if (recipe is null ||
            projection.CanonicalCompressionStepCount == 0 ||
            string.IsNullOrWhiteSpace(projection.RecipeHandlingSummary))
        {
            return;
        }

        var appliedSummary = projection.RecipeHandlingSummary
            .Replace(
                "local recipe requires review before DXF deformation",
                "recipe-aware DXF export applied canonical operations",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "recipe-aware DXF export will apply canonical operations",
                "recipe-aware DXF export applied canonical operations",
                StringComparison.OrdinalIgnoreCase)
            .Replace(
                "recipe-aware DXF export will apply CAD stretch actions",
                "recipe-aware DXF export applied CAD stretch actions",
                StringComparison.OrdinalIgnoreCase);
        if (string.Equals(appliedSummary, projection.RecipeHandlingSummary, StringComparison.Ordinal))
        {
            return;
        }

        var updatedProjection = new SheetAdjustmentProjection(
            projection.Id,
            projection.PlanSetVersionId,
            projection.DependentSheetId,
            projection.SheetRegistrationId,
            projection.CanonicalAdjustmentId,
            projection.Method,
            projection.Transform,
            projection.Confidence,
            projection.Status,
            projection.Warning,
            projection.CanonicalCompressionStepCount,
            projection.CreatedAtUtc,
            projection.RuleSummary,
            appliedSummary);

        await sheetAdjustmentProjectionRepository.UpdateAsync(updatedProjection, cancellationToken);
        if (unitOfWork is not null)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task MarkRequiresManualConfirmationAsync(
        SheetAdjustmentProjection projection,
        string warning,
        CancellationToken cancellationToken)
    {
        var manualProjection = new SheetAdjustmentProjection(
            projection.Id,
            projection.PlanSetVersionId,
            projection.DependentSheetId,
            projection.SheetRegistrationId,
            projection.CanonicalAdjustmentId,
            projection.Method,
            projection.Transform,
            projection.Confidence,
            SheetAdjustmentProjectionStatus.RequiresManualConfirmation,
            warning,
            projection.CanonicalCompressionStepCount,
            projection.CreatedAtUtc,
            projection.RuleSummary,
            BuildManualReviewRecipeHandlingSummary(projection.RecipeHandlingSummary, warning));

        await sheetAdjustmentProjectionRepository.UpdateAsync(manualProjection, cancellationToken);
        if (unitOfWork is not null)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static string BuildManualReviewRecipeHandlingSummary(string? currentSummary, string warning)
    {
        var prefix = string.IsNullOrWhiteSpace(currentSummary)
            ? "ElectricalPlan"
            : currentSummary.TrimEnd('.');

        return $"{prefix}; manual review required: {warning}";
    }

    private async Task<ProjectedPlanSheetExportRecipe?> BuildExportRecipeAsync(
        SheetAdjustmentProjection projection,
        string sourceFilePath,
        CancellationToken cancellationToken)
    {
        if (projection.Method is not SheetAdjustmentProjectionMethod.ElectricalWholeSheetSimilarity)
        {
            return null;
        }

        if (sheetRegistrationRepository is null || canonicalFloorPlanAdjustmentRepository is null)
        {
            throw new InvalidOperationException("Electrical recipe-aware export requires registration and canonical adjustment repositories.");
        }

        var registration = await sheetRegistrationRepository.GetByIdAsync(
            projection.SheetRegistrationId,
            cancellationToken);
        if (registration is null)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical export registration was not found; manual review is required.");
        }

        if (registration.PlanSetVersionId != projection.PlanSetVersionId ||
            registration.DependentSheetId != projection.DependentSheetId ||
            registration.Id != projection.SheetRegistrationId)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical export projection and registration ownership do not match; manual review is required.");
        }

        var canonicalAdjustment = await canonicalFloorPlanAdjustmentRepository.GetByIdAsync(
            projection.CanonicalAdjustmentId,
            cancellationToken);
        if (canonicalAdjustment is null)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical export canonical adjustment was not found; manual review is required.");
        }

        if (canonicalAdjustment.Id != projection.CanonicalAdjustmentId ||
            canonicalAdjustment.PlanSetVersionId != projection.PlanSetVersionId ||
            canonicalAdjustment.CanonicalFloorPlanVersionId != registration.CanonicalFloorPlanVersionId)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical export projection, registration, and canonical adjustment identities do not match; manual review is required.");
        }

        var proof = registration.WholePlanRegistrationProof;
        if (registration.Status is not SheetRegistrationStatus.Confirmed ||
            proof?.IsAuthoritative != true ||
            proof.CanonicalFloorPlanVersionId != registration.CanonicalFloorPlanVersionId ||
            proof.DependentSheetId != registration.DependentSheetId)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical export requires a confirmed, source-bound whole-plan registration proof; manual review is required.");
        }

        var dependentSourceSha256 = await ComputeSha256Async(sourceFilePath, cancellationToken);
        if (!string.Equals(
                dependentSourceSha256,
                proof.DependentSourceSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                "Electrical source bytes no longer match the confirmed whole-plan registration proof; manual review is required.");
        }

        var recipe = JsonSerializer.Deserialize<AdjustmentRecipeSummaryDto>(
            canonicalAdjustment.AdjustmentRecipeJson);
        var originalDimensions = ReadOriginalInputAuditDimensions(canonicalAdjustment.PlacementJson);
        return recipe is null
            ? throw new InvalidOperationException("Canonical floor-plan adjustment recipe is invalid.")
            : new ProjectedPlanSheetExportRecipe(
                registration.Transform,
                recipe,
                originalDimensions.Width,
                originalDimensions.Height,
                RegistrationStatus: registration.Status,
                WholePlanRegistrationProof: registration.WholePlanRegistrationProof,
                CanonicalFloorPlanExportPath: canonicalAdjustment.CanonicalFloorPlanExportPath);
    }

    private static async Task<string> ComputeSha256Async(
        string sourceFilePath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(sourceFilePath);
            return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken)).ToLowerInvariant();
        }
        catch (Exception exception) when (
            exception is ArgumentException or NotSupportedException or IOException or UnauthorizedAccessException)
        {
            throw new ProjectedPlanSheetManualReviewRequiredException(
                $"Electrical source bytes could not be verified against registration proof: {exception.Message}");
        }
    }

    private static (decimal? Width, decimal? Height) ReadOriginalInputAuditDimensions(string placementJson)
    {
        if (string.IsNullOrWhiteSpace(placementJson))
        {
            return (null, null);
        }

        using var document = JsonDocument.Parse(placementJson);
        if (!TryGetProperty(document.RootElement, "InputAudit", out var inputAudit) &&
            !TryGetProperty(document.RootElement, "inputAudit", out inputAudit))
        {
            return (null, null);
        }

        return (
            TryReadDecimal(inputAudit, "OriginalWidthInches", "originalWidthInches"),
            TryReadDecimal(inputAudit, "OriginalHeightInches", "originalHeightInches"));
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static decimal? TryReadDecimal(JsonElement element, string pascalName, string camelName)
    {
        if (!TryGetProperty(element, pascalName, out var value) &&
            !TryGetProperty(element, camelName, out value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            ? number
            : null;
    }
}
