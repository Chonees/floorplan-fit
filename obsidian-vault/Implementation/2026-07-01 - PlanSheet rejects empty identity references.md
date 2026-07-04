# PlanSheet rejects empty identity references

Date: 2026-07-01
Type: Implementation
Status: Current

## What changed
- `PlanSheet` now rejects empty ids for the sheet id, plan-set version id, imported document id, and measurement context id.
- Added `PlanSheetTests.Constructor_rejects_empty_identity_references` to document the invariant.

## Why
A dependent sheet is the core relation point between HousePlanSet import, registration, projection, and export. Empty identity references would let invalid sheets enter the plan-set graph and break downstream registration/projection trust.

## Boundary
- No new service, repository, or abstraction.
- No change to registration/projection math.
- No agent-run build/test because this repository explicitly forbids build after changes.

## Verification
- Ran scoped `git diff --check`; exit code 0, only CRLF warnings.
- Ran `rg` for the new invariant messages and test symbol.

## Related
- [[2026-07-01 - HousePlanSet design spec updated to current export loop]]
