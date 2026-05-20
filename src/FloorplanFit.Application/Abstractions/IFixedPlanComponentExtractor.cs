namespace FloorplanFit.Application.Abstractions;

public interface IFixedPlanComponentExtractor
{
    Task<IReadOnlyList<DetectedFixedPlanComponent>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken);
}
