---
type: experiment
project: floorplan-fit
date: 2026-06-08
topic_key: experiments/corrected-synthetic-setback-non-fit-dxf-examples
status: superseded
replaced_by: Experiments/2026-06-08 - Total-deficit setback non-fit DXF examples.md
replaces: Experiments/2026-06-08 - Synthetic setback non-fit DXF examples.md
---

# Corrected synthetic setback non-fit DXF examples

## What changed

Regenerated the synthetic setback DXF examples because the first batch used a toy horizontal ratio (`220" x 140"`) that did **not** match the real floor-plan height/width relation.

The corrected batch uses the measured SEMINOLE2000 structural footprint ratio from the source DXF `WALLS` layer:

```txt
WALLS bbox width  = 483.786"
WALLS bbox height = 930.000"
height / width    = 1.922338
```

## Files

Folder:

```txt
C:\Users\lucas\OneDrive\Escritorio\exports
```

Corrected DXFs:

- `setback ejemplo 01 - CORREGIDO - largo excede 1 inch - arriba.dxf`
- `setback ejemplo 02 - CORREGIDO - largo excede 2 inches - abajo.dxf`
- `setback ejemplo 03 - CORREGIDO - ancho excede 1 inch - derecha.dxf`
- `setback ejemplo 04 - CORREGIDO - ancho excede 2 inches - izquierda.dxf`
- `setback ejemplos - overview.svg`
- `setback ejemplos - README.txt`

The previous wrong `setback ejemplo*.dxf` files were removed/replaced by this corrected set.

## Geometry contract

- DXF units: inches (`$INSUNITS = 1`).
- Reference footprint: `483.786" x 930"`, matching the SEMINOLE2000 wall-structure aspect ratio.
- Layer convention:
  - `PROPERTY`: grey reference boundary.
  - `SETBACK`: orange buildable area; current reader detects it because the layer contains `SETBACK`.
  - `FLOORPLAN_FOOTPRINT_REFERENCE`: red reference footprint.
  - `OVERFLOW_STRIP`: red exact violating strip.

## Verified cases

```txt
Example 01: footprint 483.786 x 930; setback 483.786 x 929; overflow top    = 1"
Example 02: footprint 483.786 x 930; setback 483.786 x 928; overflow bottom = 2"
Example 03: footprint 483.786 x 930; setback 482.786 x 930; overflow right  = 1"
Example 04: footprint 483.786 x 930; setback 481.786 x 930; overflow left   = 2"
```

## Root cause of the bad first batch

The first examples confused a generic diagram with a fit fixture. They used arbitrary setback/footprint rectangles, so the site-plan rectangle appeared unrelated to the real tall floor-plan overlay. For Loop 2 visual QA, the fixture must preserve the real footprint ratio; otherwise the test is visually misleading even if the inch math is locally correct.

## Verification

A parser check confirmed:

- only the four corrected `setback ejemplo*.dxf` files are present;
- every DXF has `$INSUNITS = 1` and `EOF`;
- every footprint reference is exactly `483.786" x 930"`;
- `height/width = 1.922338` in every corrected fixture;
- exact overflow tuples are:
  - Example 01 `L/R/B/T = 0/0/0/1`
  - Example 02 `L/R/B/T = 0/0/2/0`
  - Example 03 `L/R/B/T = 0/1/0/0`
  - Example 04 `L/R/B/T = 2/0/0/0`

## Why it matters

These corrected files isolate Loop 2 setback-fit semantics while preserving the floor-plan's real vertical proportion. They are better visual QA fixtures for whether the setback is almost correct but misses by 1 or 2 inches.


## Superseded by total-deficit semantics

This note fixed the floor-plan aspect ratio but still modeled side-specific overflow. The user clarified the desired semantics: the setback must be smaller by the total requested width/height amount. Use:

- [[2026-06-08 - Total-deficit setback non-fit DXF examples]]
