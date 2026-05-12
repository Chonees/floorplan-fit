---
created: 2026-05-11
project: floorplan-fit
type: discovery
status: active
replaces:
replaced_by:
---

# Architectural floor-plan dimensions use inches as source units

## What
Verified that the floor-plan DXF samples and the current desktop workspace use inches as source units, with millimeters stored only as derived conversions.

## Why
The user explicitly asked whether live dimension recalculation should be based on millimeters or on the architectural unit used by the plan.

## Where
- `PLANS/originalFloorPlans/SANTA-BARBARA.dxf`
- `PLANS/originalFloorPlans/SEMINOLE2000.dxf`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDxfGateway.cs`
- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaDimensionExtractor.cs`
- `src/FloorplanFit.Desktop/bin/Debug/net10.0/workspace/app.db`

## Verified evidence
1. Sample DXFs contain `$INSUNITS` value `1`.
2. Repo mapping converts `INSUNITS=1` to `LengthUnit.Inch`.
3. Desktop workspace `measurement_contexts` rows store source unit code `4` with factor `25.4`.
4. Extracted dimension rows store `SourceUnit = 'Inch'` and visible texts like `5'-8"` and `10'-2"`.

## Learned
- For architectural floor plans in this repo, live recalculation should use inches as source truth and derive millimeters secondarily.
- Any runtime recalculation that assumes millimeters as source units would drift from the authored CAD semantics.
