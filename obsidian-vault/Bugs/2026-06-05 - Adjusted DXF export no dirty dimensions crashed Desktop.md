---
type: Bug
date: 2026-06-05
project: floorplan-fit
status: fixed
related:
  - 2026-06-05 - Adjusted DXF dimension duplication unresolved after rollback.md
tags:
  - floorplan-fit
  - export
  - adjusted-dxf
  - desktop-crash
---

# Adjusted DXF export with no dirty dimensions crashed Desktop

## Symptom

Clicking `Export adjusted DXF` crashed the Desktop app under `dotnet watch` with:

`System.InvalidOperationException: No dirty native dimensions are available to export.`

The exception escaped from `ExportAdjustedDxfHandler` through `FloorPlanReviewViewModel.ExportAdjustedDxfAsync` into an Avalonia `async void` click handler.

## Root cause

The Application handler exported only dimensions where `IsEdited && IsDirty`. After prior exports, edited dimensions can be marked clean, so there were no dirty dimensions even though the current adjusted state should still be exportable.

Desktop also did not catch this Application validation exception at the UX boundary.

## Fix

- Application now exports all edited native dimensions (`IsEdited`), not only dirty ones.
- Desktop catches the export validation exception and shows `No hay cotas modificadas para exportar.` instead of crashing.

## Verification

- RED: `HandleAsync_exports_edited_dimensions_even_when_overrides_are_not_dirty` failed with the original exception.
- GREEN: `ExportAdjustedDxfHandlerTests` passed 5/5.
- RED/GREEN Desktop: `ExportAdjustedDxfAsync_reports_no_dirty_dimensions_without_throwing` now passes.
- Desktop dimension editing slice passed 6/6 with isolated artifacts because a running Desktop process locked normal output DLLs.