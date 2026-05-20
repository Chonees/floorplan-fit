---
project: floorplan-fit
type: inbox
date: 2026-05-04
tags:
  - loop-1
  - curation
  - pinch-tool
  - ux
  - mvp
---

# MVP pinch-tool workflow proposal from existing line-by-line review

## User proposal captured

The user proposed leveraging the current line-by-line selectable review UI instead of waiting for a richer wall-body model.

### Proposed flow
- Start from the already detected/selectable geometry lines.
- Avoid manual accept-one-by-one as the main UX; instead treat the extracted review geometry as present by default and reject only invalid items.
- Add a `Pinch` tool in the review UI.
- The user clicks strategic places on selected wall geometry to create pinch markers.
- Each pinch belongs to a named group.
- Each group is tagged by adaptation intent, especially `Width` or `Height` reduction.
- When selecting a pinch group, the user can drag an outer floor-plan edge in the UX and preview how the floor plan compresses.
- The preview should show clean local shortening rather than global scaling/stretching.
- The pinch groups and their metadata become part of the saved floor-plan curation.

## Important design caution

Because real CAD walls are represented by two parallel lines/faces, the UI may select one line, but the persisted pinch should ideally resolve to a wall strip/span (or a paired-face region) rather than a single raw line only. Otherwise compression could desynchronize the two wall faces and break wall thickness.
