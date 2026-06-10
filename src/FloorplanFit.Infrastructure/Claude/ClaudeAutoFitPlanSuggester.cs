using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FloorplanFit.Application.Abstractions;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;

namespace FloorplanFit.Infrastructure.Claude;

public sealed class ClaudeAutoFitPlanSuggester : IAutoFitPlanSuggester
{
    private const string AnthropicVersion = "2023-06-01";
    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        WriteIndented = false
    };
    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly ClaudeAutoFitPlanSuggesterOptions options;

    public ClaudeAutoFitPlanSuggester(
        HttpClient httpClient,
        ClaudeAutoFitPlanSuggesterOptions options)
    {
        this.httpClient = httpClient;
        this.options = options;
    }

    public async Task<AutoFitSuggestionPlanResponse> SuggestAsync(
        AutoFitSuggestionFacts facts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (!facts.NeedsAdjustment)
        {
            var alreadyFits = new AutoFitSuggestionPlan(
                "El floor plan ya entra dentro del envelope construible.",
                [],
                "No hay déficit de Width ni Height, así que no hace falta recortar grupos de pinches.");
            return new AutoFitSuggestionPlanResponse(
                true,
                alreadyFits,
                AutoFitSuggestionPlanValidator.Validate(facts, alreadyFits),
                null);
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return Failure(
                "Claude is unavailable because ANTHROPIC_API_KEY is not configured.",
                "Set ANTHROPIC_API_KEY before requesting an LLM fit suggestion.");
        }

        using var request = BuildRequest(facts);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Failure(
                $"Claude returned HTTP {(int)response.StatusCode} {response.ReasonPhrase}.",
                responseBody);
        }

        var plan = TryParsePlan(responseBody);
        if (plan is null)
        {
            return Failure(
                "Claude response did not contain a valid fit plan JSON object.",
                responseBody);
        }

        var validation = AutoFitSuggestionPlanValidator.Validate(facts, plan);
        return new AutoFitSuggestionPlanResponse(
            validation.IsValid,
            plan,
            validation,
            validation.IsValid
                ? null
                : "Claude returned a plan that failed deterministic fit validation.");
    }

    private HttpRequestMessage BuildRequest(AutoFitSuggestionFacts facts)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(options.ResolvedBaseUri, "/v1/messages"));
        request.Headers.Add("x-api-key", options.ApiKey!.Trim());
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var body = new Dictionary<string, object?>
        {
            ["model"] = options.ResolvedModel,
            ["max_tokens"] = 1200,
            ["messages"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["role"] = "user",
                    ["content"] = BuildPrompt(facts)
                }
            }
        };
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, RequestJsonOptions),
            Encoding.UTF8,
            "application/json");

        return request;
    }

    private static string BuildPrompt(AutoFitSuggestionFacts facts)
    {
        var factsJson = JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true });
        return
            $$"""
            You suggest fit adjustment plans for Floorplan Fit.

            Deterministic code has already measured the site-plan envelope deficit and candidate pinch groups.
            You must not invent geometry. You must only pick from CandidateGroups by exact Name and matching AxisTag.

            Rules:
            - Use only group names present in CandidateGroups.
            - For each axis, the sum of ReductionInches must equal the corresponding deficit exactly:
              - Width uses Deficit.WidthInches.
              - Height uses Deficit.HeightInches.
            - Do not reduce any group beyond its CapacityInches.
            - If both Width and Height have deficits, include steps for both axes.
            - Prefer the smallest number of groups when capacity is sufficient.
            - Return only one JSON object with this shape:
              {
                "summary": "short human-readable summary",
                "steps": [
                  {
                    "groupName": "exact CandidateGroups Name",
                    "axisTag": "Width or Height",
                    "reductionInches": 1.0,
                    "reason": "why this group absorbs this exact amount"
                  }
                ],
                "explanation": "short explanation for human review"
              }

            Facts:
            {{factsJson}}
            """;
    }

    private static AutoFitSuggestionPlan? TryParsePlan(string responseBody)
    {
        try
        {
            var contentText = ExtractTextBlock(responseBody);
            if (string.IsNullOrWhiteSpace(contentText))
            {
                return null;
            }

            var json = ExtractJsonObject(contentText);
            return json is null
                ? null
                : JsonSerializer.Deserialize<AutoFitSuggestionPlan>(json, ResponseJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractTextBlock(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("content", out var content) ||
            content.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var block in content.EnumerateArray())
        {
            if (block.TryGetProperty("type", out var type) &&
                string.Equals(type.GetString(), "text", StringComparison.OrdinalIgnoreCase) &&
                block.TryGetProperty("text", out var text))
            {
                return text.GetString();
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
}

