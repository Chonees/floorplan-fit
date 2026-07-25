using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSetExportManifestWriter
{
    PlanSetVerificationReportDto BuildVerificationReport(MultiSheetExportAuditDto audit);

    Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken);

    Task WritePackageArtifactsAsync(
        MultiSheetExportAuditDto audit,
        string stagingDirectory,
        CancellationToken cancellationToken)
        => throw new NotSupportedException("This manifest writer cannot stage user package artifacts.");
}
