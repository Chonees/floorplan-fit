using FloorplanFit.Domain.Measurement;

namespace FloorplanFit.Application.Abstractions;

public interface IMeasurementContextRepository
{
    Task AddAsync(MeasurementContext context, CancellationToken cancellationToken);
}
