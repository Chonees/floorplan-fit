---
type: bug
status: fixed_static_pending_runtime
date: 2026-07-20
project: FloorplanFit
area: Loop 1 interactive compression preview
source_of_truth: code
updates: "[[2026-07-18 - Pinch compression is coordinate-global instead of topology-local]]"
replaced_by: "[[2026-07-20 - Real pinch groups are rejected by the CAD stretch compiler]]"
---

# Handle drag preview still uses legacy global compression

## Symptom

Dragging a compression handle can preview coordinate-global movement rather than the local, entity/vertex-aware CAD-style deformation used by recipe `v2` during AutoFit apply and export.

## Verified cause

`FloorPlanPreviewControl.BuildPreviewGeometry(...)` calls the six-argument legacy overload of `FloorPlanPreviewGeometry.CreatePreviewGeometry(...)` with axis, markers, requested trim and edge.

That overload iterates every geometry path and invokes `TransformPoint(...)` on every segment endpoint. The recipe-v2 overload instead delegates to `CadStretchDeformationEngine.Apply(...)`, but the handle-drag route does not call it.

## Consequence

The current handle drag is not a trustworthy visual proof of the final applied/exported result. A user may see unrelated geometry move while dragging even when the v2 operation later applies locally.

## Smallest correct repair

1. Compile the active selected pinch group and snapped drag delta into one transient recipe-v2 stretch action.
2. Render the drag preview through the existing v2 `CreatePreviewGeometry(geometry, stretchActions)` overload.
3. On release/apply, persist or dispatch the exact same logical action rather than recomputing it through a second deformation model.
4. Add a focused contract proving drag preview and applied v2 preview produce identical coordinates and leave unrelated paths unchanged.

## Implemented

- `FloorPlanPreviewControl.BuildInteractiveCompressionPreviewGeometry(...)` compiles the active group, axis, edge and snapped delta into one transient recipe-v2 action.
- The drag renderer calls only the v2 preview overload for active compression; rejected or incomplete input returns the original geometry unchanged.
- A focused Desktop test proves target shortening, declared rigid closing-side movement and unchanged unrelated geometry.
- Static whitespace verification passed. Executable and real-plan handle-drag proof remain external because no .NET/build/Desktop command was run.

## Evidence

- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs:1124-1130`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs:176-237`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewGeometry.cs:239-315`
- `src/FloorplanFit.Desktop/ViewModels/SitePlanAdjustmentViewModel.cs:314`
