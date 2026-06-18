# 2026-06-16 - OpenAI no-internet suggestion fallback

## Type
Bugfix

## Current truth
- Clicking **Sugerir** in Adjust to Site Plan no longer crashes the desktop app when `api.openai.com` cannot be resolved or the OpenAI HTTP request fails before a response exists.
- The OpenAI adapter returns an unsuccessful suggestion result instead of leaking `HttpRequestException` to Avalonia.
- `SitePlanAdjustmentViewModel.SuggestAutoFitPlanAsync` can then use the existing deterministic fallback path and show deterministic fit options instead of killing `dotnet watch`.

## Root cause
- `OpenAiAutoFitPlanSuggester.SuggestAsync` awaited `httpClient.SendAsync(...)` without catching network transport exceptions.
- With no internet/DNS, .NET threw `HttpRequestException: No such host is known. (api.openai.com:443)`.
- That exception escaped the Infrastructure adapter, bubbled through the Desktop command, and reached Avalonia's dispatcher.

## Fix
- Wrapped `httpClient.SendAsync(...)` in a `try/catch (HttpRequestException)`.
- On network failure, the adapter now returns `Succeeded = false` with a diagnostic message instead of throwing.
- Normal HTTP responses still flow through the existing response-body handling.

## Product scope
- Loop 2: Adjust to Site Plan.
- Architecture layer: Infrastructure, because the OpenAI adapter owns external HTTP failure translation.
- Desktop behavior benefits from the existing fallback branch; no UI-specific retry system was added.

## Files changed
- `src/FloorplanFit.Infrastructure/OpenAi/OpenAiAutoFitPlanSuggester.cs`
- `tests/FloorplanFit.Infrastructure.Tests/OpenAi/OpenAiAutoFitPlanSuggesterTests.cs`

## Verification
- RED:
  - `dotnet test .\tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~SuggestAsync_returns_unavailable_when_openai_network_request_fails" --artifacts-path .\.testartifacts\openai-network-red`
  - Failed with unhandled `HttpRequestException`.
- GREEN:
  - Same focused test passed 1/1 after the fix.
- Regression:
  - `OpenAiAutoFitPlanSuggesterTests` passed 4/4.
  - Desktop `SuggestAutoFitPlanAsync` tests passed 3/3.
  - `git diff --check` exited 0 with LF-to-CRLF warnings only.

## Tradeoff
The fix deliberately treats no-internet as an unavailable AI ranker, not as an app-level fatal error. A future enhancement could surface the raw connectivity reason in the UI, but the geometry authority remains deterministic candidate generation.
