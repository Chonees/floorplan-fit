---
type: Bug
date: 2026-06-05
project: floorplan-fit
status: superseded
related:
  - ../Implementation/2026-06-05 - Adjusted DXF export preserves source DXF surgically.md
  - 2026-06-05 - Adjusted DXF export duplicated dimension text.md
replaced_by:
  - 2026-06-05 - Adjusted DXF dimension duplication unresolved after rollback.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - autocad
  - dimensions
---

# Adjusted DXF AutoCAD dimension regeneration diverged from preview

> Superseded on 2026-06-05: this remains a plausible contributor, but the attempted fix did not resolve user-observed AutoCAD output. Treat as evidence, not final closure.

## Symptom

The adjusted DXF opened in AutoCAD and no longer had duplicate text overrides, but several dimensions still rendered differently than the app preview. Example: window labels/dimensions appeared rotated or displaced in AutoCAD while the Floorplan Fit preview looked correct.

## Root cause

The exporter had two authorities for the same cota:

1. it patched the anonymous dimension geometry block, which is what the Floorplan Fit preview represents;
2. it also patched native `DIMENSION` definition fields (`10/20/30`, `11/21/31`, `13/23/33`, `14/24/34`, `42`).

AutoCAD can interpret/regenerate native dimensions from those definition fields. Since our visual adjustment logic is style-preserving and block-primitive based, changing the native definition fields caused AutoCAD to render a different cota than the preview.

## Fix

`IxMiliaAdjustedDxfExporter` now leaves native DIMENSION definition fields unchanged and exports the visual adjustment through the existing anonymous dimension geometry block primitives only. Existing group-code `1` text overrides can still be updated if they existed in the source; new overrides are not inserted.

The current export file `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf` was patched in place to restore native DIMENSION definition values from the original source while keeping adjusted geometry block primitives.

## Verification

- RED test asserted native DIMENSION definition fields stay equal to the source DXF and failed before the fix.
- GREEN focused exporter test passed: `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedDxfExporterTests" --output .artifacts-test\adjusted-dxf-preview-match-final`.
- Patched current export audits with `errors=0`, `fixes=0`.
- Patched current export has `native_definition_diffs=0`, `dimension_group1=46`, and `source_group1=46`.

## Tradeoff

This is intentionally WYSIWYG-first: the export preserves the visual CAD block shown in our preview instead of trying to make AutoCAD recalculate a fully native associative dimension. A fully native associative export would require an AutoCAD/ODA-grade dimension regeneration path, not ad-hoc field edits.
