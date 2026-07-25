using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

namespace FloorplanFit.Application.Tests.FloorPlans.SitePlanAdjustment;

public sealed class AutoFitSuggestionPlanValidatorTests
{
    [Fact]
    public void Validate_accepts_exact_named_plan_split_across_width_and_height_groups()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 2m,
                HeightInches: 1.5m,
                LeftInches: 1m,
                RightInches: 1m,
                BottomInches: 0.75m,
                TopInches: 0.75m),
            [
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Patio", "Width", 1m, 10m, 20m, 0),
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Bedroom side", "Width", 1m, 30m, 40m, 0),
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Garage depth", "Height", 2m, 50m, 80m, 0)
            ],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Recortar ancho y alto con el mínimo exacto.",
            [
                new AutoFitSuggestionStep("Patio", "Width", 1m, "absorbe medio ancho"),
                new AutoFitSuggestionStep("Bedroom side", "Width", 1m, "absorbe el ancho restante"),
                new AutoFitSuggestionStep("Garage depth", "Height", 1.5m, "absorbe el alto exacto")
            ],
            "El total por eje coincide con el déficit.");

        var result = AutoFitSuggestionPlanValidator.Validate(facts, plan);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_rejects_invented_group_names()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(WidthInches: 1m, HeightInches: 0m, LeftInches: 0.5m, RightInches: 0.5m, BottomInches: 0m, TopInches: 0m),
            [new AutoFitCandidateGroupDto(Guid.NewGuid(), "Patio", "Width", 1m, 10m, 20m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Invented group",
            [new AutoFitSuggestionStep("Kitchen", "Width", 1m, "LLM hallucinated this group")],
            "Bad plan.");

        var result = AutoFitSuggestionPlanValidator.Validate(facts, plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Kitchen", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_over_reduction_because_fit_must_be_just_enough()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(WidthInches: 1m, HeightInches: 0m, LeftInches: 0.5m, RightInches: 0.5m, BottomInches: 0m, TopInches: 0m),
            [new AutoFitCandidateGroupDto(Guid.NewGuid(), "Patio", "Width", 2m, 10m, 20m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Over trims",
            [new AutoFitSuggestionStep("Patio", "Width", 1.25m, "more than needed")],
            "Bad plan.");

        var result = AutoFitSuggestionPlanValidator.Validate(facts, plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("exact", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_wrong_axis_for_named_group()
    {
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(WidthInches: 1m, HeightInches: 0m, LeftInches: 0.5m, RightInches: 0.5m, BottomInches: 0m, TopInches: 0m),
            [new AutoFitCandidateGroupDto(Guid.NewGuid(), "Garage depth", "Height", 2m, 50m, 80m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Wrong axis",
            [new AutoFitSuggestionStep("Garage depth", "Width", 1m, "tries to use a Height group for Width")],
            "Bad plan.");

        var result = AutoFitSuggestionPlanValidator.Validate(facts, plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("axis", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_rejects_repeating_one_pinch_group_as_multiple_logical_actions()
    {
        var groupId = Guid.NewGuid();
        var facts = new AutoFitSuggestionFacts(
            new AutoFitEnvelopeDeficitDto(WidthInches: 2m, HeightInches: 0m, LeftInches: 1m, RightInches: 1m, BottomInches: 0m, TopInches: 0m),
            [new AutoFitCandidateGroupDto(groupId, "Patio", "Width", 3m, 10m, 20m, 0)],
            []);
        var plan = new AutoFitSuggestionPlan(
            "Duplicate logical action",
            [
                new AutoFitSuggestionStep("Patio", "Width", 1m, "first half"),
                new AutoFitSuggestionStep("Patio", "Width", 1m, "second half")
            ],
            "A paired pinch group must carry one total delta.");

        var result = AutoFitSuggestionPlanValidator.Validate(facts, plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("one logical action", StringComparison.OrdinalIgnoreCase));
    }
}
