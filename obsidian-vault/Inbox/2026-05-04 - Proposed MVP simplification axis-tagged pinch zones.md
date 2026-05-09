---
project: floorplan-fit
type: inbox
date: 2026-05-04
tags:
  - loop-1
  - curation
  - fit-engine
  - pinch-zones
  - mvp
---

# Proposed MVP simplification: axis-tagged pinch zones

## Proposal

For the MVP, simplify adaptation curation to explicit pinch zones placed on curated wall spans.

Each pinch zone would store:
- the wall/span it belongs to
- a compression axis tag (`Width` or `Height`)
- optional group/set id
- maximum trim capacity in mm
- minimum residual span length
- distribution weight (default 1)
- optional direction/anchor hints
- notes

## Intended behavior

When a site plan shows the floor plan does not fit in width or height, the solver:
1. computes the overflow on that axis
2. gathers all pinch zones tagged for that axis
3. removes the minimum necessary amount across those pinch zones
4. distributes the trim equally by default, but respects each pinch zone's max capacity
5. never trims protected openings or protected fixture/block zones

## Why this is attractive for MVP

- simpler than full semantic wall deformation graphing
- preserves the idea of intentional, non-uniform compression
- keeps the curation manual and understandable
- gives the future fit engine explicit places where it may shorten the plan

## Caveat

Equal distribution alone is not enough; each pinch zone still needs a cap and minimum residual length, otherwise the solver can create impossible or ugly compressions.
