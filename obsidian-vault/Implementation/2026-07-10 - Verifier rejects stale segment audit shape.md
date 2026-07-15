# 2026-07-10 - Verifier rejects stale segment audit shape

## Type
Implementation / Verifier hardening

## Status
Implemented; fresh runtime export pending

## Change
The manifest verifier now rejects segment audit artifacts that predate the outline-coverage gate. It requires:
- `ComparisonMode`
- `RequiredOutlineSegmentCount`
- `AdvisoryMissingInternalWallRunCount`
- `AdvisoryExtraElectricalWallRunCount`

It also rejects any comparison mode other than `StructuralOutlineCoverageWithWallRunAdvisory` and rejects audits that detect zero required outline segments.

## Why
After ULTIMO TEST 10, the segment gate was changed from all-wall-run equality to required outline coverage plus advisory internal differences. A stale audit with the old mode must not be allowed as final proof.

## Verification
Allowed checks passed without dotnet build/test/watch:
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched verifier/test files

## Runtime classification
Current latest runtime manifest `f2df4b2006c04164afd03b5a009e2755` fails as expected because its `outline-segment-congruence-audit.json` lacks `RequiredOutlineSegmentCount`. A fresh export is required.
