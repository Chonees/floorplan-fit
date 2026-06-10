---
type: decision
project: floorplan-fit
date: 2026-06-08
topic_key: architecture/llm-assisted-fit-suggestions
status: active
---

# LLM suggests fit plans from deterministic facts

## Decision

Loop 2 auto-fit should use an LLM as a suggestion/explanation layer over deterministic fit facts, not as the raw geometry solver.

## Why

The user wants the system to suggest plans using the information it receives: deficits, named pinch groups, capacities, axes, and related dimensions. That is valid, but CAD geometry and exact inches must remain deterministic, auditable, and testable.

## Recommended split

### Deterministic code computes facts

- current floor-plan bounds vs setback/buildable bounds
- width deficit and height deficit
- compatible pinch groups by axis
- group/band capacity in inches
- existing manual-verified dimension bindings
- whether enough capacity exists
- exact geometry transforms available if a plan is applied

### LLM suggests/ranks/explains

The LLM receives only structured facts, for example:

```json
{
  "deficits": { "heightInches": 2, "widthInches": 0 },
  "groups": [
    { "name": "H-01 Pasillo central", "axis": "Height", "capacityInches": 2, "affectedDimensions": 2 },
    { "name": "H-02 Dormitorios", "axis": "Height", "capacityInches": 2, "affectedDimensions": 1 }
  ],
  "rules": [
    "Do not exceed capacity",
    "Use only matching-axis groups",
    "Prefer smallest total adjustment",
    "Return a human-in-the-loop plan"
  ]
}
```

It returns a constrained plan:

```json
{
  "summary": "Falta reducir 2 inches de alto.",
  "steps": [
    { "groupName": "H-01 Pasillo central", "axis": "Height", "reductionInches": 1 },
    { "groupName": "H-02 Dormitorios", "axis": "Height", "reductionInches": 1 }
  ],
  "explanation": "Repart? la reducci?n entre dos grupos Height para no concentrar toda la deformaci?n en una zona."
}
```

### Deterministic validator gates application

Before Apply, code validates:

- every group exists
- every group axis matches the requested deficit axis
- total reduction covers the deficit but does not overshoot beyond tolerance
- no group exceeds capacity
- dimensions affected are known/manual-verified when dimension updates are expected

If validation fails, the app rejects or repairs the LLM suggestion.

## Product UX

The UI should be human-in-the-loop:

1. Detect mismatch.
2. Show facts.
3. Ask LLM for a named suggestion.
4. Validate the suggestion.
5. Show the plan to the user.
6. User clicks Apply.
7. Deterministic code applies geometry/dimension updates.

## Important caveat

Current code facts verified earlier: group names and axis exist, but capacity is marker-based (`PinchMarkerDto.MaxTrimMm`) and `ArticulationBandProjector` sums marker capacity. If product semantics are "max N inches per group", add explicit group/band capacity semantics or resolve capacity carefully before involving the LLM.
