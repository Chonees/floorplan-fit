---
type: bug
status: fixed_static_pending_rebuild
date: 2026-07-20
project: FloorplanFit
area: Infrastructure DXF export
source_of_truth: code
---

# DXF exporter left segmentIndex unassigned

## Symptom

Desktop build failed with `CS0177` at `IxMiliaAdjustedSitePlanExporter.cs:541` because `segmentIndex` was not definitely assigned on every return path.

## Root cause

`TryReadPolylineSegmentIndex(...)` returned one short-circuit boolean expression. When either preliminary condition was false, `int.TryParse(..., out segmentIndex)` did not execute.

## Fix

Initialize `segmentIndex` to `default` before the expression. Valid `LWPOLYLINE:<id>:<segment>` input still replaces it through `int.TryParse`; invalid input returns `false` and output `0`.

No semantic DXF change was made. External rebuild remains pending.
