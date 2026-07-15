# 2026-07-10 - Verifier named edge and corner rows

## Type
Verifier hardening

## Status
Implemented; fresh runtime export pending

## Change
The runtime verifier now requires exact named segment-congruence outline rows:
- Edges: `Left`, `Right`, `Bottom`, `Top`
- Corners: `BottomLeft`, `BottomRight`, `TopLeft`, `TopRight`

Edge rows must expose `RequiredCount`, `MissingCount`, and `Mismatches`.
Corner rows must expose `DeltaX`, `DeltaY`, and `IsCovered`.

## Why
The goal explicitly requires left/right/top/bottom and principal corner evidence. Requiring only four rows was too weak because an incorrectly shaped artifact could still pass.

## Verification
Allowed checks passed without dotnet build/test/watch:
- `scripts/test-outline-segment-congruence-contract.ps1`
- `scripts/test-outline-congruence-contract.ps1`
- `scripts/test-plan-set-observability-audit-files.ps1`
- `scripts/test-plan-set-human-summary-contract.ps1`
- `scripts/test-verify-latest-plan-set-recipe-manifest.ps1`
- `git diff --check` on touched files

## Next
Fresh Desktop export, then `scripts/verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic`.
