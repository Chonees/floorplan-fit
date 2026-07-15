using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.PlanSets.ExportAudit;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Export;

public sealed class ExportMultiSheetPlanSetPackageHandler
{
    private readonly ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository;
    private readonly IPlanSheetSourceReader planSheetSourceReader;
    private readonly ExportProjectedPlanSheetHandler projectedPlanSheetHandler;
    private readonly CreateMultiSheetExportAuditHandler exportAuditHandler;
    public ExportMultiSheetPlanSetPackageHandler(
        ISheetAdjustmentProjectionRepository sheetAdjustmentProjectionRepository,
        IPlanSheetSourceReader planSheetSourceReader,
        ExportProjectedPlanSheetHandler projectedPlanSheetHandler,
        CreateMultiSheetExportAuditHandler exportAuditHandler)
    {
        this.sheetAdjustmentProjectionRepository = sheetAdjustmentProjectionRepository;
        this.planSheetSourceReader = planSheetSourceReader;
        this.projectedPlanSheetHandler = projectedPlanSheetHandler;
        this.exportAuditHandler = exportAuditHandler;
    }

    public async Task<MultiSheetExportAuditDto> HandleAsync(
        ExportMultiSheetPlanSetPackageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.DependentProjectionIds);

        if (string.IsNullOrWhiteSpace(request.PackageDirectory))
        {
            throw new ArgumentException("Package directory is required.", nameof(request));
        }

        var exportableProjections = await ResolveExportableProjectionsAsync(request, cancellationToken);
        var exportedProjectionRequests = new List<MultiSheetExportProjectionRequestDto>(exportableProjections.Count);
        var packageArtifacts = new List<PlanSetPackageArtifactDto>();
        var failureStage = PlanSetExportFailureStage.UserPackagePublication;
        var auditStarted = false;
        string? stagedFloorPath = null;
        string? finalFloorPath = null;

        try
        {
            var response = await AtomicDirectoryPublisher.PublishAsync(
                request.PackageDirectory,
                async (stagingDirectory, stagingCancellationToken) =>
                {
                    finalFloorPath = BuildCanonicalFloorPlanPath(
                        request.PackageDirectory,
                        request.CanonicalFloorPlanExportPath);
                    var packageStem = Path.GetFileNameWithoutExtension(finalFloorPath)[..^"-floorplan".Length];
                    stagedFloorPath = Path.Combine(stagingDirectory, Path.GetFileName(finalFloorPath));
                    File.Copy(request.CanonicalFloorPlanExportPath, stagedFloorPath, overwrite: false);
                    packageArtifacts.Add(new PlanSetPackageArtifactDto("FloorPlan", finalFloorPath));

                    failureStage = PlanSetExportFailureStage.DependentSheetGeneration;
                    var usedOutputNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var projection in exportableProjections)
                    {
                        if (projection.Status is not SheetAdjustmentProjectionStatus.ReadyForExport)
                        {
                            exportedProjectionRequests.Add(new MultiSheetExportProjectionRequestDto(projection.Id));
                            continue;
                        }

                        var sheetSource = await planSheetSourceReader.GetBySheetIdAsync(
                            projection.DependentSheetId,
                            stagingCancellationToken);
                        if (sheetSource is null)
                        {
                            throw new InvalidOperationException("Dependent sheet source was not found.");
                        }

                        try
                        {
                            var fileName = BuildOutputFileName(packageStem, sheetSource.SheetType, usedOutputNames);
                            var stagedOutputPath = Path.Combine(stagingDirectory, fileName);
                            var finalOutputPath = Path.Combine(request.PackageDirectory, fileName);
                            var exported = await projectedPlanSheetHandler.HandleAsync(
                                new ExportProjectedPlanSheetRequest(
                                    projection.Id,
                                    sheetSource.SourceFilePath,
                                    stagedOutputPath),
                                stagingCancellationToken);
                            exportedProjectionRequests.Add(new MultiSheetExportProjectionRequestDto(
                                exported.ProjectionId,
                                finalOutputPath,
                                exported.ExportAudit)
                            {
                                VerificationPath = stagedOutputPath
                            });
                            packageArtifacts.Add(new PlanSetPackageArtifactDto(sheetSource.SheetType, finalOutputPath));
                        }
                        catch (ProjectedPlanSheetManualReviewRequiredException)
                        {
                            exportedProjectionRequests.Add(new MultiSheetExportProjectionRequestDto(projection.Id));
                        }
                    }
                },
                async (publishPackageAsync, publicationCancellationToken) =>
                {
                    failureStage = PlanSetExportFailureStage.VerificationAndWorkspacePublication;
                    auditStarted = true;
                    var canonicalStoragePath = finalFloorPath
                        ?? throw new InvalidOperationException("Canonical floor-plan staging did not complete.");
                    var canonicalVerificationPath = stagedFloorPath
                        ?? throw new InvalidOperationException("Canonical floor-plan staging did not complete.");
                    return await exportAuditHandler.HandleAsync(
                        new CreateMultiSheetExportAuditRequest(
                            request.PlanSetVersionId,
                            request.CanonicalFloorPlanVersionId,
                            request.CanonicalAdjustmentId,
                            canonicalStoragePath,
                            exportedProjectionRequests)
                        {
                            DiscoverAllDependentSheets = request.DependentProjectionIds.Count == 0,
                            CanonicalPlacement = request.CanonicalPlacement,
                            CanonicalRecipe = request.CanonicalRecipe,
                            CanonicalFloorPlanVerificationPath = canonicalVerificationPath,
                            PackageArtifacts = packageArtifacts
                        },
                        publishPackageAsync,
                        publicationCancellationToken);
                },
                cancellationToken);
            if (request.DeleteCanonicalSourceAfterSuccess)
            {
                File.Delete(request.CanonicalFloorPlanExportPath);
            }

