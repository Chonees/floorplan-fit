---
type: bug
status: fixed-statically
date: 2026-07-21
project: FloorplanFit
area: Loop 1 commissioned opening hosts
replaces: ""
---

# Opening commissioning confuses geometry with wall host

## Root cause

`TryBuildCommissioningAuxiliaryBindings` currently takes the opening artifact's own `GeometryPathId + SegmentSortOrder` and stores that pair as though it were a wall host. Compiler validation reinforces the mistake: it accepts the auxiliary path and rejects any path already present in the accepted structural-wall set.

That is not host evidence. The opening's own display/replay geometry and the accepted wall segment that owns the opening are two different identities.

## Required correction

- Preserve the opening's own geometry identity when preview/replay needs it.
- Establish a separate, exact host identity only during one-time commissioning from accepted structural wall geometry.
- Exactly one supported wall segment may host an opening. Zero, duplicate, ambiguous, stale, crossing, or non-wall evidence leaves the house `Setup required` and later blocks planning/export.
- Persist the minimum host evidence in the existing commissioned-profile JSON; add no table and never infer by proximity in the daily path.
- Door/window size, swing/orientation, wall thickness, protected entities, and unrelated geometry remain immutable.

## Evidence

- `FloorPlanReviewViewModel.TryBuildCommissioningAuxiliaryBindings` lines 1010–1022 select `artifact.GeometryPathIds` as the alleged host.
- `CommissionExistingCurationProfileCompiler.TryValidateAuxiliaryBindings` lines 491–494 rejects an auxiliary path when it is an accepted structural path, proving the field does not currently represent a structural host.
- Goal microsteps `2.5–2.8` own the correction.

## Resolution — 2026-07-21

- The profile keeps the opening's own `GeometryPathId + SegmentSortOrder` for preview/replay and adds separate optional `HostGeometryPathId + HostSegmentSortOrder` fields for the accepted structural wall segment. The existing JSON repository requires no table or migration; older documents deserialize with null hosts and fail readiness.
- One-time productive commissioning now resolves exactly one collinear supporting accepted-wall segment after applying the opening artifact translation. Zero, multiple, crossing, missing, or nonrepresentable evidence leaves the published house `Setup required` with the opening reference and exact reason.
- Compiler and readiness validate distinct own/host identities, accepted structural membership, unique segment evidence, and action-role consistency. `CommissionedHouseFitPlanner.Plan` repeats the auxiliary-host guard before either rigid or adjusted output can be returned.
- Focused source contracts cover success plus missing, ambiguous, stale, non-wall, crossing, legacy JSON, and pre-output rejection. Scoped `git diff --check` is green; executable `.NET` proof remains reserved for the final external handoff.

## Contradictory evidence — 2026-07-21

The identity separation is correct, but the crossing claim was too broad. `TryResolveOpeningWallHost` returns success whenever it finds one collinear host, even if it also counted another accepted structural wall crossing the opening. The existing crossing fixture covers only zero valid hosts plus a crossing. Microstep `2.6` remains open until the mixed `one host + one crossing` case rejects explicitly before commissioning persists the profile.

## Contradictory branch resolved — 2026-07-21

Host resolution now succeeds only with exactly one collinear match and zero crossing accepted walls. A focused `SupportedAndCrossing` productive-publish fixture supplies one valid host plus one perpendicular accepted wall and proves that no profile is persisted, readiness remains `Setup required`, and the diagnostic names the opening and crossing. Scoped static inspection and `git diff --check` passed; executable proof remains external.

## Related

- [[Implementation/2026-07-20 - Finite goal for automatic site fitting UX#Phase 2 — close canonical Floor auxiliary identity and hosted-opening safety]]
- [[Bugs/2026-07-21 - Canonical Floor replay could not resolve auxiliary DXF roles]]
