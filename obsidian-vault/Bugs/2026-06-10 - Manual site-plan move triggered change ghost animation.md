---
type: bug
date: 2026-06-10
topic: loop2-manual-move-should-not-trigger-ghost-animation
---
# Manual site-plan move triggered change ghost animation

## Symptom
In Adjust to Site Plan, dragging/moving the floor plan manually triggered the same amber/ghost change animation used for auto-fit option Apply.

## Root cause
`FloorPlanPreviewControl.OnObservedCollectionChanged(...)` started the change-preview ghost animation whenever the observed `GeometryPaths` collection changed. Manual floor-plan move updates that same geometry collection during drag, so the control treated a continuous manual placement gesture as if it were an auto-fit geometry change.

## Fix
- Added `FloorPlanPreviewControl.ShouldStartChangePreviewAnimation(...)` to encode the animation rule explicitly.
- Geometry changes still animate when they are not part of an active manual `FloorPlanMove` drag, preserving the auto-fit Apply feedback.
- Geometry changes during active manual floor-plan movement do not start the ghost animation.

## Verification
- RED/GREEN tests cover both cases:
  - manual floor-plan move geometry changes suppress animation
  - non-manual geometry changes still animate
- Focused Desktop verification passed: `FloorPlanPreviewControlTests|PreviewRenderComposerTests|SitePlanAdjustmentPreviewProjectorTests` = 86/86.
- `git diff --check` on touched control/test files exited 0 with LF-to-CRLF warnings only.

## Product impact
Manual placement stays direct and calm: when the operator drags the plan to align it to the site, the preview moves without playing the auto-fit change animation. Auto-fit Apply still keeps its visual feedback.
