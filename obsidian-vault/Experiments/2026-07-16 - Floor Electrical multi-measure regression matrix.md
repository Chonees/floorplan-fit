---
type: experiment
date: 2026-07-16
status: ready-to-run
---

# Floor/Electrical multi-measure regression matrix

## Baseline

- Canonical SEMINOLE footprint: `39.0000 ft x 77.5000 ft` = `468 in x 930 in`.
- Decimal-foot formula: `target feet = original feet - requested inches / 12`.
- Success requires folder-only package with valid FloorPlan and ElectricalPlan, exact expected reduction on both, and final structural footprint congruence within the configured tolerance.

## Cases

| ID | Target width ft | Target height ft | Expected width cut | Expected height cut | Expected class |
|---|---:|---:|---:|---:|---|
| T00 | 39.0000 | 77.5000 | 0 in | 0 in | baseline/no compression |
| W01 | 38.9167 | 77.5000 | 1 in | 0 in | automatic width |
| W02 | 38.8333 | 77.5000 | 2 in | 0 in | automatic width |
| W03 | 38.7500 | 77.5000 | 3 in | 0 in | automatic width |
| W04 | 38.6667 | 77.5000 | 4 in | 0 in | width boundary |
| H01 | 39.0000 | 77.4167 | 0 in | 1 in | automatic height |
| H02 | 39.0000 | 77.3333 | 0 in | 2 in | automatic height |
| H03 | 39.0000 | 77.2500 | 0 in | 3 in | split height |
| H04 | 39.0000 | 77.1667 | 0 in | 4 in | height boundary |
| M11 | 38.9167 | 77.4167 | 1 in | 1 in | mixed automatic |
| M23 | 38.8333 | 77.2500 | 2 in | 3 in | mixed/split |
| M44 | 38.6667 | 77.1667 | 4 in | 4 in | mixed boundary |
| XW5 | 38.5833 | 77.5000 | 5 in | 0 in | expected capacity/manual rejection |
| XH5 | 39.0000 | 77.0833 | 0 in | 5 in | expected capacity/manual rejection |

## Per-case evidence

1. Export using the case ID as filename.
2. Successful automatic case must leave only `<ID>-plan-set/<ID>-floorplan.dxf` and `<ID>-electrical.dxf`.
3. Both DXFs must open normally in AutoCAD.
4. Run `verify-latest-plan-set-recipe-manifest.ps1 -RequireAutomatic` for automatic cases.
5. Record requested delta, Floor applied operations, Electrical applied operations, final width/height mismatch, warnings, and manual AutoCAD overlay result.
6. Boundary-exceeding cases must fail clearly/manual-review; silently trimming a different amount is a defect.

## Stop condition

Stop at the first failed automatic case, preserve its package/manifest, diagnose that class, then resume. Do not produce later cases on top of an unexplained failure.
