# 2026-07-10 - ProjectedPlanSheetDxfExporter nullable decimal compile fix

## Status
Fixed.

## Problem
`dotnet watch` reported CS0173 in `ProjectedPlanSheetDxfExporter.cs` because four ternary expressions returned `null` on one branch and `decimal` on the other.

## Root cause
C# could not infer nullable decimal from `var` in these expressions:
- source width mismatch
- source height mismatch
- output width mismatch
- output height mismatch

## Fix
Changed the four local declarations to explicit `decimal?`.

## Verification
- `scripts/test-outline-congruence-contract.ps1` passed.
- `git diff --check` passed for the touched file.
- No `dotnet build/test/watch` was run by the agent per repo rules.
