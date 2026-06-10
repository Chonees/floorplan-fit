# 2026-06-09 - Suggest button enabled with inline capacity explanation

## Type
Implementation

## Summary
Adjusted the previous no-capacity guard so the `Suggest Fit Plan (OpenAI)` button remains enabled, but clicking it does not call OpenAI when deterministic facts already prove no valid plan can exist.

## Why
The operator should be able to press Generate/Suggest and receive an explanation from the app. Disabling the button hides the reason and feels like a dead-end UX.

## Behavior
- Button enabled when there is a fit deficit and a suggester exists.
- If required Width/Height candidate capacity is missing, click shows inline explanation/warnings.
- OpenAI is not called in impossible deterministic cases.
- If capacity exists, OpenAI is called and the returned plan is still validator-gated.

## Files
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/SitePlanAdjustmentPreviewProjectorTests.cs`

## Verification
- RED: updated regression expected `CanSuggestAutoFitPlan == true` for missing candidates and failed because the button was disabled.
- GREEN: `CanSuggestAutoFitPlan` now depends only on deficit + suggester, while `SuggestAutoFitPlanAsync` guards the OpenAI call.
- Focused slices passed:
  - Application auto-fit: 6/6
  - OpenAI adapter: 3/3
  - Desktop site-plan/service/XAML: 16/16

## Replaces
Partially replaces `Bugs/2026-06-09 - OpenAI suggestion called without candidate pinch groups.md`: the no-LLM-call guard remains, but the button is no longer disabled.
