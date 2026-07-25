---
type: bug
status: fixed-statically
date: 2026-07-21
project: FloorplanFit
area: Shared commissioned-house readiness
replaces: "[[Current State#2026-07-21 - Auto-fit execution paused with unfinished critical path]]"
---

# Commissioned readiness allowed empty auxiliary coverage

## Root cause

The compiler consumed `AuxiliaryEntityBindings`, but the commissioned profile persisted only flattened immutable/protected reference sets. Readiness therefore had no expected auxiliary inventory to reconcile and could report green when the input list was empty or when expected roles disappeared later.

## Fix

The profile now persists the existing explicit binding records. Compiler and readiness both reject empty coverage. Readiness reconciles that inventory with immutable/protected sets and each action's `Fixed`/`RigidMove` role; openings require immutable-size plus path/segment identity. Missing, duplicate, stale, contradictory, incomplete, or undeclared source-only evidence fails closed.

## Scope

- Generic commissioned profiles only; no SEMINOLE hardcoding.
- No BIM/topology discovery was introduced.
- Width/depth capacity and allocation behavior are unchanged.

## Evidence

- Focused RED-by-inspection contracts cover empty inventory, missing/duplicate roles, stale/incomplete openings, contradictory flags, protected semantics, and untracked source-only roles.
- Static acceptance search and `git diff --check`/untracked-file whitespace checks pass.
- Repository policy forbids local `.NET` execution; compile and test execution remain external.

## Files

- `src/FloorplanFit.Application/FloorPlans/SitePlanAdjustment/CommissionExistingCurationProfileCompiler.cs`
- `src/FloorplanFit.Application/FloorPlans/SitePlanAdjustment/CommissionedHouseFitPlanner.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/SitePlanAdjustment/CommissionExistingCurationProfileCompilerTests.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/SitePlanAdjustment/CommissionedHouseAdaptationProfileReadinessTests.cs`
- `tests/FloorplanFit.Application.Tests/FloorPlans/SitePlanAdjustment/CommissionedHouseAdaptationProfileHandlerTests.cs`
