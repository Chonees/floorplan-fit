using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IAdjustedDxfExporter
{
    Task ExportAsync(
        string sourceFilePath,
        string outputFilePath,
        IReadOnlyList<DimensionDto> dimensions,
        CancellationToken cancellationToken);
}
