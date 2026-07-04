---
type: Implementation
date: 2026-06-05
project: floorplan-fit
status: superseded
related:
  - ../Bugs/2026-06-05 - Adjusted DXF export wrote invalid layout owners.md
replaces:
  - 2026-06-05 - Adjusted DXF export repairs DXF ownership handles.md
replaced_by:
  - ../Bugs/2026-06-05 - Adjusted DXF dimension duplication unresolved after rollback.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - dxf-validity
  - surgical-dxf
---

# Adjusted DXF export preserves source DXF surgically

> Superseded on 2026-06-05: user visual QA still showed duplicated/misrotated dimensions after later patch attempts, so the exporter/test changes were rolled back. Keep this note as historical evidence, not current implementation truth.

## Change

`IxMiliaAdjustedDxfExporter` no longer reserializes the entire CAD file with `DxfFile.Save(...)` for adjusted exports.

Instead, it reads the original DXF as Latin-1 text, preserves the source sections/handles/object graph, and patches only the dirty native dimension data:

1. the matching `DIMENSION` entity by source handle;
2. the existing geometry block primitives by their source handles (`LINE`, `TEXT`/`MTEXT`, `INSERT`, `CIRCLE`, `ARC`, `SOLID`);
3. dimension text/value/definition-point fields needed by the edited cota.

## Why

The earlier ownership-repair approach was incomplete. It made `ezdxf.readfile(...)` load the exported file, but `doc.audit()` still reported thousands of fixes and AutoCAD still opened an empty `Drawing1`.

Evidence from the bad export:

- source DXF sections: `HEADER`, `CLASSES`, `TABLES`, `BLOCKS`, `ENTITIES`, `OBJECTS`, `ACDSDATA`;
- old adjusted export lost `ACDSDATA`;
- `ezdxf` audit on the bad export reported `0` errors but `4607` fixes;
- the bad export corrupted `DIMSTYLE` text-style references such as `Standard`/`Annotative` resolving to missing `DOORS`.

## Design decision

Treat the original AutoCAD DXF as the source of truth. For this feature, changing a cota does not require rebuilding all DXF tables, dictionaries, layouts, scale lists, styles, and ACDS metadata.

The exporter now behaves like a surgical CAD patcher instead of a whole-file serializer.

## Verification

- RED confirmed the previous exporter dropped source sections: `Assert.Equal()` failed because output was missing `ACDSDATA`.
- GREEN: `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedDxfExporterTests" --output .artifacts-test\adjusted-dxf-surgical-green` passed 1/1.
- Real export smoke check: `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted-2.dxf` loads with `ezdxf`, keeps `ACDSDATA`, has `modelspace_count=3944`, and audits with `errors=0` and `fixes=0`.

## Gotchas

- Group code `330` is not universally safe to rewrite; that was the key trap in the earlier repair attempt.
- `IxMilia` can read this source DXF well enough for extraction, but whole-file saving is not safe enough for this AutoCAD-authored file.
- Future primitives without source handles may need an append-with-new-handle strategy, but current curation/export data from extracted dimensions carries source handles for the primitives being edited.
