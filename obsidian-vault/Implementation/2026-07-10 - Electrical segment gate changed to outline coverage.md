# 2026-07-10 - Electrical segment gate changed to outline coverage

## Type
Implementation / Audit refinement

## Status
Implemented; fresh runtime export pending

## Evidence from ULTIMO TEST 10
- Manifest: `f2df4b2006c04164afd03b5a009e2755/manifest.json`
- Verifier failure: `SegmentMismatchRequiresManualReview`, `missing=22`, `extra=116`.
- Audit mode before the fix: `StructuralWallCenterlineRuns`.
- Counts showed FloorPlan `92` wall runs and Electrical `183` wall runs after centerline normalization.

## Root cause
The segment gate was still too broad. It compared internal/drafting wall-run differences as if they were outline failures. That is not a safe automatic blocker for dependent Electrical sheets because the Electrical drawing can fragment or add wall linework differently while still carrying the same adjusted outline.

## Change
- Segment gate now uses `StructuralOutlineCoverageWithWallRunAdvisory`.
- Blocking condition: required canonical FloorPlan outline wall runs must be covered by Electrical after bbox-normalized comparison.
- Advisory condition: internal missing/extra wall runs are still counted and sampled, but do not block automatic proof by themselves.

## Verification
Allowed checks passed without running dotnet build/test/watch:
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched files

## Next
Generate a fresh Desktop export after this change, then run:
`powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic`
