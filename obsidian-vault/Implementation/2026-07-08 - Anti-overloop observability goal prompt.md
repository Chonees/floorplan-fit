---
type: Implementation
date: 2026-07-08
replaces: []
replaced_by: null
---

# Anti-overloop observability goal prompt

## Context
The HousePlanSet FloorPlan -> ElectricalPlan work needs finite goal prompts. The user wants the agent to keep progressing step by step, but stop instead of looping forever.

## Rule
A valid goal must define:
- closed scope: FloorPlan + ElectricalPlan only;
- explicit known-done vs remaining work;
- concrete artifacts/checks;
- a per-iteration hypothesis/change/check ledger;
- stop states: complete, blocked, or needs fresh user export;
- no repeated check without a new change or new evidence.

## Acceptance gates
- Audit artifacts exist in the plan-set package.
- Canonical operations are indexed and covered exactly once by FloorPlan and Electrical audit rows.
- DXF safety audit proves the projected Electrical DXF is present and structurally safe.
- Human summary explains what changed, what did not change, and why.
- Allowed script checks pass; fresh runtime export is required before claiming full end-to-end proof.

## Anti-loop stop rule
If the same failure appears twice with no new evidence or changed file, stop and report the blocker instead of continuing. If a fresh Desktop export is needed, stop and ask for that export rather than modifying unrelated code.
