# 2026-07-10 - PlacementJson deserialization crashed Electrical package export

## Status
Fixed.

## Problem
Fresh `ULTIMATE TEST 5` export wrote the canonical FloorPlan DXF, then crashed before generating the dependent Electrical DXF/package folder.

## Runtime error
`System.NotSupportedException`: System.Text.Json could not deserialize `AdjustedSitePlanPlacementDto` because the record has multiple parameterized constructors and no `[JsonConstructor]` disambiguation.

## Root cause
`ExportProjectedPlanSheetHandler.BuildExportRecipeAsync` deserialized the full placement record only to read `InputAudit.OriginalWidthInches` and `InputAudit.OriginalHeightInches` for outline congruence.

## Fix
Replaced full DTO deserialization with a small JSON reader that extracts only the two original input dimensions from `PlacementJson`.

## Verification
- `scripts/test-outline-congruence-contract.ps1` passed.
- `scripts/test-plan-set-human-summary-contract.ps1` passed.
- `git diff --check` passed for touched files.
- No `dotnet build/test/watch` was run by the agent per repo rules.
