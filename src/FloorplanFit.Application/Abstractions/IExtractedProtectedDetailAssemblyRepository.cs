using FloorplanFit.Domain.FloorPlans;

namespace FloorplanFit.Application.Abstractions;

public interface IExtractedProtectedDetailAssemblyRepository
{
    Task AddRangeAsync(
        IReadOnlyList<ExtractedProtectedDetailAssembly> domainAssemblies,
        IReadOnlyList<DetectedProtectedDetailAssembly> detectedAssemblies,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtractedProtectedDetailAssembly>> ListByExtractionRunAsync(Guid wallExtractionRunId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid protectedDetailAssemblyId, CancellationToken cancellationToken);
}
