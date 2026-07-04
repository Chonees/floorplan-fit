using FloorplanFit.Contracts.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface IPlanSetExportManifestWriter
{
    Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken);
}
