---
type: experiment
date: 2026-06-16
topic: ponytail-simplicity-guard
status: analysis
source:
  - https://github.com/DietrichGebert/ponytail
  - https://github.com/DietrichGebert/ponytail/blob/main/AGENTS.md
  - https://github.com/DietrichGebert/ponytail/blob/main/docs/agent-portability.md
---

# Ponytail as simplicity guard for Floorplan Fit

## Context

The user asked how to leverage `DietrichGebert/ponytail` to build this repository more solidly. Ponytail is not a geometry/DXF library; it is an agent workflow and instruction pack that pushes agents toward YAGNI, standard-library/native features, already-installed dependencies, shortest correct diffs, and explicit deferred-shortcut markers.

## Fit for this repo

Use Ponytail as a **simplicity guard**, not as a replacement for the existing architecture or strict TDD workflow.

Recommended application:

1. Add a pre-implementation question ladder to project work:
   - Does this need to exist?
   - Can the current module do it safely?
   - Can .NET/Avalonia/IxMilia/SQLite already do it?
   - Can one pure function plus one focused test prove it?
2. Run an over-engineering review pass after significant changes:
   - delete dead flexibility,
   - avoid single-use factories/strategies,
   - avoid new dependencies for geometry/rendering unless existing primitives fail with evidence.
3. Keep strict TDD for non-trivial geometry and export logic:
   - Ponytail’s “one runnable check” aligns with this repo’s TDD discipline, but this repo needs richer checks where DXF geometry/export correctness is high-risk.
4. Track intentional shortcuts with a comment/ledger pattern only when a shortcut has a named ceiling and upgrade trigger.

## Boundaries

- Do not blindly delete Application ports just because they have one Infrastructure implementation; in this repo many single-implementation interfaces are legitimate hexagonal boundaries between Application and Infrastructure.
- Do not use Ponytail as an excuse to collapse domain modules back into `SitePlanAdjustmentViewModel`.
- Do not reduce tests around adjustment, cota highlighting, export, or unit conversion; those are product-critical correctness surfaces.
- Do not install new simplification tooling unless the workflow benefit beats the additional moving part.

## Best first use

Use a manual `ponytail-review` style pass on the current Adjust-to-Site-Plan diff before the next extraction:

- keep `SitePlanAdjustmentViewModel` as orchestration/state,
- extract only pure, named behavior that is independently testable,
- avoid interfaces until a second implementation appears,
- keep all non-trivial extraction guarded by focused red/green tests.

