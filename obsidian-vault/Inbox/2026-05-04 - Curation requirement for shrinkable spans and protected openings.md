---
project: floorplan-fit
type: inbox
date: 2026-05-04
tags:
  - loop-1
  - curation
  - fit-engine
  - wall-semantics
---

# Curation requirement for shrinkable spans and protected openings

## Context

During design discussion for Loop 1 curation and future site-plan adaptation, the user clarified that real plan adjustments usually shrink selected straight wall runs without breaking junctions or changing door/window widths.

## Captured requirement

- The future fit logic should not reason over raw CAD line fragments as the canonical editable unit.
- Manual curation needs a way to mark specific wall spans as shrink/stretch zones.
- Shrinking should preserve junction topology where possible.
- Door and window opening widths should remain protected while adjacent straight wall spans absorb dimensional changes.
- The intent is to move a side of the plan inward by shortening selected horizontal or vertical wall segments, not by uniformly scaling or arbitrarily editing all lines.

## Why this matters

This requirement implies the domain should distinguish between semantic walls, editable spans, and protected openings so adaptation can change width/depth intentionally without corrupting architectural meaning.
