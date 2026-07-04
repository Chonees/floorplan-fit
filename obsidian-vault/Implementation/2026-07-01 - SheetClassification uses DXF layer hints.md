# SheetClassification uses DXF layer hints

## What changed
`SheetClassification` now accepts DXF layer hints, and dependent sheet import passes the layer names detected from the copied DXF.

## Why
Electrical, roof, and facade/elevation sheets do not share the same layer structure or amount of dimensional evidence as the canonical floor plan. Filename/title-only classification was too weak for real multi-sheet house packages.

## Implemented
- `ClassifyPlanSheetRequest` now includes optional `LayerHints`.
- `ClassifyPlanSheetHandler` matches sheet type keywords against filename/title and layer hints.
- Conflicting evidence across sheet types still returns `Unknown` and requires manual confirmation.
- `IxMiliaDxfGateway` extracts layer names from DXF layer table, top-level entities, block layers, and block entities.
- `ImportPlanSheetHandler` reads the dependent DXF once and passes detected layer names to classification when no explicit sheet type is provided.

## Boundary
No per-sheet fit engine was introduced. This is classification evidence only; registration/projection remains responsible for sheet-specific behavior.

## Verification
- Added classification tests for layer-based electrical detection and conflicting layer hints.
- Added import-handler test proving detected layer names classify an otherwise ambiguous file.
- Added DXF gateway test proving table/entity layers are returned.
- `git diff --check` passed for touched files.
- No `dotnet test` or `dotnet build` was run due repository rule.
