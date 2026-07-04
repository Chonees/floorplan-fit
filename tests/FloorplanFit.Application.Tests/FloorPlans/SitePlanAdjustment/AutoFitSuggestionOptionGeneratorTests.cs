using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class AutoFitSuggestionOptionGeneratorTests
{
    [Fact]
    public void Generate_returns_single_group_and_split_options_for_height_deficit()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 0m,
                HeightInches: 2m,
                LeftInches: 0m,
                RightInches: 0m,
                BottomInches: 1m,
                TopInches: 1m),
            [
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Ajuste 1", "Height", 4m, 10m, 20m, 1),
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Ajuste 2", "Height", 4m, 30m, 40m, 1)
            ],
            []);

        var plans = AutoFitSuggestionOptionGenerator.Generate(facts, maxPlans: 5);

        Assert.Contains(plans, plan =>
            plan.Steps.Count == 1 &&
            plan.Steps[0].GroupName == "Ajuste 1" &&
            plan.Steps[0].ReductionInches == 2m);
        Assert.Contains(plans, plan =>
            plan.Steps.Count == 1 &&
            plan.Steps[0].GroupName == "Ajuste 2" &&
            plan.Steps[0].ReductionInches == 2m);
        Assert.Contains(plans, plan =>
            plan.Steps.Count == 2 &&
            plan.Steps.All(step => step.ReductionInches == 1m) &&
            plan.Steps.Select(step => step.GroupName).Order().SequenceEqual(["Ajuste 1", "Ajuste 2"]));
        Assert.All(plans, plan => Assert.True(AutoFitSuggestionPlanValidator.Validate(facts, plan).IsValid));
    }

    [Fact]
    public void Generate_returns_no_options_when_required_axis_lacks_capacity()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 3m,
                HeightInches: 0m,
                LeftInches: 1.5m,
                RightInches: 1.5m,
                BottomInches: 0m,
                TopInches: 0m),
            [new AutoFitCandidateGroupDto(Guid.NewGuid(), "Patio", "Width", 2m, 10m, 20m, 1)],
            ["Width deficit 3 inches exceeds available capacity 2 inches."]);

        var plans = AutoFitSuggestionOptionGenerator.Generate(facts);

        Assert.Empty(plans);
    }
}
