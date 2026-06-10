using System.Net;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Infrastructure.OpenAi;

namespace FloorplanFit.Infrastructure.Tests.OpenAi;

public sealed class OpenAiAutoFitPlanSuggesterTests
{
    [Fact]
    public async Task SuggestAsync_posts_responses_request_with_candidate_plans_and_validates_ranked_plans()
    {
        var handler = new CaptureHandler(
            """
            {
              "output": [
                {
                  "type": "message",
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"plans\":[{\"summary\":\"Use Patio exactly.\",\"steps\":[{\"groupName\":\"Patio\",\"axisTag\":\"Width\",\"reductionInches\":1,\"reason\":\"Covers the exact width deficit.\"}],\"explanation\":\"No over-trim.\"},{\"summary\":\"Use Porch exactly.\",\"steps\":[{\"groupName\":\"Porch\",\"axisTag\":\"Width\",\"reductionInches\":1,\"reason\":\"Alternative exact width deficit.\"}],\"explanation\":\"Also valid.\"}]}"
                    }
                  ]
                }
              ]
            }
            """);
        var suggester = new OpenAiAutoFitPlanSuggester(
            new HttpClient(handler),
            new OpenAiAutoFitPlanSuggesterOptions(
                ApiKey: "test-openai-key",
                Model: "gpt-test-model",
                BaseUri: new Uri("https://openai.test")));
        var facts = WidthFacts();
        var candidates = CandidatePlans();

        var result = await suggester.SuggestAsync(facts, candidates, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Plan);
        Assert.True(result.Validation.IsValid);
        Assert.Equal(2, result.Plans.Count);
        Assert.Equal("Patio", result.Plans[0].Steps.Single().GroupName);
        Assert.Equal("Porch", result.Plans[1].Steps.Single().GroupName);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal(new Uri("https://openai.test/v1/responses"), handler.Request.RequestUri);
        Assert.True(handler.Request.Headers.TryGetValues("Authorization", out var authorization));
        Assert.Equal("Bearer test-openai-key", Assert.Single(authorization));
        Assert.Contains("gpt-test-model", handler.Body, StringComparison.Ordinal);
        Assert.Contains("CandidatePlans", handler.Body, StringComparison.Ordinal);
        Assert.Contains("Patio", handler.Body, StringComparison.Ordinal);
        Assert.Contains("Porch", handler.Body, StringComparison.Ordinal);
        Assert.Contains("WidthInches", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuggestAsync_returns_unavailable_without_calling_http_when_api_key_is_missing()
    {
        var handler = new CaptureHandler("{}");
        var suggester = new OpenAiAutoFitPlanSuggester(
            new HttpClient(handler),
            new OpenAiAutoFitPlanSuggesterOptions(
                ApiKey: "",
                Model: "gpt-test-model",
                BaseUri: new Uri("https://openai.test")));

        var result = await suggester.SuggestAsync(WidthFacts(), CandidatePlans(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.Plan);
        Assert.False(result.Validation.IsValid);
        Assert.Contains("OPENAI_API_KEY", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SuggestAsync_rejects_openai_ranked_plan_that_invents_group()
    {
        var handler = new CaptureHandler(
            """
            {
              "output": [
                {
                  "type": "message",
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"plans\":[{\"summary\":\"Use Kitchen.\",\"steps\":[{\"groupName\":\"Kitchen\",\"axisTag\":\"Width\",\"reductionInches\":1,\"reason\":\"Invented.\"}],\"explanation\":\"Bad fit.\"}]}"
                    }
                  ]
                }
              ]
            }
            """);
        var suggester = new OpenAiAutoFitPlanSuggester(
            new HttpClient(handler),
            new OpenAiAutoFitPlanSuggesterOptions(
                ApiKey: "test-openai-key",
                Model: "gpt-test-model",
                BaseUri: new Uri("https://openai.test")));

        var result = await suggester.SuggestAsync(WidthFacts(), CandidatePlans(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Plan);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, error => error.Contains("Kitchen", StringComparison.OrdinalIgnoreCase));
    }

    private static AutoFitSuggestionFacts WidthFacts()
        => new(
            new AutoFitEnvelopeDeficitDto(
                WidthInches: 1m,
                HeightInches: 0m,
                LeftInches: 0.5m,
                RightInches: 0.5m,
                BottomInches: 0m,
                TopInches: 0m),
            [
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Patio", "Width", 2m, 10m, 20m, 0),
                new AutoFitCandidateGroupDto(Guid.NewGuid(), "Porch", "Width", 2m, 30m, 40m, 0)
            ],
            []);

    private static IReadOnlyList<AutoFitSuggestionPlan> CandidatePlans()
        =>
        [
            new AutoFitSuggestionPlan(
                "Candidate Patio.",
                [new AutoFitSuggestionStep("Patio", "Width", 1m, "Candidate exact reduction.")],
                "Valid candidate."),
            new AutoFitSuggestionPlan(
                "Candidate Porch.",
                [new AutoFitSuggestionStep("Porch", "Width", 1m, "Candidate exact reduction.")],
                "Valid candidate.")
        ];

    private sealed class CaptureHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string Body { get; private set; } = string.Empty;

        public int CallCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Request = request;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson)
            };
        }
    }
}
