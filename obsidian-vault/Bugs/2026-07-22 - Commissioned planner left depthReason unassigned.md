---
type: bug
status: fixed
date: 2026-07-22
project: FloorplanFit
area: Loop 1 commissioned planner
---

# Commissioned planner left depthReason unassigned

## Root cause

External rebuild exposed `CS0165` in `CommissionedHouseFitPlanner.Plan(...)`: the combined
`!TryAllocate(Width, ..., out var widthReason) || !TryAllocate(Depth, ..., out var depthReason)`
condition short-circuits, so when Width allocation fails, `depthReason` is never assigned but the
rejection branch still read it. Same definite-assignment class as
[[2026-07-20 - DXF exporter left segmentIndex unassigned]].

## Fix

The condition is split into two sequential guards: Width allocates first and rejects with its own
reason (Depth is never attempted, matching the previous short-circuit order); Depth then allocates
and rejects with its own reason. Allocation order, emitted actions, and reported reasons are
behavior-identical.

## Evidence

- First `dotnet watch` rebuild after the phase 3-8 static closure reported exactly one error; the
  fix removes it without touching allocation semantics.
- A repository-wide sweep of `out var` short-circuit patterns found no other site that reads the
  second operand's out-variable inside the failure body; the remaining hits only use their
  out-variables where definite assignment is guaranteed.
- Compile/test proof continues externally via `scripts/run-commissioned-autofit-proof.ps1`.

## Files

- `src/FloorplanFit.Application/FloorPlans/SitePlanAdjustment/CommissionedHouseFitPlanner.cs`
