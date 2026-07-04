using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Infrastructure.Runtime;

namespace FloorplanFit.Infrastructure.Storage;

public sealed class PlanSetExportManifestWriter : IPlanSetExportManifestWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly AppWorkspace workspace;

    public PlanSetExportManifestWriter(AppWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public async Task<string> WriteAsync(MultiSheetExportAuditDto audit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audit);
        cancellationToken.ThrowIfCancellationRequested();

        workspace.EnsureCreated();

        var packageDirectory = Path.Combine(
            workspace.RootPath,
            "exports",
            "plan-sets",
            audit.ExportId.ToString("N"));
        Directory.CreateDirectory(packageDirectory);

        var manifestPath = Path.Combine(packageDirectory, "manifest.json");
        await File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(audit, JsonOptions),
            cancellationToken);

        return manifestPath;
    }
}
