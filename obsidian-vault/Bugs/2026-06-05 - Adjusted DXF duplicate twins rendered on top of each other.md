---
type: Bug
date: 2026-06-05
project: floorplan-fit
status: fixed-pending-autocad-visual-qa
related:
  - 2026-06-05 - Adjusted DXF dimension duplication unresolved after rollback.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - duplicate-dimensions
---

# Adjusted DXF duplicate twins rendered on top of each other

## Symptom

User visual QA showed many dimension numbers duplicated one on top of the other in AutoCAD, with some labels appearing displaced compared with the preview.

## Verified evidence

Fresh export `C:\Users\lucas\OneDrive\Escritorio\exports\SEMINOLE2000-adjusted.dxf` still contained:

- 328 native `DIMENSION` entities;
- 164 exact duplicate native DIMENSION signature groups;
- exact visual twin blocks for every native twin group.

This means the prior fix synchronized duplicate twins but kept drawing both. Synchronization alone cannot remove overdraw.

## Fix

After `DxfFile.Save(...)` and protected native metadata restoration, the exporter now computes a stable native DIMENSION deduplication signature and emits only the first record from each exact duplicate group.

The exporter still rewrites exact twins together before deduplication, so if the user edited either twin, the kept entity carries the adjusted visual geometry.

## Verification

- RED: `ExportAsync_suppresses_exact_source_duplicate_dimension_twins` failed because `*D498` was still extracted after export.
- GREEN: same test now passes; `*D169` remains adjusted and `*D498` is absent.
- Focused exporter suite passed 3/3.
- Diagnostic file `SEMINOLE2000-adjusted-dedup-diagnostic.dxf` was generated with 164 DIMENSION entities and 0 duplicate signature groups.

## Caveat

AutoCAD visual QA is still required. This fix removes duplicate renderable DIMENSION entities; it does not claim full visual parity for every text-position edge case until the diagnostic/fresh export is opened in AutoCAD.