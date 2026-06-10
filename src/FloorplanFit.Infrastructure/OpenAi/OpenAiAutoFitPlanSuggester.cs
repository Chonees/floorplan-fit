using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

namespace FloorplanFit.Infrastructure.OpenAi;

public sealed class OpenAiAutoFitPlanSuggester : IAutoFitPlanSuggester
{
    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        WriteIndented = false
    };
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly OpenAiAutoFitPlanSuggesterOptions options;

    public OpenAiAutoFitPlanSuggester(
        HttpClient httpClient,
        OpenAiAutoFitPlanSuggesterOptions options)
    {
        this.httpClient = httpClient;
        this.options = options;
    }

    public Task<AutoFitSuggestionPlanResponse> SuggestAsync(
        AutoFitSuggestionFacts facts,
        CancellationToken cancellationToken)
        => SuggestAsync(facts, AutoFitSuggestionOptionGenerator.Generate(facts), cancellationToken);

    public async Task<AutoFitSuggestionPlanResponse> SuggestAsync(
        AutoFitSuggestionFacts facts,
        IReadOnlyList<AutoFitSuggestionPlan> candidatePlans,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(candidatePlans);

        if (!facts.NeedsAdjustment)
        {
            var alreadyFits = new AutoFitSuggestionPlan(
                "El floor plan ya entra dentro del envelope construible.",
                [],
                "No hay dÃ©ficit de Width ni Height, asÃ­ que no hace falta recortar grupos de pinches.");
            var validation = AutoFitSuggestionPlanValidator.Validate(facts, alreadyFits);
            return new AutoFitSuggestionPlanResponse(true, alreadyFits, validation, null)
            {
                Plans = [alreadyFits],
                Validations = [validation]
            };
        }

        if (candidatePlans.Count == 0)
        {
            return Failure(
                "OpenAI ranking is unavailable because no deterministic candidate fit plans were provided.",
                "Generate deterministic candidate plans before requesting an LLM ranking.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return Failure(
                "OpenAI is unavailable because OPENAI_API_KEY is not configured.",
                "Set OPENAI_API_KEY before requesting an LLM fit suggestion.");
        }

        using var request = BuildRequest(facts, candidatePlans);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Failure(
                $"OpenAI returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}.",
                responseBody);
        }

        var plans = TryParsePlans(responseBody);
        if (plans.Count == 0)
        {
            return Failure(
                "OpenAI response did not contain a valid fit plan JSON object.",
                responseBody);
        }

        var acceptedPlans = new List<AutoFitSuggestionPlan>();
        var acceptedValidations = new List<AutoFitSuggestionValidationResult>();
        AutoFitSuggestionPlan? firstRejectedPlan = null;
        AutoFitSuggestionValidationResult? firstRejectedValidation = null;

        foreach (var plan in plans)
        {
            var validation = AutoFitSuggestionPlanValidator.Validate(facts, plan);
            if (validation.IsValid)
            {
                acceptedPlans.Add(plan);
                acceptedValidations.Add(validation);
                continue;
            }

            firstRejectedPlan ??= plan;
            firstRejectedValidation ??= validation;
        }

        if (acceptedPlans.Count == 0)
        {
            return new AutoFitSuggestionPlanResponse(
                false,
                firstRejectedPlan,
                firstRejectedValidation ?? new AutoFitSuggestionValidationResult(false, ["OpenAI returned no valid ranked plans."]),
                "OpenAI returned ranked plans that failed deterministic fit validation.")
            {
                Plans = plans,
                Validations = plans.Select(plan => AutoFitSuggestionPlanValidator.Validate(facts, plan)).ToArray()
            };
        }

        return new AutoFitSuggestionPlanResponse(
            true,
            acceptedPlans[0],
            acceptedValidations[0],
            null)
        {
            Plans = acceptedPlans,
            Validations = acceptedValidations
        };
    }

    private HttpRequestMessage BuildRequest(
        AutoFitSuggestionFacts facts,
        IReadOnlyList<AutoFitSuggestionPlan> candidatePlans)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(options.ResolvedBaseUri, "/v1/responses"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey!.Trim());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var body = new Dictionary<string, object?>
        {
            ["model"] = options.ResolvedModel,
            ["input"] = BuildPrompt(facts, candidatePlans)
        };
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, RequestJsonOptions),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static string BuildPrompt(
        AutoFitSuggestionFacts facts,
        IReadOnlyList<AutoFitSuggestionPlan> candidatePlans)
    {
        var payloadJson = JsonSerializer.Serialize(
            new
            {
                Facts = facts,
                CandidatePlans = candidatePlans
            },
            new JsonSerializerOptions { WriteIndented = true });
        return
            $$"""
            You rank human-in-the-loop fit adjustment options for Floorplan Fit.

            Deterministic code has already measured the site-plan envelope deficit and generated candidate plans.
            You are not the geometry authority. You must not invent geometry, groups, axes, or reductions.

            Rules:
            - Use only plans present in CandidatePlans.
            - Do not change any step groupName, axisTag, or reductionInches.
            - You may reorder the plans and improve summary/reason/explanation for human review.
            - Return only one JSON object with this shape:
              {
                "plans": [
                  {
                    "summary": "short human-readable summary",
                    "steps": [
                      {
                        "groupName": "exact groupName from a CandidatePlans step",
                        "axisTag": "exact axisTag from that step",
                        "reductionInches": 1.0,
                        "reason": "why this option is useful"
                      }
                    ],
                    "explanation": "short explanation for human review"
                  }
                ]
              }

            Prefer options that affect fewer groups first, then options that distribute impact when the human may prefer a balanced cut.

            Payload:
            {{payloadJson}}
            """;
    }

    private static IReadOnlyList<AutoFitSuggestionPlan> TryParsePlans(string responseBody)
    {
        try
        {
            var contentText = ExtractOutputText(responseBody);
            if (string.IsNullOrWhiteSpace(contentText))
            {
                return [];
            }

            var json = ExtractJsonObject(contentText);
            if (json is null)
            {
                return [];
            }

            var planSet = JsonSerializer.Deserialize<OpenAiPlanSet>(json, ResponseJsonOptions);
            if (planSet?.Plans is { Count: > 0 })
            {
                return planSet.Plans;
            }

            var singlePlan = JsonSerializer.Deserialize<AutoFitSuggestionPlan>(json, ResponseJsonOptions);
            return singlePlan is null ? [] : [singlePlan];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ExtractOutputText(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("output", out var output) ||
            output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) ||
                content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }

    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{', StringComparison.Ordinal);
        var end = text.LastIndexOf('}');
        return start < 0 || end < start
            ? null
            : text[start..(end + 1)];
    }

    private static AutoFitSuggestionPlanResponse Failure(
        string validationError,
        string? errorMessage)
    {
        return new AutoFitSuggestionPlanResponse(
            false,
            null,
            new AutoFitSuggestionValidationResult(false, [validationError]),
            errorMessage);
    }

    private sealed record OpenAiPlanSet(IReadOnlyList<AutoFitSuggestionPlan> Plans);
}
