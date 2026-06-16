---
type: bug
project: floorplan-fit
date: 2026-06-11
topic_key: bugs/autofit-placement-overflow-vs-size-deficit
status: fixed-verified
---

# 2026-06-11 - Auto-fit counted placement overflow as trim deficit

## User-observed symptom

In the Loop 2 setback preview, the floor plan can appear to miss the buildable area by about `2"` on the right side while having enough slack on the opposite side. The operator correctly reads this as: the plan is not centered / width is not being evaluated as a total fit problem, so the app should not blindly ask for a `2"` width trim.

## Evidence from code inspection

`AutoFitSuggestionFactBuilder.BuildDeficit(...)` currently computes width deficit like this:

- `left = max(0, buildable.MinX - bounds.MinX)`
- `right = max(0, bounds.MaxX - buildable.MaxX)`
- `WidthInches = left + right`

That means the fact builder is using **current-position side overflow** as if it were **minimum required size reduction**.

If the footprint width is equal to or smaller than the buildable width, but the plan is shifted right by `2"`, the current code still reports `WidthInches = 2"` because `right = 2"`. In reality, size deficit should be `max(0, footprintWidth - buildableWidth) = 0"`; the required action is placement/recentering, not trimming.

## Related centering suspicion

`SitePlanAdjustmentPreviewProjector.Project(...)` centers by selected structural placement geometry ids and `SitePlanBuildableAreaDto.CenterX/CenterY`. If the visible preview is not centered, the next verification must determine which bound is wrong:

1. detected buildable bbox does not match the yellow setback rectangle;
2. structural placement bbox does not match the real footprint;
3. initial projection centers one bbox while auto-fit computes facts from another bbox;
4. manual move changes preview/baseline geometry but `autoFitSuggestionFacts` remains the constructor-time readonly facts.

## High-confidence hypothesis

The primary bug category is not the LLM and not the option applier. It is the deterministic geometry facts layer: the app conflates **side overflow at the current placement** with **total size deficit after correct centering**.

## Verification plan before any fix

1. Add or run a diagnostic dump for the current preview:
   - buildable bbox + width/height;
   - projected structural bbox + width/height;
   - full rendered bbox + width/height;
   - center delta X/Y;
   - left/right/top/bottom overflow;
   - left/right/top/bottom slack;
   - size deficit per axis: `max(0, floorSpan - buildableSpan)`.
2. Reproduce with the fixture shown by the user, especially the case whose text says footprint width equals setback/buildable width and only total height/largo is reduced.
3. Add a failing Application test where a floor plan has equal width to buildable area but is shifted right by `2"`; expected width trim deficit is `0"`, not `2"`.
4. Add/confirm a Desktop projector test that initial projection centers the structural bbox exactly in X for `483.786"` footprint width inside `483.786"` buildable width.
5. Only after those tests prove the failing layer, implement the smallest correction.

## Fix applied

`AutoFitSuggestionFactBuilder.BuildDeficit(...)` now computes width deficit from total span:

- `buildableWidth = buildable.MaxX - buildable.MinX`
- `floorWidth = bounds.MaxX - bounds.MinX`
- `widthDeficit = max(0, floorWidth - buildableWidth)`

The width side fields are derived from that real width excess and split evenly, so shifted placement no longer masquerades as required trimming.

Height was deliberately left on the existing vertical-overflow behavior because the user confirmed the height path works correctly.

## Verification

- RED: `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .testartifacts\width-deficit-red --filter "FullyQualifiedName~AutoFitSuggestionFactBuilderTests"` failed as expected: shifted-right width-fit case expected `0"` and got `2"`.
- GREEN: `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .testartifacts\width-deficit-green --filter "FullyQualifiedName~AutoFitSuggestionFactBuilderTests"` passed 4/4.
- Broader Application slice: `dotnet test tests\FloorplanFit.Application.Tests\FloorplanFit.Application.Tests.csproj --artifacts-path .testartifacts\width-deficit-app-final --filter "FullyQualifiedName~AutoFitSuggestion"` passed 10/10.
- Desktop projector slice: `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --artifacts-path .testartifacts\width-deficit-desktop --filter "FullyQualifiedName~SitePlanAdjustmentPreviewProjectorTests"` passed 20/20.
- `git diff --check -- src/FloorplanFit.Application/FloorPlans/SitePlanAdjustment/AutoFitSuggestionFactBuilder.cs tests/FloorplanFit.Application.Tests/FloorPlans/SitePlanAdjustment/AutoFitSuggestionFactBuilderTests.cs tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs` exited 0 with LF-to-CRLF warnings only.
