# Final output audit used raw Floor WALLS tail as footprint

## What
TEST A raw FloorPlan `WALLS` width was inflated by a small horizontal tail/noise segment at the left edge, while the supported structural footprint was ~464.4", matching ElectricalPlan.

## Why
The final output congruence gate must compare the shared supported structural footprint/huella, not every raw segment extent. Raw bounds remain useful as observability warnings, not as the automatic gate.

## Evidence
- Floor raw `WALLS`: ~480.186" wide.
- Floor supported footprint: ~464.4" wide.
- Electrical `ELECTRICAL WALLS`: ~464.4" wide.
- The raw mismatch came from low-support left tail geometry, not the dominant footprint.

## Next
Update final-output audit to gate on supported final footprint and expose raw-bounds mismatch separately.
