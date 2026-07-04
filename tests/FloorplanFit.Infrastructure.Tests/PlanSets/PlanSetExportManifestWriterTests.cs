using System.Text.Json;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Infrastructure.Runtime;
using FloorplanFit.Infrastructure.Storage;

namespace FloorplanFit.Infrastructure.Tests.PlanSets;

public sealed class PlanSetExportManifestWriterTests
{
    [Fact]
    public async Task WriteAsync_writes_manifest_with_per_sheet_status_confidence_and_warnings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"floorplan-fit-manifest-{Guid.NewGuid():N}");

        try
        {
            var exportId = Guid.NewGuid();
            var audit = new MultiSheetExportAuditDto(
                exportId,
                Guid.NewGuid(),
                Guid.NewGuid(),
                "RequiresManualConfirmation",
                new ProjectionAuditSummaryDto(
                    TotalSheetCount: 3,
                    AutomaticallyProjectedSheetCount: 1,
                    ManualConfirmationRequiredSheetCount: 1,
                    LowestConfidence: 0.94m,
                    CanExportPackageAutomatically: false),
                [
                    new ExportedPlanSheetDto(
                        Guid.NewGuid(),
                        ProjectionId: null,
                        "FloorPlan",
                        "Exported",
                        @"C:\out\floor.dxf",
                        "CanonicalFloorPlanAdjustment",
                        Confidence: 1m,
                        Warning: null,
                        RuleSummary: "canonical"),
                    new ExportedPlanSheetDto(
                        Guid.NewGuid(),
                        Guid.NewGuid(),
                        "ElectricalPlan",
                        "ProjectedAutomatically",
                        @"C:\out\electrical.dxf",
                        "ElectricalWholeSheetSimilarity",
                        Confidence: 0.94m,
                        Warning: null,
                        RuleSummary: "electrical follows floor plan",
                        RecipeHandlingSummary: "ElectricalPlan: affine placement applied; local recipe requires review before DXF deformation: HorizontalCompression Right @50 delta 2."),
                    new ExportedPlanSheetDto(
                        Guid.NewGuid(),
                        ProjectionId: null,
                        "RoofPlan",
                        "MissingProjection",
                        StoragePath: null,
                        ProjectionMethod: null,
                        Confidence: null,
                        Warning: "No projection exists for this sheet and canonical adjustment.",
                        RuleSummary: null)
                ],
                PackageManifestPath: null,
                new DateTime(2026, 7, 1, 16, 0, 0, DateTimeKind.Utc),
                new PlanSetQualityReportDto(
                    Guid.NewGuid(),
                    RegistrationEventCount: 1,
                    ProjectionEventCount: 1,
                    LowestRegistrationConfidence: 0.82m,
                    LowestProjectionConfidence: 0.94m,
                    ManualRegistrationCount: 0,
                    ManualProjectionCount: 1,
                    Signals: []));
            var writer = new PlanSetExportManifestWriter(new AppWorkspace(Path.Combine(tempRoot, "workspace")));

            var manifestPath = await writer.WriteAsync(audit, CancellationToken.None);

            Assert.True(File.Exists(manifestPath));
            Assert.EndsWith(Path.Combine("exports", "plan-sets", exportId.ToString("N"), "manifest.json"), manifestPath);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
            var root = document.RootElement;
            Assert.Equal("RequiresManualConfirmation", root.GetProperty("Status").GetString());
            Assert.Equal(3, root.GetProperty("Summary").GetProperty("TotalSheetCount").GetInt32());

            var sheets = root.GetProperty("Sheets").EnumerateArray().ToArray();
            Assert.Contains(sheets, sheet =>
                sheet.GetProperty("SheetKind").GetString() == "ElectricalPlan" &&
                sheet.GetProperty("Status").GetString() == "ProjectedAutomatically" &&
                sheet.GetProperty("ProjectionMethod").GetString() == "ElectricalWholeSheetSimilarity" &&
                sheet.GetProperty("Confidence").GetDecimal() == 0.94m &&
                sheet.GetProperty("RecipeHandlingSummary").GetString()!.Contains("HorizontalCompression", StringComparison.Ordinal));
            Assert.Contains(sheets, sheet =>
                sheet.GetProperty("SheetKind").GetString() == "RoofPlan" &&
                sheet.GetProperty("Status").GetString() == "MissingProjection" &&
                sheet.GetProperty("Warning").GetString() == "No projection exists for this sheet and canonical adjustment.");
            Assert.Equal(1, root.GetProperty("QualityReport").GetProperty("ManualProjectionCount").GetInt32());
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
