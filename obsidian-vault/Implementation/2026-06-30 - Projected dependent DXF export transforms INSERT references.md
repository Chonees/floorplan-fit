# Projected dependent DXF export transforms INSERT references

## What changed
- `ProjectedPlanSheetDxfExporter` now scales DXF `INSERT` reference scale codes `41`, `42`, and `43` by the approved sheet projection scale.
- It also rotates `INSERT` reference rotation code `50` by the approved projection rotation.
- Existing point projection and CIRCLE/ARC radius scaling remain unchanged.

## Why
Electrical, roof, and facade/elevation sheets can use block references for symbols and details. Moving an INSERT insertion point without scaling/rotating the reference leaves dependent package artifacts visually incoherent.

## Boundary
- This transforms INSERT references, not block definitions/internal geometry.
- TEXT height, DIMENSION styling, and deep block internals remain intentionally deferred until real dependent sheets prove the need.
- No per-sheet fit engine or full DXF engine was introduced.

## Verification
- Test-first RED: `ProjectedPlanSheetDxfExporterTests` expected INSERT scale `41/42` and rotation `50` to be adjusted, while production did not touch those codes.
- GREEN: exporter now handles INSERT reference scale and rotation codes.
- `git diff --check` passed for touched files.
- No `dotnet test` and no `dotnet build` were run due repository rule.