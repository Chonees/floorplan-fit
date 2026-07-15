# 2026-07-08 - Anti-loop goal prompt for HousePlanSet observability finish

## Type
Decision

## Context
We need a finite goal prompt for the next agent loop to finish the FloorPlan -> Electrical HousePlanSet observability/projection work without infinite looping or scope creep.

## Decision
Use a goal prompt with explicit scope, non-negotiables, phased work, proof gates, stop rules, and blocker rules.

## Core stop rule
The loop stops when the Definition of Done is proven with artifacts, not when the agent feels done. It blocks only after the same blocker repeats for 3 consecutive attempts with evidence.

## Scope
- Finish FloorPlan + Electrical only.
- FloorPlan remains canonical.
- Electrical receives canonical placement/recipe; it must not solve independently.
- Preserve DXF validity and never silently deform unsupported geometry.
- Produce observable audit files and a clear verifier result.

## Out of scope
- RoofPlan, Facade/Elevation implementation.
- Dashboards/OpenTelemetry.
- Rewriting the whole app.
- New independent Electrical adjustment engine.
