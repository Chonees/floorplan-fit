namespace FloorplanFit.Application.Abstractions;

public interface IRoomLabelExtractor
{
    Task<IReadOnlyList<DetectedRoomLabel>> ExtractAsync(string managedFilePath, CancellationToken cancellationToken);
}
