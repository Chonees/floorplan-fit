using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.Abstractions;

public interface ISheetRegistrationRepository
{
    Task AddAsync(SheetRegistration registration, CancellationToken cancellationToken);

    Task<SheetRegistration?> GetByIdAsync(Guid registrationId, CancellationToken cancellationToken);
}
