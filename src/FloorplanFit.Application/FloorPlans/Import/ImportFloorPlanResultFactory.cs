using FloorplanFit.Contracts.FloorPlans;
using FloorplanFit.Domain.FloorPlans;
using FloorplanFit.Domain.Measurement;

namespace FloorplanFit.Application.FloorPlans.Import;

public sealed class ImportFloorPlanResultFactory
{
    public ImportFloorPlanResponse Create(
        FloorPlanTemplate template,
        FloorPlanVersion version,
        MeasurementContext measurementContext)
    {
        var item = new FloorPlanLibraryItemDto(
            template.Id,
            template.Code,
            template.Name,
            "Imported",
            version.VersionNumber,
            version.CreatedAtUtc,
            measurementContext.SourceUnit.ToString().ToLowerInvariant());

        return new ImportFloorPlanResponse(item);
    }
}
