---
type: inbox
status: proposed
created: 2026-07-01
---

# Debate dependent local compression projection

## Context
The user wants dependent sheets such as ElectricalPlan to inherit local canonical floor plan edits, e.g. if a patio is shortened, electrical symbols/wiring in that affected region should follow.

## Current truth
The current implementation projects dependent sheets with a whole-sheet transform and preserves DXF safety by transforming only model `ENTITIES`. It does not yet replay local compression steps onto dependent geometry.

## Proposed direction
Represent floor plan local edits as a first-class adjustment recipe/deformation map, then replay that recipe through the sheet registration transform onto dependent sheet `ENTITIES` only. Metadata/header/object sections must remain source-preserved.

## Safety rule
Automatic local projection should handle simple lines/polylines/inserts/text positions first, mark splines/hatches/arcs crossing edited seams for manual review, and never silently delete CAD content.
