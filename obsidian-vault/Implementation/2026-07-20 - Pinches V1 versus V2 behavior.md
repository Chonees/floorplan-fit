---
type: implementation
status: verified_comparison
date: 2026-07-20
project: FloorplanFit
area: Loop 1 pinch preview
---

# Pinches V1 versus V2 behavior

## Verified baseline

The comparison uses committed `HEAD 4b27d1d` (`feat: add precise half-inch pinch editing`) as V1 and the current working tree as V2.

## V1

- Each individual marker became an X or Y threshold.
- The requested total was divided by marker count and capped independently per marker.
- Every endpoint of every geometry path on the closing side of a threshold moved by that marker's share.
- Passing multiple thresholds accumulated multiple shares.
- No wall ownership, layer role, paired faces, topology, or connectivity participated.
- Invalid/unresolved markers were skipped, so the preview usually kept moving instead of failing atomically.

V1 therefore felt direct but could shrink wall thickness, move unrelated rooms/details, and deform any geometry merely sharing the same coordinate side.

## V2

- Two adjacent ordered markers form one physical wall-face station; four markers form two stations.
- Pair capacity is the minimum of its two faces, and one requested total is distributed across station capacities.
- Each station stretches only the two resolved target spans and rigidly moves the connected closing-side structural component.
- Multiple stations execute in spatial order and earlier actions carry later targets/components.
- Missing or ambiguous topology fails closed instead of falling back to the V1 coordinate warp.

## Required invariant

For a valid group below capacity, V1 and V2 should reduce the exterior Width/Height envelope by the same requested total. V2 should differ only by preserving unrelated geometry and wall topology. If the V2 envelope moves less, reverses, or returns while the pointer remains pressed, that is a defect or an unresolved real-data topology mapping, not an intended V2 behavior.

Both versions clear the transient preview on pointer release; that release reset is not a V1/V2 difference.

The old coordinate-based overload still exists in `FloorPlanPreviewGeometry` for legacy tests, but the active handle path now calls the V2 action overload.

