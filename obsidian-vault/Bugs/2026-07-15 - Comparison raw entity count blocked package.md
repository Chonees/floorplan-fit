---
type: bug
created: 2026-07-15
status: fixed-uncompiled
---

# Comparison raw entity count blocked package

## Runtime evidence

- Export `587dc072-2eb6-45a9-8549-4bd6884cc1f1` at `2026-07-15T14:41:59Z` exported canonical `D:\PointAIData\test adjust\4.dxf`.
- Atomic package publication correctly withheld `4-plan-set` after comparison composition reported 6,655 reloaded model entities versus a raw expectation of 7,022, while block count increased from 707 expected to 709 parsed.
- The failure occurred only in the review-only comparison artifact; individual Floor/Electrical technical export and audit behavior was not changed.

## Root cause

`PlanSetComparisonDxfComposer` treated pre-normalization entity and block counts as a post-save/reload integrity invariant. IxMilia `Normalize`/`Save`/`Load` can change those representation counts without losing either review side, so a valid comparison was falsely rejected.

## Fix

Post-reload validation now proves the comparison semantically instead of comparing raw representation counts:

- at least one visible, non-paper-space entity with finite, nondegenerate bounds remains on each `FLOOR__` and `ELECTRICAL__` source side;
- an `INSERT` counts only when its referenced block exists and resolves to finite, nondegenerate bounds;
- each review frame contains exactly four model-space lines;
- both expected review labels remain on the review label layer;
- successful `DxfFile.Load` remains the parse proof before semantic validation.

A focused validator regression preserves the lower-count valid case with a resolved geometric `INSERT`, then rejects default/zero-length, invisible, paper-space, blockless, and unresolved source entities.

## Verification

- IxMilia 0.8.4 members and constructors used by the regression were inspected from the installed assembly.
- Per task constraint, no build, restore, or test command was run. Compilation and runtime test status remain unverified.
