---
type: inbox
date: 2026-06-09
topic: loop2-multi-option-human-fit
---
# Loop 2 needs multi-option human-in-the-loop fit plans

User clarified that the current single OpenAI suggestion is not enough. The desired UX is:

- Generate **N valid fit plans**, not one text-only plan.
- Name the affected pinch groups in each plan.
- Show the options in Adjust to Site Plan so the user can click the preferred one.
- Apply/reduce **only the selected groups** by the exact inches in that selected option.
- Keep deterministic geometry/fact validation as the authority; the LLM may rank/explain but must not invent geometry.

Current verified code state: `AutoFitSuggestionPlanResponse` has a singular `Plan`, `IAutoFitPlanSuggester.SuggestAsync` returns one response, `SitePlanAdjustmentViewModel.SuggestAutoFitPlanAsync` formats one string, and `SitePlanAdjustmentWindow.axaml` renders text blocks instead of selectable options.

## Superseded
Implemented by [[Implementation/2026-06-09 - Selectable auto-fit plan options]].
replaced_by: [[Implementation/2026-06-09 - Selectable auto-fit plan options]]

