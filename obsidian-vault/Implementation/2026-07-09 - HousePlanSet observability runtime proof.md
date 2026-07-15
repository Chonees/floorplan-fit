---
type: Implementation
date: 2026-07-09
replaces: []
replaced_by: null
---

# HousePlanSet observability runtime proof

## Runtime evidence
Latest verified manifest:

`src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/exports/plan-sets/39a5188e145a4e899723235c7272b07e/manifest.json`

Verifier command:

`powershell -NoProfile -ExecutionPolicy Bypass -File ".\scripts\verify-latest-plan-set-recipe-manifest.ps1" -RequireAutomatic`

Result: passed.

## Key proof points
- Status: `ReadyForExport`
- CanonicalAdjustmentId: `12211a24-7349-49e5-8283-64d8d13d0b70`
- CanonicalSheets: `1`
- ElectricalSheets: `1`
- RecipeSheets: `1`
- CompressionRecipeSheets: `1`
- ElectricalStatus: `ProjectedAutomatically`
- ObservabilityAuditFiles: `6`
- DXF safety counts: entity count `3348`, inserts `93`, dimensions `17`, ellipses `4`, wires/curves `313`

## Input audit
The latest `input-audit.json` captured:
- original width: `468.000` inches
- original height: `930.000` inches
- requested width: `464.400` inches
- requested height: `927.600` inches
- required width delta: `3.600` inches
- required height delta: `2.400` inches

## Operation audit
- Canonical recipe operation count: `8`
- FloorPlan impact operation count: `8`
- UI summary: `FloorPlan operations applied: 8/8; affected entities 1108, vertices 14552; warnings 0.`
- UI summary: `Electrical operations applied: 6/8; affected entities 1505, vertices 4233; warnings/failures 2.`
- The two non-applied Electrical operations are explicitly audited as `NoGeometryAffected` with reason `No electrical geometry matched this canonical operation.`

## Conclusion
The observability goal is satisfied for the SEMINOLE compression case: the package now contains end-to-end evidence from user input through canonical recipe, FloorPlan impact, Electrical projection, DXF safety, manifest, and UI human summary.
