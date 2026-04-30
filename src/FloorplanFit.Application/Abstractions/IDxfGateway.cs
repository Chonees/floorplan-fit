namespace FloorplanFit.Application.Abstractions;

public interface IDxfGateway
{
    Task<DetectedFloorPlanDocument> ReadFloorPlanAsync(string filePath, CancellationToken cancellationToken);
}
