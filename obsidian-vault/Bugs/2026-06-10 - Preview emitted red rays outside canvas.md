---
type: bug
date: 2026-06-10
topic: loop1-preview-off-canvas-red-rays
superseded: true
replaced_by: "Bugs/2026-06-10 - Preview zoom red rays used unclipped long line endpoints.md"
---
# Preview emitted red rays outside the canvas when editing curated objects

## Symptom
When editing/reviewing curated objects, some highlighted red geometry could appear as long diagonal "rays" crossing the surrounding UI panels instead of staying inside the CAD preview area.

## Root cause
`PreviewRenderComposer.Render(...)` drew projected CAD content without pushing a clip region for `scene.Bounds`. Most normal geometry stayed visually inside the preview by coordinates, but long/odd block-derived primitives could be drawn outside the preview canvas and leak over the glass UI.

## Fix
- Added `context.PushClip(scene.Bounds)` around the CAD content rendering path after the viewport is resolved.
- The clip covers site-plan underlay, floor-plan geometry, curated/detected artifacts, dimensions, labels, pinch markers, handles, and change-preview ghost geometry.
- Workspace/background rendering remains outside that clip because it already receives `scene.Bounds` directly.

## Verification
- RED/GREEN Desktop test: `PreviewRenderComposerTests.Render_clips_cad_content_to_preview_bounds_to_prevent_off_canvas_rays` failed until the composer used `context.PushClip(scene.Bounds)`.
- Focused Desktop verification passed: `PreviewRenderComposerTests|FloorPlanPreviewControlTests` = 64/64.
- `git diff --check` for touched renderer/test files exited 0 with LF-to-CRLF warnings only.

## Product impact
Loop 1 preview editing is visually fenced: even if a CAD block/primitive has a weird or very long segment, it cannot paint across the inspector, queue, title, or surrounding glass shell.

## Caveat
This fixes the UI leak outside the preview bounds. If a bad primitive is still visible *inside* the preview bounds as a diagonal artifact, the next fix should inspect the extraction/normalization of the offending block geometry rather than the renderer clip.


## Superseded
This note captured the first partial fix (`PushClip(scene.Bounds)`). It is superseded by `Bugs/2026-06-10 - Preview zoom red rays used unclipped long line endpoints.md`, which documents the refined root cause and final line-endpoint clipping fix.
