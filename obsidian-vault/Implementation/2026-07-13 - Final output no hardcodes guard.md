# Final output no hardcodes guard

## What
Added a minimal PowerShell contract that scans the final-output FloorPlan/ElectricalPlan pipeline files for case-specific hardcodes.

## Why
The HousePlanSet sync must work for N FloorPlans and N ElectricalPlans. The final-output proof cannot depend on SEMINOLE, TEST A, Downloads paths, GUIDs, one-off coordinates, or known 39/77 dimensions.

## Where
- `scripts/test-plan-set-final-output-no-hardcodes-contract.ps1`

## Evidence
`powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-plan-set-final-output-no-hardcodes-contract.ps1` passes.
