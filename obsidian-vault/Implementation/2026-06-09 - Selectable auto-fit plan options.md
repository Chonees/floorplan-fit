---
type: implementation
date: 2026-06-09
topic: loop2-selectable-auto-fit-options
replaces: [[Inbox/2026-06-09 - Loop 2 multi-option human fit plans]]
---
# Selectable auto-fit plan options

Loop 2 / Adjust to Site Plan now treats OpenAI as a ranker/explainer over deterministic fit options instead of a single geometry authority.

## What changed
- Application added `AutoFitSuggestionOptionGenerator` to enumerate exact-fit options from deterministic facts.
- `AutoFitSuggestionPlanResponse` now can carry multiple `Plans` while preserving the previous single `Plan` shape.
- `IAutoFitPlanSuggester` now supports ranking a provided list of deterministic candidate plans.
- OpenAI prompt now receives `Facts` plus `CandidatePlans` and must return a `plans` array without changing groups, axes, or reductions.
- Desktop shows selectable option cards in Adjust to Site Plan.
- Applying an option is preview-only and compresses only the selected plan's pinch groups using the current markers and measured deficit edge.

## Product behavior
For a height deficit of 2 inches with `Ajuste 1` and `Ajuste 2`, the deterministic generator can offer options such as:
- `Ajuste 1 = 2"`
- `Ajuste 2 = 2"`
- `Ajuste 1 = 1" + Ajuste 2 = 1"`

The human chooses the preferred option, then Apply modifies only the listed groups in the preview.

## Verification
- Application focused auto-fit tests: 8/8 passed.
- Infrastructure focused OpenAI/Claude suggestion tests: 6/6 passed.
- Desktop focused SitePlanAdjustment/XAML/registration tests: 18/18 passed.
- `git diff --check` passed with LF-to-CRLF warnings only.

## Correction 2026-06-09
The earlier implementation note said Apply used the measured/global deficit edge. That detail is superseded for split options: Apply now infers each selected group's edge from its actual pinch marker position, with global deficit edge only as fallback.

Replaced by: [[Bugs/2026-06-09 - Auto-fit split options used global edge]]
