# Projected dependent DXF export rotates arc angles

## What changed
- `ProjectedPlanSheetDxfExporter` now rotates DXF `ARC` start/end angle group codes `50` and `51` by the approved sheet projection rotation.
- Existing point projection, CIRCLE/ARC radius scaling, TEXT/MTEXT height scaling, and INSERT reference transformation remain unchanged.

## Why
Roof and facade/elevation dependent sheets can contain arcs. If the package projection rotates the sheet but leaves ARC start/end angles unchanged, the exported arc geometry is inconsistent even when center/radius are correct.

## Boundary
- This only adjusts entity-level ARC angles.
- No full curve engine, DIMENSION styling, text style, or block definition transformation was introduced.
- No per-sheet fit engine was introduced; the approved projection transform remains the single source.

## Verification
- Test-first RED: `ProjectedPlanSheetDxfExporterTests` expected ARC `50/51` angles to rotate while production only rotated INSERT `50`.
- GREEN: exporter now rotates ARC `50/51` when current entity is `ARC`.
- `git diff --check` passed for touched files.
- No `dotnet test` and no `dotnet build` were run due repository rule.