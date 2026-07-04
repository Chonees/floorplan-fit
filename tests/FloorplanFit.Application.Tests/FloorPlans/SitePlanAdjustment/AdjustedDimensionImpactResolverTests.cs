using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Contracts.FloorPlans;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class AdjustedDimensionImpactResolverTests
{
    [Fact]
    public void ResolveAffectedDimensionIds_marks_bindings_whose_interval_overlaps_selected_candidate_band()
    {
        var pinchGroupId = Guid.NewGuid();
        var corridorId = Guid.NewGuid();
        var affectedDimensionId = Guid.NewGuid();
        var ignoredDimensionId = Guid.NewGuid();
        var steps = new[] { new AutoFitSuggestionStep("Ajuste 1", "Width", 2m, "Selected") };
        var candidates = new[] { new AutoFitCandidateGroupDto(pinchGroupId, "Ajuste 1", "Width", 4m, 150m, 240m, 2) };
        var corridors = new[] { new MeasurementCorridorDto(corridorId, "Facade", "Width", Guid.NewGuid(), 95m, 145m, "Verified", 1) };
        var bindings = new[]
        {
            new DimensionIntervalBindingDto(affectedDimensionId, corridorId, Guid.NewGuid(), Guid.NewGuid(), "ManualVerified", 100m, 224m),
            new DimensionIntervalBindingDto(ignoredDimensionId, corridorId, Guid.NewGuid(), Guid.NewGuid(), "ManualVerified", 10m, 90m)
        };

        var affectedIds = AdjustedDimensionImpactResolver.ResolveAffectedDimensionIds(steps, candidates, corridors, bindings);

        Assert.Contains(affectedDimensionId, affectedIds);
        Assert.DoesNotContain(ignoredDimensionId, affectedIds);
    }

    [Fact]
    public void ResolveAffectedDimensionIds_ignores_bindings_on_other_axes()
    {
        var corridorId = Guid.NewGuid();
        var dimensionId = Guid.NewGuid();
        var steps = new[] { new AutoFitSuggestionStep("Ajuste alto", "Height", 1m, "Selected") };
        var candidates = new[] { new AutoFitCandidateGroupDto(Guid.NewGuid(), "Ajuste alto", "Height", 2m, 300m, 340m, 1) };
        var corridors = new[] { new MeasurementCorridorDto(corridorId, "Facade", "Width", Guid.NewGuid(), 0m, 500m, "Verified", 1) };
        var bindings = new[] { new DimensionIntervalBindingDto(dimensionId, corridorId, Guid.NewGuid(), Guid.NewGuid(), "ManualVerified", 310m, 320m) };

        var affectedIds = AdjustedDimensionImpactResolver.ResolveAffectedDimensionIds(steps, candidates, corridors, bindings);

        Assert.Empty(affectedIds);
    }
}
