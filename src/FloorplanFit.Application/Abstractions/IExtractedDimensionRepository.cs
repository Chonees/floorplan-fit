using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IExtractedDimensionRepository
{
    Task AddRangeAsync(IReadOnlyList<ExtractedDimension> dimensions, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtractedDimension>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken);
}
