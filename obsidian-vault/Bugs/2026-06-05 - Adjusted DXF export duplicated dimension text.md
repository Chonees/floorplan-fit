---
type: Bug
date: 2026-06-05
project: floorplan-fit
status: superseded
related:
  - ../Implementation/2026-06-05 - Adjusted DXF export preserves source DXF surgically.md
replaced_by:
  - 2026-06-05 - Adjusted DXF dimension duplication unresolved after rollback.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - dimensions
---

# Adjusted DXF export duplicated dimension text in AutoCAD

> Superseded on 2026-06-05: this was one hypothesis/fix attempt, but user visual QA still showed duplicates later. Do not treat this note as final root cause.

## Symptom

After the surgical source-preserving DXF export started opening in AutoCAD, some dimensions appeared visually duplicated.

## Root cause

The exporter patched the anonymous dimension geometry block text, but it also inserted DXF group code `1` into `DIMENSION` entities that did not originally have a text override.

That created two text sources for AutoCAD:

1. the dimension block text already present in the source DXF;
2. a newly-added native `DIMENSION` text override.

The DXF was syntactically valid and audited cleanly, but AutoCAD could render both representations.

## Fix

`IxMiliaAdjustedDxfExporter` now only updates group code `1` when that code already exists in the source `DIMENSION` record. It does not create new text overrides for dimensions whose visible text is already represented by the geometry block.

The current user-facing file `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf` was also patched in place to remove inserted text overrides.

## Verification

- RED test confirmed the previous exporter inserted group code `1` into `*D169`/source handle dimension when the source record had none.
- GREEN focused exporter test passed: `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedDxfExporterTests" --output .artifacts-test\adjusted-dxf-no-duplicate-final`.
- Patched current export audits with `errors=0`, `fixes=0`.
- Patched current export now has `DIMENSION` group-code-1 count `46`, matching the source DXF; the duplicated old export had `125`.

## Gotcha

`ezdxf.audit()` validates DXF structure but does not catch this AutoCAD visual duplication because the file is structurally valid. This needs a semantic exporter regression test, not just an audit check.
