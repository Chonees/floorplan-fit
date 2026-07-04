namespace FloorplanFit.Infrastructure.Claude;

public sealed record ClaudeAutoFitPlanSuggesterOptions(
    string? ApiKey,
    string? Model,
    Uri? BaseUri)
{
    public const string DefaultModel = "claude-sonnet-4-5";

    public string ResolvedModel =>
        string.IsNullOrWhiteSpace(Model)
            ? DefaultModel
            : Model.Trim();

    public Uri ResolvedBaseUri => BaseUri ?? new Uri("https://api.anthropic.com");

    public static ClaudeAutoFitPlanSuggesterOptions FromEnvironment()
        => new(
            Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"),
            Environment.GetEnvironmentVariable("FLOORPLANFIT_CLAUDE_MODEL"),
            ResolveBaseUri(Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL")));

    private static Uri? ResolveBaseUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : null;
}

