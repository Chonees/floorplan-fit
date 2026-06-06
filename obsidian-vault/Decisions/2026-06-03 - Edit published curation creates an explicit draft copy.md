---
type: Decision
date: 2026-06-03
project: floorplan-fit
status: accepted
tags:
  - floorplan-fit
  - loop1
  - publish
  - edit-mode
  - curation-lifecycle
---

# Edit published curation creates an explicit draft copy

## Decision

Published Review opens read-only by default. If the operator wants to change that published state, they must click **Editar**. That action creates or resumes a draft based on the active published curation and copies the Fit data into the draft.

## Why

Opening Review and silently creating a new editable draft made the app look like published pinches, franjas, nodes, and dimension bindings disappeared. But making published rows directly mutable would be worse: it would destroy the audit boundary between “this is the published truth” and “this is the next edit.”

## Implementation consequence

- `DraftCurationId = Guid.Empty` means the visible session is published/read-only.
- **Editar** calls an explicit Application use case that starts/resumes a draft.
- The draft is cloned from the active published curation for Fit-owned rows: pinch groups, pinch markers, measurement corridors, measurement nodes, and dimension interval bindings.
- Review refreshes by curation id while editing so mutations stay on the draft instead of snapping back to the published session.
- After publish, refresh sees `Status = Published` and clears the draft id again.

## Tradeoff

Chosen: copy-on-edit. This costs more implementation work because owned IDs must be remapped, but it keeps publishing safe and auditable.

Rejected for now: mutate published rows directly. It would feel immediate, but it would make “published” no longer mean stable.

## Related

- [[2026-06-03 - Review opens the latest published curation by default]]

## 2026-06-03 follow-up fix

A real SEMINOLE2000 edit draft already had one non-Fit override row, which blocked the first copy-on-edit implementation from cloning Fit-owned rows. The decision still stands, but the implementation now distinguishes Fit-owned rows from simple override rows and commits the clone inside `EditPublishedFloorPlanCurationHandler`.

See [[2026-06-03 - Editar published curation opened empty draft when draft had non-Fit data]].
