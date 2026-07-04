---
project: floorplan-fit
type: inbox
date: 2026-05-04
tags:
  - loop-1
  - curation
  - fit-engine
  - deformation-intent
---

# Pinch zones for non-scaling floor plan compression

## Context

The user clarified a stronger adaptation need beyond wall labeling. When reducing total width or depth of a room or the whole floor plan, the desired behavior is not uniform scaling. Instead, selected strategic wall spans should absorb the dimensional reduction while preserving openings and fixtures.

## Captured requirement

- The user wants to visually place manual `pinches` with the mouse.
- A pinch represents a local compression zone where length can be removed cleanly.
- Multiple pinches can be correlated into one width-reduction or depth-reduction action.
- When the correlated set is activated, only those marked spans shrink.
- The overall plan should not be stretched proportionally.
- Door/window widths and important fixture blocks (for example toilet areas) should remain protected.
- The effect should preserve architectural logic and avoid damaging important junction behavior.

## Product implication

This suggests the curation model may need explicit deformation-intent artifacts such as `pinch zones`, `compression sets`, or `editable spans with protected openings`, rather than only wall metadata.
