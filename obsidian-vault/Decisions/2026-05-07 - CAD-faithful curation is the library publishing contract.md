---
type: Decision
date: 2026-05-07
project: floorplan-fit
status: current
tags:
  - floorplan-fit
  - loop1
  - curation
  - cad-fidelity
  - dimensions
  - fit-engine
---

# CAD-faithful curation is the library publishing contract

## Decision

Loop 1 curation must evolve from "detected wall review" into a CAD-faithful semantic review surface: the curated floor plan should visually and structurally match the source DXF closely enough that admins can publish it as a reusable library asset for future site-plan adaptation.

## Why

The future fit workflow depends on trust. When a curated plan is later pinched/compressed to fit a site plan, admins need to see precise visual consequences: walls, openings, fixed components, room labels, dimensions, hatches, and annotations must remain aligned enough to show what changed and whether the adapted output is acceptable.

## Product meaning

- The Review tool is not a generic CAD editor.
- It is an admin-grade curation tool.
- The user curates once, publishes to the library, and reuses that published truth many times against site plans.
- Site-plan adaptation should operate on the curated/published artifact, not raw DXF guesses.

## Architectural implication

Every DXF feature type should enter the system as a separate semantic stream with the same lifecycle:

1. Extract from DXF
2. Preserve original geometry/visual metadata when possible
3. Persist in SQLite
4. Expose in Review DTOs
5. Render in the preview as a layer
6. Allow selection
7. Allow curation correction/removal
8. Publish as part of reusable library truth
9. Later, feed Loop 2 fit/adaptation constraints or visual audit

## Next required capability

Before adding more DXF feature types blindly, the Review system needs a curation correction backbone:

- remove false positives
- hide non-semantic annotations
- move/rename labels
- reclassify components
- preserve user overrides across review/publish
- keep the source extraction and the curated correction distinguishable

## Upcoming feature families

- Dimensions: exterior and interior dimension strings/lines/ticks that must render in the correct location and update visually when pinch-based adaptation changes lengths.
- Hatches/material marks: wall or assembly signals such as 2x4 / 2x6 / exterior / interior when the DXF encodes them by layer or visual pattern.
- Room labels: must be moveable/renamable because DXF text may be mislocated or ambiguous.
- Openings/components/annotations: must be removable or reclassifiable when extraction is wrong.

## Dimension contract

Dimensions are not simple text labels. They must eventually be modeled as CAD-faithful measurement artifacts with:

- source DXF geometry: dimension line, extension lines, ticks/arrows, text, rotation, layer, style, and color
- measured world-space anchors, not just screen pixels
- original displayed value from the DXF
- computed measurement value from the curated geometry when possible
- live preview value during pinch/compression operations
- correction lifecycle for wrong/misaligned dimension artifacts

When admins preview or apply pinch-based adaptation, dimensions must update in real time from the transformed geometry, while preserving the ability to compare against the original DXF value. This makes dimensions part of the visual audit trail for Loop 2, not decorative annotations.

The correct sequence is:

1. First make blocks, walls, openings, windows, doors, labels, and fixed components CAD-faithful enough.
2. Then bring dimensions as a dedicated semantic/visual stream.
3. Then connect dimensions to pinch/adaptation transforms so length/width/height changes are visible and auditable in preview.

## Tradeoff

The system should stay modular and avoid a single giant "extract everything" pipeline. Feature-specific extractors and render layers are more verbose, but they keep bugs isolated and make the curation tool understandable.
