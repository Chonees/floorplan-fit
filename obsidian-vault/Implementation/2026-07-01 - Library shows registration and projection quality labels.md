---
type: Implementation
date: 2026-07-01
replaces: []
replaced_by: null
---

# Library shows registration and projection quality labels

## What
The selected HousePlanSet sheet list now shows compact quality labels for dependent-sheet registration and projection.

## Why
The HousePlanSet goal requires knowing which dependent sheets are automatic/manual and with what confidence. Previously the Library showed only registration/projection status, so a user could confirm a low-confidence registration without seeing method, confidence, or warning context.

## Changed
- `PlanSetSheetDto` now carries registration/projection method, confidence, warning, and rule summary.
- `GetPlanSetLibraryHandler` overlays latest registration/projection quality metadata onto dependent sheet rows.
- `MainWindow.axaml` displays `RegistrationQualityLabel` and `ProjectionQualityLabel` next to the statuses.

## Boundary
No calibration wizard was added. This is visibility over existing quality data, not new registration math.
