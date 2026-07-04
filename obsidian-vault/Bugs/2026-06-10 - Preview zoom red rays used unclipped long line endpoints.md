---
type: bug
date: 2026-06-10
topic: loop1-preview-zoom-line-endpoint-clipping
replaces: "Bugs/2026-06-10 - Preview emitted red rays outside canvas.md"
---
# Preview zoom red rays used unclipped long line endpoints

## Symptom
After the first renderer clip fix, red ray artifacts could still appear when zooming in/out in Review. The rays crossed surrounding UI panels while Cotas were visible and CAD geometry was being redrawn under a zoomed viewport.

## Refined root cause
The previous `PushClip(scene.Bounds)` was necessary but not sufficient. Several preview layers still projected CAD/world segments into very large or negative screen coordinates during zoom/pan and sent those long lines directly to Avalonia/Skia. For CAD-style rendering, the renderer should clip line endpoints mathematically to the preview rectangle before issuing draw calls, not rely only on the drawing backend's clip stack.

## Fix
- Added `PreviewLineClipper`, a Cohen-Sutherland style segment clipper for preview bounds.
- Routed CAD line drawing through `PreviewLineClipper.DrawLine(...)` for base geometry, site plan paths, curated/detected artifacts, protected/fixed elements, measurement corridor/interval overlays, dimensions, and change-preview ghost geometry.
- Added a root `context.PushClip(bounds)` in `FloorPlanPreviewControl.Render(...)` so the entire custom control render is fenced, including non-CAD shapes.
- Left `PreviewWorkspaceRenderer` grid lines as direct draws because it generates bounded grid lines from the preview rectangle itself.

## Verification
- RED/GREEN `PreviewRenderComposerTests.ClipLineToBounds_*` proved long zoomed segments are clipped or rejected before drawing.
- RED/GREEN `FloorPlanPreviewControl_clips_entire_render_to_local_bounds` proved the control pushes a root render clip.
- Focused Desktop verification passed: `PreviewRenderComposerTests|FloorPlanPreviewControlTests` = 67/67.
- Static check: `context.DrawLine` remains only in `PreviewLineClipper` and bounded workspace grid rendering.
- `git diff --check` for touched preview files exited 0 with LF-to-CRLF warnings only.

## Product impact
Loop 1 preview zoom/pan should no longer create red rays outside the preview, because zoomed CAD line endpoints are physically trimmed to the canvas before rendering.
