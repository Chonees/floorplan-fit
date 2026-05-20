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
            VersionCount: 1,
            CurrentVersionId: version.Id,
            CurrentVersionNumber: version.VersionNumber,
            Versions:
            [
                new FloorPlanLibraryVersionDto(
                    version.Id,
                    version.VersionNumber,
                    "Imported",
                    version.CreatedAtUtc,
                    measurementContext.SourceUnit.ToString().ToLowerInvariant(),
                    IsCurrent: true)
            ]);

        return new ImportFloorPlanResponse(item);
    }
}
