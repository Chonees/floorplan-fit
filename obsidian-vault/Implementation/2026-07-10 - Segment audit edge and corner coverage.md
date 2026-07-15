# 2026-07-10 - Segment audit edge and corner coverage

## Type
Implementation / Observability

## Status
Implemented; fresh runtime export pending

## Change
`PlanSetOutlineSegmentCongruenceAuditBuilder` now emits:
- `OutlineEdges`: left/right/bottom/top edge required counts and missing counts.
- `CornerCoverage`: bottom-left, bottom-right, top-left, top-right structural corner coverage.

The Desktop UI summary now lists failing edge/corner names when segment congruence is not OK.

The runtime verifier now requires both fields.

## Why
The objective explicitly requires comparing left/right/top/bottom, principal corners, and long horizontal/vertical runs. The previous artifact could block on required outline coverage, but it did not expose edge/corner evidence clearly enough for human debugging.

## Verification
Allowed checks passed without dotnet build/test/watch:
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched files

## Runtime classification
Latest runtime manifest `f2df4b2006c04164afd03b5a009e2755` predates this audit shape and correctly fails because it lacks `RequiredOutlineSegmentCount`. A fresh Desktop export is required.
