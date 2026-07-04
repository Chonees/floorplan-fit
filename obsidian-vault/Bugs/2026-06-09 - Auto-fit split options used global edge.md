---
type: bug
date: 2026-06-09
topic: loop2-auto-fit-apply-selected-options
replaces:
  - [[Implementation/2026-06-09 - Selectable auto-fit plan options]]
---
# Auto-fit split options used global edge

## Symptom
Clicking different Loop 2 fit options produced the right total reduction, but did not respect the selected distribution. A plan like `1" left + 1" right` could visually collapse into `2" from one side`; the same issue applied to bottom/top height splits.

## Root cause
`SitePlanAdjustmentViewModel.BuildCompressionTransform(...)` applied the selected plan steps, but resolved `AutoFitCompressionEdge` from the global envelope deficit only. When left/right or bottom/top deficits were tied, both selected steps used the same dominant fallback edge, so the selected split plan was not honored.

## Fix
Each apply step now infers the compression edge from the selected pinch group's actual marker location relative to the current geometry bounds:
- Width marker average left of center => `Left`; right of center => `Right`.
- Height marker average below center => `Bottom`; above center => `Top`.
- Ambiguous/no bounds still falls back to global deficit edge.

## Verification
- RED before fix: split Width and split Height tests failed because both steps compressed one edge.
- GREEN after fix: `ApplyAutoFitPlan_respects_split_option_groups_on_opposite_sides` and `ApplyAutoFitPlan_respects_split_option_groups_on_opposite_height_sides` passed.
- Focused Desktop tests passed 21/21.
- Focused Application auto-fit/reactive tests passed 22/22.
- Focused Infrastructure OpenAI/Claude suggestion tests passed 6/6.
- `git diff --check` passed with LF-to-CRLF warnings only.

## Impact
Loop 2 remains human-in-the-loop: OpenAI/deterministic generation may rank options, but applying a card now respects the actual selected pinch groups and their sides instead of re-deciding from a global deficit heuristic.
