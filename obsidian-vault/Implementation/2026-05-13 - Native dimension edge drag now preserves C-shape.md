---
created: 2026-05-13
updated: 2026-05-13
project: floorplan-fit
type: implementation
status: superseded
replaces:
replaced_by: 2026-05-13 - Native dimensions now preserve authored CAD families.md
---

# Native dimension edge drag now preserves the visible C-shape semantics

## Status

This note is superseded by `2026-05-13 - Native dimensions now preserve authored CAD families.md`, which captures the broader authored-family rewrite that replaced this narrower C-shape-only truth.

## Historical What

Completed the native-dimension preview fix so dragging a visible upper leg or upper corner stretched the cota like a `C` instead of mirroring it.

## Historical Why

The previous partial fix corrected offset preservation and axis locking, but it still left the editor thinking in DXF base points instead of the visible geometry the user grabs. That meant the interaction semantics were still wrong even when the math was cleaner.
