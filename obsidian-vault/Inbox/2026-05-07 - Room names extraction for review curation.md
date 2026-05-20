---
type: inbox
project: floorplan-fit
date: 2026-05-07
topic_key: requirements/room-label-extraction
status: proposed
---

# Room names extraction for review curation

## User request

Bring room names such as Kitchen, Living Room, Bathroom, Bedroom, Patio, and Garage into the floor plan review/curation workflow.

## Current truth

- The active extractor currently returns wall candidates only.
- `CuratedSpace` and `SpaceType` exist in Domain, but they are not wired into the active persistence/read-model/UI flow.
- The DXF fixtures contain room label text on layers such as `ROOM LBLS`, with additional useful text on `TEXT` / `TEXT LBLS`.

## Recommended MVP design

Add room labels as separate extraction candidates, not as metadata on wall lines.

Proposed shape:

- `DetectedRoomLabel`
  - source entity ref
  - source layer
  - label text
  - x/y position
  - confidence
  - detection notes
- persist as `extracted_room_labels`
- include `RoomLabelDto` in `FloorPlanReviewSessionDto`
- show a `Rooms` panel in Review

## Why separate from walls

Room names describe spaces/areas, not individual CAD wall segments. Mixing them into wall candidates would make the curation model harder to evolve toward real rooms/space regions later.

## Deferred

- room boundary inference
- automatic matching room labels to polygonal room regions
- manual rename/override UI
