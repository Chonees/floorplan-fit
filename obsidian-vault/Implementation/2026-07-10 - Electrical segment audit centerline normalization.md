# 2026-07-10 - Electrical segment audit centerline normalization

## Type
Bug / Implementation

## Status
Implemented; fresh runtime export pending

## Context
`ULTIMO TEST 9` generated the new `outline-segment-congruence-audit.json` and failed the automatic verifier with `SegmentMismatchRequiresManualReview`.

## Evidence
- Manifest: `03ee564a6d4a477c92badd8307ed4305/manifest.json`
- Verifier failure: structural segment congruence required; audit reported `missing=20 extra=20` samples.
- Audit details showed raw drafting-line mismatch, not necessarily recipe failure: `FloorSegmentCount=125`, `ElectricalSegmentCount=282`, many nearest candidates differed by small wall-edge offsets/fragments.

## Root cause
The first segment audit compared raw wall edge linework. Electrical sheets can draw the same wall with different edge offsets, duplicated fragments, or layer-specific wall drafting, so raw edge equality creates false positives.

## Change
- `PlanSetOutlineSegmentCongruenceAuditBuilder` now compares structural wall centerline runs instead of raw wall edges.
- It normalizes paired parallel wall edges into centerlines, merges collinear fragments, and uses a centerline tolerance for the comparison.
- It now stores total missing/extra counts while limiting `Mismatches` to a readable sample.

## Verification
Repo-allowed checks passed, without running dotnet build/test/watch:
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched files

## Next
Create a fresh Desktop export after this code change, then run:
`powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic`
