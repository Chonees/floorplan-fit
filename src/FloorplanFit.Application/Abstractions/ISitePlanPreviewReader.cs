using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface ISitePlanPreviewReader
{
    Task<SitePlanPreviewDto> ReadAsync(string filePath, CancellationToken cancellationToken);
}
