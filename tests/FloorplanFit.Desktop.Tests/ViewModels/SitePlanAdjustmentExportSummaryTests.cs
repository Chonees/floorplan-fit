using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Desktop.ViewModels;
using Xunit;

namespace FloorplanFit.Desktop.Tests.ViewModels;

public sealed class SitePlanAdjustmentExportSummaryTests : IDisposable
{
    private readonly string tempRoot = Path.Combine(
        Path.GetTempPath(),
        $"floorplan-fit-nullable-audit-{Guid.NewGuid():N}");

    [Fact]
    public void Nullable_numeric_audit_fields_preserve_manual_package_result()
    {
        var auditDirectory = Path.Combine(tempRoot, "audit");
        Directory.CreateDirectory(auditDirectory);
        File.WriteAllText(
            Path.Combine(auditDirectory, "outline-segment-congruence-audit.json"),
            """
            {"sheets":[{"segmentCongruence":{"Status":"InsufficientData","MissingInElectricalCount":null,"RequiredOutlineSegmentCount":null,"AdvisoryMissingInternalWallRunCount":null,"AdvisoryExtraElectricalWallRunCount":null,"Reason":"Electrical output is missing."}}]}
            """);
        File.WriteAllText(
            Path.Combine(auditDirectory, "final-output-congruence-audit.json"),
            """
            {"sheets":[{"finalOutputCongruence":{"Status":"InsufficientData","WidthMismatchInches":null,"HeightMismatchInches":null,"RawWidthMismatchInches":null,"RawHeightMismatchInches":null,"FloorStructuralBounds":null,"ElectricalStructuralBounds":null}}]}
            """);
        var audit = CreateManualAudit(Path.Combine(tempRoot, "manifest.json"));

        var lines = SitePlanAdjustmentViewModel.BuildPlanSetExportAuditLines(audit);
        var status = SitePlanAdjustmentViewModel.BuildExportStatus(
            new AdjustedSitePlanExportResult("floor-plan.dxf", 0, []),
            audit);

        Assert.Contains(lines, line => line.Contains("manual review required", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, line => line.Contains("datos insuficientes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(lines, line => line.Contains("n/a", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("HousePlanSet NO listo", status, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MissingExpectedOutput", status, StringComparison.Ordinal);
        Assert.DoesNotContain("requires an element of type 'Number'", status, StringComparison.OrdinalIgnoreCase);
    }

    private static MultiSheetExportAuditDto CreateManualAudit(string manifestPath)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "RequiresManualConfirmation",
            new ProjectionAuditSummaryDto(2, 0, 1, null, false),
            [],
            manifestPath,
            new DateTime(2026, 7, 15, 10, 34, 59, DateTimeKind.Utc))
        {
            HumanSummary = ["ElectricalPlan: manual review required; projected output is missing."],
            Verification = new PlanSetVerificationReportDto(
                PlanSetVerificationReportDto.CurrentSchemaVersion,
                new PlanSetVerificationOutputDto(2, 1, [Guid.NewGuid()]),
                MissingCheck(),
                new PlanSetVerificationOperationDto(0, 0, 0, 0),
                new PlanSetVerificationOperationDto(0, 0, 0, 0),
                MissingCheck(),
                MissingCheck(),
                MissingCheck(),
                new PlanSetVerificationCheckDto(PlanSetVerificationCheckStatus.Passed, 1, 1, 0, 0),
                [new PlanSetVerificationReasonDto(
                    PlanSetVerificationReasonCode.MissingExpectedOutput,
                    "outputs",
                    null,
                    "Electrical output is missing.")])
        };

    private static PlanSetVerificationCheckDto MissingCheck()
        => new(PlanSetVerificationCheckStatus.InsufficientData, 1, 0, 0, 1);

    public void Dispose()
    {
        if (Directory.Exists(tempRoot))
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
