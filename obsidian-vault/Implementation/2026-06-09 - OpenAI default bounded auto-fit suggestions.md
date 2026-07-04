# 2026-06-09 - OpenAI default bounded auto-fit suggestions

## Type
Implementation

## Summary
Switched the Loop 2 auto-fit LLM suggestion provider from Claude to OpenAI while keeping the deterministic fact builder and validator unchanged.

## Product scope
- Loop 2: site plan adaptation.
- Purpose: use OpenAI to suggest a human-readable fit plan from deterministic facts.

## Architecture
- `Application`: unchanged provider-agnostic `IAutoFitPlanSuggester` port plus auto-fit facts/validator.
- `Infrastructure`: new `OpenAiAutoFitPlanSuggester` adapter using OpenAI Responses API.
- `Desktop`: now registers OpenAI as the default `IAutoFitPlanSuggester` and labels the UI as OpenAI.

## Important behavior
- OpenAI receives the same structured facts used by Claude.
- OpenAI must return a JSON object with `summary`, `steps`, and `explanation`.
- The deterministic validator still rejects hallucinated group names, wrong axes, over-capacity reductions, and non-exact reductions.

## Configuration
- `OPENAI_API_KEY`: required.
- `FLOORPLANFIT_OPENAI_MODEL`: optional model override.
- `OPENAI_BASE_URL`: optional endpoint override for tests/proxies.
- Default model: `gpt-4.1`.

## Verification
- External key/auth probe: `/v1/models` returned HTTP 200, and `/v1/responses` with `gpt-4.1` returned HTTP 200.
- `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter FullyQualifiedName~OpenAiAutoFitPlanSuggester --artifacts-path .testartifacts\openai-final-infrastructure --nologo`
- `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter FullyQualifiedName~AutoFitSuggestion --artifacts-path .testartifacts\openai-final-application --nologo`
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~SitePlanAdjustmentPreviewProjectorTests|FullyQualifiedName~DesktopServiceRegistrationTests|FullyQualifiedName~AppXamlInitializationTests" --artifacts-path .testartifacts\openai-final-desktop --nologo`

## Security note
The OpenAI key was pasted into chat for testing. Treat it as exposed: rotate it and set the rotated value as `OPENAI_API_KEY`.
