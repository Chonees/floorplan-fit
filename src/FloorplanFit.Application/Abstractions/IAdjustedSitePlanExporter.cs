using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

/// <summary>
/// Exports the adjusted floor plan combined with its site plan into one DXF: the raw floor
/// plan file is preserved (layers, blocks, native dimensions) with the applied auto-fit
/// compression patched into its coordinates, and the site plan entities are injected into
/// it transformed by the inverse placement affine, on their own layers.
/// </summary>
public interface IAdjustedSitePlanExporter
{
    Task<AdjustedSitePlanExportResult> ExportAsync(
        string floorPlanSourcePath,
        string sitePlanSourcePath,
        string outputFilePath,
        AdjustedSitePlanPlacementDto placement,
        CancellationToken cancellationToken);
}

public sealed record AdjustedSitePlanExportResult(
    string OutputFilePath,
    int InjectedSitePlanEntityCount,
    IReadOnlyList<string> Warnings);
