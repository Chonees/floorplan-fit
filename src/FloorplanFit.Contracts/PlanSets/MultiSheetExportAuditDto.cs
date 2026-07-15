using System.Text.Json.Serialization;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record MultiSheetExportAuditDto(
    Guid ExportId,
    Guid PlanSetVersionId,
    Guid CanonicalAdjustmentId,
    string Status,
    ProjectionAuditSummaryDto Summary,
    IReadOnlyList<ExportedPlanSheetDto> Sheets,
    string? PackageManifestPath,
    DateTime CreatedAtUtc,
    PlanSetQualityReportDto? QualityReport = null,
    AdjustedSitePlanPlacementDto? CanonicalPlacement = null,
    AdjustmentRecipeSummaryDto? CanonicalRecipe = null)
{
    public const int CurrentSchemaVersion = 2;

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public PlanSetVerificationReportDto? Verification { get; init; }

    public IReadOnlyList<string> HumanSummary { get; init; } = [];

    public IReadOnlyList<PlanSetPackageArtifactDto> Artifacts { get; init; } = [];
}
