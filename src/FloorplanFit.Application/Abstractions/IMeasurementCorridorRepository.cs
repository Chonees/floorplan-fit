using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IMeasurementCorridorRepository
{
    Task AddAsync(MeasurementCorridor corridor, CancellationToken cancellationToken);

    Task DeleteAsync(Guid corridorId, CancellationToken cancellationToken);

    Task<MeasurementCorridor?> GetByIdAsync(Guid corridorId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MeasurementCorridor>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken);

    Task UpdateAsync(MeasurementCorridor corridor, CancellationToken cancellationToken);
}
