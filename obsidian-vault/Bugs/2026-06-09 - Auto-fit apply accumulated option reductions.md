---
type: bug
date: 2026-06-09
topic: loop2-autofit-apply-baseline
---
# Auto-fit apply accumulated option reductions

## Symptom
In Adjust to Site Plan, clicking an auto-fit option applied its reduction correctly once, but clicking the same option again or switching to another option stacked the new reduction on top of the already-mutated preview.

Expected behavior: every Apply should first return to the pre-auto-fit preview baseline, then apply only the selected option. If the floor plan was manually moved first, that moved placement is the baseline.

## Root cause
`SitePlanAdjustmentViewModel.ApplyAutoFitPlan(...)` used `FloorPlanGeometryPaths`, `RoomLabels`, `OpeningLabels`, and `Dimensions` as its starting state. Those collections are the live preview collections, so after the first Apply they already contain compressed geometry.

That made Apply non-idempotent: spamming the same button kept shrinking the plan, and option switching produced mixed results from multiple options.

## Fix
- Added explicit auto-fit baseline collections in `SitePlanAdjustmentViewModel` for geometry, room labels, opening labels, and dimensions.
- Changed `ApplyAutoFitPlan(...)` to start from the baseline every time, while still accumulating multiple steps inside the selected option.
- Changed `MoveFloorPlanBy(...)` to translate both the live preview and the auto-fit baseline, so manual placement remains the user's new starting point for later option trials.

## Verification
- RED/GREEN test: reapplying the same right-side option keeps bounds at `0..99` instead of shrinking to `0..98`.
- RED/GREEN test: after moving the plan by `+10`, applying right then left resets to the moved baseline and ends at `11..110`, not `11..109`.
- Focused Desktop tests passed 84/84.
- Focused Application auto-fit/reactive tests passed 22/22.
- Focused Infrastructure OpenAI/Claude suggestion tests passed 6/6.
- `git diff --check` passed with LF-to-CRLF warnings only.

## Product impact
Loop 2 option cards are now true human-in-the-loop alternatives. The user can try option A, then option B, then option C without corrupting the preview by accumulating previous choices.
