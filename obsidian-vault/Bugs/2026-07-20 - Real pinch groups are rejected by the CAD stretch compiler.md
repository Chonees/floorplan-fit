---
type: bug
status: fixed_static_pending_runtime
date: 2026-07-20
project: FloorplanFit
area: Loop 1 interactive CAD stretch preview
source_of_truth: runtime_database_and_code
replaces: "[[2026-07-20 - Handle drag preview still uses legacy global compression]]"
---

# Real pinch groups are rejected by the CAD stretch compiler

## Symptom

- Width/Height compression handles can be captured and dragged, but the floor-plan geometry does not change.
- Pinch markers remain gray, so the UI also gives no active-drag feedback.

## Verified runtime cause

The interactive route now calls recipe `v2`, but `BuildInteractiveCompressionPreviewGeometry(...)` returns the untouched geometry whenever `CadStretchRecipeCompiler` rejects the selected group. The rejection is intentionally swallowed at this UI seam, which makes the drag look inert.

The stable runtime database proves that the current compiler assumptions do not match valid curated data:

- active groups contain either `2` or `4` markers;
- adjacent sort-order markers form physical target pairs;
- paired marker stations can have different cut coordinates;
- the full accepted wall set contains hundreds of unrelated spans that can cross a marker coordinate without belonging to that local operation.

The compiler currently requires exactly two markers, one common cut coordinate, and classifies every accepted wall globally. Therefore all real SEMINOLE groups reject even though their targets are valid.

## Correct finite repair

1. Compile an even marker group as adjacent physical pairs.
2. Treat the requested trim as one group total and distribute it deterministically across pair capacities.
3. Resolve each target against its own marker station.
4. From the two closing endpoints, traverse only accepted structural paths that are wholly beyond both local cuts. Move that connected downstream component rigidly; disconnected or cut-crossing geometry remains implicit `Fixed`.
5. For a multi-station group, execute stations from fixed side toward closing side. Each earlier station must also rigidly carry the explicit targets/components of every later station, even when those station islands are disconnected in the extracted wall graph.
6. Render the active handle and active group's markers in green during pointer capture, then return them to gray on release.

## Non-goals for this slice

- Do not reintroduce coordinate-global deformation as a fallback.
- Do not hardcode SEMINOLE coordinates, entity references, or marker counts.
- Do not replace the connected downstream component with a bridge-only shortcut. Runtime evidence also proves that later stations can be disconnected islands; the explicit group order, not global coordinate classification, carries them under earlier actions so total bounds shrink correctly.
- Do not claim Floor/Electrical export parity until those adapters consume the same operation-local scope.

## Evidence

- `src/FloorplanFit.Application/FloorPlans/SitePlanAdjustment/CadStretchRecipeCompiler.cs`
- `src/FloorplanFit.Desktop/Controls/FloorPlanPreviewControl.cs`
- `src/FloorplanFit.Desktop/Controls/Preview/PinchMarkerPreviewLayerRenderer.cs`
- Runtime curation `661220cc-91ca-485e-89fa-c79ec8580d39` in `%LOCALAPPDATA%/FloorplanFit/workspace/app.db`

## 2026-07-20 visual-state slice

- A valid edge press now invalidates immediately.
- The captured compression handle and every marker in the active group/axis render green during drag.
- Other groups/axes remain gray; on release the existing state reset returns the interaction to idle styling.
- Exact marker selection remains green while idle, preserving the curation-selection contract.
- This visual slice alone did not claim geometric success; the operation-local group implementation below supersedes that intermediate state.

## 2026-07-20 operation-local group implementation

- `CadStretchRecipeCompiler.CompileGroup(...)` now accepts real even marker groups and pairs adjacent markers as physical wall stations.
- One requested Width/Height trim is water-filled across pair capacities; a four-marker group no longer applies the same total independently to both stations.
- Each station resolves its two faces at their own marker coordinates and moves only its connected closing-side component. Unrelated crossing or disconnected geometry remains unchanged.
- Stations execute fixed-side to closing-side (`Right`/`Top` ascending, `Left`/`Bottom` descending). Earlier actions carry later station targets/components so disconnected station islands still preserve the total requested bound reduction.
- Pressing a valid handle invalidates immediately. The pressed handle and only the active group/axis markers render green until release.
- `FloorPlanPreviewControl` now renders every compiled station action instead of silently requiring one exact two-marker action.
- Focused source-level tests cover unequal marker cuts, unrelated crossings, connected local scope, disconnected multi-station carry, water-fill, reverse ordering, odd groups, over-capacity groups, green active state, and sequential preview geometry.
- Static inspection and `git diff --check` pass. Per repository policy no .NET/build/test/watch/Desktop command ran, so the runtime result is still pending a fresh user launch.
- This closes the live Loop 1 preview defect statically. It does **not** prove that canonical Floor/Electrical export adapters already consume the same multi-station local scope.
