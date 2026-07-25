---
type: implementation
status: complete-statically
date: 2026-07-21
project: FloorplanFit
area: Loop 1 commissioned-house setup
implements: "[[2026-07-20 - Replace pinches with parametric adaptation profiles]]"
---

# Productive publish commissions exact Auto-fit profile

## Product loop

The existing Review `Publish Curation` path is now the one-time commissioning seam. After `PublishFloorPlanCurationHandler` commits the draft, Desktop compiles the review-time Width/Height pinch metadata into the existing commissioned Width/Depth profile and persists it through `SaveCommissionedHouseAdaptationProfileHandler`.

The saved document carries the exact `FloorPlanVersionId + PublishedCurationId` pair. Daily Site fitting still reads only that persisted profile; it does not rediscover topology or fall back to Pinches V1/V2.

## Fail-closed behavior

- Missing exact version identity, unit context, Width/Depth coverage, explicit auxiliary evidence, safe closing-edge resolution, or compiler/readiness evidence leaves the newly published curation as `Setup required`.
- The Review status exposes one actionable `Publicado, pero no quedó Auto-fit ready: ...` reason.
- Zero or multiple safe closing-edge combinations are rejected instead of guessing.
- Curated fixed/protected geometry must retain one exact geometry-path binding; a multi-path object fails closed instead of being mislabeled as a source-only annotation.
- A successful save reports the commissioned Width and Depth capacities.

## Scope

- Reused the productive publish path; no commissioning wizard or daily decision was added.
- Reused the existing Application compiler and save handler.
- No SEMINOLE-specific production rule and no runtime Pinches fallback were introduced.
- Review XAML and code-behind did not need changes; the existing status surface already presents the result.

## Evidence

- Focused Desktop contracts cover persistence under the exact published curation identity, retained protected-geometry binding, and a visible fail-closed result when Depth cannot be commissioned.
- Static source checks prove publish precedes compile/save, compile/save precede refresh, DI already resolves the save handler, and no forbidden runtime Pinches/suggester symbol was added.
- `git diff --check` passes for the two touched implementation/test files.
- Repository policy forbids local `.NET` execution; executable verification remains external.

## Files

- `src/FloorplanFit.Desktop/ViewModels/FloorPlanReviewViewModel.cs`
- `tests/FloorplanFit.Desktop.Tests/ViewModels/FloorPlanReviewViewModelTests.cs`

## Related

- [[2026-07-20 - Finite goal for automatic site fitting UX]]
- [[2026-07-21 - Commissioned readiness allowed empty auxiliary coverage]]
