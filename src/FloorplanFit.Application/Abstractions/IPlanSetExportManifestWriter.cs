using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSetExportManifestWriter
{
    PlanSetVerificationReportDto BuildVerificationReport(MultiSheetExportAuditDto audit);

    Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken);
}
