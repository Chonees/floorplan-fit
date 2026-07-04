using System.Net;
using FloorplanFit.Application.FloorPlans.SitePlanAdjustment;
using FloorplanFit.Infrastructure.Claude;

namespace FloorplanFit.Infrastructure.Tests.Claude;

public sealed class ClaudeAutoFitPlanSuggesterTests
{
    [Fact]
    public async Task SuggestAsync_posts_anthropic_messages_request_and_validates_returned_plan()
    {
        var handler = new CaptureHandler(
            """
            {
              "content": [
                {
                  "type": "text",
                  "text": "{\"summary\":\"Use Patio exactly.\",\"steps\":[{\"groupName\":\"Patio\",\"axisTag\":\"Width\",\"reductionInches\":1,\"reason\":\"Covers the exact width deficit.\"}],\"explanation\":\"No over-trim.\"}"
                }
              ]
            }
            """);
        var suggester = new ClaudeAutoFitPlanSuggester(
            new HttpClient(handler),
            new ClaudeAutoFitPlanSuggesterOptions(
                ApiKey: "test-api-key",
                Model: "claude-test-model",
                BaseUri: new Uri("https://anthropic.test")));

        var result = await suggester.SuggestAsync(WidthFacts(), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Plan);
        Assert.True(result.Validation.IsValid);
        var step = Assert.Single(result.Plan.Steps);
        Assert.Equal("Patio", step.GroupName);
        Assert.Equal("Width", step.AxisTag);
        Assert.Equal(1m, step.ReductionInches);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal(new Uri("https://anthropic.test/v1/messages"), handler.Request.RequestUri);
        Assert.True(handler.Request.Headers.TryGetValues("x-api-key", out var apiKeys));
        Assert.Equal("test-api-key", Assert.Single(apiKeys));
        Assert.True(handler.Request.Headers.TryGetValues("anthropic-version", out var versions));
        Assert.Equal("2023-06-01", Assert.Single(versions));
        Assert.Contains("claude-test-model", handler.Body, StringComparison.Ordinal);
        Assert.Contains("Patio", handler.Body, StringComparison.Ordinal);
        Assert.Contains("WidthInches", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SuggestAsync_returns_unavailable_without_calling_http_when_api_key_is_missing()
    {
        var handler = new CaptureHandler("{}");
        var suggester = new ClaudeAutoFitPlanSuggester(
            new HttpClient(handler),
            new ClaudeAutoFitPlanSuggesterOptions(
                ApiKey: "",
                Model: "claude-test-model",
                BaseUri: new Uri("https://anthropic.test")));

        var result = await suggester.SuggestAsync(WidthFacts(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(result.Plan);
        Assert.False(result.Validation.IsValid);
        Assert.Contains("ANTHROPIC_API_KEY", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SuggestAsync_rejects_claude_plan_that_over_trims()
    {
        var handler = new CaptureHandler(
            """
            {
              "content": [
                {
                  "type": "text",
                  "text": "{\"summary\":\"Over trim Patio.\",\"steps\":[{\"groupName\":\"Patio\",\"axisTag\":\"Width\",\"reductionInches\":1.25,\"reason\":\"Too much.\"}],\"explanation\":\"Bad fit.\"}"
                }
              ]
            }
            """);
        var suggester = new ClaudeAutoFitPlanSuggester(
            new HttpClient(handler),
            new ClaudeAutoFitPlanSuggesterOptions(
                ApiKey: "test-api-key",
                Model: "claude-test-model",
                BaseUri: new Uri("https://anthropic.test")));

        var result = await suggester.SuggestAsync(WidthFacts(), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Plan);
        Assert.False(result.Validation.IsValid);
        Assert.Contains(result.Validation.Errors, error => error.Contains("exact", StringComparison.OrdinalIgnoreCase));
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
            [new AutoFitCandidateGroupDto(Guid.NewGuid(), "Patio", "Width", 2m, 10m, 20m, 0)],
            []);

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
