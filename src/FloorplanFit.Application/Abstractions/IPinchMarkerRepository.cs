using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IPinchMarkerRepository
{
    Task AddAsync(PinchMarker marker, CancellationToken cancellationToken);

    Task<PinchMarker?> GetByIdAsync(Guid pinchMarkerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PinchMarker>> ListByCurationAsync(Guid curationId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid pinchMarkerId, CancellationToken cancellationToken);

    Task RemoveByGroupAsync(Guid curationId, Guid pinchGroupId, CancellationToken cancellationToken);

    Task RemoveBySourceCandidateAsync(Guid curationId, Guid sourceCandidateId, CancellationToken cancellationToken);
}
