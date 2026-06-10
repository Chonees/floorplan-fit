namespace FloorplanFit.Infrastructure.OpenAi;

public sealed record OpenAiAutoFitPlanSuggesterOptions(
    string? ApiKey,
    string? Model,
    Uri? BaseUri)
{
    public const string DefaultModel = "gpt-4.1";

    public string ResolvedModel =>
        string.IsNullOrWhiteSpace(Model)
            ? DefaultModel
            : Model.Trim();

    public Uri ResolvedBaseUri => BaseUri ?? new Uri("https://api.openai.com");

    public static OpenAiAutoFitPlanSuggesterOptions FromEnvironment()
        => new(
            Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
            Environment.GetEnvironmentVariable("FLOORPLANFIT_OPENAI_MODEL"),
            ResolveBaseUri(Environment.GetEnvironmentVariable("OPENAI_BASE_URL")));

    private static Uri? ResolveBaseUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri
            : null;
}

