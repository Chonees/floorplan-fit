---
type: implementation
created: 2026-07-05
status: ready
---

# Goal prompt final FloorPlan Electrical compression proof

This note captures the finite anti-loop goal prompt for finishing the FloorPlan -> Electrical local compression proof.

## Current truth
- Electrical can be related to FloorPlan through HousePlanSet and confirmed SheetRegistrationTransform.
- Electrical should not be curated as an authority; FloorPlan is canonical and Electrical receives the canonical FloorPlan recipe through coordinates.
- Latest runtime smoke manifest was ProjectedAutomatically but affine-only: no local compression operations.
- Final proof still requires a real local FloorPlan compression/pinch export and an ElectricalPlan exported automatically with preserved DXF entities.

## Stop rule
Do not loop indefinitely. Stop as complete only when static checks pass and the latest real SEMINOLE compression export passes the verifier. Stop as blocked when the same blocker appears three times or when external app/CAD action is required.
