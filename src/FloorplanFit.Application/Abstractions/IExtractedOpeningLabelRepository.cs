using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IExtractedOpeningLabelRepository
{
    Task AddRangeAsync(IReadOnlyList<ExtractedOpeningLabel> labels, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtractedOpeningLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid openingLabelId, CancellationToken cancellationToken);
}
