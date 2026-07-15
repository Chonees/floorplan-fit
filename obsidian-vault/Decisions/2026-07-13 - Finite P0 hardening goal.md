# 2026-07-13 - Finite P0 hardening goal

## Type
Decision

## Context
The whole-app audit found multiple P0 integrity and trust blockers. Trying to combine those blockers, all internal modularization, Desktop cleanup, performance work, and Roof/Facade implementation in one goal would create an unbounded loop.

## Decision
The active goal closes only P0 data safety and HousePlanSet trust. P1/P2 modularization remains backlog for later goals.

## Phases
0. Baseline and impact matrix.
1. Durable data, safe migrations, SQLite policy, stable user-data root, and backup.
2. Canonical identity, aggregate ownership, historical repair, delete guard, and cleanup ordering.
3. Honest capabilities: Electrical remains recipe-aware; unsupported Roof/Facade compression cannot become ready.
4. Typed in-product verification before `ReadyForExport`.
5. Atomic package publication with manifest last.
6. Allowed checks, one external runtime action if needed, documentation, and closure evidence.

## Definition of Done
- Unknown legacy schema cannot silently delete curation data.
- Managed data is no longer newly written beside the executable.
- Sheet registrations reference the real canonical FloorPlan version.
- Referenced canonical versions cannot be deleted.
- Unsupported Roof/Facade projections cannot be promoted to ready.
- `ReadyForExport` derives from a typed final verification report.
- Partial packages cannot look complete.
- FloorPlan + Electrical proven behavior is preserved.
- No build/runtime claim is made without user/CI evidence.

## Anti-loop rules
- Maximum two attempts with one hypothesis; a third needs new evidence.
- Maximum three hypotheses per gate; then stop editing if external state is required.
- Ask for a runtime action once; later turns only check whether new evidence appeared.
- Mark blocked only after the same blocker repeats for three consecutive goal turns.
- No P1/P2 side work, new dependency, new project, speculative abstraction, or plan-specific hardcode.
- Complete only from gate evidence, never from token use or subjective confidence.

## Constraints
- Preserve the dirty worktree and inspect existing diffs before touching target files.
- Never reset, stash, checkout, delete, commit, or push without explicit instruction.
- Do not run `dotnet build`, `dotnet test`, or `dotnet watch`.
- Use the smallest regression check before each non-trivial production change.
- Obsidian remains the primary durable record; Engram is shadow persistence.

## Related
- [[2026-07-13 - Whole app robustness and modularization audit]]
- [[Current State]]

