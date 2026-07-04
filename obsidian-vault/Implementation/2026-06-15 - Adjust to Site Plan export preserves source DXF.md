---
type: Implementation
date: 2026-06-15
project: floorplan-fit
status: current
related:
  - ../Bugs/2026-06-15 - Adjust to Site Plan export opens black blank DXF.md
  - ../Bugs/2026-06-05 - Adjusted DXF dedup reintroduced AutoCAD blank Drawing1.md
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - export
  - dxf-validity
  - surgical-dxf
---

# Adjust to Site Plan export preserves source DXF

## Change

`IxMiliaAdjustedSitePlanExporter` now follows the same source-preserving rule learned from adjusted floor-plan export: it does not use IxMilia to reserialize the real AutoCAD-authored floor-plan DXF.

Instead, it reads the floor-plan source as Latin-1 DXF group-code pairs, patches compression coordinate changes directly, injects supported site-plan entities, and writes the preserved pair stream back out.

## Why

The exported `plano-ajustado-al-sitio.dxf` reproduced the old AutoCAD black/blank failure pattern: missing `ACDSDATA` and `ezdxf` failure `required BLOCK_RECORD #0 for layout 'Layout1' does not exist`.

That is the exact architectural smell: a DXF can contain visible-looking entities and still be structurally invalid if the original CAD sections/object graph are not preserved.

## Product copy

Visible export actions now say **Exportar DXF**. Internal class names can remain explicit (`AdjustedSitePlan`) because they describe architecture, but operator-facing copy stays simple.

## Verification

- Infrastructure exporter tests passed 3/3.
- Desktop XAML/export wiring focused tests passed 11/11.
- `git diff --check` exited 0 with CRLF warnings only.

## Gotchas

- The exporter intentionally skips object-dependent site-plan entities such as inserts, dimensions, and hatches rather than injecting dangling references.
- Compression patching is coordinate-pair based and source-preserving; if future site-plan export needs richer CAD object support, add explicit serializers instead of returning to whole-file `DxfFile.Save(...)`.

## Follow-up: HANDSEED advances past injected handles

A fresh source-preserving export still failed in AutoCAD. This time the DXF preserved `ACDSDATA` and loaded/audited cleanly in `ezdxf`, but `ezdxf` emitted duplicate-handle warnings for handles assigned by the site-plan injection.

Root cause: injected handles advanced beyond the original source max, but `$HANDSEED` was still preserved from the source header. The exporter now updates `$HANDSEED` to the next available generated handle after all layer/entity injection.

This keeps the source-preserving architecture while also maintaining the CAD allocator metadata needed after appending entities.

## Follow-up: injected layers include AutoCAD plot-style metadata

AutoCAD Core Console showed the source-preserving combined export still failed because injected `LAYER` records were too minimal for SEMINOLE2000's AutoCAD DXF table schema.

`BuildLayerRecord(...)` now emits the same required metadata tail used by source layers:

```txt
370 -3
390 F
347 98
348 0
```

This preserves the original table shape expectation and lets AutoCAD load/audit the combined export.

