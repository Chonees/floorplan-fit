---
type: Implementation
date: 2026-06-16
project: floorplan-fit
status: current
replaces:
  - ../Implementation/2026-06-15 - Synthetic site plans use Pointe layer appearance.md#Generated files
related:
  - ../Decisions/2026-06-16 - Synthetic site plans max adaptation capacity four.md
  - ../Current State.md
tags:
  - floorplan-fit
  - loop2
  - site-plan
  - synthetic-fixtures
  - adaptation-capacity
---

# Synthetic site plans use descriptive deficit titles

## Change

The generated normal synthetic site-plan corpus now uses descriptive file names that show the tested deficit directly in the picker.

Current generated files:

- `SYNTH FALTA 1 ANCHO - RECTANGULAR - OAK.dxf`
- `SYNTH FALTA 2 ANCHO - CHAFLAN - PINE.dxf`
- `SYNTH FALTA 2 ANCHO - FILLETS - CEDAR.dxf`
- `SYNTH FALTA 1 ALTO - FRENTE CURVO - MESA.dxf`
- `SYNTH FALTA 2 ALTO - RECTANGULAR - RIO.dxf`
- `SYNTH FALTA 2 ALTO - CHAFLAN CURVO - PARK.dxf`

## Product truth

The synthetic fixtures are terrain/site-plan examples for Loop 2 `Adjust to Site Plan`. Their job is to test whether the floor plan can adapt to realistic buildable envelopes, not to make the site plan magically obey the floor plan.

Normal fixtures now stay within the floor plan adaptation envelope:

- width/ancho examples: `1"` and `2"`
- height/alto examples: `1"` and `2"`
- shaped terrain examples keep their shape variation but no longer use over-capacity `5"` deficits

If a future fixture intentionally exceeds capacity, it must be named/documented as a negative/torture case.

## Implementation notes

- `generate_synthetic_siteplans.py` now generates descriptive file names from axis, deficit, shape, and fake short name.
- The former `5.0` deficit cases were reduced to `2.0`:
  - `FILLETS` is now `FALTA 2 ANCHO`.
  - `CHAFLAN CURVO` is now `FALTA 2 ALTO`.
- The generator now guards normal cases against deficits greater than `4"`.
- The generator attempts to remove old `SYNTH *.dxf` files that no longer match the current descriptive-name corpus, so the picker is less confusing.
- `verify_synthetic_siteplans.py` now expects the descriptive file names and validates both CAD appearance and exact expected deficit geometry against the structural footprint.

## Verification

- RED: `python verify_synthetic_siteplans.py` failed because the expected descriptive synth files did not exist yet.
- GREEN: `python generate_synthetic_siteplans.py` generated the six descriptive files and removed old synth names from the picker.
- GREEN: `python verify_synthetic_siteplans.py` passed with `PASS: 6 synthetic site plan(s) match Pointe layer/title appearance and expected <= 4" deficits`.
- GREEN: `git diff --check` exited `0` with only CRLF warnings.

## Relevant files

- `generate_synthetic_siteplans.py` - generator matrix, descriptive file names, capacity guard, stale synth cleanup.
- `verify_synthetic_siteplans.py` - expected descriptive cases and exact deficit/capacity verification.
- `D:\PointAIData\PLANS\originalsSitePlans\README.txt` - generated corpus readme listing buildable size and width/height deficit per file.
