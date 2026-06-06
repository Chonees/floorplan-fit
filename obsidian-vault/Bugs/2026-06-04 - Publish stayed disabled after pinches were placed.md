---
type: Bug
date: 2026-06-04
project: floorplan-fit
status: fixed
tags:
  - floorplan-fit
  - loop1
  - publish
  - desktop
  - fit-curation
---

# Publish stayed disabled after pinches were placed

## Symptom

After the previous no-pinch publish crash fix, **Publish Curation** could remain disabled even after the operator placed pinches.

## Root Cause

`CanPublishCuration` was tied to the Desktop ViewModel projection:

```csharp
DraftCurationId != Guid.Empty && PinchMarkers.Count > 0
```

That made the button depend on the UI cache being perfectly up to date. If the persisted pinch marker existed but the refreshed session projection still had zero `PinchMarkers`, Desktop blocked publish before the Application use case could check the real persisted state.

## Fix

- `CanPublishCuration` now means only: an editable draft exists.
- Missing-pinch validation stays in `PublishFloorPlanCurationHandler`, which reads the persisted `PinchMarker` rows.
- `PublishAsync` still catches the known Application validation failure and converts it to the Spanish status message instead of crashing.

## Verification

- RED tests reproduced both sides of the bug:
  - no pinches: button should stay enabled but click should show validation;
  - persisted pinches with stale projection: publish should use repository truth, not `PinchMarkers.Count`.
- Green focused Desktop publish tests passed 2/2.
- Broader FloorPlan Review ViewModel slice passed 58/58.
- `git diff --check` exited 0 with only existing line-ending warnings.

## Related

- [[../Implementation/2026-06-04 - Publish button stays enabled for editable drafts]]
- [[2026-06-04 - Publish without pinches crashed Desktop dispatcher]]
