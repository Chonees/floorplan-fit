# 2026-06-09 - Claude bounded auto-fit suggestions

## Type
Implementation

## Summary
Implemented the first Loop 2 auto-fit suggestion slice: deterministic facts + deterministic validation + Claude as a bounded suggestion adapter + minimal Desktop UI.

## Product scope
- Loop 2: site plan adaptation.
- Purpose: answer ?does it fail by Width, Height, or both?? and ask Claude for a human-readable plan using named pinch groups.

## Architecture
- `Application`: owns fit facts, candidate group facts, plan DTOs, and validation.
- `Infrastructure`: owns the Anthropic/Claude HTTP adapter.
- `Desktop`: wires the service and displays a human-review suggestion panel in Adjust to Site Plan.

## Important behavior
- Claude receives structured facts and must return JSON.
- Deterministic code validates:
  - group exists by name
  - axis matches group axis
  - reduction is positive
  - group capacity is not exceeded
  - total reduction per axis is exact within tolerance
- Invalid Claude plans are displayed as failed validation; they are not applied.

## Configuration
- `ANTHROPIC_API_KEY`: required to call Claude.
- `FLOORPLANFIT_CLAUDE_MODEL`: optional model override.
- `ANTHROPIC_BASE_URL`: optional endpoint override for tests/proxies.

## Caveat
Current candidate capacity uses `ArticulationBandDto.MaxTrimMm`. Since `ArticulationBandProjector` currently sums marker capacity, a product rule like ?max 2 inches per group? needs a real group/band cap field or a capacity override strategy.

## Verification
- `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --filter FullyQualifiedName~AutoFitSuggestion --artifacts-path .testartifacts\auto-fit-final-application --nologo`
- `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter FullyQualifiedName~ClaudeAutoFitPlanSuggester --artifacts-path .testartifacts\auto-fit-final-infrastructure --nologo`
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~SitePlanAdjustmentPreviewProjectorTests|FullyQualifiedName~DesktopServiceRegistrationTests" --artifacts-path .testartifacts\auto-fit-final-desktop-2 --nologo`
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter FullyQualifiedName~AppXamlInitializationTests --artifacts-path .testartifacts\auto-fit-xaml --nologo`
