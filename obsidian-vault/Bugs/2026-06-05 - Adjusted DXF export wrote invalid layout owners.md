---
type: Bug
date: 2026-06-05
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - dxf-validity
  - ixmilia
---

# Adjusted DXF export wrote invalid layout owners

## Symptom

Opening the adjusted DXF from `C:\Users\lucas\OneDrive\Escritorio\exports` appeared blank / invalid.

## Evidence

The exported file was not empty and contained DXF sections/entities, but `ezdxf.readfile(...)` failed with:

```txt
required BLOCK_RECORD #0 for layout 'Layout1' does not exist
```

After the first layout-only repair, `ezdxf` could load the file, but audit still reported many invalid owner-handle repairs.

## Root cause

`IxMiliaAdjustedDxfExporter` used `DxfFile.Save(...)` to reserialize the full source DXF after changing dimension geometry. For this real AutoCAD DXF, IxMilia rewrote some handles but preserved stale ownership references:

- `LAYOUT` objects lost or kept wrong `BLOCK_RECORD` references.
- `BLOCK_RECORD` table records still pointed to stale table owners.
- `BLOCK` definitions and their child entities still pointed to stale block-record owners.
- Modelspace entities could keep stale modelspace owner references.

That made the file structurally unreliable even though it still looked like a text DXF.

## Fix

The exporter now repairs the serialized DXF text after IxMilia save:

- copies source layout-to-block-record semantics,
- maps those block-record names to the output handles,
- restores `LAYOUT -> BLOCK_RECORD` references,
- repairs symbol-table record owners,
- repairs block/entity owner handles,
- repairs modelspace entity owner handles,
- touches only the owner `330` before the first `AcDb...` subclass marker, avoiding non-owner `330` references like hatch boundary links.

## Data cleanup

The invalid `SEMINOLE2000-adjusted.dxf` was deleted from the Desktop exports folder. The latest draft's dimension override export timestamps were cleared so the user can export again after restarting the app.

A DB backup was created before that cleanup:

```txt
src\FloorplanFit.Desktop\bin\Debug\net10.0\workspace\app.db.backup-before-adjusted-dxf-reexport-20260605-153651
```

## Verification

- RED: `IxMiliaAdjustedDxfExporterTests` failed because `Layout1` had no valid block-record reference.
- RED: same exporter test failed because `BLOCK_RECORD` rows were owned by stale handle `1` instead of the output block-record table handle.
- GREEN: `IxMiliaAdjustedDxfExporterTests` passed 1/1 with layout and owner assertions.
- Desktop export-path/status tests passed 2/2.
- DB check: active Seminole draft has 89 overrides and 89 marked dirty for re-export.
- Invalid old export file no longer exists.
