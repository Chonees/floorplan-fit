# 2026-06-09 - OpenAI suggestion called without candidate pinch groups

## Type
Bug

## Symptom
Adjust to Site Plan showed:

- `Deficit: Width 78.459"; Height 0". No candidate pinch groups are available for the required axes.`
- Then after pressing `Suggest Fit Plan (OpenAI)`: `OpenAI suggestion failed deterministic validation` because Width needed `78.459"` but the plan trimmed `0"`.

## Root cause
Desktop enabled/called the suggestion flow based only on `NeedsAdjustment`. That tells us the floor plan does not fit, but it does **not** prove a valid plan can exist.

A valid plan also requires enough candidate pinch capacity on every required axis. In this case Width needed `78.459"`, but candidate Width capacity was `0"`.

## Fix
- Added `AutoFitSuggestionFacts.HasRequiredCandidateCapacity`.
- Updated `SitePlanAdjustmentViewModel.CanSuggestAutoFitPlan` to require enough candidate capacity.
- Added a direct guard in `SuggestAutoFitPlanAsync` so programmatic calls also avoid the LLM when deterministic facts are impossible.
- Updated the initial status to say OpenAI, not stale Claude wording.

## Verification
- RED: `SuggestAutoFitPlanAsync_does_not_call_openai_when_required_axis_has_no_candidate_groups` failed because `CanSuggestAutoFitPlan` was true.
- GREEN: same test passed after the guard.
- Focused slices passed:
  - Application auto-fit: 6/6
  - OpenAI adapter: 3/3
  - Desktop site-plan/service/XAML: 16/16

## Remaining product action
For this specific site-plan case, the floor plan needs curated `Width` pinch groups with enough capacity, or the buildable/placement dimensions need correction if `78.459"` is not the intended deficit.
