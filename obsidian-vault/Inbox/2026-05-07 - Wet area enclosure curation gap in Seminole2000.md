---
type: Inbox
date: 2026-05-07
project: floorplan-fit
status: open
tags:
  - floorplan-fit
  - loop1
  - curation
  - seminole2000
  - wet-area
---

# Wet area enclosure curation gap in Seminole2000

## Observation

In `SEMINOLE2000`, the Master Bath wet-area/tub/shower zone has geometry that visually reads like enclosure/wall/glass/curb detail, while the app currently extracts the associated shower door/opening more visibly than the surrounding enclosure.

## Evidence

Current code only treats layers containing `WALL` as wall candidates through `DxfExtractionProfile.PointeHomes`.

DXF inspection around the `24"DR.` / `27" R.O.` area shows mixed layers:

- `DOORS` for the shower door geometry
- `DOORTEXT` for `24"DR.` and `27" R.O.`
- `WALLS` for some nearby structural lines
- `MISC`, `L1`, `HATCH`, `CABS-FLOORPLAN`, `FIXTURES`, `DIMS`, and labels like `BENCH`, `SAFETY GLASS`, `TUB`

## Recommended direction

Do not globally promote `MISC` / `L1` / shower detail layers into structural walls.

Instead, create or extend a protected wet-area / fixed-detail curation concept:

- group related shower/tub enclosure geometry
- classify shower door as a wet-area door/detail, not necessarily a building door for fit protection
- keep it protected during site-plan fit
- let admins remove or reclassify false positives from Review
- later add profile-backed auto-detection when the pattern repeats across Pointe plans

## Why

Treating every shower/tub enclosure line as a wall would pollute structural wall extraction and make the future fit engine pinch/move details incorrectly. The better model is semantic: structural walls stay walls; wet-area enclosures become protected fixed/detail assemblies.
