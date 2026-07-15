---
date: 2026-07-14
status: implemented-static-review-complete
loop: Loop 2
layer: Infrastructure
---

# Connected frame-pair registration root fix

## Root cause

The real SEMINOLE failure was **independent local axis/frame selection**, not missing DXF entity support.  Independent frame choices could look individually plausible while describing incompatible local coordinate systems.

## Architectural correction

- The selector now constructs **only connected four-corner frames**.
- Registration evaluates **canonical × electrical connected-frame pairs**.
- It rejects nonuniform-dimension fits before the expensive geometric proof.
- A transform is authorized only when there is **exactly one tolerance-distinct, evidence-complete** candidate. Zero candidates or multiple candidates require manual review.

This deliberately adds no tolerance relaxation, raw-bbox global scaling, or SEMINOLE-specific branch.

## Scope

- **Product loop:** Loop 2
- **Architecture layer:** Infrastructure
- **Affected source files:** the connected-frame selector and registration (`Register`) pipeline.
- **Affected test files:** selector connected-four-corner coverage and registration coverage for nonuniform-fit pruning and zero/ambiguous candidate review.

## Verification status

Static review found no blockers. Actual build/test execution and runtime proof through `Register` remain pending.
