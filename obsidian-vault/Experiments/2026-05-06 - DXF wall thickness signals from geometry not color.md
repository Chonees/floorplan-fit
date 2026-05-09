---
type: experiment
project: floorplan-fit
date: 2026-05-06
topic_key: experiments/wall-thickness-dxf-signals
status: analyzed
---

# DXF wall thickness signals from geometry, not color

## What

Investigated whether 2x4 vs 2x6 wall assembly hints can be inferred from the current DXF floor plans without relying on wall notes.

## Evidence

- `SEMINOLE2000.dxf`
  - `WALLS` layer color: ACI `7`
  - `WALLS` layer lineweight: `60`
  - `WALLS` entities are `BYLAYER`
  - close parallel wall-face distances cluster strongly around `4"` and `6"`
- `SANTA-BARBARA.dxf`
  - `WALLS` layer color: ACI `7`
  - `WALLS` layer lineweight: `50`
  - `WALLS` entities are `BYLAYER`
  - close parallel wall-face distances also cluster around `4"` and `6"`
- `ROOM LBLS`, `TEXT`, and `TEXT LBLS` contain room names and useful annotations.

## Conclusion

The robust MVP signal for 2x4 / 2x6 is **geometry spacing between parallel wall faces**, not per-wall color. The visible cyan/gray distinction may come from rendering/layer styling, but the DXF wall entities themselves do not carry reliable per-entity wall color in the tested files.

## Recommendation

Add a geometry-based wall assembly inference pass:

1. detect axis-aligned wall-face candidates from `WALLS`
2. pair parallel overlapping faces
3. classify spacing near `4"` as likely `2x4`
4. classify spacing near `6"` as likely `2x6`
5. store the result as an inferred hint with confidence, not as immutable truth
6. expose it in curation UI for manual override later

Room names should be extracted from `ROOM LBLS` / text entities as separate room label candidates.
