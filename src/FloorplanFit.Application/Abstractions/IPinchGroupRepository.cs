using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IPinchGroupRepository
{
    Task AddAsync(PinchGroup group, CancellationToken cancellationToken);

    Task<PinchGroup?> GetByIdAsync(Guid pinchGroupId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PinchGroup>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid pinchGroupId, CancellationToken cancellationToken);

    Task UpdateAsync(PinchGroup group, CancellationToken cancellationToken);
}
