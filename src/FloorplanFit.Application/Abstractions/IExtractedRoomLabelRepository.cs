using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IExtractedRoomLabelRepository
{
    Task AddRangeAsync(IReadOnlyList<ExtractedRoomLabel> labels, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtractedRoomLabel>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken);
}
