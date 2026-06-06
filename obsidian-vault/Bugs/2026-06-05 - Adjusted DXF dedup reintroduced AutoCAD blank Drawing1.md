---
type: Bug
date: 2026-06-05
project: floorplan-fit
status: fixed-pending-autocad-visual-qa
related:
  - 2026-06-05 - Adjusted DXF duplicate twins rendered on top of each other.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - autocad
  - dxf-validity
---

# Adjusted DXF dedup reintroduced AutoCAD blank Drawing1

## Symptom

After deduplicating DIMENSION twins, AutoCAD again opened the export with the black screen / `Press ENTER` flow and then a blank `Drawing1`.

## Verified root cause

The export was still based on `DxfFile.Save(...)` from IxMilia. The fresh `SEMINOLE2000-adjusted.dxf` lacked the source `ACDSDATA` section and `ezdxf.readfile(...)` failed with:

`required BLOCK_RECORD #0 for layout 'Layout1' does not exist`

So the dedup logic was applied to a structurally unsafe whole-file serialization.

## Fix

`IxMiliaAdjustedDxfExporter` is now source-preserving:

- reads the original DXF as Latin-1 group-code pairs;
- patches only existing dimension geometry-block primitives for edited dimensions and their source twins;
- does not call `DxfFile.Save(...)`;
- deduplicates exact native `DIMENSION` records;
- removes `DIMASSOC` objects owned by removed dimensions;
- removes dictionary entries that referenced removed `DIMASSOC` objects.

## Verification

- RED: exporter output lacked `ACDSDATA` before the fix.
- GREEN: exporter tests passed 4/4.
- Diagnostic file `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted-source-preserving-diagnostic.dxf` has `ACDSDATA`, 164 DIMENSION records, 0 duplicate signature groups, and `ezdxf.audit()` reports `errors=0`, `fixes=0`.

## Caveat

AutoCAD visual QA is still required. Structural validity is verified; visual positioning must be checked by opening the source-preserving diagnostic/fresh export.