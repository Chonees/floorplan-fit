# Verifier final output detail fields

## What
The manifest verifier output now includes final FloorPlan/ElectricalPlan output paths, supported footprint width/height for both outputs, final mismatch values, and optional raw visible-bounds mismatch values.

## Why
The goal requires the verifier to show what was compared, not only pass/fail status. Earlier output exposed final status and mismatch but not the compared footprint dimensions/paths.

## Where
- `scripts/verify-latest-plan-set-recipe-manifest.ps1`

## Gotcha fixed
Raw mismatch fields are optional for older fixture/audit shapes, so they are read safely with `Get-JsonValue` and initialized to null for affine/manual branches.

## Evidence
`powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\test-verify-latest-plan-set-recipe-manifest.ps1` passes.

## Update
The verifier self-check now asserts the final-output fields are present in command output, including reason, tolerance, paths, compared Floor/Electrical W/H, and mismatch values.
