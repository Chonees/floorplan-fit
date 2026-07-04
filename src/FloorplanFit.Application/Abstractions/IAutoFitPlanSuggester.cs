using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

namespace FloorplanFit.Application.Abstractions;

public interface IAutoFitPlanSuggester
{
    Task<AutoFitSuggestionPlanResponse> SuggestAsync(
        AutoFitSuggestionFacts facts,
        CancellationToken cancellationToken);

    Task<AutoFitSuggestionPlanResponse> SuggestAsync(
        AutoFitSuggestionFacts facts,
        IReadOnlyList<AutoFitSuggestionPlan> candidatePlans,
        CancellationToken cancellationToken)
        => SuggestAsync(facts, cancellationToken);
}
