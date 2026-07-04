# Projected dependent DXF export scales text height

## What changed
- `ProjectedPlanSheetDxfExporter` now scales DXF `TEXT` and `MTEXT` height group code `40` by the approved sheet projection scale.
- Existing point projection, CIRCLE/ARC radius scaling, and INSERT reference transformation remain unchanged.

## Why
Dependent sheets include labels, notes, electrical annotations, roof notes, and elevation text. Moving projected text while leaving its height at source scale makes the exported package visually inconsistent.

## Boundary
- This scales entity-level text height only for `TEXT`/`MTEXT`.
- Text styles, DIMENSION styles, and deeper annotation internals remain intentionally deferred until real dependent sheets prove the need.
- No full DXF engine or per-sheet fit engine was introduced.

## Verification
- Test-first RED: `ProjectedPlanSheetDxfExporterTests` expected `TEXT` and `MTEXT` code `40` heights to scale while production did not handle text height.
- GREEN: exporter now scales code `40` when current entity is `TEXT` or `MTEXT`.
- `git diff --check` passed for touched files.
- No `dotnet test` and no `dotnet build` were run due repository rule.