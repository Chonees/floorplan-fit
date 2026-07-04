---
type: Bug
date: 2026-05-13
project: floorplan-fit
status: superseded
replaced_by: 2026-05-13 - Native dimensions now preserve authored CAD families.md
tags:
  - floorplan-fit
  - loop1
  - dimensions
  - preview
  - input
---

# Native dimension edge drag could mirror because the editor used DXF base points instead of the visible C shape

## Status

This bug note is superseded by the broader authored-family rewrite captured in `2026-05-13 - Native dimensions now preserve authored CAD families.md`.

## Historical What happened

Dragging a native dimension from one upper leg or upper corner could make the cota jump, tilt, or mirror instead of stretching the visible `C` shape.

## Historical expected behavior

The editable mental model was a three-sided rectangle / `C` with pata izquierda, pata derecha y techo horizontal. Dragging one upper leg / upper corner had to preserve that topology and never flip the dimension backward.
