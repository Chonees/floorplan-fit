---
type: Bug
date: 2026-06-05
project: floorplan-fit
status: superseded
related:
  - ../Implementation/2026-06-05 - Adjusted DXF export preserves source DXF surgically.md
  - 2026-06-05 - Adjusted DXF AutoCAD dimension regeneration diverged from preview.md
replaced_by:
  - 2026-06-05 - Adjusted DXF dimension duplication unresolved after rollback.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - dimensions
  - duplicate-dimensions
---

# Adjusted DXF separated source duplicate dimensions

> Superseded on 2026-06-05: source twin dimensions are verified evidence, but synchronizing twins did not resolve the user's AutoCAD visual QA. Root cause remains unresolved.

## Symptom

Several dimensions appeared duplicated in AutoCAD after adjusted export, but not every dimension duplicated.

## Root cause

The original SEMINOLE2000 DXF already contains many duplicate native DIMENSION pairs that are exactly superposed. In the source file they look like one cota because both twins have the same text and geometry.

The exporter patched only the edited dimension block. If the edited cota had a source twin, the adjusted twin moved/changed while the unedited twin stayed at the original position. That separated the pair and made the duplicate visible.

This explains why the issue affected many dimensions but not all: only source dimensions with an exact twin and only when one twin was adjusted.

Example verified from source:

- `*D169` and `*D498` have identical native DIMENSION geometry and identical block text `5'-8"` at the same location.
- Before the fix, exporting an adjustment to one block left the other block unchanged.

## Fix

`IxMiliaAdjustedDxfExporter` now computes a source DIMENSION twin signature from stable native fields. When a dirty dimension is exported, it patches its own geometry block and also patches any source twin geometry blocks by ordinal primitive order.

The current user-facing file `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf` was patched in place so adjusted duplicate twin groups are visually synchronized again.

## Verification

- RED test confirmed `*D498` stayed at `5'-8"` while adjusted `*D169` became `6'-0"`.
- GREEN focused exporter test passed: `dotnet test tests/FloorplanFit.Infrastructure.Tests/FloorplanFit.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IxMiliaAdjustedDxfExporterTests" --output .artifacts-test\adjusted-dxf-duplicate-twin-final`.
- Patched current export audits with `errors=0`, `fixes=0`.
- Patched current export has `remaining_changed_duplicate_text_position_mismatches=0`.

## Gotcha

The source DXF contains exact-overlap duplicates, so entity counts alone are not enough to detect the visual bug. The regression has to check that source twin blocks move together.
