using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IMeasurementNodeRepository
{
    Task AddAsync(MeasurementNode node, CancellationToken cancellationToken);

    Task DeleteAsync(Guid nodeId, CancellationToken cancellationToken);

    Task DeleteByCorridorAsync(Guid corridorId, CancellationToken cancellationToken);

    Task<MeasurementNode?> GetByIdAsync(Guid nodeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MeasurementNode>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MeasurementNode>> ListByCorridorAsync(Guid corridorId, CancellationToken cancellationToken);

    Task UpdateAsync(MeasurementNode node, CancellationToken cancellationToken);
}
