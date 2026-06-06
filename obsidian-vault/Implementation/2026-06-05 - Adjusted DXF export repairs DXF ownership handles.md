---
type: Implementation
date: 2026-06-05
project: floorplan-fit
status: superseded

replaced_by:
  - 2026-06-05 - Adjusted DXF export preserves source DXF surgically.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - dxf-validity
---

# Adjusted DXF export repairs DXF ownership handles

## Change

`IxMiliaAdjustedDxfExporter` now performs a post-save DXF text repair pass after `DxfFile.Save(...)`.

## Why

IxMilia can reserialize the source DXF with changed handles while leaving stale owner references. Real CAD readers care about those references. A DXF can contain visible entities and still be invalid if layouts, block records, blocks, and modelspace entities point to missing or wrong owner handles.

## Design

The repair pass is intentionally surgical:

1. Read the source DXF layout ownership map.
2. Read the output block-record handles produced by IxMilia.
3. Reconnect output layouts to the matching output block records.
4. Re-own symbol-table records to their serialized table handles.
5. Re-own block definitions and block child entities to their serialized block-record handles.
6. Re-own modelspace entities to the serialized `*Model_Space` block record.

Important gotcha: group code `330` is not always the owner. Some entities use later `330` values as internal references. The repair only changes/inserts the owner before the first `100 AcDb...` subclass marker.

## Verification

- `dotnet test tests\FloorplanFit.Infrastructure.Tests\FloorplanFit.Infrastructure.Tests.csproj --filter FullyQualifiedName~IxMiliaAdjustedDxfExporterTests --no-restore --nologo --output .artifacts-test\adjusted-dxf-validity-final -v minimal` passed 1/1.
- `dotnet test tests\FloorplanFit.Desktop.Tests\FloorplanFit.Desktop.Tests.csproj --filter "FullyQualifiedName~AddDesktopSlice1_routes_adjusted_dxf_exports_to_desktop_exports_folder|FullyQualifiedName~ExportAdjustedDxfAsync_runs_handler_refreshes_review_and_reports_status" --no-restore --nologo --output .artifacts-test\adjusted-dxf-desktop-status-final -v minimal` passed 2/2.
- `git diff --check` exited 0 with only existing LF-to-CRLF warnings.


## Superseded

This note is superseded by Implementation/2026-06-05 - Adjusted DXF export preserves source DXF surgically.md. The ownership-repair pass was incomplete: AutoCAD still opened the export as an empty Drawing1, and ezdxf.audit() still reported thousands of fixes. The current implementation avoids whole-file reserialization and patches only the changed dimensions in the original DXF text.
