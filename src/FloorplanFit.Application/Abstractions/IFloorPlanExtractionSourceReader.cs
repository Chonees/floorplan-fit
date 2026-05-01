namespace FloorplanFit.Application.Abstractions;

public interface IFloorPlanExtractionSourceReader
{
    Task<FloorPlanExtractionSource?> GetCurrentSourceAsync(Guid templateId, CancellationToken cancellationToken);
}
