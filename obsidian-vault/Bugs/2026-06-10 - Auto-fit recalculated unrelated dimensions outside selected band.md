---
type: bug
date: 2026-06-10
topic: loop2-autofit-band-scoped-reactive-dimensions
---
# Auto-fit recalculated unrelated same-axis dimensions outside the selected band

## Symptom
After applying a selectable auto-fit option, unrelated cotas on the same axis could turn red and show tiny fractional drift such as `9'-1/16"`, even though the chosen pinch group should not affect that measurement. In the reported case, choosing option 3 for the patio/`Ajuste 1` made a nearby unrelated vertical cota look changed while the expected fractional adjustment was not clear.

## Root cause
`DimensionIntervalReactiveProjector.Project(...)` only checked whether a bound dimension used the same axis as the active pinch band. If the cota was `Height` and the active group was also `Height`, the projector rebuilt the cota measurement even when the dimension's authored anchor interval did **not** overlap the selected articulation band.

That was too broad: same axis is necessary, but not sufficient. The dimension must also overlap the selected band's source/projected authored coordinate interval.

## Fix
- Same-axis dimensions now rebuild their measurement only when the authored anchor interval overlaps the active articulation band.
- Same-axis dimensions outside the selected band are translated only, preserving their visible number and avoiding false red highlights.
- The overlap check uses resolved authored anchor coordinates, not stale raw binding coordinates, so projected source geometry from Loop 2 remains compatible with older/unprojected measurement-node data.

## Verification
- RED/GREEN Application regression: a `Height` cota with authored span `100..208` and active band `300..340` previously recalculated from `108"` to `108.5"`; now it stays `9'-0"`.
- Desktop regression: applying a `0.5"` top auto-fit reduction still changes the preview geometry, but a bound dimension outside `Ajuste 1` is not added to `ChangedNumberDimensionIds`.
- Focused verification passed: Application Review tests `18/18`; Desktop SitePlanAdjustment/XAML tests `26/26`; `git diff --check` exited 0 with LF-to-CRLF warnings only.

## Product impact
Loop 2 fit options now respect the named pinch group as the authority. Cotas only turn red when their own authored interval actually participates in the selected group reduction, not merely because they share `Height` or `Width`.
