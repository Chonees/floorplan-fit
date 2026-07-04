---
type: Bug
date: 2026-06-05
project: floorplan-fit
status: fixed-pending-autocad-visual-qa
related:
  - ../Implementation/2026-06-05 - Adjusted DXF export preserves source DXF surgically.md
replaces:
  - 2026-06-05 - Adjusted DXF export duplicated dimension text.md
  - 2026-06-05 - Adjusted DXF AutoCAD dimension regeneration diverged from preview.md
  - 2026-06-05 - Adjusted DXF separated source duplicate dimensions.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - autocad
  - dimensions
  - unresolved
---

# Adjusted DXF dimension duplication unresolved after rollback

## Current truth

User visual QA in AutoCAD still shows several adjusted DXF dimensions duplicated and some dimension text orientation changing compared with the Floorplan Fit preview.

The previous exporter/test patch attempts for this issue were rolled back on 2026-06-05. Do not continue stacking fixes on those attempts without a single-dimension reproduction.

## Verified evidence

- `src/FloorplanFit.Infrastructure/Dxf/IxMiliaAdjustedDxfExporter.cs` and `tests/FloorplanFit.Infrastructure.Tests/Export/IxMiliaAdjustedDxfExporterTests.cs` were reverted to HEAD/original after user QA rejected the attempted fixes.
- The source `SEMINOLE2000.dxf` contains exact-overlap native DIMENSION twins: 164 twin groups / 328 twinned DIMENSION entities by native signature.
- Example source twin evidence: handles `1672` and `3AB5`, blocks `*D169` and `*D498`, share the same native fields and same visible block text/position.
- Generated files `SEMINOLE2000-adjusted-2.dxf`, `SEMINOLE2000-adjusted-3.dxf`, and `SEMINOLE2000-adjusted-4.dxf` remain locked by another process in `C:\Users\lucas\OneDrive\Escritorio\exports`.

## Working hypothesis

There are likely two separate mechanisms:

1. source twin dimensions that overlap in the original and become visible when adjusted inconsistently;
2. AutoCAD interpretation/regeneration of native `DIMENSION` data that may change text placement/orientation independently of the preview's rendered primitives.

The exact split is not proven yet.

## Required debugging path

1. Close AutoCAD and delete stale generated adjusted DXFs.
2. Pick one bad visible dimension and identify its source handle/block.
3. Generate controlled one-dimension diagnostic exports:
   - untouched source copy;
   - patch only the selected dimension;
   - patch selected dimension plus exact source twin(s);
   - if needed, compare geometry-block-only vs native-DIMENSION-field changes.
4. Compare those variants in AutoCAD before implementing another exporter patch.

## Tradeoff

Continuing to patch all adjusted dimensions at once hides causality. A one-dimension diagnostic is slower up front, but it prevents more false positives and more broken exports.
## Resolution on 2026-06-05

Technical fix applied after RED/GREEN diagnostics:

- RED confirmed `*D169` adjusted to `6'-0"` while its source twin `*D498` stayed at `5'-8"`.
- Exporter now detects exact native DIMENSION twin groups and rewrites all twin geometry blocks together, keeping duplicated source cotas superposed after adjustment.
- RED confirmed the exporter/native serializer inserted or dropped protected native DIMENSION metadata such as group `1` and group `42`.
- Exporter now restores protected native DIMENSION metadata from the source DXF after IxMilia serialization, so adjusted exports are block-visual-authoritative while native fields remain source-stable.
- Verification: `IxMiliaAdjustedDxfExporterTests` passed 3/3; `git diff --check` exited 0 with only LF-to-CRLF warnings.

Remaining caveat: user AutoCAD visual QA is still required on a fresh export because the old `SEMINOLE2000-adjusted-2/3/4.dxf` files remain locked by AutoCAD/another process.