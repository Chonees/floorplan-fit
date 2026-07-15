using System.Text.Json.Serialization;

namespace FloorplanFit.Contracts.PlanSets;

public sealed record MultiSheetExportProjectionRequestDto(
    Guid ProjectionId,
    string? ExportPath = null,
    ProjectedPlanSheetExportAuditDto? ExportAudit = null)
{
    [JsonIgnore]
    public string? VerificationPath { get; init; }
}
