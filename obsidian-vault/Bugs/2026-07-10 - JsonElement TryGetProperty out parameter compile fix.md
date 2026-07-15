# 2026-07-10 - JsonElement TryGetProperty out parameter compile fix

## Status
Fixed.

## Problem
`dotnet watch` reported CS0177 in `ExportProjectedPlanSheetHandler.cs`: out parameter `value` was not definitely assigned before method exit.

## Root cause
The expression-bodied helper used short-circuit `&&` with `JsonElement.TryGetProperty`; when the left side was false, C# could not prove the out parameter was assigned.

## Fix
Changed the helper to a block body that assigns `value = default` before returning false.

## Verification
- `scripts/test-outline-congruence-contract.ps1` passed.
- `git diff --check` passed for the touched file.
- No dotnet build/test/watch was run by the agent per repo rules.
