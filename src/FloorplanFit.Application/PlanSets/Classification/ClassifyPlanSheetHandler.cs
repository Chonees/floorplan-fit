using FloorplanFit.Contracts.PlanSets;
using FloorplanFit.Domain.PlanSets;

namespace FloorplanFit.Application.PlanSets.Classification;

public sealed class ClassifyPlanSheetHandler
{
    private static readonly ClassificationRule[] Rules =
    [
        new(PlanSheetType.ElectricalPlan, 0.9m, ["electrical", "electric", "lighting", "power", "elec"]),
        new(PlanSheetType.RoofPlan, 0.9m, ["roof", "roofing"]),
        new(PlanSheetType.FacadeElevation, 0.85m, ["facade", "elevation", "elev", "front", "rear"]),
        new(PlanSheetType.FloorPlan, 0.9m, ["floor plan", "floorplan", "foundation plan"])
    ];

    public Task<ClassifyPlanSheetResponse> HandleAsync(
        ClassifyPlanSheetRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(request);

        var text = Normalize($"{Path.GetFileNameWithoutExtension(request.FileName)} {request.SheetTitle}");
        var layerText = Normalize(string.Join(" ", request.LayerHints ?? []));
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(layerText))
        {
            throw new ArgumentException("A sheet file name, title, or layer hint is required.", nameof(request));
        }

        var namedMatches = MatchRules(text);
        var layerMatches = MatchRules(layerText);
        var matches = namedMatches
            .Concat(layerMatches)
            .DistinctBy(rule => rule.SheetType)
            .ToArray();

        if (matches.Length == 1)
        {
            var match = matches[0];
            var source = layerMatches.Contains(match) ? "layer" : "sheet";
            return Task.FromResult(new ClassifyPlanSheetResponse(
                match.SheetType.ToString(),
                match.Confidence,
                RequiresManualConfirmation: false,
                $"Matched {match.SheetType} {source} keywords."));
        }

        if (matches.Length > 1)
        {
            return Task.FromResult(new ClassifyPlanSheetResponse(
                PlanSheetType.Unknown.ToString(),
                0.4m,
                RequiresManualConfirmation: true,
                "Matched multiple sheet-type keyword groups; manual classification required."));
        }

        return Task.FromResult(new ClassifyPlanSheetResponse(
            PlanSheetType.Unknown.ToString(),
            0m,
            RequiresManualConfirmation: true,
            "No sheet-type keyword matched; manual classification required."));
    }

    private static string Normalize(string value)
        => value.Trim().ToLowerInvariant();

    private static IEnumerable<ClassificationRule> MatchRules(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return Rules.Where(rule => rule.Keywords.Any(text.Contains));
    }

    private sealed record ClassificationRule(
        PlanSheetType SheetType,
        decimal Confidence,
        IReadOnlyList<string> Keywords);
}
