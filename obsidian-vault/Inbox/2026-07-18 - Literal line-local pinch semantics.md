---
type: inbox
status: superseded
replaced_by: "[[Decisions/2026-07-18 - Retire coordinate-global pinch deformation]]"
date: 2026-07-18
project: FloorplanFit
area: Loop 1 compression
---

# Literal line-local pinch semantics

## User clarification

- A pinch belongs to one exact line.
- Its circle is the center of the total removable length.
- Only that line may shorten; no other entity should move, stretch, or deform.
- Width and Height pinches are placed throughout the FloorPlan and their capacities collectively determine whether the target size can be reached.
- Two pinches on the two faces of a wall are one synchronized wall intent, not two independent additive reductions.

## Unresolved geometric invariant

Shortening only one line while leaving every connected line fixed either creates gaps at its endpoints or leaves the overall FloorPlan bounds unchanged. A closed footprint can shrink only if some connected endpoints or components also move.

Before implementation, product semantics must choose explicitly between:

1. **Literal path-only shortening:** only target path coordinates change; disconnected endpoints and unchanged overall bounds are accepted.
2. **Local deformation plus rigid closure:** only target span changes length, while a connected component translates rigidly to preserve closure; no unrelated shape deforms.

Do not implement another global coordinate-threshold transform.

## Resolution

Superseded on 2026-07-19 by the closed-plan choice: only the selected span deforms; the connected downstream component translates rigidly to preserve closure; unrelated shapes remain unchanged.
