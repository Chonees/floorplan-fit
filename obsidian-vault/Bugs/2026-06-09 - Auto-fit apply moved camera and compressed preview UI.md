---
type: bug
date: 2026-06-09
topic: loop2-autofit-ui-viewport
---
# Auto-fit apply moved camera and compressed preview UI

## Symptom
After applying a Loop 2 fit option, the map/preview felt like the camera moved and the UI became unprofessional because the options stack consumed vertical space and squeezed the preview canvas.

## Root cause
Two things combined:
- `SitePlanAdjustmentWindow.axaml` rendered fit options as vertically stacked cards inside the top `Auto` row, so three options plus applied details pushed the `FloorPlanPreviewControl` down and reduced its height.
- `FloorPlanPreviewControl` recalculated the base viewport from mutable geometry/bounds after geometry changes, so even a correct apply could visually shift the camera.

## Fix
- Reworked the option cards into a bounded horizontal scroll strip (`AutoFitOptionsScroller`) so options no longer steal unbounded height from the preview canvas.
- Added applied-option visual state (`IsApplied`) and animated card background/border/scale transitions.
- Added viewport preservation across geometry collection changes by anchoring the same world point to the same screen point when the base viewport changes.
- Added a short amber ghost/fade overlay of the previous geometry after apply so the changed area is visible without moving the camera.

## Verification
- RED/GREEN coverage added for bounded horizontal options strip, camera preservation helper, geometry ghost opacity, and selected option visual state.
- Focused Desktop tests passed 81/81.
- Focused Application auto-fit/reactive tests passed 22/22.
- Focused Infrastructure OpenAI/Claude suggestion tests passed 6/6.
- `git diff --check` passed with LF-to-CRLF warnings only.

## Product impact
Loop 2 remains a technical decision surface: the user can compare options, click Apply, keep the same viewport/camera context, and see a professional visual cue of what changed instead of the canvas being crushed by the control panel.