            return response;
        }
        catch (Exception exception)
        {
            if (!auditStarted)
            {
                try
                {
                    await exportAuditHandler.TryRecordFailureAsync(
                        request.PlanSetVersionId,
                        request.CanonicalFloorPlanVersionId,
                        request.CanonicalAdjustmentId,
                        request.CanonicalFloorPlanExportPath,
                        failureStage,
                        exception);
                }
                catch (Exception persistenceException)
                {
                    exception.Data["FailurePersistenceError"] = persistenceException.Message;
                }
            }

            throw;
        }
    }

    private async Task<IReadOnlyList<SheetAdjustmentProjection>> ResolveExportableProjectionsAsync(
        ExportMultiSheetPlanSetPackageRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DependentProjectionIds.Count == 0)
        {
            var projections = await sheetAdjustmentProjectionRepository.ListByPlanSetVersionAndCanonicalAdjustmentAsync(
                request.PlanSetVersionId,
                request.CanonicalAdjustmentId,
                cancellationToken);
            return projections
                .GroupBy(projection => projection.DependentSheetId)
                .Select(group => group
                    .OrderBy(projection => projection.CreatedAtUtc)
                    .ThenBy(projection => projection.Id)
                    .Last())
                .Where(projection => projection.Status is SheetAdjustmentProjectionStatus.ReadyForExport)
                .OrderBy(projection => projection.Id)
                .ToArray();
        }

        var selected = new List<SheetAdjustmentProjection>(request.DependentProjectionIds.Count);
        foreach (var projectionId in request.DependentProjectionIds)
        {
            var projection = await sheetAdjustmentProjectionRepository.GetByIdAsync(projectionId, cancellationToken);
            if (projection is null)
            {
                throw new InvalidOperationException("Sheet adjustment projection was not found.");
            }

            if (projection.PlanSetVersionId != request.PlanSetVersionId)
            {
                throw new ArgumentException(
                    "Sheet adjustment projection does not belong to the requested plan-set version.",
                    nameof(request));
            }

            if (projection.CanonicalAdjustmentId != request.CanonicalAdjustmentId)
            {
                throw new ArgumentException(
                    "Sheet adjustment projection does not belong to the requested canonical adjustment.",
                    nameof(request));
            }

            selected.Add(projection);
        }

        return selected;
    }

    private static string BuildOutputFileName(
        string packageStem,
        string sheetType,
        ISet<string> usedNames)
    {
        var role = SanitizeFileStem(sheetType
            .Replace("Plan", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Sheet", "", StringComparison.OrdinalIgnoreCase));
        var baseName = $"{packageStem}-{role.ToLowerInvariant()}";
        var candidate = $"{baseName}.dxf";
        for (var suffix = 2; !usedNames.Add(candidate); suffix++)
        {
            candidate = $"{baseName}-{suffix}.dxf";
        }

        return candidate;
    }

    public static string BuildCanonicalFloorPlanPath(
        string packageDirectory,
        string canonicalFloorPlanSourcePath)
    {
        var packageStem = SanitizeFileStem(Path.GetFileNameWithoutExtension(canonicalFloorPlanSourcePath));
        const string floorPlanSuffix = "-floorplan";
        if (packageStem.EndsWith(floorPlanSuffix, StringComparison.OrdinalIgnoreCase))
        {
            packageStem = packageStem[..^floorPlanSuffix.Length];
        }

        return Path.Combine(packageDirectory, $"{packageStem}-floorplan.dxf");
    }

    private static string SanitizeFileStem(string? value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "plan" : value.Trim();
        var characters = new List<char>(source.Length);
        var pendingSeparator = false;
        foreach (var character in source)
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && characters.Count > 0)
                {
                    characters.Add('-');
                }

                characters.Add(character);
                pendingSeparator = false;
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                pendingSeparator = characters.Count > 0;
            }
        }

        return characters.Count == 0 ? "plan" : new string(characters.ToArray());
    }
}
